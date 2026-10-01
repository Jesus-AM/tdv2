using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MySqlConnector;
namespace Tdv2.Synchronization;

public interface ISourceConnections { DbConnection Create(string source); }
public sealed class SourceConnections(IConfiguration configuration) : ISourceConnections
{
    public DbConnection Create(string source)
    {
        var value = configuration.GetConnectionString(source == "sii" ? "Sii" : "Ilda");
        if (string.IsNullOrWhiteSpace(value)) throw new SyncProblem("La conexión de origen no está configurada en el servidor.");
        if (source == "sii")
        {
            var sql = new SqlConnectionStringBuilder(value) { ApplicationIntent = ApplicationIntent.ReadOnly, PersistSecurityInfo = false };
            return new SqlConnection(sql.ConnectionString);
        }
        if (source != "ilda") throw new SyncProblem("Fuente no válida.");
        var mysql = new MySqlConnectionStringBuilder(value) { Database = "ilda_db", PersistSecurityInfo = false,
            TreatTinyAsBoolean = false, AllowZeroDateTime = true, ConvertZeroDateTime = false, AllowLoadLocalInfile = false };
        return new MySqlConnection(mysql.ConnectionString);
    }
}
// Only these fixed SELECT statements reach the remote sources; no user-supplied identifiers.
public sealed class CatalogSource(ISourceConnections connections, IOptions<SyncOptions> options) : ICatalogSource
{
    public const string SiiSelect = "SELECT [ID_UR],[EJERCICIO],[CVE_UR],[DESC_UR],[NUM_EMPLEADO],[ENCARGADO],[ID_UR_PERTENECE],[TIPO_UR],[NIVEL_UR],[ESTATUS_UR] FROM [poa].[UNIDADES_RESPONSABLES_POA]";
    public const string SiiLatest = " WHERE [EJERCICIO]=(SELECT MAX([EJERCICIO]) FROM [poa].[UNIDADES_RESPONSABLES_POA])";
    public const string IldaSelect = "SELECT * FROM `ilda_db`.`informacion_area` ORDER BY `id` LIMIT @limit";
    public static readonly string[] SiiColumns = ["id_ur","ejercicio","cve_ur","desc_ur","num_empleado","encargado","id_ur_pertenece","tipo_ur","nivel_ur","estatus_ur"];
    public async Task<IReadOnlyList<JsonObject>> Read(string source, CancellationToken ct)
    {
        var settings = options.Value; settings.Validate();
        if (source is not ("sii" or "ilda")) throw new SyncProblem("Fuente no válida.");
        if (source == "ilda" && !settings.IldaEnabled) throw new SyncProblem("Configura y habilita la conexión ILDA en el servidor.");
        await using var connection = connections.Create(source); await connection.OpenAsync(ct);
        if (source == "sii")
        {
            await using var isolation = connection.CreateCommand(); isolation.CommandText = "SET TRANSACTION ISOLATION LEVEL READ COMMITTED";
            isolation.CommandTimeout = settings.CommandTimeoutSeconds; await isolation.ExecuteNonQueryAsync(ct);
        }
        await using var command = connection.CreateCommand(); command.CommandTimeout = settings.CommandTimeoutSeconds;
        command.CommandText = source == "sii" ? SiiSelect + (settings.SiiLatestExercise ? SiiLatest : "") : IldaSelect;
        if (source == "ilda") { var limit = command.CreateParameter(); limit.ParameterName = "@limit"; limit.Value = settings.MaxRows + 1; command.Parameters.Add(limit); }
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
        var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length
            || (source == "sii" ? SiiColumns.Select(n => n.ToUpperInvariant()) : ["id", "ur2", "informacion_generada"]).Any(n => !names.Contains(n, StringComparer.Ordinal)))
            throw new SyncProblem("El origen no contiene las columnas requeridas o contiene columnas duplicadas.");
        var rows = new List<JsonObject>(); long bytes = 0;
        while (await reader.ReadAsync(ct))
        {
            if (rows.Count >= settings.MaxRows) throw new SyncProblem("La descarga excede el límite de filas. Se conservó la copia anterior.");
            var row = new JsonObject();
            for (var i = 0; i < names.Length; i++)
            {
                var key = source == "sii" ? names[i].ToLowerInvariant() : names[i];
                if (source == "sii" && !SiiColumns.Contains(key)) continue;
                var value = reader.IsDBNull(i) ? null : Value(reader, i);
                row[key] = source == "sii" && key is not ("ejercicio" or "nivel_ur") && value is not null
                    ? JsonValue.Create(value is JsonValue scalar && scalar.TryGetValue<bool>(out var boolean) ? (boolean ? "1" : "0") : Scalar(value)) : value;
            }
            if (source == "ilda" && (bytes += Encoding.UTF8.GetByteCount(row.ToJsonString())) > settings.IldaMaxBytes)
                throw new SyncProblem("ILDA excede el tamaño de descarga permitido. Se conservó la copia anterior.");
            rows.Add(row);
        }
        return rows; // An interrupted reader never returns/publishes a partial snapshot.
    }
    public static string? Scalar(JsonNode? node) => node is null ? null : node is JsonValue ? node.ToString() : throw new SyncProblem("El origen contiene un valor no escalar en una columna de búsqueda.");
    private static JsonNode? Value(DbDataReader reader, int i)
    {
        // MySQL decimals may exceed System.Decimal's precision; retain their exact text.
        if (reader is MySqlDataReader mysql && reader.GetDataTypeName(i).Equals("DECIMAL", StringComparison.OrdinalIgnoreCase))
            return JsonValue.Create(mysql.GetMySqlDecimal(i).ToString());
        var value = reader.GetValue(i);
        return value switch
        {
            null or DBNull => null, string s => JsonValue.Create(s), bool b => JsonValue.Create(b),
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
