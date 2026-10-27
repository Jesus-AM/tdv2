using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Tdv2.Competencias;
using Tdv2.Domain;
using Tdv2.Integrations.Microsoft;
using Tdv2.Infrastructure;

namespace Tdv2.NativeVerification;

internal sealed partial class DigitalTests(NativeDatabase shared)
{
    private const string Base = "/competencias-digitales";
    private string diagnosis = "", directory = "", appConnection = "", nexoConnection = "";
    private Dictionary<string, string?> Configuration => new() { ["ConnectionStrings:CompetenciasDigitales"] = appConnection,
        ["ConnectionStrings:CompetenciasDigitalesNexo"] = nexoConnection, ["CompetenciasDigitales:Nexo:ApplicationId"] = "87" };
    private static void Check(bool value, string message = "Condición incumplida") { if (!value) throw new InvalidOperationException(message); }
    private static async Task<JsonObject> Json(HttpResponseMessage response, int status = 200)
    { Check((int)response.StatusCode == status, $"HTTP {(int)response.StatusCode}, esperado {status}"); return (await response.Content.ReadFromJsonAsync<JsonObject>())!; }
    private static async Task Status(Task<HttpResponseMessage> response, int status) { using var result = await response; Check((int)result.StatusCode == status, $"HTTP {(int)result.StatusCode}, esperado {status}"); }
    private static async Task<object?> Sql(string connectionString, string sql, params object[] parameters)
    {
        await using var connection = new NpgsqlConnection(connectionString); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection); foreach (var p in parameters) command.Parameters.AddWithValue(p);
        return await command.ExecuteScalarAsync();
    }
    private Task<object?> Nexo(string sql, params object[] p) => Sql(directory, sql, p);
    private Task<object?> Data(string sql, params object[] p) => Sql(diagnosis, sql, p);
    private DigitalDbContext Db() => new(new DbContextOptionsBuilder<DigitalDbContext>().UseNpgsql(diagnosis, o => o.MigrationsHistoryTable("__CDMigrationsHistory")).Options);
    private async Task Initialize()
    {
        await shared.ValidateCluster();
        // Bases nuevas y nombres exclusivamente sintéticos en el clúster desechable ya validado.
        await shared.Sql("CREATE DATABASE cd_native_test"); await shared.Sql("CREATE DATABASE cd_nexo_test");
        diagnosis = new NpgsqlConnectionStringBuilder(shared.Admin) { Database = "cd_native_test" }.ConnectionString;
        directory = new NpgsqlConnectionStringBuilder(shared.Admin) { Database = "cd_nexo_test" }.ConnectionString;
        await using (var db = Db()) { await db.Database.MigrateAsync(); await db.Database.MigrateAsync(); }
        await Nexo(await File.ReadAllTextAsync(Path.Combine(shared.Workspace, "tests/TDV2.NativeVerification/nexo-fixture.sql")));
        await Nexo((await File.ReadAllTextAsync(Path.Combine(shared.Workspace, "tests/TDV2.NativeVerification/access-fixture.sql"))).Replace("nexo_a47_", "nexo_a87_"));
        var password = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await shared.Sql($"CREATE ROLE cd_native_app LOGIN PASSWORD '{password}'; CREATE ROLE cd_native_nexo LOGIN PASSWORD '{password}'");
        await Data("GRANT USAGE ON SCHEMA public TO cd_native_app; GRANT SELECT,INSERT,UPDATE ON cd_surveys,cd_consents TO cd_native_app; GRANT SELECT,INSERT,UPDATE,DELETE ON cd_targets TO cd_native_app; GRANT SELECT ON cd_control TO cd_native_app; GRANT EXECUTE ON FUNCTION public.cd_prepare_reset(uuid,text,text,boolean), public.cd_execute_reset(uuid,text,text) TO cd_native_app");
        await Nexo("GRANT USAGE ON SCHEMA public TO cd_native_nexo; GRANT SELECT ON nexo_aplicacion,nexo_usuarios,nexo_usuario_rol,nexo_modulos,nexo_modulo_rol,nexo_concesiones TO cd_native_nexo; GRANT EXECUTE ON FUNCTION nexo_a87_representacion(jsonb) TO cd_native_nexo");
        appConnection = new NpgsqlConnectionStringBuilder(diagnosis) { Username = "cd_native_app", Password = password }.ConnectionString;
        nexoConnection = new NpgsqlConnectionStringBuilder(directory) { Username = "cd_native_nexo", Password = password }.ConnectionString;
    }
    private async Task Reset()
    {
        await shared.Reset();
        await shared.Sql("""
            INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,desc_ur,id_ur_pertenece,nivel_ur,estatus_ur,presente,num_empleado)
            VALUES('A31',2026,'120','Área hermana A31','A',3,'Activo',true,'0031'),('B4',2026,'210','Área ajena B4','B',4,'Activo',true,null);
            INSERT INTO sincronizacion_catalogos VALUES('sii',6,now());
            """);
        await Data("TRUNCATE cd_surveys,cd_consents,cd_targets,cd_reset_operations");
        await Nexo("""
            TRUNCATE fixture_app,fixture_users,fixture_roles,fixture_modules,fixture_module_roles,fixture_grants,fixture_units,
              fixture_capability,fixture_representation,fixture_calls,fixture_faults RESTART IDENTITY;
            INSERT INTO fixture_app VALUES(87,'cd_synthetic','Competencias sintéticas',1);
            INSERT INTO fixture_users VALUES(10,20,'persona@uacj.mx','Persona sintética','individual','0001','A4','adscripcion'),
              (11,21,'otra@uacj.mx','Otra persona sintética','individual','0002','A3','adscripcion');
            INSERT INTO fixture_roles VALUES(10,'persona@uacj.mx',30,'participante','Participante'),(11,'otra@uacj.mx',31,'participante','Participante');
            INSERT INTO fixture_capability VALUES('persona@uacj.mx',true,true,'institucional',null,30);
            INSERT INTO fixture_units VALUES('A',null,'0001',2),('A3','A','0002',3),('A4','A3',null,4),('A31','A','0031',3),('B',null,'0003',2),('B4','B',null,4);
            """);
        for (var i = 0; i < 5; i++)
        {
            await Nexo("INSERT INTO fixture_modules VALUES($1,87,$2,$3,$4,null,$5,null)", 100L + i, "synthetic_module_" + i, DigitalRoutes.Names[i], Base + "/" + DigitalRoutes.Slugs[i], i);
            await Nexo("INSERT INTO fixture_module_roles VALUES(30,$1),(31,$1)", 100L + i);
        }
    }
    private Task<object?> Role(string key) => Nexo("UPDATE fixture_roles SET rol_clave=$1 WHERE email='persona@uacj.mx'", key);
    private Task Exclusive() => shared.Sql("DELETE FROM fixture_roles WHERE email='persona@uacj.mx'");
    private static async Task Login(NativeApplication app, HttpClient client, string path = Base + "/encuesta")
    {
        var start = await client.GetAsync("/connect?returnUrl=" + Uri.EscapeDataString(path));
        Check(start.StatusCode == HttpStatusCode.Redirect, "Inicio OAuth sintético");
        var query = QueryHelpers.ParseQuery(start.Headers.Location!.Query); app.Microsoft.ExpectedChallenge = query["code_challenge"];
        var response = await client.GetAsync("/connect?code=synthetic-code&state=" + Uri.EscapeDataString(query["state"].ToString()));
        Check(response.Headers.Location?.OriginalString == path, "Retorno OAuth no conserva destino");
        var csrf = await Json(await client.GetAsync("/session/csrf")); client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf["token"]!.ToString());
    }
    private static async Task<DigitalSurveyView> Survey(HttpClient client)
    {
        var response = await client.GetAsync(Base + "/api/encuesta"); Check(response.IsSuccessStatusCode, "Lectura de encuesta");
        return (await response.Content.ReadFromJsonAsync<DigitalSurveyView>(DigitalInstrument.Json))!;
    }
    private static async Task<DigitalSurveyView> Accept(HttpClient client)
    {
        var view = await Survey(client);
        await Status(client.PostAsJsonAsync(Base + "/api/aviso", new { generation = (await Survey(client)).Generation, version = view.Instrument["notice"]!["version"]!.ToString() }), 200);
        return await Survey(client);
    }
    private static DigitalSave Complete(DigitalSurveyView view, int level = 1)
    {
        var instrument = new DigitalInstrument(view.Instrument.ToJsonString()); var profile = new JsonObject();
        foreach (var field in instrument.Resource["profile"]!.AsArray()) profile[field!["id"]!.ToString()] = field["multiple"]?.GetValue<bool>() == true ? new JsonArray(field["options"]![0]!.DeepClone()) : field["options"]![0]!.DeepClone();
        var answers = instrument.Blocks.SelectMany(b => b.Tools.Where(t => !t.Opt && !t.Doc).Select(t => new KeyValuePair<string, DigitalAnswer>(t.Id,
            b.Id is "ciber" or "datos" or "sii" ? new("level", Checks: string.Concat(Enumerable.Range(0, 3).Select(i => i < level ? "1110" : "0000"))) : new("level", level)))).ToDictionary();
        return new(view.Version, profile, answers, view.Generation);
    }
    private static async Task<DigitalSurveyView> CompleteAndSave(HttpClient client, int level = 1)
    {
        var view = await Accept(client); var input = Complete(view, level);
        await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, input.Profile, view.Answers, view.Generation)), 200);
        view = await Survey(client);
        foreach (var block in new DigitalInstrument(view.Instrument.ToJsonString()).Blocks)
        {
            foreach (var tool in block.Tools) if (input.Answers.TryGetValue(tool.Id, out var answer)) view.Answers[tool.Id] = answer;
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, input.Profile, view.Answers, view.Generation)), 200); view = await Survey(client);
        }
        return view;
    }
    private async Task Seed(string employee, string unit, int level, bool completed)
    {
        var instrument = new DigitalInstrument(await File.ReadAllTextAsync(Path.Combine(shared.Workspace, "tdv2/Contracts/competencias-digitales-v1.json")));
        var evaluation = instrument.Evaluate(new(), new Dictionary<string, DigitalAnswer>());
        var initial = new DigitalSurveyView(0, null, new(), new(), new(), instrument.Resource, false, evaluation, instrument.Navigation(evaluation, true));
        var input = Complete(initial, level); var report = instrument.Evaluate(input.Profile, input.Answers).Report;
        await using var db = Db();
        db.Surveys.Add(new() { Employee = employee, Email = employee + "@uacj.mx", Name = "Persona de prueba " + employee, UnitId = unit,
            ProfileJson = input.Profile.ToJsonString(), AnswersJson = JsonSerializer.Serialize(input.Answers, DigitalInstrument.Json), InstrumentVersion = instrument.Version, InstrumentJson = instrument.Resource.ToJsonString(),
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow, CompletedAt = completed ? DateTimeOffset.UtcNow : null, ReportJson = completed ? JsonSerializer.Serialize(report, DigitalInstrument.Json) : null });
        await db.SaveChangesAsync();
    }
    internal static async Task<int> Run(NativeDatabase database)
    {
        var suite = new DigitalTests(database); await suite.Initialize(); return await suite.Execute();
    }
    private async Task<int> Execute()
    {
        var cases = new List<(string Name, Func<NativeApplication, HttpClient, Task> Work)>();
        void Test(string name, Func<NativeApplication, HttpClient, Task> run) => cases.Add((name, run));
        Test("Conexiones: dos historias EF, tablas y permisos separados; repetición sin cambios", async (_, _) =>
        {
            Check(await Data("SELECT to_regclass('public.formatos_ur')::text") is DBNull);
            Check(await shared.Scalar("SELECT to_regclass('public.cd_surveys')::text") is DBNull);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM \"__CDMigrationsHistory\"")) == 2);
            await using var db = Db(); Check(!db.Database.HasPendingModelChanges()); Check(db.Database.GetMigrations().Count() == 2);
            Check(!(bool)(await Data("SELECT has_table_privilege('cd_native_app','cd_surveys','DELETE')"))!);
            Check(!(bool)(await Nexo("SELECT has_table_privilege('cd_native_nexo','fixture_users','SELECT')"))!);
        });
        Test("Sin configuración del subsitio: portal, /inicio y autorización TDV2 conservados", async (_, _) =>
        {
            await using var absent = new NativeApplication(shared); using var client = absent.Client();
            await Status(client.GetAsync("/"), 200); await Login(absent, client, "/inicio"); await Status(client.GetAsync("/inicio"), 200);
            await Status(client.GetAsync(Base + "/encuesta"), 503);
        });
        Test("Migraciones equivocadas rechazan tablas ajenas y configuración nunca reutiliza TDV2", async (_, _) =>
        {
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Tdv2"] = shared.Admin, ["ConnectionStrings:CompetenciasDigitales"] = shared.Admin }).Build();
            try { DigitalDbContext.Configure(new DbContextOptionsBuilder<DigitalDbContext>(), config); throw new InvalidOperationException("Se aceptó la misma base"); } catch (DomainProblem e) when (e.Status == 503) { }
            await using (var wrong = new DigitalDbContext(new DbContextOptionsBuilder<DigitalDbContext>().UseNpgsql(shared.Admin, o => o.MigrationsHistoryTable("__CDMigrationsHistory")).Options))
            { try { await wrong.Database.MigrateAsync(); throw new InvalidOperationException("Se cruzaron migraciones CD"); } catch (PostgresException e) when (e.SqlState == "P0001") { } }
            await using (var wrong = new Tdv2DbContext(new DbContextOptionsBuilder<Tdv2DbContext>().UseNpgsql(diagnosis).Options))
            { try { await wrong.Database.MigrateAsync(); throw new InvalidOperationException("Se cruzaron migraciones TDV2"); } catch (PostgresException e) when (e.SqlState == "55000") { } catch (InvalidOperationException e) when (e.InnerException is PostgresException { SqlState: "55000" }) { } }
            Check(await shared.Scalar("SELECT to_regclass('public.cd_surveys')::text") is DBNull && await Data("SELECT to_regclass('public.formatos_ur')::text") is DBNull);
        });
        Test("OAuth: empleado de Competencias sin roles TDV2 y destino profundo conservado", async (app, client) =>
        {
            await shared.Sql("DELETE FROM fixture_roles"); await Login(app, client, Base + "/mi-informe-ruta");
            await Status(client.GetAsync(Base + "/api/informe"), 200); await Status(client.GetAsync("/inicio"), 403);
        });
        Test("Anónimo conserva destino, rutas profundas y recargas requieren autorización", async (app, client) =>
        {
            foreach (var slug in DigitalRoutes.Slugs)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, Base + "/" + slug); request.Headers.Accept.ParseAdd("text/html");
                var response = await client.SendAsync(request); Check(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.OriginalString.Contains("returnUrl="));
            }
            await Role("administrador"); await Login(app, client);
            foreach (var slug in DigitalRoutes.Slugs)
            {
                await Status(client.GetAsync(Base + "/" + slug), 200);
                using var request = new HttpRequestMessage(HttpMethod.Get, Base + "/" + slug); request.Headers.Accept.ParseAdd("text/html");
                var html = await client.SendAsync(request); Check(html.IsSuccessStatusCode && (await html.Content.ReadAsStringAsync()).Contains("id=\"app\""));
            }
        });
        Test("Roles de otra aplicación no conceden permisos; empleado obligatorio y módulos reales", async (app, client) =>
        {
            await shared.Sql("UPDATE fixture_roles SET rol_clave='administrador'"); await Login(app, client);
            await Status(client.GetAsync(Base + "/api/resultados"), 403);
            await Nexo("UPDATE fixture_users SET num_empleado=NULL WHERE email='persona@uacj.mx'"); await Status(client.GetAsync(Base + "/api/encuesta"), 403);
            await Nexo("UPDATE fixture_users SET num_empleado='0001' WHERE email='persona@uacj.mx'; DELETE FROM fixture_module_roles WHERE modulo_id=102");
            await Status(client.GetAsync(Base + "/api/encuesta"), 403);
        });
        Test("Nexo: aplicación incorrecta y caída deniegan sin secreto ni permiso en caché", async (app, client) =>
        {
            await Login(app, client); await Nexo("UPDATE fixture_app SET id=47"); await Status(client.GetAsync(Base + "/api/encuesta"), 503);
            await Nexo("UPDATE fixture_app SET id=87; REVOKE SELECT ON nexo_usuarios FROM cd_native_nexo");
            var response = await client.GetAsync(Base + "/api/encuesta"); Check((int)response.StatusCode == 503 && !(await response.Content.ReadAsStringAsync()).Contains("fixture"));
            await Nexo("GRANT SELECT ON nexo_usuarios TO cd_native_nexo");
        });
        foreach (var role in new[] { "administrador", "responsable_ur_supervisor" })
            Test(role + ": consulta institucional; Configuración exclusiva del administrador", async (app, client) =>
            {
                await Role(role); await Login(app, client); var aggregate = await Json(await client.GetAsync(Base + "/api/avance"));
                Check(aggregate["options"]!.AsArray().Count == 6 && aggregate["levels"]!.AsArray().Count == 3);
                await Status(client.GetAsync(Base + "/configuracion"), role == "administrador" ? 200 : 403);
                await Status(client.PutAsJsonAsync(Base + "/api/metas/A", new { generation = (await Survey(client)).Generation, value = 10, version = 0 }), role == "administrador" ? 200 : 403);
            });
        foreach (var (employee, expected) in new[] { ("0001", new[] { "A", "A3", "A4", "A31" }), ("0002", new[] { "A3", "A4" }), ("0031", new[] { "A31" }) })
            Test("Responsable " + employee + ": rama exacta, sin nivel institucional ni filtros ajenos", async (app, client) =>
            {
                await Role("responsable_ur"); await Nexo("UPDATE fixture_users SET num_empleado=$1 WHERE email='persona@uacj.mx'", employee); await Login(app, client);
                var aggregate = await Json(await client.GetAsync(Base + "/api/resultados"));
                Check(aggregate["options"]!.AsArray().Select(u => u!["id"]!.ToString()).Order().SequenceEqual(expected.Order())); Check(aggregate["levels"]!.AsArray().Count == 0);
                foreach (var endpoint in new[] { "avance", "resultados", "resultados/descarga" }) await Status(client.GetAsync(Base + "/api/" + endpoint + "?unit=B"), 403);
                await Status(client.GetAsync(Base + "/api/avance?level=2"), 403);
                if (employee == "0002") { await Status(client.GetAsync(Base + "/api/resultados?unit=A"), 403); await Status(client.GetAsync(Base + "/api/resultados?unit=A31"), 403); }
            });
        Test("Responsable de otros niveles y adscripción sin responsabilidad conservan solo capacidades personales", async (app, client) =>
        {
            await Role("responsable_ur"); await Nexo("UPDATE fixture_users SET num_empleado='0040',\"ID_UR\"='A' WHERE email='persona@uacj.mx'");
            await shared.Sql("UPDATE unidades_responsables_poa SET num_empleado='0040' WHERE id_ur='A4'"); await Login(app, client);
            await Status(client.GetAsync(Base + "/api/avance"), 403); await Status(client.GetAsync(Base + "/api/encuesta"), 200);
            await Role("participante"); await Nexo("UPDATE fixture_users SET num_empleado='0001' WHERE email='persona@uacj.mx'"); await Status(client.GetAsync(Base + "/api/resultados"), 403);
        });
        Test("Responsabilidades múltiples forman una unión autorizada sin hermanos ni duplicados", async (app, client) =>
        {
            await Role("responsable_ur"); await shared.Sql("UPDATE unidades_responsables_poa SET num_empleado=NULL WHERE id_ur='A'; UPDATE unidades_responsables_poa SET num_empleado='0001' WHERE id_ur IN ('A3','B')"); await Login(app, client);
            var data = await Json(await client.GetAsync(Base + "/api/avance")); Check(data["roots"]!.AsArray().Count == 2);
            Check(data["options"]!.AsArray().Select(u => u!["id"]!.ToString()).Order().SequenceEqual(new[] { "A3", "A4", "B", "B4" }.Order()));
            await Status(client.GetAsync(Base + "/api/avance?unit=A31"), 403);
        });
        Test("Perfil: antecesoras exactas, adscripción N2 sin N3, múltiples/incompletas/ciclos", async (app, client) =>
        {
            await Login(app, client); var initial = await Survey(client);
            Check(initial.Institution["unit"]!["id"]!.ToString() == "A4" && initial.Institution["level2"]!["id"]!.ToString() == "A" && initial.Institution["level3"]!["id"]!.ToString() == "A3");
            await Nexo("UPDATE fixture_users SET \"ID_UR\"='A' WHERE email='persona@uacj.mx'"); var direct = await Survey(client);
            Check(direct.Institution["status"]!.ToString() == "available" && direct.Institution["level3"] is null);
            await Nexo("UPDATE fixture_users SET origen_ur='multiple',\"ID_UR\"=NULL WHERE email='persona@uacj.mx'"); Check((await Survey(client)).Institution["status"]!.ToString() == "incomplete");
            await Nexo("UPDATE fixture_users SET origen_ur='adscripcion',\"ID_UR\"='A4' WHERE email='persona@uacj.mx'");
            await shared.Sql("UPDATE unidades_responsables_poa SET id_ur_pertenece='A4' WHERE id_ur='A'"); Check((await Survey(client)).Institution["status"]!.ToString() == "incomplete");
        });
        Test("Privacidad: API sin consentimiento, CSRF, versión, idempotencia y aislamiento por empleado", async (app, client) =>
        {
            await Login(app, client); var view = await Survey(client); var input = Complete(view);
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", input), 403);
            await Status(client.PostAsJsonAsync(Base + "/api/aviso", new { generation = (await Survey(client)).Generation, version = "inventada" }), 409);
            using (var noCsrf = app.Client()) { await Login(app, noCsrf); noCsrf.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); await Status(noCsrf.PostAsJsonAsync(Base + "/api/aviso", new { generation = (await Survey(client)).Generation, version = view.Instrument["notice"]!["version"]!.ToString() }), 419); }
            await Accept(client); await Accept(client); Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_consents")) == 1 && Convert.ToInt64(await Data("SELECT count(*) FROM cd_surveys")) == 1);
            await Nexo("UPDATE fixture_users SET num_empleado='0009' WHERE email='persona@uacj.mx'"); Check(!(await Survey(client)).Accepted);
        });
        Test("Instrumento: 84 herramientas y rúbricas consecutivas, cero válido, no aplica y complementarias", async (app, client) =>
        {
            await Login(app, client); var view = await Accept(client); var input = Complete(view, 0); var instrument = new DigitalInstrument(view.Instrument.ToJsonString());
            Check(instrument.Blocks.SelectMany(b => b.Tools).Count() == 84);
            Check(DigitalInstrument.ChecklistLevel("000011111111") == 0 && DigitalInstrument.ChecklistLevel("111011001111") == 1 && DigitalInstrument.ChecklistLevel("111011101110") == 3);
            var eval = instrument.Evaluate(input.Profile, input.Answers); Check(eval.Pending.Length == 0 && eval.Progress == 100 && eval.Report.Global == 0 && eval.Total == 54);
            foreach (var key in input.Answers.Keys.ToArray()) input.Answers[key] = new("na"); eval = instrument.Evaluate(input.Profile, input.Answers);
            Check(eval.Report.Global is null && eval.Report.Blocks.All(b => b.Score is null) && eval.Report.Tools.Where(t => t.Value == -1).All(t => t.Gap == 0 && !eval.Report.Route.Any(r => r.Tool == t.Id)));
            input.Answers["m-word"] = new("level", 0); input.Answers["m-excel"] = new("level", 3);
            Check(instrument.Evaluate(input.Profile, input.Answers).Report.Global == 50);
            input.Answers.Remove("m-word"); Check(instrument.Evaluate(input.Profile, input.Answers).Pending.Contains("m-word"));
            var teacherOnly = instrument.Blocks.SelectMany(b => b.Tools).First(t => t.Doc);
            input.Answers[teacherOnly.Id] = new("pending"); Check(!instrument.Evaluate(input.Profile, input.Answers).Pending.Contains(teacherOnly.Id));
            input.Profile["puesto"] = "Personal docente"; Check(instrument.Evaluate(input.Profile, input.Answers).Pending.Contains(teacherOnly.Id));
            input.Answers[teacherOnly.Id] = new("unused"); Check(!instrument.Evaluate(input.Profile, input.Answers).Pending.Contains(teacherOnly.Id));
        });
        Test("Finalización incompleta rechaza todas las preguntas saltadas y adscripción incompleta", async (app, client) =>
        {
            await Login(app, client); var view = await Accept(client); await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new { generation = (await Survey(client)).Generation, version = view.Version }), 422);
            var input = Complete(view); input.Answers.Clear(); await Status(client.PutAsJsonAsync(Base + "/api/encuesta", input), 200);
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new { generation = (await Survey(client)).Generation, version = view.Version + 1 }), 422);
            await CompleteAndSave(client); await Nexo("UPDATE fixture_users SET origen_ur='multiple' WHERE email='persona@uacj.mx'");
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new { generation = (await Survey(client)).Generation, version = (await Survey(client)).Version }), 422);
        });
        Test("Guardado no admite identidad, porcentajes, estados ni niveles de rúbrica inventados", async (app, client) =>
        {
            await Login(app, client); var view = await Accept(client); var input = Complete(view); input.Profile["nombre"] = "Impostor";
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", input), 422); input.Profile.Remove("nombre");
            input.Answers["c-contrasenas"] = new("level", 3); await Status(client.PutAsJsonAsync(Base + "/api/encuesta", input), 422);
            var payload = JsonSerializer.SerializeToNode(Complete(view), DigitalInstrument.Json)!.AsObject(); payload["completedAt"] = "2026-01-01";
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", payload), 400);
        });
        Test("Dos pestañas/sesiones: versión evita sobrescrituras y borrador retoma en otro cliente", async (app, client) =>
        {
            await Login(app, client); var view = await Accept(client); using var second = app.Client(); await Login(app, second);
            var first = Complete(view, 1); first.Answers.Clear(); var other = Complete(view, 2); other.Answers.Clear(); other.Profile["sup"] = "No";
            var writes = await Task.WhenAll(client.PutAsJsonAsync(Base + "/api/encuesta", first), second.PutAsJsonAsync(Base + "/api/encuesta", other));
            Check(writes.Count(r => r.StatusCode == HttpStatusCode.OK) == 1 && writes.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1);
            Check((await Survey(second)).Version == view.Version + 1);
        });
        Test("Finalización transaccional idempotente; doble clic, reintentos y guardados atrasados inmutables", async (app, client) =>
        {
            await Login(app, client); var view = await CompleteAndSave(client); var old = Complete(view);
            var finals = await Task.WhenAll(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new { generation = (await Survey(client)).Generation, version = view.Version }), client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new { generation = (await Survey(client)).Generation, version = view.Version }));
            Check(finals.All(r => r.IsSuccessStatusCode)); var final = await Survey(client); Check(final.CompletedAt is not null && final.Version == view.Version + 1);
            var before = await Data("SELECT row_to_json(s)::text FROM cd_surveys s");
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", old), 409); await Status(client.PostAsJsonAsync(Base + "/api/aviso", new { generation = (await Survey(client)).Generation, version = final.Instrument["notice"]!["version"]!.ToString() }), 409);
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new { generation = (await Survey(client)).Generation, version = 0 }), 200);
            Check((string)before! == (string)(await Data("SELECT row_to_json(s)::text FROM cd_surveys s"))!);
            await Status(client.GetAsync(Base + "/api/informe/descarga"), 501);
            foreach (var sql in new[] { "UPDATE cd_surveys SET \"CompletedAt\"=NULL,\"ReportJson\"=NULL", "DELETE FROM cd_surveys" })
            { try { await Data(sql); throw new InvalidOperationException("Inmutabilidad SQL omitida"); } catch (PostgresException e) when (e.SqlState == "P0001") { } }
        });
        Test("Informe y contexto final permanecen tras traslado; sin acceso por ID ajeno", async (app, client) =>
        {
            await Login(app, client); var view = await CompleteAndSave(client, 2); await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new { generation = (await Survey(client)).Generation, version = view.Version }), 200);
            var before = await Survey(client); await Nexo("UPDATE fixture_users SET \"ID_UR\"='B4',nombre='Nombre cambiado' WHERE email='persona@uacj.mx'");
            var after = await Survey(client); Check(after.Institution.ToJsonString() == before.Institution.ToJsonString()); Check(after.Evaluation.Report.Global == before.Evaluation.Report.Global);
            await Role("administrador"); var own = await Json(await client.GetAsync(Base + "/api/informe?employee=0002")); Check(own["institution"]!["employee"]!.ToString() == "0001");
            Check(!(await client.GetAsync(Base + "/api/informe/0002")).IsSuccessStatusCode);
        });
        Test("Nueva versión técnica o aviso no permiten segundo intento ni reabren el definitivo", async (app, client) =>
        {
            await Login(app, client); var view = await CompleteAndSave(client); await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new { generation = (await Survey(client)).Generation, version = view.Version }), 200);
            var instrument = app.Services.GetRequiredService<DigitalInstrument>(); instrument.Resource["version"] = "synthetic-next"; instrument.Resource["notice"]!["version"] = "synthetic-notice-next";
            var done = await Survey(client); Check(done.CompletedAt is not null && done.Accepted && done.Instrument["version"]!.ToString() != "synthetic-next");
            await Status(client.PostAsJsonAsync(Base + "/api/aviso", new { generation = (await Survey(client)).Generation, version = "synthetic-notice-next" }), 409);
            try { await Seed("0001", "B", 3, false); throw new InvalidOperationException("Duplicado aceptado"); } catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "23505" }) { }
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_surveys")) == 1);
        });
        Test("Cambio de aviso en captura exige confirmación vigente conservando respuestas", async (app, client) =>
        {
            await Login(app, client); var view = await CompleteAndSave(client);
            app.Services.GetRequiredService<DigitalInstrument>().Resource["notice"]!["version"] = "synthetic-notice-next";
            Check(!(await Survey(client)).Accepted);
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", Complete(view)), 403);
            await Status(client.PostAsJsonAsync(Base + "/api/aviso", new { generation = (await Survey(client)).Generation, version = "synthetic-notice-next" }), 200);
            Check((await Survey(client)).Answers.Count == 48 && Convert.ToInt64(await Data("SELECT count(*) FROM cd_consents")) == 2);
        });
        Test("Agregados: personas únicas, nivel no excluye descendientes, promedio por persona y sin metas", async (app, client) =>
        {
            await Seed("p1", "A", 3, true); await Seed("p2", "A3", 0, true); await Seed("p3", "A4", 0, true); await Seed("p4", "A4", 0, false); await Seed("p5", "B", 1, true);
            await Role("administrador"); await Login(app, client);
            var data = await Json(await client.GetAsync(Base + "/api/resultados?unit=A&level=2"));
            Check(data["summary"]!["completed"]!.GetValue<int>() == 3 && data["summary"]!["inProgress"]!.GetValue<int>() == 0);
            Check(Math.Abs(data["summary"]!["maturity"]!.GetValue<double>() - 100d / 3) < .001);
            Check(data["units"]!.AsArray().Count == 1 && data["summary"]!["participation"] is null);
            Check(data["blocks"]![0]!["count"]!.GetValue<int>() == 3);
            var progress = await Json(await client.GetAsync(Base + "/api/avance?unit=A&level=2"));
            Check(progress["summary"]!["inProgress"]!.GetValue<int>() == 1);
            await Seed("p6", "A31", 0, false);
            var results = await Json(await client.GetAsync(Base + "/api/resultados?unit=A"));
            Check(results["summary"]!["participatingUnits"]!.GetValue<int>() == 3);
            var lower = await Json(await client.GetAsync(Base + "/api/resultados?unit=A&level=3"));
            Check(lower["summary"]!["completed"]!.GetValue<int>() == 2 && lower["summary"]!["maturity"]!.GetValue<double>() == 0);
        });
        Test("Metas directas: suma sin duplicar, ausencia/0 explícito, edición concurrente", async (app, client) =>
        {
            await Seed("p1", "A4", 1, true); await Role("administrador"); await Login(app, client);
            foreach (var (id, value) in new[] { ("A", 2), ("A3", 3), ("A4", 5) }) await Status(client.PutAsJsonAsync(Base + "/api/metas/" + id, new { generation = (await Survey(client)).Generation, value, version = 0 }), 200);
            var incomplete = await Json(await client.GetAsync(Base + "/api/avance?unit=A")); Check(incomplete["summary"]!["target"] is null);
            await Status(client.PutAsJsonAsync(Base + "/api/metas/A31", new { generation = (await Survey(client)).Generation, value = 0, version = 0 }), 200);
            var result = await Json(await client.GetAsync(Base + "/api/avance?unit=A")); Check(result["summary"]!["target"]!.GetValue<int>() == 10 && result["summary"]!["participation"]!.GetValue<double>() == 10);
            await Status(client.PutAsJsonAsync(Base + "/api/metas/A", new { generation = (await Survey(client)).Generation, value = 9, version = 0 }), 409);
        });
        Test("Alcance aplicado a opciones, totales, recomendaciones, puestos, herramientas y exportación", async (app, client) =>
        {
            await Seed("privadaA", "A4", 0, true); await Seed("privadaB", "B4", 3, true); await Role("responsable_ur"); await Login(app, client);
            var aggregate = await Json(await client.GetAsync(Base + "/api/resultados")); Check(aggregate["summary"]!["completed"]!.GetValue<int>() == 1 && aggregate["summary"]!["maturity"]!.GetValue<double>() == 0);
            var csv = await (await client.GetAsync(Base + "/api/resultados/descarga")).Content.ReadAsStringAsync();
            Check(!csv.Contains("privada") && !csv.Contains("@uacj.mx") && !csv.Contains("Área ajena") && csv.Contains("Recomendación"));
            Check(!aggregate.ToJsonString().Contains("privadaB") && !aggregate.ToJsonString().Contains("Área ajena"));
            var empty = await Json(await client.GetAsync(Base + "/api/resultados?position=" + Uri.EscapeDataString("Personal docente")));
            Check(empty["summary"]!["completed"]!.GetValue<int>() == 0 && empty["summary"]!["maturity"] is null && empty["recommendations"]!.AsArray().Count == 0);
            var filtered = await Json(await client.GetAsync(Base + "/api/resultados?position=" + Uri.EscapeDataString("Personal operativo")));
            Check(filtered["summary"]!["completed"]!.GetValue<int>() == 1 && filtered["summary"]!["participation"] is null);
            var export = await (await client.GetAsync(Base + "/api/resultados/descarga?position=" + Uri.EscapeDataString("Personal docente"))).Content.ReadAsStringAsync(); Check(!export.Contains("privada") && !export.Contains("Recomendación\""));
        });
        Test("Representación: identidad efectiva, sin permisos heredados/escrituras/anidación, salida y auditoría", async (app, client) =>
        {
            await Role("administrador"); await Login(app, client);
            await Status(client.PostAsJsonAsync(Base + "/api/representacion", new { email = "otra@uacj.mx", motivo = "Verificar navegación" }), 200);
            var page = await Json(await client.GetAsync(Base)); var props = page["props"]!;
            Check(props["digital"]!["user"]!.ToString() == "Otra persona sintética" && props["digital"]!["readOnly"]!.GetValue<bool>());
            Check(props["digital"]!["navigation"]!.AsArray().Count == 2 && !props.ToJsonString().Contains("token"));
            client.DefaultRequestHeaders.Add("X-TDV2-Context", props["contextoEdicion"]!.ToString());
            await Status(client.GetAsync(Base + "/configuracion"), 403); await Status(client.GetAsync("/inicio"), 409);
            var represented = await Survey(client); Check(represented.Institution["employee"]!.ToString() == "0002");
            await Status(client.PostAsJsonAsync(Base + "/api/aviso", new { generation = (await Survey(client)).Generation, version = represented.Instrument["notice"]!["version"]!.ToString() }), 403);
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", Complete(represented)), 403);
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new { generation = (await Survey(client)).Generation, version = 0 }), 403);
            await Status(client.PutAsJsonAsync(Base + "/api/metas/A", new { generation = (await Survey(client)).Generation, value = 3, version = 0 }), 403);
            await Status(client.PostAsJsonAsync(Base + "/api/representacion", new { email = "persona@uacj.mx", motivo = "Anidar" }), 403);
            await Nexo("INSERT INTO fixture_faults VALUES('representacion','P0001')"); await Status(client.GetAsync(Base + "/api/encuesta"), 422);
            await Status(client.DeleteAsync(Base + "/api/representacion"), 200);
            Check(Convert.ToInt64(await shared.Scalar("SELECT count(*) FROM activity_logs WHERE entity='representacion_competencias'")) == 2);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_surveys")) == 0);
        });
        Test("Guardado: reintento idéntico tras respuesta perdida es idempotente; documento distinto no sobrescribe", async (app, client) =>
        {
            await Login(app, client); var view = await Accept(client); var request = new DigitalSave(view.Version, Complete(view).Profile, new(), view.Generation);
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", request), 200); var committed = await Data("SELECT row_to_json(s)::text FROM cd_surveys s");
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", request), 200); Check((string)committed! == (string)(await Data("SELECT row_to_json(s)::text FROM cd_surveys s"))!);
            request.Profile["sup"] = "No"; await Status(client.PutAsJsonAsync(Base + "/api/encuesta", request), 409);
        });
        Test("Secuencia: seis campos del perfil obligatorios; URL y API no saltan secciones", async (app, client) =>
        {
            await Login(app, client); var view = await Accept(client);
            foreach (var field in view.Instrument["profile"]!.AsArray())
            {
                var input = Complete(view); input.Answers.Clear(); input.Profile.Remove(field!["id"]!.ToString());
                await Status(client.PutAsJsonAsync(Base + "/api/encuesta", input), 200); view = await Survey(client);
                Check(view.Navigation.Resume == "perfil" && view.Navigation.Steps.Count(s => s.Accessible) == 1);
                var jump = new DigitalSave(view.Version, view.Profile, new() { ["m-word"] = new("level", 0) }, view.Generation);
                await Status(client.PutAsJsonAsync(Base + "/api/encuesta", jump), 422);
                var api = await Json(await client.GetAsync(Base + "/api/encuesta?paso=ia")); Check(api["navigation"]!["current"]!.ToString() == "perfil");
                var page = await Json(await client.GetAsync(Base + "/encuesta?paso=sii")); Check(page["props"]!["digital"]!["content"]!["navigation"]!["current"]!.ToString() == "perfil");
            }
            // No se desbloquea un bloque llenando el perfil y sus respuestas en una misma solicitud.
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", Complete(view)), 422);
        });
        Test("Secuencia: cada bloque exige todas sus respuestas; cero, NA y complemento seleccionado", async (app, client) =>
        {
            await Login(app, client); var view = await Accept(client); var complete = Complete(view, 0);
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, complete.Profile, new(), view.Generation)), 200); view = await Survey(client);
            var blocks = new DigitalInstrument(view.Instrument.ToJsonString()).Blocks;
            for (var i = 0; i < blocks.Length; i++)
            {
                var block = blocks[i]; Check(view.Navigation.Resume == block.Id);
                var main = block.Tools.Where(t => !t.Doc && !t.Opt).ToArray();
                foreach (var tool in main.Skip(1)) view.Answers[tool.Id] = complete.Answers[tool.Id];
                await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, view.Profile, view.Answers, view.Generation)), 200); view = await Survey(client);
                Check(!view.Navigation.Steps[i + 1].Complete && view.Navigation.Steps[i + 1].Pending.Contains(main[0].Id));
                if (i < 4)
                {
                    var next = blocks[i + 1].Tools.First(t => !t.Doc && !t.Opt);
                    var forbidden = new Dictionary<string, DigitalAnswer>(view.Answers) { [next.Id] = new("na") };
                    await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, view.Profile, forbidden, view.Generation)), 422);
                }
                view.Answers[main[0].Id] = new("na");
                var optional = block.Tools.FirstOrDefault(t => t.Opt && !t.Doc);
                if (optional is not null) view.Answers[optional.Id] = new("pending");
                await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, view.Profile, view.Answers, view.Generation)), 200); view = await Survey(client);
                if (optional is not null) { Check(view.Navigation.Steps[i + 1].Pending.SequenceEqual([optional.Id])); view.Answers[optional.Id] = new("na");
                    await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, view.Profile, view.Answers, view.Generation)), 200); view = await Survey(client); }
                Check(view.Navigation.Steps[i + 1].Complete);
            }
            Check(view.Navigation.Steps.All(s => s.Complete && s.Accessible));
        });
        Test("Secuencia: retroceso conserva respuestas posteriores y vuelve a bloquear tras editar perfil", async (app, client) =>
        {
            await Login(app, client); var view = await CompleteAndSave(client); var answers = JsonSerializer.Serialize(view.Answers);
            view.Profile.Remove("antig");
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, view.Profile, view.Answers, view.Generation)), 200); view = await Survey(client);
            Check(view.Navigation.Resume == "perfil" && view.Navigation.Steps.Count(s => s.Accessible) == 1 && JsonSerializer.Serialize(view.Answers) == answers);
            using var second = app.Client(); await Login(app, second); Check((await Survey(second)).Navigation.Resume == "perfil");
            var changed = new Dictionary<string, DigitalAnswer>(view.Answers) { ["m-word"] = new("na") };
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, view.Profile, changed, view.Generation)), 422);
            // Guardar el mismo documento retenido sí es válido, aunque contenga bloques ahora inaccesibles.
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, view.Profile, view.Answers, view.Generation)), 200); view = await Survey(client);
            view.Profile["antig"] = Complete(view).Profile["antig"]!.DeepClone();
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, view.Profile, view.Answers, view.Generation)), 200); view = await Survey(client);
            Check(view.Navigation.Steps.All(s => s.Accessible) && JsonSerializer.Serialize(view.Answers) == answers);
            view.Answers["m-word"] = new("pending"); await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, view.Profile, view.Answers, view.Generation)), 200);
            view = await Survey(client); Check(view.Navigation.Resume == "m365" && !view.Navigation.Steps[2].Accessible);
        });
        Test("Secuencia: adscripción incompleta bloquea perfil; nivel 2 sin nivel 3 es válido", async (app, client) =>
        {
            await Login(app, client); var view = await Accept(client); await Nexo("UPDATE fixture_users SET origen_ur='multiple' WHERE email='persona@uacj.mx'");
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, Complete(view).Profile, new(), view.Generation)), 200); view = await Survey(client);
            Check(view.Navigation.Steps[0].Pending.SequenceEqual(["perfil:institucion"]));
            await Nexo("UPDATE fixture_users SET origen_ur='adscripcion',\"ID_UR\"='A' WHERE email='persona@uacj.mx'"); view = await Survey(client);
            Check(view.Institution["level3"] is null && view.Navigation.Steps[1].Accessible);
        });
        Test("Foto: participante exclusivo usa cuenta compartida; contexto de otra aplicación se rechaza", async (app, client) =>
        {
            await shared.Sql("DELETE FROM fixture_roles WHERE email='persona@uacj.mx'"); await Login(app, client); app.Microsoft.PhotoAvailable = true;
            var page = await Json(await client.GetAsync(Base + "/encuesta")); var props = page["props"]!;
            Check(props["auth"]!["user"]!["name"]!.ToString() == "Persona sintética" && props["digital"]!["user"]!.ToString() == "Persona sintética");
            await Status(client.GetAsync("/user/photo"), 403);
            client.DefaultRequestHeaders.Add("X-TDV2-Application", "competencias-digitales"); client.DefaultRequestHeaders.Add("X-TDV2-Photo-Context", props["photoContext"]!.ToString());
            var photo = await Json(await client.GetAsync("/user/photo")); Check(photo["photo"]?.ToString().StartsWith("data:image/png;base64,") == true && photo["contexto"]!.ToString() == props["photoContext"]!.ToString());
            client.DefaultRequestHeaders.Remove("X-TDV2-Photo-Context"); client.DefaultRequestHeaders.Add("X-TDV2-Photo-Context", "old-other-app"); await Status(client.GetAsync("/user/photo"), 409);
        });
        Test("Foto: representación usa identidad objetivo y nunca fotografía del administrador", async (app, client) =>
        {
            await Role("administrador"); await Login(app, client); app.Microsoft.PhotoAvailable = true;
            client.DefaultRequestHeaders.Add("X-TDV2-Application", "competencias-digitales"); Check((await Json(await client.GetAsync("/user/photo")))["photo"] is not null);
            await Status(client.PostAsJsonAsync(Base + "/api/representacion", new { email = "otra@uacj.mx", motivo = "Comprobar identidad representada" }), 200);
            var page = await Json(await client.GetAsync(Base + "/encuesta")); Check(page["props"]!["representacion"]!["nombre"]!.ToString() == "Otra persona sintética");
            Check(page["props"]!["auth"]!["user"]!["name"]!.ToString() == "Persona sintética");
            Check((await Json(await client.GetAsync("/user/photo")))["photo"] is null);
        });
        AddResetTests(cases);
        var results = new List<object>(); var failures = 0;
        foreach (var (name, work) in Environment.GetCommandLineArgs().Contains("--browser-only") ? [] : cases)
        {
            try { await Reset(); await using var app = new NativeApplication(shared) { DigitalConfiguration = Configuration }; using var client = app.Client(); await work(app, client); results.Add(new { name, passed = true }); Console.WriteLine("PASS Competencias/" + name); }
            catch (Exception e) { failures++; results.Add(new { name, passed = false, type = e.GetType().Name, detail = e is InvalidOperationException ? e.Message : "Fallo técnico; sin detalles sensibles" }); Console.WriteLine("FAIL Competencias/" + name + " [" + e.GetType().Name + "] " + (e is InvalidOperationException ? e.Message : "")); }
        }
        if (failures == 0 && Environment.GetEnvironmentVariable("TDV2_TEST_SKIP_BROWSER") != "true")
        {
            try { await Reset(); await Role("administrador"); await Browser(); results.Add(new { name = "Navegador real y PostgreSQL aislado", passed = true }); }
            catch (Exception e) { failures++; results.Add(new { name = "Navegador real y PostgreSQL aislado", passed = false, type = e.GetType().Name }); Console.WriteLine("FAIL Competencias/Browser [" + e.GetType().Name + "]"); }
        }
        await File.WriteAllTextAsync(Path.Combine(shared.Artifacts, "competencias-verification.json"), JsonSerializer.Serialize(new { syntheticSources = true, isolatedPostgreSql = true, failures, results }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Competencias: {results.Count - failures} correctas; {failures} fallidas."); return failures == 0 ? 0 : 1;
    }
    private async Task Browser()
    {
        await Seed("synthetic-browser-a", "A4", 1, true); await Seed("synthetic-browser-b", "B4", 3, true);
        using var cert = NativeApplication.Certificate(true);
        await using var app = new NativeApplication(shared) { DigitalConfiguration = Configuration, Browser = true };
        app.AdditionalControls = new DigitalBrowserControls(this, app.ControlToken);
        app.UseKestrel(o => o.Listen(IPAddress.Loopback, 0)); using var client = app.CreateClient(new() { AllowAutoRedirect = false });
        var upstream = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single().TrimEnd('/');
        await using var proxy = await SyntheticTlsProxy.Start(shared.Workspace, upstream, app.ControlToken, cert);
        app.Services.GetRequiredService<IOptions<MicrosoftSettings>>().Value.PublicOrigin = proxy.Origin;
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("TDV2_TEST_NODE") ?? "node") { WorkingDirectory = Path.Combine(shared.Workspace, "ClientApp"), UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        start.ArgumentList.Add("tests/browser/competencias-native.mjs"); start.Environment["TDV2_BROWSER_ORIGIN"] = proxy.Origin; start.Environment["TDV2_BROWSER_KEY"] = app.ControlToken; start.Environment["TDV2_BROWSER_ARTIFACTS"] = shared.Artifacts;
        start.Environment.Remove("TDV2_TEST_CONNECTION"); start.Environment.Remove("PGPASSWORD");
        using var process = Process.Start(start)!;
        async Task Forward(StreamReader reader) { while (await reader.ReadLineAsync() is { } line) Console.WriteLine(line); }
        var outputs = Task.WhenAll(Forward(process.StandardOutput), Forward(process.StandardError)); await process.WaitForExitAsync(); await outputs; Check(process.ExitCode == 0, "Falló navegador de Competencias");
    }
    private sealed class DigitalBrowserControls(DigitalTests suite, string token) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (http, rest) =>
            {
                if (!http.Request.Path.StartsWithSegments("/__digitalfixture", out var action)) { await rest(); return; }
                if (!IPAddress.IsLoopback(http.Connection.RemoteIpAddress ?? IPAddress.None) || http.Request.Headers["X-Fixture-Key"] != token) { http.Response.StatusCode = 403; return; }
                switch (action.Value)
                {
                    case "/admin": await suite.Role("administrador"); await suite.Nexo("UPDATE fixture_users SET num_empleado='0001' WHERE email='persona@uacj.mx'"); break;
                    case "/supervisor": await suite.Role("responsable_ur_supervisor"); break;
                    case "/n2": await suite.Role("responsable_ur"); await suite.Nexo("UPDATE fixture_users SET num_empleado='0001' WHERE email='persona@uacj.mx'"); break;
                    case "/n3": await suite.Role("responsable_ur"); await suite.Nexo("UPDATE fixture_users SET num_empleado='0002' WHERE email='persona@uacj.mx'"); break;
                    case "/participant": await suite.Role("participante"); await suite.Nexo("UPDATE fixture_users SET num_empleado='0001' WHERE email='persona@uacj.mx'"); break;
                    case "/exclusive": await suite.Role("participante"); await suite.Exclusive(); break;
                    default: http.Response.StatusCode = 404; return;
                }
                await http.Response.WriteAsJsonAsync(new { ok = true });
            }); next(app);
        };
    }
}
