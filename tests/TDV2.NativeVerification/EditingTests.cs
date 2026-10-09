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
    private static Task FinalStage(NativeDatabase database) => database.Sql("""
        INSERT INTO fixture_roles SELECT 10,'persona@uacj.mx',34,'administrador','Administrador'
        WHERE NOT EXISTS(SELECT 1 FROM fixture_roles WHERE email='persona@uacj.mx' AND rol_clave='administrador')
        """);
    // Existing historical responses are seeded only in the disposable database; PUT capture is retired.
    private static async Task SeedEditing(NativeDatabase database, JsonObject content)
    {
        await FinalStage(database);
        await database.Sql("""
            INSERT INTO formatos_ur(id_ur,ejercicio,contenido,version,porcentaje,actualizado_por,created_at,updated_at)
            VALUES('A',2026,$1::json,1,$2,'persona@uacj.mx',now(),now())
            ON CONFLICT(id_ur) DO UPDATE SET contenido=EXCLUDED.contenido,version=1,porcentaje=EXCLUDED.porcentaje
            """, content.ToJsonString(), FormSchema.Progress(content));
        await database.Sql("DELETE FROM formato_bloques WHERE id_ur='A'");
        foreach (var key in FormBlocks.Split(content).Keys)
            await database.Sql("INSERT INTO formato_bloques(id_ur,bloque,version,revision_contexto,participante,color) VALUES('A',$1,1,0,'',0)", key);
    }
    internal static JsonObject Complete(FormSchema schema)
    {
        var content = schema.Blank(new("A", "100", "Área sintética A", 2, null, 2026));
        // Los datos de sesión retirados permanecen vacíos; el formato completo no los necesita.
        foreach (var section in new[] { "identificacion", "sistemas", "datos", "acuerdos" })
            foreach (var key in content[section]![0]!.AsObject().Select(p => p.Key).ToArray())
                if (key != "id") content[section]![0]![key] = "Respuesta sintética";
        content["identificacion"]![0]!["codigo"] = "PO-01"; content["identificacion"]![0]!["prioridad"] = "5"; content["identificacion"]![0]!["validacion"] = "V";
        content["identificacion"]![0]!["usuario"] = new JsonArray("Docentes");
        content["sistemas"]![0]!["proceso"] = "PO-01"; content["sistemas"]![0]!["estado"] = "Funciona";
        content["datos"]![0]!["proceso"] = "PO-01"; content["datos"]![0]!["origen"] = "Se origina en este proceso";
        content["acuerdos"]![0]!["fecha"] = "2026-10-10";
        foreach (var row in content["preguntas"]!.AsArray()) row!["respuesta"] = row["opciones"] is JsonArray choices ? choices[0]!.DeepClone() : JsonValue.Create("Respuesta sintética");
        content["evaluaciones"]!["PO-01"] = new JsonArray(schema.Definition()["criterios"]!.AsArray().Select(c => (JsonNode)new JsonObject { ["criterio"] = c!.DeepClone(), ["valor"] = "5", ["obs"] = "" }).ToArray());
        return content;
    }
    private static void RegisterEditingCases(NativeDatabase database, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Edición/regresión: códigos y criterios confirmados atómicamente para dos altas simultáneas", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var first = Guid.NewGuid(); var second = Guid.NewGuid();
            var row = (await Live(client, first))["contenido"]!["identificacion"]![0]!.DeepClone();
            row["validacion"] = "V"; row["tramite"] = "Proceso uno";
            row["usuario"] = new JsonArray("Docentes"); row["resultado"] = "Entrega"; row["responsable"] = "Área"; row["prioridad"] = "3";
            var other = row.DeepClone(); other["id"] = "segundo"; other["tramite"] = "Proceso dos";
            var a = await Lease(client, first, "identificacion:inicial"); var b = await Lease(client, second, "identificacion:segundo");
            var incomplete = row.DeepClone().AsObject(); incomplete.Remove("codigo");
            var invalid = await Patch(client, Edit(first, a with { Value = incomplete }));
            Check(invalid.StatusCode == HttpStatusCode.UnprocessableEntity);
            Check((await Json(invalid))["validation"]![0]!["field"]!.ToString() == "codigo");
            var outcomes = await Task.WhenAll(Patch(client, Edit(first, a with { Value = row })), Patch(client, Edit(second, b with { Value = other })));
            Check(outcomes.All(r => r.IsSuccessStatusCode));
            var live = await Live(client, first); var content = live["contenido"]!;
            var codes = content["identificacion"]!.AsArray().Select(r => r!["codigo"]!.ToString()).Order().ToArray();
            Check(codes.SequenceEqual(new[] { "PO-01", "PO-02" }));
            Check(codes.All(c => content["evaluaciones"]![c]!.AsArray().Count == 9));
            Check(live["bloques"]!.AsArray().Count(b => b!["key"]!.ToString().StartsWith("evaluaciones:") && b["version"]!.GetValue<int>() == 1) == 18);
            var system = content["sistemas"]![0]!.DeepClone(); system["proceso"] = codes[0];
            var link = await Lease(client, first, "sistemas:inicial");
            Check((await Patch(client, Edit(first, link with { Value = system }))).IsSuccessStatusCode);
            // Un código confirmado no se renombra ni deja vínculos huérfanos.
            row = content["identificacion"]![0]!.DeepClone(); row["codigo"] = "PO-98";
            Check((await Patch(client, Edit(first, a with { Version = 1, Value = row }))).StatusCode == HttpStatusCode.UnprocessableEntity);
            foreach (var cleared in new string?[] { "", null })
            {
                row["codigo"] = cleared;
                Check((await Patch(client, Edit(first, a with { Version = 1, Value = row }))).StatusCode == HttpStatusCode.UnprocessableEntity);
            }
        });
        Test("Edición/regresión: un vínculo inválido histórico no paraliza otro registro ni se limpia al guardarlo", async (app, client) =>
        {
            await FinalStage(database);
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var agreement = await Lease(client, tab, "acuerdos:inicial");
            // Fixture sintético de la incidencia previa; nunca se ejecuta sobre una conexión institucional.
            await database.Sql("UPDATE formatos_ur SET contenido=jsonb_set(contenido::jsonb,'{sistemas,0,proceso}','\"PO-99\"')::json WHERE id_ur='A'");
            var original = (await Live(client, tab))["contenido"]!;
            var row = original["acuerdos"]![0]!.DeepClone(); row["acuerdo"] = "Independiente";
            Check((await Patch(client, Edit(tab, agreement with { Value = row }))).IsSuccessStatusCode);
            var after = (await Live(client, tab))["contenido"]!;
            Check(JsonNode.DeepEquals(after["sistemas"], original["sistemas"]));
            var system = after["sistemas"]![0]!.DeepClone(); system["sistema"] = "excel";
            var lease = await Lease(client, tab, "sistemas:inicial");
            Check((await Patch(client, Edit(tab, lease with { Value = system }))).IsSuccessStatusCode);
            Check((await Live(client, tab))["contenido"]!["sistemas"]![0]!["proceso"]!.ToString() == "PO-99");
            system["proceso"] = ""; // Corrección explícita de la referencia histórica, conservando la reserva.
            Check((await Patch(client, Edit(tab, lease with { Version = 1, Value = system }))).IsSuccessStatusCode);
        });
        Test("Edición/reproducción: referencia a código local no confirmado rechaza incluso un bloque independiente", async (app, client) =>
        {
            await FinalStage(database);
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var content = (await Live(client, tab))["contenido"]!;
            var system = content["sistemas"]![0]!.DeepClone(); system["proceso"] = "PO-01";
            var agreement = content["acuerdos"]![0]!.DeepClone(); agreement["acuerdo"] = "Respuesta independiente";
            var a = await Lease(client, tab, "sistemas:inicial"); var b = await Lease(client, tab, "acuerdos:inicial");
            var rejected = await Patch(client, Edit(tab, a with { Value = system }, b with { Value = agreement }));
            Check(rejected.StatusCode == HttpStatusCode.UnprocessableEntity);
            Check((await Json(rejected))["validation"]![0]!["field"]!.ToString() == "proceso");
            Check((await Live(client, tab))["contenido"]!["acuerdos"]![0]!["acuerdo"]!.ToString() == "");
            // Misma sesión, testigos y versiones vigentes: es validación de referencias, no concurrencia.
            system["proceso"] = "";
            Check((await Patch(client, Edit(tab, a with { Value = system }, b with { Value = agreement }))).IsSuccessStatusCode);
        });
        Test("Edición/usuarios múltiples: colección vacía pendiente, validación estricta, guardado y recarga sin limpieza", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            var tab = Guid.NewGuid(); var row = (await Live(client, tab))["contenido"]!["identificacion"]![0]!.DeepClone();
            Check(row["usuario"] is JsonArray { Count: 0 });
            var block = await Lease(client, tab, "identificacion:inicial");
            foreach (var invalid in new JsonNode?[] { JsonValue.Create("Docentes"), null, new JsonArray("Docentes", "Docentes"), new JsonArray("Ajeno"), new JsonArray(1), new JsonObject() })
            {
                row["usuario"] = invalid;
                Check((await Patch(client, Edit(tab, block with { Value = row }))).StatusCode == HttpStatusCode.UnprocessableEntity);
            }
            row["usuario"] = new JsonArray("Comunidad universitaria", "Docentes", "Público en general");
            Check((await Patch(client, Edit(tab, block with { Value = row }))).IsSuccessStatusCode);
            var live = await Live(client, tab);
            Check(JsonNode.DeepEquals(row["usuario"], live["contenido"]!["identificacion"]![0]!["usuario"]));
            row["resultado"] = "Cambio posterior independiente";
            Check((await Patch(client, Edit(tab, block with { Version = 1, Value = row }))).IsSuccessStatusCode);
            Check(JsonNode.DeepEquals(row["usuario"], (await Live(client, tab))["contenido"]!["identificacion"]![0]!["usuario"]));
            row["usuario"] = new JsonArray();
            Check((await Patch(client, Edit(tab, block with { Version = 2, Value = row }))).IsSuccessStatusCode);
            var complete = Complete(app); var progress = FormSchema.Progress(complete);
            complete["identificacion"]![0]!["usuario"] = new JsonArray();
            Check(progress == 100 && FormSchema.Progress(complete) < 100);
            Check(FormReview.Inspect(complete).pendientes.Any(p => p.campo == "usuario" && p.bloque == "identificacion:inicial"));
        });
        Test("Edición/usuarios: envío rechaza colección vacía y acepta opciones confirmadas sin alterar prioridad", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            var content = Complete(app); content["identificacion"]![0]!["usuario"] = new JsonArray();
            await SeedEditing(database, content);
            var tab = Guid.NewGuid(); var live = await Live(client, tab);
            Check(!live["revisionEnvio"]!["listo"]!.GetValue<bool>() && live["revisionEnvio"]!["pendientes"]!.AsArray().Count == 2);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).StatusCode == HttpStatusCode.UnprocessableEntity);
            content["identificacion"]![0]!["usuario"] = new JsonArray("Estudiantes", "Docentes");
            var block = await Lease(client, tab, "identificacion:inicial", 1);
            Check((await Patch(client, Edit(tab, block with { Value = content["identificacion"]![0]!.DeepClone() }) with { Release = true })).IsSuccessStatusCode);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 2))).IsSuccessStatusCode);
            live = await Live(client, tab);
            Check(!live["editable"]!.GetValue<bool>() && live["contenido"]!["identificacion"]![0]!["prioridad"]!.ToString() == "5");
            Check(JsonNode.DeepEquals(content["identificacion"]![0]!["usuario"], live["contenido"]!["identificacion"]![0]!["usuario"]));
        });
        Test("Edición/revisión omite sesión, recalcula borradores sin escribir y conserva historia al enviar", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var complete = Complete(app);
            // Un encabezado heredado irregular no puede convertirse en un requisito invisible.
            complete["encabezado"] = new JsonObject { ["area"] = "Área histórica", ["fecha"] = "fecha heredada", ["responsable"] = null };
            await SeedEditing(database, complete);
            await database.Sql("UPDATE formatos_ur SET porcentaje=45 WHERE id_ur='A'");
            var before = (await database.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'"))!.ToString();
            var tab = Guid.NewGuid(); var live = await Live(client, tab);
            Check(live["revisionEnvio"]!["listo"]!.GetValue<bool>() && live["porcentaje"]!.GetValue<int>() == 100);
            Check((await Form(client))["porcentaje"]!.GetValue<int>() == 100);
            var index = await Json(await client.GetAsync("/inicio"));
            Check(index["props"]!["formatos"]!.AsArray().Single(r => r!["id_ur"]!.ToString() == "A")!["porcentaje"]!.GetValue<int>() == 100);
            Check(Convert.ToInt32(await database.Scalar("SELECT porcentaje FROM formatos_ur WHERE id_ur='A'")) == 45);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).IsSuccessStatusCode);
            Check((await database.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'"))!.ToString() == before);
            live = await Live(client, tab);
            Check(live["enviadoPor"]!.ToString() == "persona@uacj.mx" && live["revisionEnvio"] is null && !live["editable"]!.GetValue<bool>());
        });
        Test("Edición/prioridad histórica reduce avance y pendientes usan ID estable", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await SeedEditing(database, Complete(app));
            await database.Sql("UPDATE formatos_ur SET contenido=jsonb_set(contenido::jsonb,'{identificacion,0,prioridad}','\"17\"')::json WHERE id_ur='A'");
            var tab = Guid.NewGuid(); var live = await Live(client, tab);
            Check(live["porcentaje"]!.GetValue<int>() < 100 && !live["revisionEnvio"]!["listo"]!.GetValue<bool>());
            var pending = live["revisionEnvio"]!["pendientes"]!.AsArray().Single(p => p!["seccion"]!.ToString() == "identificacion")!;
            Check(pending["bloque"]!.ToString() == "identificacion:inicial" && pending["campo"]!.ToString() == "prioridad");
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).StatusCode == HttpStatusCode.UnprocessableEntity);
            Check((await Live(client, tab))["editable"]!.GetValue<bool>());
        });
        Test("Edición/lectura preserva avance, contenido e instantánea de un envío histórico", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await SeedEditing(database, Complete(app));
            await database.Sql("""
                UPDATE formatos_ur SET porcentaje=37,enviado_en=clock_timestamp(),enviado_por='actor@example.test',
                  enviado_como='responsable@example.test',envio_id=gen_random_uuid(),
                  instantanea_envio=jsonb_build_object('unidad',jsonb_build_object('id_ur','A','cve_ur','100','desc_ur','Área histórica'),
                    'contenido',contenido::jsonb,'version',version) WHERE id_ur='A'
                """);
            var before = (await database.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f WHERE id_ur='A'"))!.ToString();
            var live = await Live(client, Guid.NewGuid()); var page = await Form(client);
            Check(live["porcentaje"]!.GetValue<int>() == 37 && page["porcentaje"]!.GetValue<int>() == 37);
            Check(page["unidad"]!["desc_ur"]!.ToString() == "Área histórica" && page["enviadoPor"]!.ToString() == "responsable@example.test");
            Check(!live["editable"]!.GetValue<bool>() && live["revisionEnvio"] is null);
            var index = await Json(await client.GetAsync("/inicio"));
            Check(index["props"]!["formatos"]!.AsArray().Single(r => r!["id_ur"]!.ToString() == "A")!["porcentaje"]!.GetValue<int>() == 37);
            Check((await database.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f WHERE id_ur='A'"))!.ToString() == before);
        });
        Test("Edición/eliminar proceso exige reservas de sus relaciones y conserva registros ILDA", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await SeedEditing(database, Complete(app));
            var tab = Guid.NewGuid(); var process = await Lease(client, tab, "identificacion:inicial", 1);
            Check((await Patch(client, Edit(tab, process with { Value = null }))).StatusCode == HttpStatusCode.UnprocessableEntity);
            var other = await Lease(client, Guid.NewGuid(), "sistemas:inicial", 1);
            var content = (await Live(client, tab))["contenido"]!.AsObject();
            var system = content["sistemas"]![0]!.DeepClone(); system["proceso"] = "";
            var removal = Edit(tab, new BlockRequest(process.Key, 1), new BlockRequest(other.Key, 1)) with { Removal = new("identificacion", "inicial") };
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", removal)).StatusCode == HttpStatusCode.Conflict);
            Check((await Live(client, tab))["contenido"]!["identificacion"]!.AsArray().Count == 1);
            await database.Sql("UPDATE formatos_ur SET contenido=jsonb_set(contenido::jsonb,'{identificacion,0,id}','\"ilda:17\"')::json WHERE id_ur='A'");
            var ilda = await Lease(client, tab, "identificacion:ilda:17");
            Check((await Patch(client, Edit(tab, ilda with { Value = null }))).StatusCode == HttpStatusCode.UnprocessableEntity);
        });
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
        Test("Edición/lectura durante adquisición queda anterior a la confirmación y no invalida su reserva", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            await database.Sql("""
                CREATE FUNCTION fixture_pause_reserve() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN PERFORM pg_advisory_xact_lock(606,6); PERFORM pg_sleep(1.5); RETURN NEW; END $$;
                CREATE TRIGGER pause_reserve BEFORE INSERT ON formato_bloques FOR EACH ROW EXECUTE FUNCTION fixture_pause_reserve();
                """);
            try
            {
                var acquisition = client.PostAsJsonAsync("/formatos/A/reservas", Edit(tab, new BlockRequest("contexto", 0)));
                var deadline = DateTime.UtcNow.AddSeconds(5);
                while (Convert.ToInt64(await database.Scalar("SELECT count(*) FROM pg_locks WHERE locktype='advisory' AND classid=606 AND objid=6 AND granted")) == 0)
                { Check(DateTime.UtcNow < deadline); await Task.Delay(10); }
                var during = await Live(client, tab);
                Check(during["bloques"]!.AsArray().All(b => b!["reserva"] is null));
                var response = await acquisition; Check(response.IsSuccessStatusCode); var confirmed = await Json(response);
                Check(DateTimeOffset.Parse(confirmed["servidorEn"]!.ToString()) > DateTimeOffset.Parse(during["servidorEn"]!.ToString()));
                Check(confirmed["bloques"]![0]!["reserva"]!["propia"]!.GetValue<bool>());
            }
            finally { await database.Sql("DROP TRIGGER pause_reserve ON formato_bloques; DROP FUNCTION fixture_pause_reserve()"); }
        });
        Test("Edición/bloques distintos guardan concurrentemente sin sobrescribir JSON", async (app, client) =>
        {
            // Este escenario cubre preguntas de etapas posteriores; la captura restringida se verifica aparte.
            await database.Sql("UPDATE fixture_roles SET rol_clave='administrador'");
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
            Check(foreign["otraPestana"]!.GetValue<bool>());
            using var separate = app.Client(); await Login(app, separate); await Csrf(separate);
            var independent = (await Live(separate, Guid.NewGuid()))["bloques"]![0]!["reserva"]!;
            Check(!independent["otraPestana"]!.GetValue<bool>() && independent["id"] is null);
        });
        Test("Edición/adquisición confirma contenido vigente y rechaza versión obsoleta", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var first = await Lease(client, tab);
            Check((await Patch(client, Edit(tab, first with { Value = Header("Respuesta vigente") }) with { Release = true })).IsSuccessStatusCode);
            var next = Guid.NewGuid();
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(next, new BlockRequest("contexto", 0)))).StatusCode == HttpStatusCode.Conflict);
            var grant = await Json(await client.PostAsJsonAsync("/formatos/A/reservas", Edit(next, new BlockRequest("contexto", 1))));
            Check(grant["bloques"]![0]!["value"]!["responsable"]!.ToString() == "Respuesta vigente");
            Check(grant["bloques"]![0]!["version"]!.GetValue<int>() == 1);
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
            await FinalStage(database);
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0))).StatusCode == HttpStatusCode.UnprocessableEntity);
            await SeedEditing(database, Complete(app));
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
            await Login(app, client); await Csrf(client); await SeedEditing(database, Complete(app));
            var results = await Task.WhenAll(client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(Guid.NewGuid(), Guid.NewGuid(), 1)),
                client.PostAsJsonAsync("/formatos/A/reservas", Edit(Guid.NewGuid(), new BlockRequest("contexto", 1))));
            Check(results.Count(r => r.IsSuccessStatusCode) == 1 && results.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1);
        });
        Test("Edición/envío y guardado simultáneos de la misma sesión no cruzan el cierre", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await SeedEditing(database, Complete(app));
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
            var race = await Task.WhenAll(client.PostAsJsonAsync("/formatos/A3/reservas", Edit(Guid.NewGuid(), new BlockRequest("identificacion:inicial", 0))),
                helper.PostAsJsonAsync("/formatos/A3/reservas", Edit(Guid.NewGuid(), new BlockRequest("identificacion:inicial", 0))));
            Check(race.Count(r => r.IsSuccessStatusCode) == 1 && race.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1);
            Check((await Patch(helper, Edit(tab, block with { Value = Header("Colaboradora") }), "A3")).IsSuccessStatusCode);
            var ownerTab = Guid.NewGuid();
            Check((await client.PostAsJsonAsync("/formatos/A3/reservas", Edit(ownerTab, new BlockRequest("contexto", 1)))).StatusCode == HttpStatusCode.Conflict);
            var ownerBlock = await Lease(client, ownerTab, "medios", unit: "A3");
            var medios = (await Live(client, ownerTab, "A3"))["contenido"]!["medios"]!.DeepClone();
            Check((await Patch(client, Edit(ownerTab, ownerBlock with { Value = new JsonObject { ["medios"] = medios, ["medioOtro"] = "Otro usuario, otro bloque" } }), "A3")).IsSuccessStatusCode);
            var holder = (await Live(client, ownerTab, "A3"))["bloques"]!.AsArray().Single(b => b!["key"]!.ToString() == "contexto")!["reserva"]!;
            Check(holder["titular"]!.ToString() == "Colaboradora sintética" && !holder["otraPestana"]!.GetValue<bool>());
            Check((await helper.PostAsJsonAsync("/formatos/A3/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).StatusCode == HttpStatusCode.Forbidden);
            var foreign = await helper.PostAsJsonAsync("/formatos/B/reservas", Edit(tab, new BlockRequest("contexto", 0)));
            Check(foreign.StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Edición/enviado conserva ILDA, unidad y respuestas aunque cambien los catálogos", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var complete = Complete(app); complete["identificacion"]![0]!["id"] = "ilda:17";
            await SeedEditing(database, complete);
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
