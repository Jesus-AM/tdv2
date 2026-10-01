using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Synchronization;
namespace Tdv2.Web;

public static class SyncEndpoints
{
    public static void MapSynchronizations(this WebApplication app)
    {
        app.MapGet("/configuracion/sincronizaciones",async (HttpContext http, RequestAccess access, DatabaseConnections connections, IOptions<SyncOptions> options) =>
        {
            var profile = await access.Profile(http,"sincronizaciones");
            await using var connection = await connections.Open("Tdv2",http.RequestAborted);
            // One coherent snapshot of settings, history and catalog counters, even during publication.
            await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead,http.RequestAborted);
            var db = new SyncSql(connection,transaction); var s = await db.Settings(http.RequestAborted,false); var now = await db.Now(http.RequestAborted);
            var history = JsonNode.Parse((string)(await db.Scalar("SELECT coalesce(json_agg(r),'[]'::json)::text FROM (SELECT * FROM sincronizacion_ejecuciones ORDER BY solicitada_en DESC,id DESC LIMIT 50) r",http.RequestAborted))!)!.AsArray();
            foreach (var run in history.OfType<JsonObject>()) foreach (var key in new[] { "solicitada_en","iniciada_en","terminada_en" }) run[key]=SyncSql.Iso(run[key]);
            var catalogs = await db.Object("""
                SELECT json_build_object(
                  'sii',json_build_object('registros',(SELECT count(*) FROM unidades_responsables_poa WHERE presente=true),'completada_en',coalesce((SELECT completada_en FROM sincronizacion_catalogos WHERE fuente='sii'),(SELECT max(completada_en) FROM sincronizaciones_institucionales))),
                  'ilda',json_build_object('registros',(SELECT count(*) FROM ilda_informacion_area WHERE presente=true),'completada_en',(SELECT completada_en FROM sincronizacion_catalogos WHERE fuente='ilda')))::text
                """,http.RequestAborted);
            foreach (var c in new[] { "sii","ilda" }) catalogs[c]!["completada_en"]=SyncSql.Iso(catalogs[c]!["completada_en"]);
            await transaction.CommitAsync(http.RequestAborted);
            return PageResponse.Page(http,"Sincronizaciones",new() { ["configuracion"]=SyncSql.PublicSettings(s),["historial"]=history,["catalogos"]=catalogs,
                ["estado"]=new { ejecucion_activa=SyncSql.Text(s,"ejecucion_activa"),proxima_en=SyncSql.Date(s["proxima_en"]),procesador_visto_en=SyncSql.Date(s["procesador_visto_en"]),
                    procesador_reciente=SyncSql.Date(s["procesador_visto_en"]) > now.AddMinutes(-3),ilda_habilitada=options.Value.IldaEnabled } },profile);
        });
        app.MapPut("/configuracion/sincronizaciones/programacion",async (JsonObject input, HttpContext http, RequestAccess access, SyncCoordinator sync) =>
        {
            static int Integer(JsonObject input,string name) => input[name] is JsonValue v && v.TryGetValue<int>(out var number) && number > 0 ? number : throw Inputs.Invalid(name,"Revisa el valor indicado.");
            var schedule = new SyncSchedule(Integer(input,"version"),Inputs.Bool(input,"activa"),Integer(input,"intervalo_minutos"),Inputs.Text(input,"hora",5,5),Inputs.Text(input,"zona_horaria",1,64),Inputs.Bool(input,"incluir_ilda"));
            try { await sync.Configure(schedule,access.Actor(http),true,http.RequestAborted); }
            catch (SyncProblem error) { throw new DomainProblem(422,error.Message); }
            return Results.Json(new { message="Programación guardada." });
        }).RequireRateLimiting("sync-config");
        app.MapPost("/configuracion/sincronizaciones/ejecutar",async (JsonObject input, HttpContext http, RequestAccess access, SyncCoordinator sync) =>
        {
            var id = await sync.Enqueue(Inputs.Text(input,"fuentes",3,5),"manual",access.Actor(http),true,http.RequestAborted);
            return Results.Json(new { message="Sincronización en cola. Se procesará en el siguiente ciclo del servicio.",id },statusCode:202);
        }).RequireRateLimiting("sync-start");
    }
}
