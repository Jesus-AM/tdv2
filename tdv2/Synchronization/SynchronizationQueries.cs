using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Tdv2.Infrastructure;
namespace Tdv2.Synchronization;

/// <summary>Lee configuración, historial y contadores de una sola instantánea PostgreSQL.</summary>
public sealed class SynchronizationQueries(DatabaseConnections connections, IOptions<SyncOptions> options)
{
    public async Task<Dictionary<string, object?>> Snapshot(CancellationToken ct)
    {
        await using var connection = await connections.Open("Tdv2", ct);
        // La instantánea evita mezclar configuración, historial y contadores de distintas publicaciones.
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var db = new SyncSql(connection, transaction); var s = await db.Settings(ct, false); var now = await db.Now(ct);
        var history = JsonNode.Parse((string)(await db.Scalar("SELECT coalesce(json_agg(r),'[]'::json)::text FROM (SELECT * FROM sincronizacion_ejecuciones ORDER BY solicitada_en DESC,id DESC LIMIT 50) r", ct))!)!.AsArray();
        foreach (var run in history.OfType<JsonObject>()) foreach (var key in new[] { "solicitada_en", "iniciada_en", "terminada_en" }) run[key] = SyncSql.Iso(run[key]);
        var catalogs = await db.Object("""
                SELECT json_build_object(
                  'sii',json_build_object('registros',(SELECT count(*) FROM unidades_responsables_poa WHERE presente=true),'completada_en',coalesce((SELECT completada_en FROM sincronizacion_catalogos WHERE fuente='sii'),(SELECT max(completada_en) FROM sincronizaciones_institucionales))),
                  'ilda',json_build_object('registros',(SELECT count(*) FROM ilda_informacion_area WHERE presente=true),'completada_en',(SELECT completada_en FROM sincronizacion_catalogos WHERE fuente='ilda')))::text
                """, ct);
        foreach (var c in new[] { "sii", "ilda" }) catalogs[c]!["completada_en"] = SyncSql.Iso(catalogs[c]!["completada_en"]);
        await transaction.CommitAsync(ct);
        return new Dictionary<string, object?>()
        {
            ["configuracion"] = SyncSql.PublicSettings(s),
            ["historial"] = history,
            ["catalogos"] = catalogs,
            ["estado"] = new
            {
                ejecucion_activa = SyncSql.Text(s, "ejecucion_activa"),
                proxima_en = SyncSql.Date(s["proxima_en"]),
                procesador_visto_en = SyncSql.Date(s["procesador_visto_en"]),
                procesador_reciente = SyncSql.Date(s["procesador_visto_en"]) > now.AddMinutes(-3),
                ilda_habilitada = options.Value.IldaEnabled
            }
        };
    }
}
