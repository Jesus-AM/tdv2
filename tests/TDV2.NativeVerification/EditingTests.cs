using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.WebUtilities;
using Tdv2.Domain;
using Tdv2.Services;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    private static EditRequest Edit(Guid tab, params BlockRequest[] blocks) => new(tab, Guid.NewGuid(), blocks);
    private static async Task<BlockRequest> Lease(HttpClient client, Guid tab, string key = "contexto", int version = 0, string unit = "A")
    {
        var response = await client.PostAsJsonAsync($"/formatos/{unit}/reservas", Edit(tab, new BlockRequest(key, version)));
        Check(response.IsSuccessStatusCode, "Reserva: HTTP " + (int)response.StatusCode);
        var block = (await Json(response))["bloques"]![0]!;
        return new(key, version, Guid.Parse(block["reserva"]!["id"]!.ToString()));
    }
    private static Task<HttpResponseMessage> Patch(HttpClient client, EditRequest input, string unit = "A") => client.PatchAsJsonAsync($"/formatos/{unit}/bloques", input);
    private static JsonObject Header(string responsible) => new() { ["fecha"] = "2026-10-05", ["area"] = "Área sintética A", ["responsable"] = responsible };
    private static async Task<JsonObject> Live(HttpClient client, Guid tab, string unit = "A") => await Json(await client.GetAsync($"/formatos/{unit}/estado?tab={tab}"));
    private static JsonObject Complete(NativeApplication app) => Complete(app.Services.GetRequiredService<FormSchema>());
    internal static JsonObject Complete(FormSchema schema)
    {
        var content = schema.Blank(new("A", "100", "Área sintética A", 2, null, 2026));
        content["encabezado"] = Header("Responsable sintético");
        foreach (var section in new[] { "identificacion", "sistemas", "datos", "acuerdos" })
            foreach (var key in content[section]![0]!.AsObject().Select(p => p.Key).ToArray())
                if (key != "id") content[section]![0]![key] = "Respuesta sintética";
        content["identificacion"]![0]!["codigo"] = "PO-01"; content["identificacion"]![0]!["prioridad"] = "5"; content["identificacion"]![0]!["validacion"] = "V";
        content["sistemas"]![0]!["proceso"] = "PO-01"; content["sistemas"]![0]!["estado"] = "Funciona";
        content["datos"]![0]!["proceso"] = "PO-01"; content["datos"]![0]!["origen"] = "Se origina en este proceso";
        content["acuerdos"]![0]!["fecha"] = "2026-10-10";
        foreach (var row in content["preguntas"]!.AsArray()) row!["respuesta"] = row["opciones"] is JsonArray choices ? choices[0]!.DeepClone() : JsonValue.Create("Respuesta sintética");
        content["evaluaciones"]!["PO-01"] = new JsonArray(schema.Definition()["criterios"]!.AsArray().Select(c => (JsonNode)new JsonObject { ["criterio"] = c!.DeepClone(), ["valor"] = "5", ["obs"] = "" }).ToArray());
        return content;
    }
    private static void RegisterEditingCases(NativeDatabase database, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Edición/SignalR permite el origen público tras proxy y rechaza otro origen", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            var csrf = client.DefaultRequestHeaders.GetValues("X-CSRF-TOKEN").Single();
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            Check((int)(await client.SendAsync(new HttpRequestMessage(new HttpMethod("CONNECT"), "/form-events"))).StatusCode == 419);
            Check((int)(await client.PostAsync("/form-events/negotiate?negotiateVersion=1", null)).StatusCode == 419);
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf);
            app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Tdv2.Integrations.Microsoft.MicrosoftSettings>>().Value.PublicOrigin = "https://tdv2.synthetic.example.test";
            client.DefaultRequestHeaders.Add("Origin", "https://tdv2.synthetic.example.test");
            Check((await client.PostAsync("/form-events/negotiate?negotiateVersion=1", null)).IsSuccessStatusCode);
            client.DefaultRequestHeaders.Remove("Origin"); client.DefaultRequestHeaders.Add("Origin", "https://foreign.example.test");
            Check((await client.PostAsync("/form-events/negotiate?negotiateVersion=1", null)).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Edición/rol supervisor funciona solo, rama editable y consulta ajena sin módulos administrativos", async (app, client) =>
        {
            await database.Sql("UPDATE fixture_roles SET rol_clave='responsable_ur_supervisor'"); await Login(app, client); await Csrf(client);
            foreach (var ur in new[] { "A", "A3" })
            { var view = await Live(client, Guid.NewGuid(), ur); Check(view["editable"]!.GetValue<bool>() && view["puedeEnviar"]!.GetValue<bool>()); await Lease(client, Guid.NewGuid(), unit: ur); }
            var foreign = await Live(client, Guid.NewGuid(), "B"); Check(!foreign["editable"]!.GetValue<bool>() && !foreign["puedeEnviar"]!.GetValue<bool>());
            Check((await client.PostAsJsonAsync("/formatos/B/reservas", Edit(Guid.NewGuid(), new BlockRequest("contexto", 0)))).StatusCode == HttpStatusCode.Forbidden);
            foreach (var path in new[] { "/configuracion", "/configuracion/sincronizaciones", "/configuracion/pruebas-acceso", "/actuar-como-usuario/personas?q=persona" })
                Check((await client.GetAsync(path)).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Edición/dos sesiones y dos pestañas del mismo usuario compiten por un único bloque", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); using var other = app.Client(); await Login(app, other); await Csrf(other);
            var requests = new[] { client, client, other }.Select(c => c.PostAsJsonAsync("/formatos/A/reservas", Edit(Guid.NewGuid(), new BlockRequest("contexto", 0))));
            var responses = await Task.WhenAll(requests);
            Check(responses.Count(r => r.IsSuccessStatusCode) == 1 && responses.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 2);
        });
        Test("Edición/bloques distintos guardan concurrentemente sin sobrescribir JSON", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); using var other = app.Client(); await Login(app, other); await Csrf(other);
            var a = Guid.NewGuid(); var b = Guid.NewGuid(); var first = await Lease(client, a); var second = await Lease(other, b, "preguntas:0");
            var question = (await Live(client, a))["contenido"]!["preguntas"]![0]!.DeepClone(); question["respuesta"] = question["opciones"] is JsonArray opts ? opts[0]!.DeepClone() : JsonValue.Create("Respuesta de otra sesión");
            var results = await Task.WhenAll(Patch(client, Edit(a, first with { Value = Header("Primer bloque") })), Patch(other, Edit(b, second with { Value = question })));
            Check(results.All(r => r.IsSuccessStatusCode)); var live = await Live(client, a);
            Check(DateTimeOffset.Parse(live["actualizadoEn"]!.ToString(), System.Globalization.CultureInfo.InvariantCulture).Offset == TimeSpan.Zero);
            Check(live["contenido"]!["encabezado"]!["responsable"]!.ToString() == "Primer bloque" && JsonNode.DeepEquals(live["contenido"]!["preguntas"]![0], question));
        });
        Test("Edición/vencimiento, renovación, reasignación y guardado atrasado cercado por testigo", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var first = await Lease(client, tab);
            Check((await client.PostAsJsonAsync("/formatos/A/reservas/actividad", Edit(tab, first))).IsSuccessStatusCode);
            await database.Sql("UPDATE formato_bloques SET vence_en=clock_timestamp()-interval '1 second'");
            Check((await client.PostAsJsonAsync("/formatos/A/reservas/actividad", Edit(tab, first))).StatusCode == HttpStatusCode.Conflict);
            var next = Guid.NewGuid(); var reassigned = await Lease(client, next); Check(reassigned.LeaseId != first.LeaseId);
            Check((await Patch(client, Edit(tab, first with { Value = Header("Atrasado") }))).StatusCode == HttpStatusCode.Conflict);
            Check((await Patch(client, Edit(next, reassigned with { Value = Header("Vigente") }))).IsSuccessStatusCode);
            Check((await Live(client, next))["contenido"]!["encabezado"]!["responsable"]!.ToString() == "Vigente");
        });
        Test("Edición/lectura pasiva no renueva ni permite liberar una reserva ajena", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var block = await Lease(client, tab);
            var before = await database.Scalar("SELECT vence_en FROM formato_bloques"); await Live(client, tab);
            await client.PostAsJsonAsync("/formatos/A/reservas/liberar", Edit(Guid.NewGuid(), block));
            Check(Equals(before, await database.Scalar("SELECT vence_en FROM formato_bloques")));
            var foreign = (await Live(client, Guid.NewGuid()))["bloques"]![0]!["reserva"]!;
            Check(foreign["id"] is null && !foreign["propia"]!.GetValue<bool>() && foreign["titular"]!.ToString() == "Persona sintética");
        });
        Test("Edición/reintento idéntico confirma una vez; operación reutilizada y versión vieja rechazadas", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var block = await Lease(client, tab);
            var request = Edit(tab, block with { Value = Header("Primera") }) with { Release = true };
            var responses = await Task.WhenAll(Patch(client, request), Patch(client, request)); Check(responses.All(r => r.IsSuccessStatusCode));
            Check(JsonNode.DeepEquals(await Json(responses[0]), await Json(responses[1])));
            Check(Convert.ToInt32(await database.Scalar("SELECT count(*) FROM formato_operaciones")) == 1);
            Check((await Patch(client, request with { Blocks = [block with { Value = Header("Distinta") }] })).StatusCode == HttpStatusCode.Conflict);
            Check((await Patch(client, Edit(tab, block with { Value = Header("Vieja") }))).StatusCode == HttpStatusCode.Conflict);
        });
        Test("Edición/revocación y contexto obsoleto rechazan reserva y escritura", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var block = await Lease(client, tab);
            client.DefaultRequestHeaders.Add("X-TDV2-Context", "viejo"); Check((await Patch(client, Edit(tab, block with { Value = Header("No") }))).StatusCode == HttpStatusCode.Conflict);
            client.DefaultRequestHeaders.Remove("X-TDV2-Context"); await database.Sql("DELETE FROM fixture_roles");
            Check((await Patch(client, Edit(tab, block with { Value = Header("No") }))).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Edición/operación y auditoría son atómicas y el fallo no pierde propuesta ni versión", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var block = await Lease(client, tab);
            await database.Sql(AuditFailureTrigger);
            try { Check((await Patch(client, Edit(tab, block with { Value = Header("No confirmado") }))).StatusCode == HttpStatusCode.ServiceUnavailable); }
            finally { await database.Sql(RemoveAuditFailure); }
            Check(Convert.ToInt32(await database.Scalar("SELECT version FROM formato_bloques")) == 0 && Convert.ToInt32(await database.Scalar("SELECT count(*) FROM formato_operaciones")) == 0);
        });
        Test("Edición/envío incompleto y reservas ajenas se rechazan; doble envío es idempotente", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0))).StatusCode == HttpStatusCode.UnprocessableEntity);
            Check((await Save(client, 0, Complete(app))).IsSuccessStatusCode);
            var lease = await Lease(client, Guid.NewGuid(), "contexto", 1);
            var submission = new SubmitRequest(tab, Guid.NewGuid(), 1);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", submission)).StatusCode == HttpStatusCode.Conflict);
            await database.Sql("UPDATE formato_bloques SET vence_en=clock_timestamp()-interval '1 second'");
            var responses = await Task.WhenAll(client.PostAsJsonAsync("/formatos/A/enviar", submission), client.PostAsJsonAsync("/formatos/A/enviar", submission));
            Check(responses.All(r => r.IsSuccessStatusCode)); Check(JsonNode.DeepEquals(await Json(responses[0]), await Json(responses[1])));
            Check(Convert.ToInt32(await database.Scalar("SELECT count(*) FROM activity_logs WHERE action='enviar'")) == 1);
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(tab, new BlockRequest("contexto", 1)))).StatusCode == HttpStatusCode.Conflict);
            Check((await Patch(client, Edit(tab, lease with { Value = Header("Después") }))).StatusCode == HttpStatusCode.Conflict);
            var live = await Live(client, tab); Check(!live["editable"]!.GetValue<bool>() && live["enviadoEn"] is not null);
        });
        Test("Edición/envío simultáneo con adquisición de reserva conserva una sola decisión", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); Check((await Save(client, 0, Complete(app))).IsSuccessStatusCode);
            var results = await Task.WhenAll(client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(Guid.NewGuid(), Guid.NewGuid(), 1)),
                client.PostAsJsonAsync("/formatos/A/reservas", Edit(Guid.NewGuid(), new BlockRequest("contexto", 1))));
            Check(results.Count(r => r.IsSuccessStatusCode) == 1 && results.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1);
        });
        Test("Edición/envío y guardado simultáneos de la misma sesión no cruzan el cierre", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); Check((await Save(client, 0, Complete(app))).IsSuccessStatusCode);
            var tab = Guid.NewGuid(); var block = await Lease(client, tab, "contexto", 1);
            var responses = await Task.WhenAll(Patch(client, Edit(tab, block with { Value = Header("Último cambio") })),
                client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1)));
            Check(responses.Count(r => r.IsSuccessStatusCode) == 1 && responses.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1);
        });
        Test("Edición/reserva que vence durante el commit revierte respuestas, versión y auditoría", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var block = await Lease(client, tab);
            await database.Sql("""
                CREATE FUNCTION fixture_delay_edit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN PERFORM pg_sleep(0.4); RETURN NEW; END $$;
                CREATE TRIGGER delay_edit BEFORE UPDATE ON formatos_ur FOR EACH ROW EXECUTE FUNCTION fixture_delay_edit();
                UPDATE formato_bloques SET vence_en=clock_timestamp()+interval '0.2 seconds';
                """);
            try { Check((await Patch(client, Edit(tab, block with { Value = Header("No confirmado") }))).StatusCode == HttpStatusCode.Conflict); }
            finally { await database.Sql("DROP TRIGGER delay_edit ON formatos_ur; DROP FUNCTION fixture_delay_edit()"); }
            Check(Convert.ToInt32(await database.Scalar("SELECT version FROM formatos_ur")) == 0);
            Check(Convert.ToInt32(await database.Scalar("SELECT count(*) FROM formato_operaciones")) == 0);
        });
        Test("Edición/otra persona colaboradora edita dentro de su formato pero no puede enviarlo", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); Check((await AddCollaborator(client)).IsSuccessStatusCode);
            using var helper = app.Client(); app.Microsoft.Email = app.Microsoft.Mail = "colaboradora@uacj.mx"; app.Microsoft.Id = Guid.NewGuid().ToString();
            await Login(app, helper); await Csrf(helper); var tab = Guid.NewGuid(); var block = await Lease(helper, tab, unit: "A3");
            Check((await Patch(helper, Edit(tab, block with { Value = Header("Colaboradora") }), "A3")).IsSuccessStatusCode);
            Check((await helper.PostAsJsonAsync("/formatos/A3/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).StatusCode == HttpStatusCode.Forbidden);
            var foreign = await helper.PostAsJsonAsync("/formatos/B/reservas", Edit(tab, new BlockRequest("contexto", 0)));
            Check(foreign.StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Edición/enviado conserva ILDA, unidad y respuestas aunque cambien los catálogos", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var complete = Complete(app); complete["identificacion"]![0]!["id"] = "ilda:17";
            Check((await Save(client, 0, complete)).IsSuccessStatusCode);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(Guid.NewGuid(), Guid.NewGuid(), 1))).IsSuccessStatusCode);
            var before = (await Form(client))["contenido"]!.ToJsonString();
            await database.Sql("UPDATE unidades_responsables_poa SET desc_ur='Descripción nueva' WHERE id_ur='A'; INSERT INTO sincronizacion_catalogos(fuente,registros,completada_en) VALUES('ilda',1,timezone('UTC',now())); INSERT INTO ilda_informacion_area(id_origen,ur2,informacion_generada,datos,presente,sincronizado_en) VALUES('999','100','Otro trámite','{}',true,timezone('UTC',now()))");
            var page = await Form(client); Check(page["contenido"]!.ToJsonString() == before && page["unidad"]!["desc_ur"]!.ToString() == "Área sintética A");
            var rejected = false; try { await database.Sql("UPDATE formatos_ur SET contenido='{}'"); } catch (Npgsql.PostgresException e) when (e.SqlState == "55000") { rejected = true; }
            Check(rejected);
        });
        Test("Edición/prioridad no se selecciona por defecto; 5 repetible y valores históricos conservados", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var content = (await Form(client))["contenido"]!.AsObject();
            Check(content["identificacion"]![0]!["prioridad"]!.ToString() == "");
            var row = content["identificacion"]![0]!.DeepClone(); row["prioridad"] = "5";
            var tab = Guid.NewGuid(); var block = await Lease(client, tab, "identificacion:inicial");
            Check((await Patch(client, Edit(tab, block with { Value = row }))).IsSuccessStatusCode);
            row["prioridad"] = "6"; Check((await Patch(client, Edit(tab, block with { Version = 1, Value = row }))).StatusCode == HttpStatusCode.UnprocessableEntity);
            await database.Sql("UPDATE formatos_ur SET contenido=jsonb_set(contenido::jsonb,'{identificacion,0,prioridad}','\"17\"')::json");
            var historical = (await Live(client, tab))["contenido"]!["identificacion"]![0]!.DeepClone(); historical["tramite"] = "Conservar orden anterior";
            Check((await Patch(client, Edit(tab, block with { Version = 1, Value = historical }))).IsSuccessStatusCode);
            Check((await Live(client, tab))["contenido"]!["identificacion"]![0]!["prioridad"]!.ToString() == "17");
            historical["prioridad"] = "4";
            Check((await Patch(client, Edit(tab, block with { Version = 2, Value = historical }))).IsSuccessStatusCode);
            var next = historical.DeepClone(); next["id"] = "otro";
            var second = await Lease(client, tab, "identificacion:otro");
            Check((await Patch(client, Edit(tab, second with { Value = next }))).IsSuccessStatusCode);
        });
        Test("Edición/OIDC rechaza firma y nonce ajenos, no fuerza selector habitual y usa logout_hint opaco", async (app, client) =>
        {
            var response = await client.GetAsync("/connect"); var query = QueryHelpers.ParseQuery(response.Headers.Location!.Query);
            Check(!query.ContainsKey("prompt") && query["nonce"].ToString().Length == 64);
            app.Microsoft.InvalidNonce = true; Check(DeniedLogin(await Callback(client, query["state"]!)));
            app.Microsoft.InvalidNonce = false; app.Microsoft.InvalidSignature = true; Check(DeniedLogin(await Callback(client, await Start(app, client))));
            app.Microsoft.InvalidSignature = false; await Login(app, client); await Csrf(client);
            Check((await client.PostAsJsonAsync("/logout", new { })).IsSuccessStatusCode);
            var logout = QueryHelpers.ParseQuery((await client.GetAsync("/session/microsoft-logout")).Headers.Location!.Query);
            Check(logout["logout_hint"] == app.Microsoft.LoginHint && !logout["logout_hint"].ToString().Contains("@"));
            var again = QueryHelpers.ParseQuery((await client.GetAsync("/connect")).Headers.Location!.Query); Check(again["login_hint"] == app.Microsoft.LoginHint);
            var other = QueryHelpers.ParseQuery((await client.GetAsync("/connect?account=other")).Headers.Location!.Query); Check(other["prompt"] == "select_account" && !other.ContainsKey("login_hint"));
            // Sin claim opcional, el correo real orienta el login pero jamás se publica como logout_hint.
            app.Microsoft.LoginHint = null;
            using var withoutHint = app.Client(); await Login(app, withoutHint); await Csrf(withoutHint);
            await withoutHint.PostAsync("/logout", null);
            var plainLogout = QueryHelpers.ParseQuery((await withoutHint.GetAsync("/session/microsoft-logout")).Headers.Location!.Query);
            Check(!plainLogout.ContainsKey("logout_hint"));
            var knownAccount = QueryHelpers.ParseQuery((await withoutHint.GetAsync("/connect")).Headers.Location!.Query);
            Check(knownAccount["login_hint"] == app.Microsoft.Email && !knownAccount.ContainsKey("prompt"));
            using var switcher = app.Client(); await Login(app, switcher); await Csrf(switcher);
            var switched = await switcher.PostAsync("/session/use-another-account", null);
            Check(switched.IsSuccessStatusCode && (await Json(switched))["redirect"]!.ToString() == "/connect?account=other");
            Check((await switcher.GetAsync("/inicio")).StatusCode == HttpStatusCode.Unauthorized);
        });
    }
}
