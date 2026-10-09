using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Tdv2.Synchronization;
namespace Tdv2.Integrations.Catalogs;

public interface ISourceConnections { DbConnection Create(string source); }
public sealed class SourceConnections(IConfiguration configuration) : ISourceConnections
{
    public DbConnection Create(string source)
    {
        var value = configuration.GetConnectionString(source is "sii" or "sii_modulos" ? "Sii" : "Ilda");
        if (string.IsNullOrWhiteSpace(value)) throw new SyncProblem("La conexión de origen no está configurada en el servidor.");
        if (source is "sii" or "sii_modulos")
        {
            var sql = new SqlConnectionStringBuilder(value) { ApplicationIntent = ApplicationIntent.ReadOnly, PersistSecurityInfo = false };
            return new SqlConnection(sql.ConnectionString);
        }
        if (source != "ilda") throw new SyncProblem("Fuente no válida.");
        var mysql = new MySqlConnectionStringBuilder(value)
        {
            Database = "ilda_db",
            PersistSecurityInfo = false,
            TreatTinyAsBoolean = false,
            AllowZeroDateTime = true,
            ConvertZeroDateTime = false,
            AllowLoadLocalInfile = false
        };
        return new MySqlConnection(mysql.ConnectionString);
    }
}
// Sólo estos SELECT fijos llegan a las fuentes remotas; nunca identificadores enviados por el usuario.
public sealed class CatalogSource(ISourceConnections connections, IOptions<SyncOptions> options) : ICatalogSource
{
    public const string SiiSelect = "SELECT [ID_UR],[EJERCICIO],[CVE_UR],[DESC_UR],[NUM_EMPLEADO],[ENCARGADO],[ID_UR_PERTENECE],[TIPO_UR],[NIVEL_UR],[ESTATUS_UR] FROM [poa].[UNIDADES_RESPONSABLES_POA]";
    public const string SiiLatest = " WHERE [EJERCICIO]=(SELECT MAX([EJERCICIO]) FROM [poa].[UNIDADES_RESPONSABLES_POA])";
    public const string SiiModulesSelect = "SELECT [ID_MODULO],[DESC_MODULO] FROM [sii].[MODULOS_SII]";
    public static readonly string[] SiiModuleColumns = ["id_modulo", "desc_modulo"];
    public const string IldaSelect = "SELECT * FROM `ilda_db`.`informacion_area` ORDER BY `id` LIMIT @limit";
    public static readonly string[] SiiColumns = ["id_ur", "ejercicio", "cve_ur", "desc_ur", "num_empleado", "encargado", "id_ur_pertenece", "tipo_ur", "nivel_ur", "estatus_ur"];
    public async Task<IReadOnlyList<JsonObject>> Read(string source, CancellationToken ct)
    {
        var settings = options.Value; settings.Validate();
        if (source is not ("sii" or "sii_modulos" or "ilda")) throw new SyncProblem("Fuente no válida.");
        if (source == "ilda" && !settings.IldaEnabled) throw new SyncProblem("Configura y habilita la conexión ILDA en el servidor.");
        await using var connection = connections.Create(source); await connection.OpenAsync(ct);
        if (source is "sii" or "sii_modulos")
        {
            await using var isolation = connection.CreateCommand(); isolation.CommandText = "SET TRANSACTION ISOLATION LEVEL READ COMMITTED";
            isolation.CommandTimeout = settings.CommandTimeoutSeconds; await isolation.ExecuteNonQueryAsync(ct);
        }
        await using var command = connection.CreateCommand(); command.CommandTimeout = settings.CommandTimeoutSeconds;
        command.CommandText = source == "sii" ? SiiSelect + (settings.SiiLatestExercise ? SiiLatest : "")
            : source == "sii_modulos" ? SiiModulesSelect : IldaSelect;
        if (source == "ilda") { var limit = command.CreateParameter(); limit.ParameterName = "@limit"; limit.Value = settings.MaxRows + 1; command.Parameters.Add(limit); }
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
        var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length
            || (source == "sii" ? SiiColumns.Select(n => n.ToUpperInvariant()) : source == "sii_modulos" ? SiiModuleColumns.Select(n => n.ToUpperInvariant()) : ["id", "ur2", "informacion_generada"]).Any(n => !names.Contains(n, StringComparer.Ordinal)))
            throw new SyncProblem("El origen no contiene las columnas requeridas o contiene columnas duplicadas.");
        var rows = new List<JsonObject>(); long bytes = 0;
        while (await reader.ReadAsync(ct))
        {
            if (rows.Count >= settings.MaxRows) throw new SyncProblem("La descarga excede el límite de filas. Se conservó la copia anterior.");
            var row = new JsonObject();
            for (var i = 0; i < names.Length; i++)
            {
                var key = source != "ilda" ? names[i].ToLowerInvariant() : names[i];
                if (source == "sii" && !SiiColumns.Contains(key)) continue;
                if (source == "sii_modulos" && !SiiModuleColumns.Contains(key)) continue;
                var value = reader.IsDBNull(i) ? null : Value(reader, i);
                row[key] = source != "ilda" && key is not ("ejercicio" or "nivel_ur") && value is not null
                    ? JsonValue.Create(source == "sii" && value is JsonValue scalar && scalar.TryGetValue<bool>(out var boolean) ? (boolean ? "1" : "0") : Scalar(value)) : value;
            }
            if (source == "ilda" && (bytes += Encoding.UTF8.GetByteCount(row.ToJsonString())) > settings.IldaMaxBytes)
                throw new SyncProblem("ILDA excede el tamaño de descarga permitido. Se conservó la copia anterior.");
            rows.Add(row);
        }
        return rows; // Una lectura interrumpida nunca devuelve ni publica una descarga parcial.
    }
    public static string? Scalar(JsonNode? node) => node is null ? null : node is JsonValue ? node.ToString() : throw new SyncProblem("El origen contiene un valor no escalar en una columna de búsqueda.");
    private static JsonNode? Value(DbDataReader reader, int i)
    {
        // Los decimales MySQL pueden superar System.Decimal; conservar su texto exacto.
        if (reader is MySqlDataReader mysql && reader.GetDataTypeName(i).Equals("DECIMAL", StringComparison.OrdinalIgnoreCase))
            return JsonValue.Create(mysql.GetMySqlDecimal(i).ToString());
        var value = reader.GetValue(i);
        return value switch
        {
            null or DBNull => null,
            string s => JsonValue.Create(s),
            bool b => JsonValue.Create(b),
            byte[] bytes => new JsonObject { ["$binary_base64"] = Convert.ToBase64String(bytes) },
            MySqlDateTime date => JsonValue.Create(date.ToString()),
            DateTime date => JsonValue.Create(date.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture)),
            TimeSpan time => JsonValue.Create(time.ToString("c", CultureInfo.InvariantCulture)),
            Guid guid => JsonValue.Create(guid.ToString()),
            byte or sbyte or short or ushort or int or uint or long or ulong or decimal or double or float => System.Text.Json.JsonSerializer.SerializeToNode(value),
            _ => throw new SyncProblem("El origen contiene un tipo que no se puede conservar sin pérdida. No se publicó la descarga.")
        };
    }
}
