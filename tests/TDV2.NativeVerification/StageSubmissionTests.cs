using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Services;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    private static async Task MustRejectStageSql(NativeDatabase db, string sql)
    {
        try { await db.Sql(sql); } catch (PostgresException) { return; }
        throw new InvalidOperationException("Una escritura sobre respuestas enviadas fue aceptada.");
    }
    private static void RegisterStageSubmissionCases(NativeDatabase db, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        foreach (var role in new[] { "responsable_ur", "administrador", "colaborador_local", "consulta_institucional" })
            Test("Edición/etapas: revisión y permiso de envío para " + role, async (app, client) =>
            {
                if (role == "colaborador_local") { await CentralCollaborator(db); await db.Sql("UPDATE fixture_users SET \"ID_UR\"='A'"); }
                else await db.Sql("UPDATE fixture_roles SET rol_clave=$1", role);
                await SeedStage(db, StageOnly(app.Services.GetRequiredService<FormSchema>())); await Login(app, client); await Csrf(client);
                var tab = Guid.NewGuid(); var live = await Live(client, tab); var allowed = role is "responsable_ur" or "administrador";
                Check(live["entrega"]!["revision"]!["listo"]!.GetValue<bool>());
                Check(live["puedeEnviar"]!.GetValue<bool>() == allowed);
                Check((await client.PostAsJsonAsync("/formatos/A/posicion", new FormPositionRequest("revision"))).StatusCode
                    == (role == "consulta_institucional" ? HttpStatusCode.Forbidden : HttpStatusCode.OK));
                Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0))).StatusCode == (allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden));
                if (role == "administrador")
                    Check((await client.PostAsJsonAsync("/formatos/B/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0))).StatusCode == HttpStatusCode.Forbidden);
            });
        Test("Edición/etapas: migración conserva envíos sin evidencia y no los atribuye a primera", async (app, client) =>
        {
            await using var ef = new Tdv2DbContext(new DbContextOptionsBuilder<Tdv2DbContext>().UseNpgsql(db.Admin,
                p => p.MigrationsHistoryTable("__EFMigrationsHistory", "public")).Options);
            await ef.GetService<IMigrator>().MigrateAsync("20261009080533_SiiModulesCatalog");
            var content = RetiredValidationFixture(app.Services.GetRequiredService<FormSchema>());
            await SeedStage(db, content);
            await db.Sql("""
                UPDATE formatos_ur SET enviado_en=clock_timestamp(),enviado_por='actor@example.test',enviado_como='persona@uacj.mx',
                envio_id=gen_random_uuid(),instantanea_envio=jsonb_build_object('contenido',contenido,'version',version),porcentaje=37
                """);
            var before = await db.Scalar("SELECT to_jsonb(f)::text FROM formatos_ur f");
            await ef.Database.MigrateAsync(); await ef.Database.MigrateAsync();
            await db.Sql("GRANT SELECT,INSERT ON formato_envios_etapas TO tdv2_native_app"); // Sólo rol de la base desechable.
            Check(Equals(before, await db.Scalar("SELECT (to_jsonb(f)-'etapa_activa')::text FROM formatos_ur f")));
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM formato_envios_etapas")) == 0);
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var live = await Live(client, tab);
            Check(live["entrega"]!["etapa"]!["estado"]!.ToString() == "historico" && !live["editable"]!.GetValue<bool>());
            Check(JsonNode.DeepEquals(content, live["contenido"]));
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0))).StatusCode == HttpStatusCode.Conflict);
            await MustRejectStageSql(db, "UPDATE formatos_ur SET etapa_activa=2");
        });
        Test("Edición/etapas: revisión incompleta, D/N y ubicación independiente del avance", async (app, client) =>
        {
            var content = RetiredValidationFixture(app.Services.GetRequiredService<FormSchema>());
            await SeedStage(db, content); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var before = await Live(client, tab); var delivery = before["entrega"]!;
            Check(delivery["etapa"]!["nombre"]!.ToString() == "Primera etapa" && delivery["etapa"]!["ejercicio"]!.GetValue<int>() == 2026);
            Check(delivery["secciones"]!.AsArray().Count == 3 && !delivery["revision"]!["listo"]!.GetValue<bool>());
            Check(delivery["resumen"]!["procedimientos"]!.GetValue<int>() == 2);
            Check(delivery["revision"]!["pendientes"]!.AsArray().Any(p => p!["campo"]!.ToString() == "validacion" && p["mensaje"]!.ToString().Contains("Registro 1")));
            Check((await client.PostAsJsonAsync("/formatos/A/posicion", new FormPositionRequest("revision"))).IsSuccessStatusCode);
            var after = await Live(client, tab);
            Check(after["seccion"]!.ToString() == "revision" && after["version"]!.ToString() == before["version"]!.ToString());
            Check(JsonNode.DeepEquals(before["contenido"], after["contenido"]) && JsonNode.DeepEquals(before["entrega"], after["entrega"]));
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(tab, new BlockRequest("revision", 0)))).StatusCode == HttpStatusCode.UnprocessableEntity);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0))).StatusCode == HttpStatusCode.UnprocessableEntity);
        });
        Test("Edición/etapas: envío concurrente y reintento confirman una instantánea parcial y auditoría", async (app, client) =>
        {
            var content = StageOnly(app.Services.GetRequiredService<FormSchema>());
            await SeedStage(db, content); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var request = new SubmitRequest(tab, Guid.NewGuid(), 0);
            var responses = await Task.WhenAll(client.PostAsJsonAsync("/formatos/A/enviar", request), client.PostAsJsonAsync("/formatos/A/enviar", request));
            Check(responses.All(r => r.IsSuccessStatusCode)); Check(JsonNode.DeepEquals(await Json(responses[0]), await Json(responses[1])));
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", request)).IsSuccessStatusCode);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", request with { OperationId = Guid.NewGuid() })).StatusCode == HttpStatusCode.Conflict);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM formato_envios_etapas WHERE etapa=1")) == 1);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM activity_logs WHERE action='enviar' AND meta->'detalles'->>'etapa'='1'")) == 1);
            Check(await db.Scalar("SELECT enviado_en FROM formatos_ur") is DBNull);
            var frozen = JsonNode.Parse((string)(await db.Scalar("SELECT instantanea::text FROM formato_envios_etapas"))!)!;
            Check(JsonNode.DeepEquals(FormStages.Capture(content, 1), frozen["contenido"]));
            Check(frozen["contenido"]!["datos"] is null && frozen["contenido"]!["preguntas"] is null);
            var after = await Live(client, tab);
            Check(after["entrega"]!["etapa"]!["estado"]!.ToString() == "enviada" && after["entrega"]!["etapa"]!["enviadoNombre"]!.ToString() == "Persona sintética");
            Check(JsonNode.DeepEquals(after["contenido"], content));
            Check((await Patch(client, Edit(tab, new BlockRequest("identificacion:inicial", 0, Guid.NewGuid(), content["identificacion"]![0])))).StatusCode == HttpStatusCode.Conflict);
            await MustRejectStageSql(db, "UPDATE formatos_ur SET contenido=jsonb_set(contenido::jsonb,'{identificacion,0,tramite}','\"Alterado\"')::json");
            await MustRejectStageSql(db, "UPDATE formato_envios_etapas SET nombre='Cambio'");
            await MustRejectStageSql(db, "DELETE FROM formato_envios_etapas");
        });
        Test("Edición/etapas: reservas y versión ajenas impiden confirmar revisión obsoleta", async (app, client) =>
        {
            var content = StageOnly(app.Services.GetRequiredService<FormSchema>());
            await SeedStage(db, content); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var other = Guid.NewGuid();
            var lease = await Lease(client, other, "identificacion:inicial");
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0))).StatusCode == HttpStatusCode.Conflict);
            var row = content["identificacion"]![0]!.DeepClone(); row["resultado"] = "";
            Check((await Patch(client, Edit(other, lease with { Value = row }) with { Release = true })).IsSuccessStatusCode);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0))).StatusCode == HttpStatusCode.Conflict);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).StatusCode == HttpStatusCode.UnprocessableEntity);
        });
        Test("Edición/etapas: auditoría fallida revierte envío, recibo y versión", async (app, client) =>
        {
            await SeedStage(db, StageOnly(app.Services.GetRequiredService<FormSchema>())); await Login(app, client); await Csrf(client);
            var request = new SubmitRequest(Guid.NewGuid(), Guid.NewGuid(), 0);
            await db.Sql(AuditFailureTrigger);
            try { Check((await client.PostAsJsonAsync("/formatos/A/enviar", request)).StatusCode == HttpStatusCode.ServiceUnavailable); }
            finally { await db.Sql(RemoveAuditFailure); }
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM formato_envios_etapas")) == 0);
            Check(Convert.ToInt32(await db.Scalar("SELECT version FROM formatos_ur")) == 0);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM formato_operaciones")) == 0);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", request)).IsSuccessStatusCode);
        });
        Test("Edición/etapas: segunda deshabilitada y envíos independientes sin alterar primera", async (app, client) =>
        {
            await SeedStage(db, StageOnly(app.Services.GetRequiredService<FormSchema>())); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0, 2))).StatusCode == HttpStatusCode.Conflict);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0))).IsSuccessStatusCode);
            var first = await db.Scalar("SELECT row_to_json(s)::text FROM formato_envios_etapas s WHERE etapa=1");
            // Simula una futura etapa sólo en la base desechable. No la habilita en el código de producción.
            await db.Sql("UPDATE formatos_ur SET etapa_activa=2");
            await using (var ef = MigrationTests.Context(db.Admin))
            {
                var form = await ef.UnitForms.Include(f => f.StageSubmissions).SingleAsync();
                var rejected = false;
                try { FormStages.RequireWritable(form, ["identificacion:inicial", "medios"]); }
                catch (DomainProblem problem) when (problem.Status == 409) { rejected = true; }
                Check(rejected); FormStages.RequireWritable(form, ["datos:inicial"]);
            }
            var waiting = await Live(client, tab);
            Check(waiting["enviadoEn"] is null && waiting["entrega"]!["etapa"]!["estado"]!.ToString() == "borrador");
            Check(!waiting["entrega"]!["etapa"]!["habilitada"]!.GetValue<bool>() && !waiting["puedeEnviar"]!.GetValue<bool>());
            Check(waiting["entrega"]!["secciones"]!.AsArray().Count == 0 && !waiting["entrega"]!["revision"]!["listo"]!.GetValue<bool>());
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1, 2))).StatusCode == HttpStatusCode.Conflict);
            await db.Sql("UPDATE formatos_ur SET contenido=jsonb_set(contenido::jsonb,'{datos,0,dato}','\"Respuesta de prueba posterior\"')::json");
            await db.Sql("""
                INSERT INTO formato_envios_etapas(id_ur,etapa,ejercicio,version,operacion,enviado_en,actor,efectivo,nombre,instantanea)
                SELECT id_ur,2,ejercicio,2,gen_random_uuid(),clock_timestamp()+interval '1 minute','otro@example.test','otro@example.test','Otra persona sintética',
                  jsonb_build_object('contenido',jsonb_build_object('datos',contenido::jsonb->'datos')) FROM formatos_ur
                """);
            var second = await Live(client, tab);
            Check(second["entrega"]!["etapa"]!["enviadoNombre"]!.ToString() == "Otra persona sintética");
            Check(Equals(first, await db.Scalar("SELECT row_to_json(s)::text FROM formato_envios_etapas s WHERE etapa=1")));
            await MustRejectStageSql(db, "UPDATE formatos_ur SET contenido=jsonb_set(contenido::jsonb,'{datos,0,dato}','\"Alterado\"')::json");
            await MustRejectStageSql(db, "UPDATE formatos_ur SET contenido=jsonb_set(contenido::jsonb,'{identificacion,0,tramite}','\"Alterado\"')::json");
        });
    }
}
