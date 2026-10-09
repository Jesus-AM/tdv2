using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tdv2.Synchronization;

namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    private static async Task Until(Func<Task<bool>> condition)
    {
        var timeout = Stopwatch.StartNew();
        while (!await condition())
        {
            Check(timeout.Elapsed < TimeSpan.FromSeconds(20), "Timed out awaiting persistent synchronization state.");
            await Task.Delay(40);
        }
    }
    private static async Task<JsonObject> AwaitRun(NativeDatabase db, string id)
    {
        await Until(async () => (await db.Scalar("SELECT estado NOT IN ('pendiente','ejecutando') FROM sincronizacion_ejecuciones WHERE id=$1", Guid.Parse(id))) is true);
        return JsonNode.Parse((string)(await db.Scalar("SELECT to_jsonb(r)::text FROM sincronizacion_ejecuciones r WHERE id=$1", Guid.Parse(id)))!)!.AsObject();
    }
    private static async Task<string> RequestSync(HttpClient client, string source = "ambas")
    {
        var request = await client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = source });
        Check(request.StatusCode == HttpStatusCode.Accepted, "Expected HTTP 202 for the manual request.");
        return (await Json(request))["id"]!.ToString();
    }
    private static Process SyncChild(NativeDatabase db, string mode)
    {
        var start = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = db.Workspace };
        start.ArgumentList.Add(typeof(NativeTests).Assembly.Location); start.ArgumentList.Add(mode);
        start.Environment["TDV2_SYNC_APP_CONNECTION"] = db.AppConnection;
        start.Environment["TDV2_SYNC_NEXO_CONNECTION"] = db.NexoConnection;
        return Process.Start(start)!;
    }
    private static void RegisterManualSyncCases(NativeDatabase db, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Sync/web: manual con horarios pausados, HTTP breve y lectura del avance sin CLI", async (app, client) =>
        {
            await SetupSync(db); await Login(app, client); await Csrf(client);
            app.Sources.Block = "ilda";
            var watch = Stopwatch.StartNew(); var id = await RequestSync(client);
            Check(watch.Elapsed < TimeSpan.FromSeconds(3));
            await app.Sources.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var props = (await Json(await client.GetAsync(SyncPath)))["props"]!;
            Check(props["estado"]!["ejecucion_activa"]!.ToString() == id);
            Check(props["historial"]![0]!["estado"]!.ToString() == "ejecutando");
            Check(!props["configuracion"]!["activa"]!.GetValue<bool>());
            Check(!props["estado"]!["procesador_reciente"]!.GetValue<bool>());
            app.Sources.Continue.TrySetResult(); Check(Completed(await AwaitRun(db, id)));
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_catalogos")) == 3);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM formatos_ur")) == 0);
        });
        Test("Sync/web: horarios vencidos no se activan; SII funciona con ILDA deshabilitada", async (app, client) =>
        {
            await SetupSync(db); await Login(app, client); await Csrf(client);
            await db.Sql("UPDATE sincronizacion_configuracion SET activa=true,proxima_en=timezone('UTC',clock_timestamp())-interval '1 day'");
            var due = await db.Scalar("SELECT proxima_en FROM sincronizacion_configuracion");
            app.Services.GetRequiredService<IOptions<SyncOptions>>().Value.IldaEnabled = false;
            Check(Completed(await AwaitRun(db, await RequestSync(client, "sii"))));
            await Task.Delay(2200);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones")) == 1);
            Check(Equals(due, await db.Scalar("SELECT proxima_en FROM sincronizacion_configuracion")));
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_catalogos")) == 2);
            Check((int)(await client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = "ilda" })).StatusCode == 422);
        });
        Test("Sync/web: doble solicitud y dos administradores distintos crean un solo trabajo", async (app, client) =>
        {
            await SetupSync(db); await Login(app, client); await Csrf(client);
            await db.Sql("INSERT INTO fixture_users VALUES(60,60,'segunda@uacj.mx','Administradora sintética','individual','0060','A','adscripcion'); INSERT INTO fixture_roles VALUES(60,'segunda@uacj.mx',34,'administrador','Administrador')");
            using var other = app.Client(); app.Microsoft.Email = app.Microsoft.Mail = "segunda@uacj.mx";
            app.Microsoft.Id = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
            await Login(app, other); await Csrf(other); app.Sources.Block = "ilda";
            var responses = await Task.WhenAll(client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = "ambas" }),
                client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = "ambas" }), other.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = "ambas" }));
            Check(responses.Count(r => r.StatusCode == HttpStatusCode.Accepted) == 1);
            Check(responses.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 2);
            var id = (await Json(responses.Single(r => r.StatusCode == HttpStatusCode.Accepted)))["id"]!.ToString();
            foreach (var conflict in responses.Where(r => r.StatusCode == HttpStatusCode.Conflict)) Check((await Json(conflict))["id"]!.ToString() == id);
            await app.Sources.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            app.Sources.Continue.TrySetResult(); Check(Completed(await AwaitRun(db, id)));
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones")) == 1);
            Check(app.Sources.Queries.Count(q => q == Tdv2.Integrations.Catalogs.CatalogSource.IldaSelect) == 1);
        });
        Test("Sync/web: proceso CLI externo y web simultáneos conservan exclusión", async (app, client) =>
        {
            await SetupSync(db); await Login(app, client); await Csrf(client); app.Sources.Block = "ilda";
            var id = await RequestSync(client); await app.Sources.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using var child = SyncChild(db, "--sync-once-child");
            var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            try { await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30)); Check(child.ExitCode == 0); }
            finally { if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); } }
            await Task.WhenAll(output, errors);
            Check((await db.Scalar("SELECT estado FROM sincronizacion_ejecuciones WHERE id=$1", Guid.Parse(id)))!.ToString() == "ejecutando");
            app.Sources.Continue.TrySetResult(); Check(Completed(await AwaitRun(db, id)));
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM activity_logs WHERE action='sincronizar'")) == 3);
        });
        Test("Sync/web: fallos independientes UR, módulos e ILDA; resultados reales y reintento", async (app, client) =>
        {
            await SetupSync(db); await Login(app, client); await Csrf(client);
            Check(Completed(await AwaitRun(db, await RequestSync(client))));
            foreach (var name in new[] { "sii", "sii_modulos", "ilda" })
            {
                app.Sources.Failure = name;
                var run = await AwaitRun(db, await RequestSync(client));
                Check(run["estado"]!.ToString() == "parcial");
                Check(run["resultado"]![name]!["estado"]!.ToString() == "fallida");
                Check(run["resultado"]!.AsObject().Count(p => p.Value?["estado"]?.ToString() == "completada") == 2);
                Check(!run.ToJsonString().Contains("SECRET"));
            }
            app.Sources.Failure = null; Check(Completed(await AwaitRun(db, await RequestSync(client))));
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM activity_logs WHERE meta::text LIKE '%SECRET%'")) == 0);
        });
        Test("Sync/web: no puede reclamar; diagnóstico visible, sin duplicados y recuperación de la misma solicitud", async (app, client) =>
        {
            await SetupSync(db); await Login(app, client); await Csrf(client);
            await db.Sql("CREATE FUNCTION fixture_claim_failure() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.propietario IS NOT NULL THEN RAISE EXCEPTION 'SYNTHETIC_CLAIM_SECRET'; END IF; RETURN NEW; END $$; CREATE TRIGGER fail_claim BEFORE UPDATE ON sincronizacion_configuracion FOR EACH ROW EXECUTE FUNCTION fixture_claim_failure()");
            var id = await RequestSync(client);
            await Until(() => Task.FromResult(app.Services.GetRequiredService<ManualSyncDispatcher>().Error is not null));
            await db.Sql("UPDATE sincronizacion_ejecuciones SET solicitada_en=timezone('UTC',clock_timestamp())-interval '1 minute'");
            var props = (await Json(await client.GetAsync(SyncPath)))["props"]!;
            Check(props["estado"]!["inicio_demorado"]!.GetValue<bool>());
            Check(props["estado"]!["procesamiento_manual"]!["error"]!.ToString().Contains("cola local"));
            var retry = await client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = "sii" });
            Check(retry.StatusCode == HttpStatusCode.ServiceUnavailable && !(await retry.Content.ReadAsStringAsync()).Contains("SECRET"));
            Check(app.Sources.Queries.IsEmpty);
            await db.Sql("DROP TRIGGER fail_claim ON sincronizacion_configuracion; DROP FUNCTION fixture_claim_failure()");
            Check(Completed(await AwaitRun(db, id)));
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones")) == 1);
        });
        Test("Sync/web: reinicio recupera una solicitud persistida sin señal en memoria", async (app, client) =>
        {
            await app.Services.GetServices<IHostedService>().OfType<ManualSyncWorker>().Single().StopAsync(default);
            using var scope = app.Services.CreateScope();
            var id = await scope.ServiceProvider.GetRequiredService<SyncCoordinator>().Enqueue("sii", "manual", "persona@uacj.mx", false, default);
            await using var restarted = new NativeApplication(db) { ManualProcessing = true };
            using var newClient = restarted.Client();
            Check(Completed(await AwaitRun(db, id.ToString())));
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones")) == 1);
        });
        Test("Sync/web: terminar proceso web conserva parcial; reinicio respeta caducidad y recupera", async (app, client) =>
        {
            await app.Services.GetServices<IHostedService>().OfType<ManualSyncWorker>().Single().StopAsync(default);
            using var scope = app.Services.CreateScope();
            var id = await scope.ServiceProvider.GetRequiredService<SyncCoordinator>().Enqueue("ambas", "manual", "persona@uacj.mx", false, default);
            using (var child = SyncChild(db, "--manual-sync-child"))
            {
                var errors = child.StandardError.ReadToEndAsync();
                try { Check(await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(30)) == "SYNC_RESERVED"); }
                finally { if (!child.HasExited) child.Kill(true); await child.WaitForExitAsync(); }
                await errors;
            }
            await using var restarted = new NativeApplication(db) { ManualProcessing = true };
            using var newClient = restarted.Client(); await Task.Delay(2200);
            Check((await db.Scalar("SELECT estado FROM sincronizacion_ejecuciones"))!.ToString() == "ejecutando");
            Check(restarted.Sources.Queries.IsEmpty);
            await db.Sql("UPDATE sincronizacion_configuracion SET reserva_hasta=timezone('UTC',clock_timestamp())-interval '1 minute'");
            var recovered = await AwaitRun(db, id.ToString()); Check(recovered["estado"]!.ToString() == "parcial");
            Check(recovered["resultado"]!["interrupcion"] is not null);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_catalogos")) == 2);
            await SetupSync(db); await Login(restarted, newClient); await Csrf(newClient);
            Check(Completed(await AwaitRun(db, await RequestSync(newClient, "ilda"))));
        });
        Test("Sync/web: autorización, CSRF, representación y revocación se comprueban en HTTP", async (app, client) =>
        {
            await Csrf(client);
            Check((await client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = "sii" })).StatusCode == HttpStatusCode.Unauthorized);
            await Login(app, client); await Csrf(client);
            Check((await client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = "sii" })).StatusCode == HttpStatusCode.Forbidden);
            await SetupSync(db); client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            Check((int)(await client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = "sii" })).StatusCode == 419); await Csrf(client);
            Check((await StartRepresentation(client)).IsSuccessStatusCode); await Context(client);
            Check((await client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = "sii" })).StatusCode == HttpStatusCode.Conflict);
            await client.DeleteAsync("/actuar-como-usuario"); await Context(client);
            await db.Sql("DELETE FROM fixture_module_roles WHERE modulo_id=51");
            Check((await client.PostAsJsonAsync(SyncPath + "/ejecutar", new { fuentes = "sii" })).StatusCode == HttpStatusCode.Forbidden);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones")) == 0 && app.Sources.Queries.IsEmpty);
        });
    }
}
