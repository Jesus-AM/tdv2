using Tdv2.Integrations.Microsoft;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tdv2.Domain;
using Tdv2.Security;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    internal const string FailureTrigger = """
        CREATE FUNCTION fixture_fail_save() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'SYNTHETIC_DATABASE_FAILURE'; END $$;
        CREATE TRIGGER fail_save BEFORE INSERT OR UPDATE ON formatos_ur FOR EACH ROW EXECUTE FUNCTION fixture_fail_save();
        """;
    private static void Check(bool condition, string message = "Expected condition failed.") { if (!condition) throw new InvalidOperationException(message); }
    private static async Task<JsonObject> Json(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    private static async Task<string> Start(NativeApplication app, HttpClient client)
    {
        var response = await client.GetAsync("/connect");
        Check(response.StatusCode == HttpStatusCode.Redirect, "OAuth did not start.");
        var target = response.Headers.Location!;
        Check(target.Host == "login.microsoftonline.com" && target.AbsolutePath == $"/{FakeMicrosoft.Tenant}/oauth2/v2.0/authorize");
        var query = QueryHelpers.ParseQuery(target.Query);
        Check(query["code_challenge_method"] == "S256" && query["scope"].ToString().Contains("User.Read"));
        app.Microsoft.ExpectedChallenge = query["code_challenge"].ToString();
        return query["state"].ToString();
    }
    private static async Task<HttpResponseMessage> Callback(HttpClient client, string state) => await client.GetAsync("/connect?code=synthetic-code&state=" + Uri.EscapeDataString(state));
    private static async Task<HttpResponseMessage> Login(NativeApplication app, HttpClient client)
    {
        var response = await Callback(client, await Start(app, client));
        Check(response.Headers.Location?.OriginalString == "/inicio", "Login failed before reaching /inicio.");
        return response;
    }
    private static async Task Csrf(HttpClient client)
    {
        var response = await Json(await client.GetAsync("/session/csrf"));
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", response["token"]!.GetValue<string>());
    }
    private static async Task<JsonObject> Form(HttpClient client) => (await Json(await client.GetAsync("/formatos/A")))["props"]!.AsObject();
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<HttpClient, System.Collections.Concurrent.ConcurrentDictionary<string, Guid>> EditorTabs = new();
    private static Task<HttpResponseMessage> Save(HttpClient client, int version, JsonNode content) => SaveAll(client, "A", version, content);
    // Adaptador de los escenarios históricos: envía todos los bloques explícitamente reservados.
    // Las pruebas nuevas ejercitan parches mínimos y sesiones/pestañas independientes.
    private static async Task<HttpResponseMessage> SaveAll(HttpClient client, string unit, int version, JsonNode content)
    {
        var values = FormBlocks.Split(content.AsObject());
        for (var i = 0; i < 12; i++) values.TryAdd("preguntas:" + i, null);
        var tab = EditorTabs.GetOrCreateValue(client).GetOrAdd(unit, _ => Guid.NewGuid());
        var read = await client.GetAsync($"/formatos/{unit}/estado?tab={tab}"); if (!read.IsSuccessStatusCode) return read;
        var current = await Json(read);
        foreach (var key in FormBlocks.Split(current["contenido"]!.AsObject()).Keys) values.TryAdd(key, null);
        var versions = current["bloques"]!.AsArray().ToDictionary(b => b!["key"]!.ToString(), b => b!["version"]!.GetValue<int>());
        int Expected(string key) => Math.Min(version, versions.GetValueOrDefault(key));
        var reserved = await client.PostAsJsonAsync($"/formatos/{unit}/reservas", new { tabId = tab, operationId = Guid.NewGuid(), blocks = values.Select(p => new { key = p.Key, version = Expected(p.Key) }) });
        if (!reserved.IsSuccessStatusCode) return reserved;
        var leases = (await Json(reserved))["bloques"]!.AsArray().ToDictionary(b => b!["key"]!.ToString(), b => b!["reserva"]!["id"]!.ToString());
        return await client.PatchAsJsonAsync($"/formatos/{unit}/bloques", new { tabId = tab, operationId = Guid.NewGuid(), release = true,
            blocks = values.Select(p => new { key = p.Key, version = Expected(p.Key), leaseId = leases[p.Key], value = p.Value }) });
    }
    private static bool DeniedLogin(HttpResponseMessage response) => response.Headers.Location?.OriginalString.StartsWith("/?authError=", StringComparison.Ordinal) == true;

    public static async Task<int> Main(string[] args)
    {
        if (args.Contains("--database-check")) return await DatabaseInspection.Run(args);
        var database = new NativeDatabase();
        if (args.Contains("--nexo-delegation-only")) return await NexoDelegationTests.Run(database);
        if (args.Contains("--migrations-only"))
        {
            await database.ValidateCluster();
            return await MigrationTests.Run(database);
        }
        if (args.Contains("--sync-child"))
        {
            await database.Attach(); await using var childApp=new NativeApplication(database); childApp.Sources.Block="ilda";
            using var scope=childApp.Services.CreateScope(); var work=scope.ServiceProvider.GetRequiredService<Tdv2.Synchronization.SyncCoordinator>().Tick(default);
            await childApp.Sources.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30)); Console.WriteLine("SYNC_RESERVED");
            await work; return 0;
        }
        await database.Initialize();
        if (args.Contains("--transition-only")) return await TransitionTests.Run(database);
        var results = new List<object>(); var failed = 0; var passed = 0;
        var cases = new List<(string, Func<NativeApplication, HttpClient, Task>)>();
        void Test(string name, Func<NativeApplication, HttpClient, Task> run) => cases.Add((name, run));
        Test("OAuth: tenant institucional, state, PKCE y cookie segura", async (app, client) =>
        {
            var state = await Start(app, client); Check(state.Length >= 40);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_oauth_attempts")) == 1);
            Check((await database.Scalar("SELECT verifier FROM tdv2_oauth_attempts"))!.ToString()!.StartsWith("aspnet:v1:"));
            Check(app.Microsoft.Exchanges == 0);
        });
        Test("OAuth: tenant common es rechazado antes de crear intento", async (app, client) =>
        {
            app.Services.GetRequiredService<IOptions<MicrosoftSettings>>().Value.TenantId = "common";
            Check((await client.GetAsync("/connect")).StatusCode == HttpStatusCode.ServiceUnavailable);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_oauth_attempts")) == 0);
        });
        Test("OAuth: state ajeno no consume intento legítimo", async (app, client) =>
        {
            var state = await Start(app, client);
            Check(DeniedLogin(await Callback(client, new string('x', 43))) && app.Microsoft.Exchanges == 0);
            Check((await Callback(client, state)).Headers.Location?.OriginalString == "/inicio");
        });
        Test("OAuth: callback sin cookie de navegador no autentica", async (app, client) =>
        {
            var state = await Start(app, client); using var other = app.Client();
            Check(DeniedLogin(await Callback(other, state)) && app.Microsoft.Exchanges == 0);
        });
        Test("OAuth: caducidad y error del proveedor no canjean código", async (app, client) =>
        {
            var state = await Start(app, client); await database.Sql("UPDATE tdv2_oauth_attempts SET expires_at=now()-interval '1 second'");
            Check(DeniedLogin(await Callback(client, state)) && app.Microsoft.Exchanges == 0);
            state = await Start(app, client);
            Check(DeniedLogin(await client.GetAsync("/connect?error=access_denied&state=" + state)) && app.Microsoft.Exchanges == 0);
            Check(DeniedLogin(await Callback(client, state)) && app.Microsoft.Exchanges == 0);
        });
        Test("OAuth: callback concurrente se consume una sola vez en PostgreSQL", async (app, client) =>
        {
            var state = await Start(app, client);
            var responses = await Task.WhenAll(Callback(client, state), Callback(client, state));
            Check(responses.Count(r => r.Headers.Location?.OriginalString == "/inicio") == 1 && app.Microsoft.Exchanges == 1);
        });
        Test("Microsoft simulado + Nexo SQL: identidad, tokens cifrados y sesión revocable", async (app, client) =>
        {
            app.Microsoft.Mail = ""; app.Microsoft.Email = "PERSONA@UACJ.MX";
            var login = await Login(app, client);
            var cookies = string.Join(";", login.Headers.GetValues("Set-Cookie"));
            Check(cookies.Contains("secure", StringComparison.OrdinalIgnoreCase) && cookies.Contains("httponly", StringComparison.OrdinalIgnoreCase));
            Check(!cookies.Contains("synthetic-access") && !cookies.Contains("persona@uacj.mx"));
            Check((await database.Scalar("SELECT name FROM users"))?.ToString() == "Persona sintética");
            Check((await database.Scalar("SELECT microsoft_id::text FROM users"))?.ToString() == FakeMicrosoft.ObjectId);
            var ciphertext = (await database.Scalar("SELECT access_token FROM ms_graph_tokens"))!.ToString()!;
            Check(ciphertext != "synthetic-access-0" && app.Services.GetRequiredService<ProtectedValues>().Unprotect("graph-access", ciphertext) == "synthetic-access-0");
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_sessions")) == 1);
            Check((await client.GetAsync("/inicio")).IsSuccessStatusCode);
            Check((await client.GetAsync("/")).IsSuccessStatusCode);
        });
        Test("Nexo: usuario sin aplicación no crea identidad ni tokens locales", async (app, client) =>
        {
            await database.Sql("DELETE FROM fixture_users");
            Check(DeniedLogin(await Callback(client, await Start(app, client))));
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 0 && Convert.ToInt64(await database.Scalar("SELECT count(*) FROM ms_graph_tokens")) == 0);
        });
        Test("Nexo: aplicación no publicada y conexión ambigua deniegan", async (app, client) =>
        {
            await database.Sql("DELETE FROM fixture_app"); Check(DeniedLogin(await Callback(client, await Start(app, client))));
            await database.Sql("INSERT INTO fixture_app VALUES(47,'tdv2','x',2),(48,'tdv2','y',2)");
            Check(DeniedLogin(await Callback(client, await Start(app, client)))); Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 0);
        });
        Test("Nexo: clave, nivel e ID publicados deben ser válidos", async (app, client) =>
        {
            foreach (var mutation in new[] { "UPDATE fixture_app SET clave='otra'", "UPDATE fixture_app SET clave='tdv2',nivel_control=1", "UPDATE fixture_app SET nivel_control=2,id=0" })
            { await database.Sql(mutation); Check(DeniedLogin(await Callback(client, await Start(app, client)))); }
        });
        Test("Nexo: cuenta no individual o sin roles denegada", async (app, client) =>
        {
            await database.Sql("UPDATE fixture_users SET tipo_cuenta='servicio'"); Check(DeniedLogin(await Callback(client, await Start(app, client))));
            await database.Sql("UPDATE fixture_users SET tipo_cuenta='individual'; DELETE FROM fixture_roles");
            Check(DeniedLogin(await Callback(client, await Start(app, client))));
        });
        Test("Microsoft: correo externo y object ID inválido no autentican", async (app, client) =>
        {
            app.Microsoft.Mail = "persona@external.test"; Check(DeniedLogin(await Callback(client, await Start(app, client))));
            app.Microsoft.Mail = "persona@uacj.mx"; app.Microsoft.Id = "invalid";
            Check(DeniedLogin(await Callback(client, await Start(app, client)))); Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 0);
        });
        Test("Microsoft: mismo correo no permite sustituir object ID local", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await client.PostAsJsonAsync("/logout", new { });
            app.Microsoft.Id = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
            Check(DeniedLogin(await Callback(client, await Start(app, client))));
            Check((await database.Scalar("SELECT microsoft_id::text FROM users"))?.ToString() == FakeMicrosoft.ObjectId);
        });
        Test("Sesión activa no se sustituye por otro callback", async (app, client) =>
        {
            await Login(app, client); app.Microsoft.Mail = "otra@uacj.mx";
            Check((await Callback(client, "ignored")).Headers.Location?.OriginalString == "/inicio" && app.Microsoft.Exchanges == 1);
        });
        Test("Logout: CSRF, Nexo caído, revocación de cookie copiada y salida Microsoft", async (app, client) =>
        {
            var login = await Login(app, client); var cookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("tdv2.aspnet.session=")).Split(';')[0];
            Check((int)(await client.PostAsJsonAsync("/logout", new { })).StatusCode == 419);
            await Csrf(client); await database.Sql("REVOKE SELECT ON nexo_usuarios FROM tdv2_native_nexo");
            Check((await client.PostAsJsonAsync("/logout", new { })).IsSuccessStatusCode);
            using var replay = app.CreateClient(new() { BaseAddress = new Uri(app.PublicOrigin), HandleCookies = false, AllowAutoRedirect = false }); replay.DefaultRequestHeaders.Add("Cookie", cookie);
            Check((await replay.GetAsync("/inicio")).StatusCode == HttpStatusCode.Unauthorized);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_sessions")) == 0);
            Check((await client.GetAsync("/session/microsoft-logout")).Headers.Location?.Host == "login.microsoftonline.com");
        });
        Test("CSRF previo a login no autoriza guardados de nueva identidad", async (app, client) =>
        {
            await Csrf(client); await Login(app, client); var form = await Form(client);
            Check((int)(await Save(client, 0, form["contenido"]!)).StatusCode == 419);
        });
        Test("Módulo y ruta se exigen en HTML y JSON aunque React envíe rol/UR", async (app, client) =>
        {
            await Login(app, client); await database.Sql("DELETE FROM fixture_module_roles");
            client.DefaultRequestHeaders.Add("X-Role", "administrador");
            Check((await client.GetAsync("/inicio?role=administrador&ur=A")).StatusCode == HttpStatusCode.Forbidden);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/inicio"); request.Headers.Accept.ParseAdd("text/html");
            Check((await client.SendAsync(request)).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("INSERT INTO fixture_module_roles VALUES(30,9); UPDATE fixture_modules SET ruta='/otra' WHERE id=9");
            Check((await client.GetAsync("/formatos/A")).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Configuración y jerarquía Nexo: padre, permiso y ruta exactos", async (app, client) =>
        {
            await database.Sql("UPDATE fixture_roles SET rol_clave='administrador'; INSERT INTO fixture_module_roles VALUES(30,50),(30,51)");
            await Login(app, client); Check((await client.GetAsync("/configuracion")).IsSuccessStatusCode);
            Check((await client.GetAsync("/configuracion/sincronizaciones")).IsSuccessStatusCode);
            await database.Sql("UPDATE fixture_modules SET modulo_padre_id=999 WHERE id=51");
            Check((await client.GetAsync("/configuracion/sincronizaciones")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("DELETE FROM fixture_module_roles WHERE modulo_id=50");
            Check((await client.GetAsync("/configuracion")).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Nexo: permisos se revocan al siguiente request sin cierre de sesión", async (app, client) =>
        {
            await Login(app, client); Check((await client.GetAsync("/inicio")).IsSuccessStatusCode);
            await database.Sql("DELETE FROM fixture_roles"); Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Responsable edita subordinadas de su rama y rechaza UR ajena", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var form = await Form(client);
            Check((await client.GetAsync("/formatos/B")).StatusCode == HttpStatusCode.Forbidden);
            Check((await SaveAll(client, "B", 0, form["contenido"]!)).StatusCode == HttpStatusCode.Forbidden);
            Check((await SaveAll(client, "A3", 0, form["contenido"]!)).IsSuccessStatusCode);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM formato_operaciones")) == 1);
        });
        Test("Administrador con rol responsable conserva límite de edición de su rama", async (app, client) =>
        {
            await database.Sql("""
                UPDATE fixture_roles SET rol_clave='administrador';
                INSERT INTO fixture_roles VALUES(10,'persona@uacj.mx',31,'responsable_ur','Responsable');
                UPDATE unidades_responsables_poa SET num_empleado='0001' WHERE id_ur='B';
                """);
            await Login(app, client); await Csrf(client); var form = await Form(client);
            Check((await client.GetAsync("/formatos/B")).IsSuccessStatusCode);
            Check((await SaveAll(client, "B", 0, form["contenido"]!)).StatusCode == HttpStatusCode.Forbidden);
            Check((await SaveAll(client, "A3", 0, form["contenido"]!)).IsSuccessStatusCode);
            await database.Sql("UPDATE fixture_users SET \"ID_UR\"=null");
            Check((await Save(client, 0, form["contenido"]!)).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Colaboración exige vínculo local y concesión SQL vigente; revocación inmediata", async (app, client) =>
        {
            await database.Sql("""
                UPDATE fixture_roles SET rol_clave='colaborador_local';
                INSERT INTO colaboraciones_ur(email,num_empleado,nombre,id_ur_origen,id_ur_alcance,tipo,nexo_concesion_id,nexo_rol_id,otorgado_por,ur_otorgante)
                  VALUES('persona@uacj.mx','0001','Persona sintética','A4','A3','local',99,30,'responsable@uacj.mx','A');
                """);
            await Login(app, client); await Csrf(client);
            Check((await client.GetAsync("/formatos/A3")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("INSERT INTO fixture_grants VALUES(99,'persona@uacj.mx',30,'A4','aplicacion'),(100,'persona@uacj.mx',31,null,'central')");
            var form = (await Json(await client.GetAsync("/formatos/A3")))["props"]!;
            Check((await SaveAll(client, "A3", 0, form["contenido"]!)).IsSuccessStatusCode);
            Check((await client.GetAsync("/formatos/A")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("DELETE FROM fixture_grants WHERE concesion_id=99");
            Check((await client.GetAsync("/formatos/A3")).StatusCode == HttpStatusCode.Forbidden);
            Check((await SaveAll(client, "A3", 1, form["contenido"]!)).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Nexo caído deniega lecturas y guardados sin filtrar error SQL", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var form = await Form(client);
            await database.Sql("REVOKE SELECT ON nexo_usuarios FROM tdv2_native_nexo");
            var response = await Save(client, 0, form["contenido"]!);
            Check(response.StatusCode == HttpStatusCode.ServiceUnavailable && !(await response.Content.ReadAsStringAsync()).Contains("permission denied"));
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.ServiceUnavailable);
            Check((await client.GetAsync("/")).IsSuccessStatusCode);
            await database.Sql("GRANT SELECT ON nexo_usuarios TO tdv2_native_nexo"); Check((await client.GetAsync("/inicio")).IsSuccessStatusCode);
        });
        Test("PostgreSQL: lectura no crea filas y guardado conserva respuestas canónicas", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var form = await Form(client);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM formatos_ur")) == 0);
            var content = form["contenido"]!; content["encabezado"]!["responsable"] = "Respuesta sintética con áéíóú";
            content["encabezado"]!["area"] = "forjada"; content["porcentaje"] = 100;
            var saved = await Save(client, 0, content); Check(saved.IsSuccessStatusCode);
            var result = await Json(saved); Check(result["version"]!.GetValue<int>() == 1 && result["porcentaje"]!.GetValue<int>() == 0);
            var again = await Form(client); Check(again["contenido"]!["encabezado"]!["responsable"]!.GetValue<string>() == "Respuesta sintética con áéíóú");
            Check(again["contenido"]!["encabezado"]!["area"]!.GetValue<string>() == "forjada");
            Check((await database.Scalar("SELECT actualizado_por FROM formatos_ur"))?.ToString() == "persona@uacj.mx");
        });
        Test("PostgreSQL: dos primeros guardados concurrentes crean una fila y un 409", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var form = await Form(client);
            var responses = await Task.WhenAll(Save(client, 0, form["contenido"]!), Save(client, 0, form["contenido"]!));
            Check(responses.Count(r => r.IsSuccessStatusCode) == 1 && responses.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM formatos_ur")) == 1);
        });
        Test("PostgreSQL: dos editores con misma versión no sobrescriben respuestas", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var form = await Form(client); Check((await Save(client, 0, form["contenido"]!)).IsSuccessStatusCode);
            var first = form["contenido"]!.DeepClone(); first["encabezado"]!["responsable"] = "Ganadora 1";
            var second = form["contenido"]!.DeepClone(); second["encabezado"]!["responsable"] = "Ganadora 2";
            var responses = await Task.WhenAll(Save(client, 1, first), Save(client, 1, second));
            Check(responses.Count(r => r.IsSuccessStatusCode) == 1 && responses.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1);
            Check(Convert.ToInt32(await database.Scalar("SELECT version FROM formatos_ur")) == 2);
        });
        Test("PostgreSQL: error transaccional conserva versión/respuesta y permite reintento", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var content = (await Form(client))["contenido"]!;
            Check((await Save(client, 0, content)).IsSuccessStatusCode); await database.Sql(FailureTrigger);
            content["encabezado"]!["responsable"] = "Recuperada";
            var failedSave = await Save(client, 1, content);
            Check(failedSave.StatusCode == HttpStatusCode.ServiceUnavailable && !(await failedSave.Content.ReadAsStringAsync()).Contains("SYNTHETIC_DATABASE_FAILURE"));
            Check(Convert.ToInt32(await database.Scalar("SELECT version FROM formatos_ur")) == 1);
            Check((await database.Scalar("SELECT contenido->'encabezado'->>'responsable' FROM formatos_ur"))?.ToString() == "");
            await database.Sql("DROP TRIGGER fail_save ON formatos_ur; DROP FUNCTION fixture_fail_save()");
            Check((await Save(client, 1, content)).IsSuccessStatusCode && Convert.ToInt32(await database.Scalar("SELECT version FROM formatos_ur")) == 2);
        });
        Test("PostgreSQL: payload inválido o contexto obsoleto no modifica filas", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var content = (await Form(client))["contenido"]!;
            content["preguntas"] = new JsonArray(); Check((int)(await Save(client, 0, content)).StatusCode == 422);
            client.DefaultRequestHeaders.Add("X-TDV2-Context", "obsolete"); Check((await Save(client, 0, content)).StatusCode == HttpStatusCode.Conflict);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM formato_operaciones")) == 0);
            Check(Convert.ToInt32(await database.Scalar("SELECT version FROM formatos_ur")) == 0);
        });
        Test("Sesión PostgreSQL: expiración y cambio de identidad local invalidan acceso", async (app, client) =>
        {
            await Login(app, client); await database.Sql("UPDATE users SET microsoft_id='dddddddd-dddd-4ddd-8ddd-dddddddddddd'");
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Unauthorized);
            await database.Sql($"UPDATE users SET microsoft_id='{FakeMicrosoft.ObjectId}'; UPDATE tdv2_sessions SET expires_at=now()-interval '1 second'");
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Unauthorized);
        });
        Test("Graph: refresco concurrente una vez, rotación cifrada y foto fallida sin logout", async (app, client) =>
        {
            await Login(app, client); await database.Sql("UPDATE ms_graph_tokens SET expires='1'");
            var requests = await Task.WhenAll(client.GetAsync("/user/photo"), client.GetAsync("/user/photo"));
            Check(requests.All(r => r.IsSuccessStatusCode) && app.Microsoft.Refreshes == 1);
            var encrypted = (await database.Scalar("SELECT refresh_token FROM ms_graph_tokens"))!.ToString()!;
            Check(app.Services.GetRequiredService<ProtectedValues>().Unprotect("graph-refresh", encrypted) == "synthetic-refresh-1");
            app.Microsoft.PhotoFailure = true;
            Check((await client.GetAsync("/user/photo")).IsSuccessStatusCode && (await client.GetAsync("/inicio")).IsSuccessStatusCode);
        });
        Test("Graph: sin rotación conserva refresh anterior; error no cambia token", async (app, client) =>
        {
            await Login(app, client); app.Microsoft.RotateRefresh = false; await database.Sql("UPDATE ms_graph_tokens SET expires='1'");
            Check((await client.GetAsync("/user/photo")).IsSuccessStatusCode);
            var stored = (await database.Scalar("SELECT refresh_token FROM ms_graph_tokens"))!.ToString()!;
            Check(app.Services.GetRequiredService<ProtectedValues>().Unprotect("graph-refresh", stored) == "synthetic-refresh-0");
            app.Microsoft.TokenFailure = true; await database.Sql("UPDATE ms_graph_tokens SET expires='1'");
            Check((await client.GetAsync("/user/photo")).IsSuccessStatusCode);
            Check((await database.Scalar("SELECT refresh_token FROM ms_graph_tokens"))!.ToString() == stored);
        });

        RegisterAccessCases(database, Test);
        RegisterCentralCollaborationCases(database, Test);
        RegisterPerformanceCases(database, Test);
        RegisterPhotoCases(database, Test);
        RegisterParticipationCases(database, Test);
        RegisterSyncCases(database, Test);
        RegisterEditingCases(database, Test);
        if (Environment.GetEnvironmentVariable("TDV2_TEST_CASE_PREFIX") is { Length: > 0 } prefix)
            cases.RemoveAll(c => !prefix.Split('|').Any(p => c.Item1.StartsWith(p, StringComparison.Ordinal)));
        // El alcance de colaboradores centrales forma parte de la autorización de captura.
        foreach (var (name, run) in args.Contains("--browser-only") ? [] : args.Contains("--editing-only") ? cases.Where(c=>c.Item1.StartsWith("Edición/") || c.Item1.StartsWith("Central/")).ToList() : args.Contains("--sync-only") ? cases.Where(c=>c.Item1.StartsWith("Sync/")).ToList() : cases)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                await database.Reset(); await using var app = new NativeApplication(database); using var client = app.Client();
                await run(app, client); passed++; Console.WriteLine("PASS " + name); results.Add(new { name, passed = true, milliseconds = watch.ElapsedMilliseconds });
            }
            catch (Exception error)
            {
                failed++; Console.WriteLine("FAIL " + name + " [" + error.GetType().Name + "]");
                Console.WriteLine(string.Join('\n', error.StackTrace?.Split('\n').Where(line => line.Contains("Tests.cs")).Take(4) ?? []));
                if (error.StackTrace?.Contains("NativeTests.Check") == true) Console.WriteLine(error.Message);
                // No SQL/provider exception text: may include connection or token data.
                results.Add(new { name, passed = false, exceptionType = error.GetType().Name });
            }
        }
        if (Environment.GetEnvironmentVariable("TDV2_TEST_SKIP_BROWSER") != "true")
        {
            try { await Browser(database); passed++; results.Add(new { name = "React → ASP.NET → PostgreSQL / Edge", passed = true }); }
            catch (Exception error)
            {
                failed++; Console.WriteLine("FAIL Browser [" + error.GetType().Name + "]");
                // Test-host diagnostics only; never print SQL, connection strings or provider responses.
                if (error is InvalidOperationException) Console.WriteLine(error.Message);
                Console.WriteLine(error.StackTrace);
                results.Add(new { name = "Browser", passed = false, exceptionType = error.GetType().Name });
            }
        }
        var report = new { utc = DateTimeOffset.UtcNow, postgres = (await database.Scalar("SELECT version()"))!.ToString(), realPostgreSql = true,
            microsoft = "simulated HTTP peer", nexo = "synthetic publisher via PostgreSQL views and published functions; no institutional service",
            sources = "SQL Server/MySQL ADO.NET readers simulated; production SELECT/mapping code; no institutional connectivity", passed, failed, results };
        await File.WriteAllTextAsync(Path.Combine(database.Artifacts, "verification.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"NATIVE TOTAL {passed + failed}; PASS {passed}; FAIL {failed}");
        return failed == 0 ? 0 : 1;
    }

    private static async Task Browser(NativeDatabase database)
    {
        var selectedFlow = Environment.GetEnvironmentVariable("TDV2_TEST_BROWSER_FLOW") ?? "all";
        if (selectedFlow == "visual-studio") { await VerifyDevelopmentStartup(database); return; }
        Check(new[] { "all", "formats", "access", "scope", "sync", "performance", "capture-matrix", "participants", "presentation", "users-served", "photos" }.Contains(selectedFlow), "Unknown browser flow.");
        await database.Reset();
        if (selectedFlow == "photos") await SetupSync(database);
        if (selectedFlow == "presentation")
        {
            await SetupSync(database);
            await database.Sql("INSERT INTO fixture_modules VALUES(53,47,'configuracion_procesos','Configuración procesos','/configuracion/procesos','mdi-tune',13,50); INSERT INTO fixture_module_roles VALUES(34,53)");
        }
        using var certificate = NativeApplication.Certificate();
        await using var app = new NativeApplication(database) { Browser = true };
        app.UseKestrel(options => options.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(certificate)));
        using var client = app.CreateClient(new() { AllowAutoRedirect = false });
        var origin = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single().TrimEnd('/');
        app.Services.GetRequiredService<IOptions<MicrosoftSettings>>().Value.PublicOrigin = origin;
        var start = new ProcessStartInfo("node")
        { WorkingDirectory = Path.Combine(database.Workspace, "ClientApp"), UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("tests/browser/native-flow.mjs");
        start.Environment["TDV2_BROWSER_ORIGIN"] = origin; start.Environment["TDV2_BROWSER_KEY"] = app.ControlToken;
        start.Environment["TDV2_BROWSER_ARTIFACTS"] = database.Artifacts;
        // Browser runner needs no database credentials.
        start.Environment.Remove("TDV2_TEST_CONNECTION"); start.Environment.Remove("PGPASSWORD");
        if (selectedFlow is "performance" or "capture-matrix" or "participants" or "presentation" or "users-served" or "photos")
        {
            start.ArgumentList.Clear(); start.ArgumentList.Add(selectedFlow switch {
                "photos" => "tests/browser/photos-flow.mjs",
                "users-served" => "tests/browser/users-served-flow.mjs", "presentation" => "tests/browser/presentation-flow.mjs", "participants" => "tests/browser/participants-browser.mjs", "performance" => "tests/browser/performance-flow.mjs", _ => "tests/browser/capture-matrix.mjs" });
            using var measured = Process.Start(start)!;
            var output = measured.StandardOutput.ReadToEndAsync(); var errors = measured.StandardError.ReadToEndAsync();
            await measured.WaitForExitAsync(); Console.Write(await output); Console.Write(await errors);
            Check(measured.ExitCode == 0, "Browser verification failed: " + selectedFlow); return;
        }
        if (selectedFlow is "all" or "formats")
        {
        using var process = Process.Start(start)!;
        async Task Report(StreamReader reader) { while (await reader.ReadLineAsync() is { } line) Console.WriteLine(line); }
        var output = Report(process.StandardOutput); var errorOutput = Report(process.StandardError);
        await process.WaitForExitAsync();
        await Task.WhenAll(output, errorOutput);
        Check(process.ExitCode == 0, "Browser verification failed.");
        }
        await database.Reset(); await SetupAccess(database);
        start.ArgumentList.Clear(); start.ArgumentList.Add("tests/browser/access-flow.mjs");
        if (selectedFlow is "all" or "access")
        {
        using var accessProcess = Process.Start(start)!;
        var accessOutput = accessProcess.StandardOutput.ReadToEndAsync(); var accessErrors = accessProcess.StandardError.ReadToEndAsync();
        await accessProcess.WaitForExitAsync(); Console.Write(await accessOutput); Console.Write(await accessErrors);
        Check(accessProcess.ExitCode == 0, "Access browser verification failed.");
        }
        await database.Reset(); await SetupAccess(database);
        start.ArgumentList.Clear(); start.ArgumentList.Add("tests/browser/scope-flow.mjs");
        if (selectedFlow is "all" or "scope")
        {
        using var scopeProcess = Process.Start(start)!;
        var scopeOutput = scopeProcess.StandardOutput.ReadToEndAsync(); var scopeErrors = scopeProcess.StandardError.ReadToEndAsync();
        await scopeProcess.WaitForExitAsync(); Console.Write(await scopeOutput); Console.Write(await scopeErrors);
        Check(scopeProcess.ExitCode == 0, "Scope browser verification failed.");
        }
        await database.Reset(); await SetupSync(database);
        start.ArgumentList.Clear(); start.ArgumentList.Add("tests/browser/sync-flow.mjs");
        if (selectedFlow is "all" or "sync")
        {
        using var syncProcess=Process.Start(start)!;
        var syncOutput=syncProcess.StandardOutput.ReadToEndAsync(); var syncErrors=syncProcess.StandardError.ReadToEndAsync();
        await syncProcess.WaitForExitAsync(); Console.Write(await syncOutput); Console.Write(await syncErrors);
        Check(syncProcess.ExitCode==0,"Synchronization browser verification failed.");
        }
        Console.WriteLine("PASS React → ASP.NET → PostgreSQL / Edge");
    }
}
