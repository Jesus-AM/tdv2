using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Infrastructure;

namespace Tdv2.Synchronization;

public sealed record ApplicationListItem(string Id, string Name);
public sealed record ApplicationList(string Category, IReadOnlyList<ApplicationListItem> Items);
public sealed record ApplicationLists(string Edition, string Hash, IReadOnlyList<ApplicationList> Catalogs)
{
    public const string FileName = "aplicaciones-2026-10-14.json";
    public static ApplicationLists Parse(byte[] bytes)
    {
        try
        {
            var root = JsonNode.Parse(bytes)!.AsObject();
            if (root["version"]?.GetValue<int>() != 1 || root["edicion"]?.GetValue<string>() is not { Length: > 0 and <= 64 } edition)
                throw new FormatException();
            var catalogs = root["catalogos"]!.AsArray().Select(c => new ApplicationList(c!["categoria"]!.GetValue<string>(),
                c["aplicaciones"]!.AsArray().Select(a => new ApplicationListItem(a!["id"]!.GetValue<string>(), a["nombre"]!.GetValue<string>())).ToArray())).ToArray();
            if (catalogs.Length != 2 || catalogs.Select(c => c.Category).Distinct().Count() != 2) throw new FormatException();
            foreach (var c in catalogs)
            {
                var count = c.Category switch { "web" => 207, "moviles" => 26, _ => -1 };
                if (c.Items.Count != count || c.Items.Select(i => i.Id).Distinct(StringComparer.Ordinal).Count() != count
                    || c.Items.Select(i => i.Name).Distinct(StringComparer.Ordinal).Count() != count
                    || c.Items.Any(i => !SystemApplications.LocalId(i.Id) || string.IsNullOrWhiteSpace(i.Name)
                        || i.Name != i.Name.Trim() || i.Name.Contains('\0') || i.Name.EnumerateRunes().Count() > 4000)) throw new FormatException();
            }
            return new(edition, Convert.ToHexStringLower(SHA256.HashData(bytes)), catalogs);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        { throw new SyncProblem("El archivo de aplicaciones es inválido o incompleto. Se requieren 207 nombres web y 26 móviles, únicos por categoría, con claves locales estables."); }
    }
    public static async Task<ApplicationLists> Load(CancellationToken ct) => Parse(await File.ReadAllBytesAsync(
        Path.Combine(AppContext.BaseDirectory, "Contracts", FileName), ct));
}

/// <summary>Importación explícita local. Ambas listas, metadatos y auditoría comparten commit.</summary>
public sealed class ApplicationListImport(DatabaseConnections connections)
{
    private sealed record Assignment(ApplicationListItem Item, string Id, bool New, bool Matched);
    public async Task<JsonObject> Run(ApplicationLists lists, bool preview, CancellationToken ct)
    {
        // Incluso los llamadores internos deben pasar por la validación del contrato completo.
        var validation = new JsonObject { ["version"] = 1, ["edicion"] = lists.Edition, ["catalogos"] = new JsonArray(lists.Catalogs.Select(c => (JsonNode)new JsonObject
        { ["categoria"] = c.Category, ["aplicaciones"] = new JsonArray(c.Items.Select(i => (JsonNode)new JsonObject { ["id"] = i.Id, ["nombre"] = i.Name }).ToArray()) }).ToArray()) };
        ApplicationLists.Parse(System.Text.Encoding.UTF8.GetBytes(validation.ToJsonString()));
        await using var connection = await connections.Open("Tdv2", ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var db = new SyncSql(connection, transaction);
        await ApplicationCatalogSchema.Require(db, SystemApplications.Web, ct);
        await db.Execute("SELECT pg_advisory_xact_lock(74822015)", ct);
        var report = new JsonObject { ["comprobacion"] = preview, ["edicion"] = lists.Edition, ["sha256"] = lists.Hash };
        foreach (var catalog in lists.Catalogs)
        {
            var old = JsonNode.Parse((string)(await db.Scalar("SELECT coalesce(json_agg(r),'[]'::json)::text FROM (SELECT * FROM public.aplicaciones_catalogo WHERE categoria=$1 ORDER BY id FOR UPDATE) r", ct, catalog.Category))!)!.AsArray().OfType<JsonObject>().ToArray();
            var assignments = new List<Assignment>(); var ambiguous = new JsonArray(); var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in catalog.Items)
            {
                var mapped = old.SingleOrDefault(r => r["clave_lista"]?.ToString() == item.Id);
                var candidates = old.Where(r => r["nombre"]!.ToString().Trim() == item.Name && r["clave_lista"] is null).ToArray();
                var match = mapped ?? (candidates.Length == 1 ? candidates[0] : null);
                var id = match?["id"]!.ToString() ?? item.Id;
                if (!used.Add(id) || match is null && old.Any(r => r["id"]!.ToString() == id))
                    throw new SyncProblem("Hay una colisión de claves locales. No se importó ninguna lista.");
                if (candidates.Length > 1) ambiguous.Add(new JsonObject { ["nombre"] = item.Name,
                    ["anteriores"] = new JsonArray(candidates.Select(r => r["id"]!.DeepClone()).ToArray()), ["seleccion_local"] = id });
                assignments.Add(new(item, id, match is null, mapped is null && match is not null));
            }
            var result = new JsonObject { ["disponibles"] = assignments.Count, ["nuevas"] = assignments.Count(a => a.New),
                ["correspondencias"] = assignments.Count(a => a.Matched), ["conservadas"] = assignments.Count(a => !a.New && !a.Matched),
                ["retiradas"] = old.Count(r => !used.Contains(r["id"]!.ToString())), ["ambiguas"] = ambiguous };
            report[catalog.Category] = result;
            if (preview) continue;
            await db.Execute("UPDATE public.aplicaciones_catalogo SET disponible=false WHERE categoria=$1", ct, catalog.Category);
            foreach (var a in assignments)
                await db.Execute("INSERT INTO public.aplicaciones_catalogo(categoria,id,nombre,clave_lista,disponible) VALUES($1,$2,$3,$4,true) ON CONFLICT(categoria,id) DO UPDATE SET nombre=EXCLUDED.nombre,clave_lista=EXCLUDED.clave_lista,disponible=true", ct,
                    catalog.Category, a.Id, a.Item.Name, a.Item.Id);
            await db.Execute("INSERT INTO public.aplicaciones_importaciones(categoria,edicion,sha256,importado_en,resultado) VALUES($1,$2,$3,timezone('UTC',clock_timestamp()),$4) ON CONFLICT(categoria) DO UPDATE SET edicion=EXCLUDED.edicion,sha256=EXCLUDED.sha256,importado_en=EXCLUDED.importado_en,resultado=EXCLUDED.resultado", ct,
                catalog.Category, lists.Edition, lists.Hash, result);
        }
        if (preview) await transaction.RollbackAsync(ct);
        else
        {
            await db.Audit("consola", "importar_listas_aplicaciones", null, report, ct);
            await transaction.CommitAsync(ct);
        }
        return report;
    }
}

public static class ApplicationListCommands
{
    public static async Task<int> Run(IServiceProvider services, bool preview)
    {
        using var cancel = new CancellationTokenSource();
        using var terminate = OperatingSystem.IsWindows() ? null : System.Runtime.InteropServices.PosixSignalRegistration.Create(
            System.Runtime.InteropServices.PosixSignal.SIGTERM, c => { c.Cancel = true; cancel.Cancel(); });
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancel.Cancel(); }; Console.CancelKeyPress += handler;
        try
        {
            using var scope = services.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<ApplicationListImport>().Run(await ApplicationLists.Load(cancel.Token), preview, cancel.Token);
            Console.WriteLine(result.ToJsonString(new() { WriteIndented = true }));
            return 0;
        }
        catch (SyncProblem error) { Console.Error.WriteLine(error.Message); return 1; }
        catch (Exception error)
        { Console.Error.WriteLine("No se importaron las listas. Verifica el archivo publicado, las migraciones y la conexión local. Tipo: " + error.GetType().Name); return 1; }
        finally { Console.CancelKeyPress -= handler; }
    }
}
