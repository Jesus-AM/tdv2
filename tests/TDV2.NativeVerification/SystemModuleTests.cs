using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tdv2.Domain;
using Tdv2.Services;
using Tdv2.Synchronization;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    private static void RegisterSystemModuleCases(NativeDatabase db, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        async Task<string> Modules() => (string)(await db.Scalar("SELECT coalesce(json_agg(m ORDER BY id_modulo),'[]')::text FROM sii_modulos m"))!;
        async Task<string> Forms() => (string)(await db.Scalar("SELECT coalesce(json_agg(f ORDER BY id_ur),'[]')::text FROM formatos_ur f"))!;
        Test("Sync/módulos: esquema ausente informa al administrador y falla sin consultar la fuente de módulos", async (app, client) =>
        {
            await SetupSync(db); await Login(app, client); await Csrf(client);
            await db.Sql("ALTER TABLE public.sii_modulos RENAME TO fixture_missing_modules");
            try
            {
                var page = await Json(await client.GetAsync(SyncPath));
                var catalog = page["props"]!["catalogos"]!["sii_modulos"]!;
                Check(catalog["registros"] is null && catalog["error"]!.ToString().Contains("public.sii_modulos"));
                Check((await Json(await client.GetAsync("/formatos/A/modulos-sii")))["estado"]!.ToString() == "error");
                var run = await RunSync(app, "sii");
                Check(run["estado"]!.ToString() == "parcial" && run["resultado"]!["sii_modulos"]!["estado"]!.ToString() == "fallida");
                Check(run["resultado"]!["sii_modulos"]!["mensaje"]!.ToString().Contains("public.sii_modulos"));
                Check(!app.Sources.Queries.Any(q => q.Contains("MODULOS_SII")));
                Check(!run.ToJsonString().Contains("SECRET"));
            }
            finally { await db.Sql("ALTER TABLE public.fixture_missing_modules RENAME TO sii_modulos"); }
        });
        Test("Sync/módulos: inicial/repetida con 1501, descripciones duplicadas y IDs estables", async (app, client) =>
        {
            app.Sources.Modules.Rows.Clear();
            for (var i = 1; i <= 1501; i++) app.Sources.Modules.Rows.Add(i, i <= 2 ? "Duplicado" : "Módulo " + i);
            Check(Completed(await RunSync(app, "sii"))); Check(Completed(await RunSync(app, "sii")));
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM sii_modulos WHERE presente")) == 1501);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM sii_modulos WHERE desc_modulo='Duplicado'")) == 2);
            Check(Convert.ToInt32(await db.Scalar("SELECT registros FROM sincronizacion_catalogos WHERE fuente='sii_modulos'")) == 1501);
            Check((bool)(await db.Scalar("SELECT NOT activa FROM sincronizacion_configuracion"))!);
        });
        Test("Sync/módulos: fallo parcial, IDs y lectura incompleta conservan última copia sin secretos", async (app, client) =>
        {
            Check(Completed(await RunSync(app, "sii"))); var before = await Modules();
            foreach (var failure in new[] { "connection", "partial", "duplicate", "invalid", "empty", "columns" })
            {
                app.Sources.Modules = SyntheticSources.Table(["ID_MODULO", "DESC_MODULO"], [1, "A"], [2, "B"], [3, "C"], [4, "D"], [5, "E"]);
                app.Sources.Failure = failure == "connection" ? "sii_modulos" : null;
                app.Sources.FailAfter = failure == "partial" ? 2 : -1;
                if (failure == "duplicate") app.Sources.Modules.Rows.Add(1, "Incompatible");
                if (failure == "invalid") app.Sources.Modules.Rows[0][0] = "-1";
                if (failure == "empty") app.Sources.Modules.Rows.Clear();
                if (failure == "columns") app.Sources.Modules.Columns.Remove("DESC_MODULO");
                var run = await RunSync(app, "sii");
                Check(run["resultado"]!["sii_modulos"]!["estado"]!.ToString() == "fallida");
                Check(run["estado"]!.ToString() is "parcial" or "fallida"); Check(await Modules() == before);
                Check(!run.ToJsonString().Contains("SECRET"));
            }
        });
        Test("Sync/módulos: publicación/metadatos/resultado/auditoría revierten juntos", async (app, client) =>
        {
            Check(Completed(await RunSync(app, "sii"))); var before = await Modules();
            var metadata = await db.Scalar("SELECT completada_en FROM sincronizacion_catalogos WHERE fuente='sii_modulos'");
            await db.Sql("CREATE FUNCTION fixture_modules_fail() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.action='sincronizar' AND NEW.meta->>'fuente'='sii_modulos' THEN RAISE EXCEPTION 'SYNTHETIC_SECRET'; END IF; RETURN NEW; END $$; CREATE TRIGGER modules_fail BEFORE INSERT ON activity_logs FOR EACH ROW EXECUTE FUNCTION fixture_modules_fail()");
            try
            {
                app.Sources.Modules.Rows[0][1] = "Renombrado"; var run = await RunSync(app, "sii");
                Check(run["estado"]!.ToString() == "parcial"); Check(await Modules() == before);
                Check(Equals(metadata, await db.Scalar("SELECT completada_en FROM sincronizacion_catalogos WHERE fuente='sii_modulos'")));
            }
            finally { await db.Sql("DROP TRIGGER modules_fail ON activity_logs; DROP FUNCTION fixture_modules_fail()"); }
        });
        Test("Sync/módulos: manual y automática incluyen módulos con ILDA activada/desactivada", async (app, client) =>
        {
            await SetupSync(db); await Login(app, client); await Csrf(client);
            using var scope = app.Services.CreateScope(); var sync = scope.ServiceProvider.GetRequiredService<SyncCoordinator>();
            foreach (var include in new[] { false, true })
            {
                app.Services.GetRequiredService<IOptions<SyncOptions>>().Value.IldaEnabled = include;
                var queued = await client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = include ? "ambas" : "sii" }); Check(queued.StatusCode == HttpStatusCode.Accepted);
                var manual = (await sync.Tick(default))!; Check(Completed(manual) && manual["resultado"]!["sii_modulos"] is not null);
                await db.Sql("UPDATE sincronizacion_configuracion SET activa=true,incluir_ilda=$1,proxima_en=clock_timestamp()-interval '1 minute'", include);
                var automatic = (await sync.Tick(default))!; Check(Completed(automatic));
                Check(automatic["origen"]!.ToString() == "programada" && automatic["resultado"]!["sii_modulos"] is not null);
                Check((automatic["resultado"]!["ilda"] is not null) == include);
            }
        });
        Test("Edición/módulos: catálogo autorizado local, pendiente, módulo inválido y snapshot histórico", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            Check((await Json(await client.GetAsync("/formatos/A/modulos-sii")))["estado"]!.ToString() == "pendiente");
            Check((await client.GetAsync("/formatos/B/modulos-sii")).StatusCode == HttpStatusCode.Forbidden);
            Check(Completed(await RunSync(app, "sii"))); var calls = app.Sources.Queries.Count;
            var options = await Json(await client.GetAsync("/formatos/A/modulos-sii"));
            Check(options["modulos"]!.AsArray().Count == 5 && app.Sources.Queries.Count == calls);
            var tab = Guid.NewGuid();
            async Task<HttpResponseMessage> Store(JsonNode row)
            {
                var live = await Live(client, tab); var key = "sistemas:" + row["id"];
                var version = live["bloques"]!.AsArray().FirstOrDefault(b => b!["key"]!.ToString() == key)?["version"]?.GetValue<int>() ?? 0;
                var lease = await Lease(client, tab, key, version);
                return await Patch(client, Edit(tab, lease with { Value = row }) with { Release = true });
            }
            var row = (await Live(client, tab))["contenido"]!["sistemas"]![0]!.DeepClone(); row["sistema"] = "sii_v2";
            Check((await Store(row)).IsSuccessStatusCode); // Borrador sin módulo.
            row["moduloSiiId"] = "999"; Check((await Store(row)).StatusCode == HttpStatusCode.UnprocessableEntity);
            row["moduloSiiId"] = "1"; row["moduloSiiDescripcion"] = "Texto no autorizado";
            Check((await Store(row)).IsSuccessStatusCode);
            row = (await Live(client, tab))["contenido"]!["sistemas"]![0]!.DeepClone(); Check(row["moduloSiiDescripcion"]!.ToString() == "Solicitudes");
            var before = await Forms(); app.Sources.Modules.Rows[0][1] = "Descripción nueva";
            Check(Completed(await RunSync(app, "sii"))); Check(await Forms() == before);
            app.Sources.Modules.Rows.RemoveAt(0); Check(Completed(await RunSync(app, "sii"))); Check(await Forms() == before);
            Check((bool)(await db.Scalar("SELECT NOT presente FROM sii_modulos WHERE id_modulo='1'"))!);
            row["fallas"] = "Conservar referencia retirada"; Check((await Store(row)).IsSuccessStatusCode);
            Check((await Live(client, tab))["contenido"]!["sistemas"]![0]!["moduloSiiDescripcion"]!.ToString() == "Solicitudes");
            var another = row.DeepClone(); another["id"] = "segunda"; Check((await Store(another)).StatusCode == HttpStatusCode.UnprocessableEntity);
            await db.Sql("UPDATE formatos_ur SET enviado_en=clock_timestamp(),instantanea_envio=jsonb_build_object('contenido',contenido) WHERE id_ur='A'");
            before = await Forms(); Check(Completed(await RunSync(app, "sii"))); Check(await Forms() == before);
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(tab, new BlockRequest("sistemas:inicial", 0)))).StatusCode == HttpStatusCode.Conflict);
        });
        Test("Edición/módulos: filas independientes con mismo procedimiento, Otra/Otro y respuestas históricas", async (app, client) =>
        {
            var content = Complete(app); var original = content["sistemas"]![0]!;
            original["sistema"] = "Programa anterior"; original["uso"] = "Uso anterior"; original["estado"] = "Parcial";
            await SeedStage(db, content); await Login(app, client); await Csrf(client);
            var tab = Guid.NewGuid(); var row = original.DeepClone(); row["fallas"] = "Comentario nuevo";
            var first = await Lease(client, tab, "sistemas:inicial"); Check((await Patch(client, Edit(tab, first with { Value = row }) with { Release = true })).IsSuccessStatusCode);
            row = original.DeepClone(); row["id"] = "segunda"; row["sistema"] = "otra"; row["sistemaOtro"] = "Herramienta propia";
            row["uso"] = "otro"; row["usoOtro"] = "Uso específico"; row["estado"] = "fallas"; row["fallas"] = "";
            var second = await Lease(client, tab, "sistemas:segunda"); Check((await Patch(client, Edit(tab, second with { Value = row }) with { Release = true })).IsSuccessStatusCode);
            var saved = (await Live(client, tab))["contenido"]!["sistemas"]!.AsArray(); Check(saved.Count == 2);
            Check(saved.All(r => r!["proceso"]!.ToString() == "PO-01")); Check(saved[0]!["sistema"]!.ToString() == "Programa anterior");
            Check(saved[1]!["sistemaOtro"]!.ToString() == "Herramienta propia" && saved[1]!["usoOtro"]!.ToString() == "Uso específico");
        });
    }
}
