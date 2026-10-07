using Tdv2.Integrations.Catalogs;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
namespace Tdv2.Synchronization;

public sealed record CatalogSnapshot(string Source, IReadOnlyList<JsonObject> Rows, JsonObject Summary);
public sealed class CatalogPublication(IOptions<SyncOptions> options)
{
    private static int? Integer(JsonObject row, string field, bool required)
    {
        var value = CatalogSource.Scalar(row[field]);
        if (value is null && !required) return null;
        if (!int.TryParse(value, out var n) || n < 0) throw new SyncProblem("La descarga institucional contiene un número inválido.");
        return n;
    }
    private static string? Text(JsonObject row, string field, int max, bool required = false)
    {
        var text = CatalogSource.Scalar(row[field]);
        if (required && string.IsNullOrWhiteSpace(text) || text?.EnumerateRunes().Count() > max) throw new SyncProblem("La descarga institucional contiene una columna inválida.");
        return text;
    }
    public CatalogSnapshot Validate(string source, IReadOnlyList<JsonObject> input)
    {
        options.Value.Validate();
        if (input.Count > options.Value.MaxRows) throw new SyncProblem("La descarga excede el límite de filas. Se conservó la copia anterior.");
        var rows = new List<JsonObject>(); var ids = new HashSet<string>(StringComparer.Ordinal); var columns = new HashSet<string>(StringComparer.Ordinal);
        int? year = null; long bytes = 0;
        if (source == "sii")
        {
            if (input.Count == 0) throw new SyncProblem("El origen institucional está vacío o incompleto.");
            foreach (var inputRow in input)
            {
                if (CatalogSource.SiiColumns.Any(c => !inputRow.ContainsKey(c))) throw new SyncProblem("El origen institucional está incompleto.");
                var row = new JsonObject();
                foreach (var (field, max, required) in new[] { ("id_ur", 32, true), ("cve_ur", 32, true), ("desc_ur", 500, false), ("num_empleado", 32, false), ("encargado", 255, false), ("id_ur_pertenece", 32, false), ("tipo_ur", 32, false), ("estatus_ur", 32, false) })
                    row[field] = Text(inputRow, field, max, required);
                var id = row["id_ur"]!.ToString();
                if (!ids.Add(id)) throw new SyncProblem("ID_UR duplicado en la descarga institucional.");
                var exercise = Integer(inputRow, "ejercicio", true)!.Value; year ??= exercise;
                if (year != exercise) throw new SyncProblem("La descarga contiene más de un ejercicio.");
                row["ejercicio"] = exercise; row["nivel_ur"] = Integer(inputRow, "nivel_ur", false);
                if (row["id_ur_pertenece"]?.ToString() == "") row["id_ur_pertenece"] = null;
                rows.Add(row);
            }
            var index = rows.ToDictionary(r => r["id_ur"]!.ToString()); var done = new HashSet<string>();
            foreach (var row in rows)
            {
                var cursor = row["id_ur"]!.ToString(); var visited = new HashSet<string>();
                while (cursor is not null && index.TryGetValue(cursor, out var current) && !done.Contains(cursor))
                {
                    if (!visited.Add(cursor)) throw new SyncProblem("La jerarquía institucional contiene un ciclo.");
                    cursor = current["id_ur_pertenece"]?.ToString();
                }
                done.UnionWith(visited);
            }
            return new(source, rows, new() { ["ejercicio"] = year, ["unidades"] = rows.Count, ["comprobacion"] = false });
        }
        if (source != "ilda") throw new SyncProblem("Fuente no válida.");
        foreach (var data in input)
        {
            if (new[] { "id", "ur2", "informacion_generada" }.Any(k => !data.ContainsKey(k))) throw new SyncProblem("ILDA no contiene las columnas requeridas.");
            var id = CatalogSource.Scalar(data["id"]) ?? "";
            if (!Regex.IsMatch(id, "\\A[0-9]{1,40}\\z") || !ids.Add(id)) throw new SyncProblem("ILDA contiene identificadores inválidos o duplicados.");
            var code = CatalogSource.Scalar(data["ur2"])?.Trim();
            if (code?.EnumerateRunes().Count() > 255) throw new SyncProblem("Una clave ur2 de ILDA excede el tamaño admitido.");
            if ((bytes += Encoding.UTF8.GetByteCount(data.ToJsonString())) > options.Value.IldaMaxBytes) throw new SyncProblem("ILDA excede el tamaño de descarga permitido.");
            columns.UnionWith(data.Select(p => p.Key));
            rows.Add(new() { ["id_origen"] = id, ["ur2"] = code, ["informacion_generada"] = CatalogSource.Scalar(data["informacion_generada"]), ["datos"] = data.DeepClone() });
        }
        return new(source, rows, new() { ["registros"] = rows.Count, ["columnas"] = columns.Count, ["comprobacion"] = false });
    }
    public async Task CheckReduction(SyncSql db, CatalogSnapshot snapshot, CancellationToken ct)
    {
        var table = snapshot.Source == "sii" ? "unidades_responsables_poa" : "ilda_informacion_area";
        var before = Convert.ToInt64(await db.Scalar("SELECT count(*) FROM " + table + " WHERE presente=true", ct));
        if (snapshot.Rows.Count * 5L < before * 4L) throw new SyncProblem("La descarga reduce demasiado los registros de " + snapshot.Source.ToUpperInvariant() + ". Se conservó la copia anterior.");
    }
    /// <summary>Publica únicamente catálogos locales; no sobrescribe formatos ni respuestas.</summary>
    /// <remarks>El coordinador incluye resultado, metadatos y auditoría en esta misma transacción por fuente.</remarks>
    public async Task Apply(SyncSql db, CatalogSnapshot snapshot, CancellationToken ct)
    {
        // Serializa la publicación con la confirmación de participación, sin bloquear lecturas o capturas entre sí.
        await db.Scalar("SELECT version FROM configuracion_procesos WHERE id=1 FOR SHARE", ct);
        await CheckReduction(db, snapshot, ct);
        var sii = snapshot.Source == "sii"; var table = sii ? "unidades_responsables_poa" : "ilda_informacion_area";
        var fields = sii ? CatalogSource.SiiColumns : new[] { "id_origen", "ur2", "informacion_generada", "datos" };
        await db.Execute("UPDATE " + table + " SET presente=false", ct);
        var sql = "INSERT INTO " + table + "(" + string.Join(',', fields) + ",presente,sincronizado_en) VALUES(" + string.Join(',', Enumerable.Range(1, fields.Length).Select(i => "$" + i)) + ",true,timezone('UTC',clock_timestamp())) ON CONFLICT(" + fields[0] + ") DO UPDATE SET "
            + string.Join(',', fields.Skip(1).Select(f => f + "=EXCLUDED." + f)) + ",presente=true,sincronizado_en=EXCLUDED.sincronizado_en";
        foreach (var chunk in snapshot.Rows.Chunk(100))
        {
            await using var batch = new NpgsqlBatch(db.Connection, db.Transaction);
            foreach (var row in chunk)
            {
                var command = new NpgsqlBatchCommand(sql);
                foreach (var field in fields)
                    if (field == "datos") command.Parameters.AddWithValue(NpgsqlDbType.Json, row[field]!.ToJsonString());
                    else if (field is "ejercicio" or "nivel_ur") command.Parameters.Add(new() { NpgsqlDbType = NpgsqlDbType.Integer, Value = (object?)row[field]?.GetValue<int>() ?? DBNull.Value });
                    else command.Parameters.Add(new() { NpgsqlDbType = NpgsqlDbType.Text, Value = (object?)row[field]?.ToString() ?? DBNull.Value });
                batch.BatchCommands.Add(command);
            }
            await batch.ExecuteNonQueryAsync(ct);
        }
        if (sii) await db.Execute("INSERT INTO sincronizaciones_institucionales(resumen,completada_en) VALUES($1,timezone('UTC',clock_timestamp()))", ct, snapshot.Summary);
        await db.Execute("INSERT INTO sincronizacion_catalogos(fuente,registros,completada_en) VALUES($1,$2,timezone('UTC',clock_timestamp())) ON CONFLICT(fuente) DO UPDATE SET registros=EXCLUDED.registros,completada_en=EXCLUDED.completada_en", ct, snapshot.Source, snapshot.Rows.Count);
    }
}
