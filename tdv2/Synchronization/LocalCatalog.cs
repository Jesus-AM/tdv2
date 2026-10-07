using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace Tdv2.Synchronization;

public sealed record UnitInventory(string State, IReadOnlyList<JsonObject> Rows, string? Notice);
/// <summary>Consulta únicamente las réplicas de TDV2; una página jamás conecta SII o ILDA.</summary>
public interface ILocalCatalog
{
    Task<UnitInventory> Inventory(Unit unit, CancellationToken ct);
    async Task<IReadOnlyDictionary<string, UnitInventory>> Inventories(Unit[] units, CancellationToken ct)
    {
        var result = new Dictionary<string, UnitInventory>();
        foreach (var unit in units) result[unit.Id] = await Inventory(unit, ct);
        return result;
    }
    Task<DateTimeOffset?> LastSii(CancellationToken ct);
}
public sealed class LocalCatalog(Tdv2DbContext db, ILogger<LocalCatalog> logger) : ILocalCatalog
{
    public sealed record InventoryRow(string Code, string Id, string? Information);
    public async Task<IReadOnlyDictionary<string, UnitInventory>> Inventories(Unit[] units, CancellationToken ct)
    {
        var result = units.ToDictionary(u => u.Id, _ => new UnitInventory("pendiente", [], null));
        if (units.Length == 0) return result;
        try
        {
            if (!await db.CatalogSynchronizations.AnyAsync(c => c.Id == "ilda", ct)) return result;
            var codes = units.Select(u => u.Code.Trim()).Where(c => c.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
            // Una consulta acotada por clave: conserva orden C y 200 filas por UR, sin cargar toda la réplica
            // ni ejecutar consultas concurrentes sobre este DbContext. El registro 201 detecta excedentes.
            var records = await db.Database.SqlQueryRaw<InventoryRow>("""
                SELECT c.code AS "Code", r.id_origen AS "Id", r.informacion_generada AS "Information"
                FROM unnest(@codes::text[]) AS c(code)
                CROSS JOIN LATERAL (SELECT id_origen,informacion_generada FROM ilda_informacion_area
                    WHERE presente AND ur2 COLLATE "C" = c.code COLLATE "C"
                    ORDER BY id_origen COLLATE "C" LIMIT 201) r
                """, new Npgsql.NpgsqlParameter("codes", codes)).ToListAsync(ct);
            var grouped = records.ToLookup(r => r.Code, StringComparer.Ordinal);
            foreach (var unit in units)
            {
                var code = unit.Code.Trim();
                if (code.Length == 0) { result[unit.Id] = new("sin_clave", [], "Esta área no tiene una clave institucional para consultar ILDA."); continue; }
                var source = grouped[code].ToArray(); var rows = new List<JsonObject>(); var invalid = false;
                foreach (var record in source.Take(200))
                {
                    var text = record.Information?.Trim() ?? "";
                    if (!Regex.IsMatch(record.Id, "\\A[0-9]{1,40}\\z") || text == "" || text.EnumerateRunes().Count() > 4000) { invalid = true; continue; }
                    rows.Add(new() { ["id"] = "ilda:" + record.Id, ["codigo"] = "", ["prioridad"] = "", ["fuente"] = "ILDA", ["area"] = "", ["tramite"] = text, ["usuario"] = "", ["resultado"] = "", ["responsable"] = "", ["validacion"] = "" });
                }
                result[unit.Id] = new("disponible", rows, source.Length > 200 ? "ILDA tiene más de 200 registros para esta área. Se muestran los primeros 200; solicita revisar el inventario."
                    : invalid ? "Algunos registros de ILDA no tienen un trámite válido o exceden el tamaño permitido. Solicita revisar el inventario." : null);
            }
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning("Inventario ILDA local no disponible. Tipo {Type}", error.GetType().Name);
            foreach (var unit in units) result[unit.Id] = new("no_disponible", [], "No fue posible consultar la copia local de ILDA. Puedes continuar con la información guardada.");
        }
        return result;
    }
    public async Task<DateTimeOffset?> LastSii(CancellationToken ct)
    {
        var date = await db.InstitutionalSynchronizations.MaxAsync(s => (DateTime?)s.CompletedAt, ct);
        return date is DateTime time ? new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc)) : null;
    }
    public async Task<UnitInventory> Inventory(Unit unit, CancellationToken ct)
    {
        try
        {
            if (!await db.CatalogSynchronizations.AnyAsync(c => c.Id == "ilda", ct)) return new("pendiente", [], null);
            // El límite de 200 corresponde al formulario; la réplica conserva todas las filas y columnas.
            var code = unit.Code.Trim();
            if (code == "") return new("sin_clave", [], "Esta área no tiene una clave institucional para consultar ILDA.");
            var records = await db.IldaAreaInformations.AsNoTracking()
                .Where(r => r.Present && EF.Functions.Collate(r.Ur2!, "C") == code)
                .OrderBy(r => EF.Functions.Collate(r.Id, "C")).Take(201)
                .Select(r => new { r.Id, r.Information }).ToListAsync(ct);
            var rows = new List<JsonObject>(); var invalid = false;
            foreach (var record in records.Take(200))
            {
                var id = record.Id; var text = record.Information?.Trim() ?? "";
                if (!Regex.IsMatch(id, "\\A[0-9]{1,40}\\z") || text == "" || text.EnumerateRunes().Count() > 4000) { invalid = true; continue; }
                rows.Add(new() { ["id"] = "ilda:" + id, ["codigo"] = "", ["prioridad"] = "", ["fuente"] = "ILDA", ["area"] = "", ["tramite"] = text, ["usuario"] = "", ["resultado"] = "", ["responsable"] = "", ["validacion"] = "" });
            }
            return new("disponible", rows, records.Count > 200 ? "ILDA tiene más de 200 registros para esta área. Se muestran los primeros 200; solicita revisar el inventario."
                : invalid ? "Algunos registros de ILDA no tienen un trámite válido o exceden el tamaño permitido. Solicita revisar el inventario." : null);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning("Inventario ILDA local no disponible. Tipo {Type}", error.GetType().Name);
            return new("no_disponible", [], "No fue posible consultar la copia local de ILDA. Puedes continuar con la información guardada.");
        }
    }
    public static (JsonObject Content, object Status) Merge(JsonObject content, UnitInventory inventory)
    {
        var rows = content["identificacion"]!.AsArray();
        if (inventory.Rows.Count > 0 && rows.Count == 1 && rows[0]?["id"]?.ToString() == "inicial"
            && new[] { "codigo", "prioridad", "area", "tramite", "usuario", "resultado", "responsable", "validacion" }.All(k => string.IsNullOrWhiteSpace(rows[0]?[k]?.ToString()))) rows.Clear();
        var ids = rows.Select(r => r!["id"]!.ToString()).ToHashSet(StringComparer.Ordinal); var added = 0; var pending = 0;
        foreach (var row in inventory.Rows)
        {
            if (ids.Contains(row["id"]!.ToString())) continue;
            if (rows.Count >= 200) { pending++; continue; }
            rows.Add(row.DeepClone()); ids.Add(row["id"]!.ToString()); added++;
        }
        return (content, new
        {
            estado = inventory.State,
            nuevos = added,
            total = inventory.Rows.Count,
            aviso = pending > 0 ? "El formato alcanzó 200 filas. Hay registros de ILDA pendientes de incorporar." : inventory.Notice
        });
    }
}
