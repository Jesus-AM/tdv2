using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Tdv2.Domain;
using Tdv2.Infrastructure;
namespace Tdv2.Synchronization;

public sealed record UnitInventory(string State, IReadOnlyList<JsonObject> Rows, string? Notice);
public interface ILocalCatalog
{
    Task<UnitInventory> Inventory(Unit unit, CancellationToken ct);
    Task<DateTimeOffset?> LastSii(CancellationToken ct);
}
public sealed class LocalCatalog(DatabaseConnections connections, ILogger<LocalCatalog> logger) : ILocalCatalog
{
    public async Task<DateTimeOffset?> LastSii(CancellationToken ct)
    {
        await using var connection = await connections.Open("Tdv2",ct);
        var date = await new SyncSql(connection,null).Scalar("SELECT max(completada_en) FROM sincronizaciones_institucionales",ct);
        return date is DateTime time ? new DateTimeOffset(DateTime.SpecifyKind(time,DateTimeKind.Utc)) : null;
    }
    public async Task<UnitInventory> Inventory(Unit unit, CancellationToken ct)
    {
        try
        {
            await using var connection = await connections.Open("Tdv2",ct); var db = new SyncSql(connection,null);
            if (await db.Scalar("SELECT 1 FROM sincronizacion_catalogos WHERE fuente='ilda'",ct) is null) return new("pendiente",[],null);
            var code = unit.Code.Trim();
            if (code == "") return new("sin_clave",[],"Esta área no tiene una clave institucional para consultar ILDA.");
            var raw = (string)(await db.Scalar("SELECT coalesce(json_agg(r),'[]'::json)::text FROM (SELECT id_origen,informacion_generada FROM ilda_informacion_area WHERE presente=true AND ur2 COLLATE \"C\"=$1 ORDER BY id_origen COLLATE \"C\" LIMIT 201) r",ct,code))!;
            var records = JsonNode.Parse(raw)!.AsArray(); var rows = new List<JsonObject>(); var invalid = false;
            foreach (var record in records.Take(200))
            {
                var id = record!["id_origen"]!.ToString(); var text = record["informacion_generada"]?.ToString().Trim() ?? "";
                if (!Regex.IsMatch(id,"\\A[0-9]{1,40}\\z") || text == "" || text.EnumerateRunes().Count() > 4000) { invalid=true; continue; }
                rows.Add(new() { ["id"]="ilda:"+id,["codigo"]="",["prioridad"]="",["fuente"]="ILDA",["area"]="",["tramite"]=text,["usuario"]="",["resultado"]="",["responsable"]="",["validacion"]="" });
            }
            return new("disponible",rows,records.Count > 200 ? "ILDA tiene más de 200 registros para esta área. Se muestran los primeros 200; solicita revisar el inventario."
                : invalid ? "Algunos registros de ILDA no tienen un trámite válido o exceden el tamaño permitido. Solicita revisar el inventario." : null);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning("Inventario ILDA local no disponible. Tipo {Type}",error.GetType().Name);
            return new("no_disponible",[],"No fue posible consultar la copia local de ILDA. Puedes continuar con la información guardada.");
        }
    }
    public static (JsonObject Content, object Status) Merge(JsonObject content, UnitInventory inventory)
    {
        var rows = content["identificacion"]!.AsArray();
        if (inventory.Rows.Count > 0 && rows.Count == 1 && rows[0]?["id"]?.ToString() == "inicial"
            && new[] { "codigo","prioridad","area","tramite","usuario","resultado","responsable","validacion" }.All(k => string.IsNullOrWhiteSpace(rows[0]?[k]?.ToString()))) rows.Clear();
        var ids = rows.Select(r => r!["id"]!.ToString()).ToHashSet(StringComparer.Ordinal); var added = 0; var pending = 0;
        foreach (var row in inventory.Rows)
        {
            if (ids.Contains(row["id"]!.ToString())) continue;
            if (rows.Count >= 200) { pending++; continue; }
            rows.Add(row.DeepClone()); ids.Add(row["id"]!.ToString()); added++;
        }
        return (content,new { estado=inventory.State,nuevos=added,total=inventory.Rows.Count,
            aviso=pending > 0 ? "El formato alcanzó 200 filas. Hay registros de ILDA pendientes de incorporar." : inventory.Notice });
    }
}
