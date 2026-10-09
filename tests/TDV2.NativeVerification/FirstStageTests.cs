using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Tdv2.Domain;
using Tdv2.Services;
using Tdv2.Synchronization;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    // Sólo fixture PostgreSQL aislado: permite probar capturas e instantáneas anteriores sin usar el API nuevo para inventar códigos.
    internal static Task SeedStage(NativeDatabase database, JsonObject content) => database.Sql("""
        INSERT INTO formatos_ur(id_ur,ejercicio,contenido,version,porcentaje,actualizado_por,created_at,updated_at)
        VALUES('A',2026,$1::json,0,0,'persona@uacj.mx',now(),now())
        """, content.ToJsonString());
    private static async Task<EditRequest> RemovalRequest(HttpClient client, Guid tab, RowRemoval target, string unit = "A")
    {
        var live = await Live(client, tab, unit); var plan = FormRemoval.Changes(live["contenido"]!.AsObject(), target);
        var versions = live["bloques"]!.AsArray().ToDictionary(b => b!["key"]!.ToString(), b => b!["version"]!.GetValue<int>());
        var request = Edit(tab, plan.Select(p => new BlockRequest(p.Key, versions.GetValueOrDefault(p.Key))).ToArray()) with { Removal = target };
        var response = await client.PostAsJsonAsync($"/formatos/{unit}/reservas", request);
        Check(response.IsSuccessStatusCode, "Reservas de retiro: " + (int)response.StatusCode);
        var leases = (await Json(response))["bloques"]!.AsArray().ToDictionary(b => b!["key"]!.ToString(), b => Guid.Parse(b!["reserva"]!["id"]!.ToString()));
        return request with { Release = true, Blocks = request.Blocks.Select(b => b with { LeaseId = leases[b.Key], Value = plan[b.Key] }).ToArray() };
    }
    private static void RegisterFirstStageCases(NativeDatabase database, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Edición/retiro renovación: reproducción de actividad tardía tras commit", async (app, client) =>
        {
            await SeedStage(database, Complete(app)); await Login(app, client); await Csrf(client);
            var input = await RemovalRequest(client, Guid.NewGuid(), new("identificacion", "inicial"));
            var renewal = input with { Blocks = [input.Blocks[0] with { Value = null }], Release = false };
            Check((await Patch(client, input)).IsSuccessStatusCode);
            var late = await client.PostAsJsonAsync("/formatos/A/reservas/actividad", renewal);
            Check(late.StatusCode == HttpStatusCode.Conflict);
            Check((await Json(late))["code"]!.ToString() == "registro_eliminado");
            Check((await Patch(client, input)).IsSuccessStatusCode); // El recibo idéntico sí confirma la operación original.
            Check((await Json(await client.PostAsJsonAsync("/formatos/A/reservas/actividad", renewal with { Removal = null })))["code"]!.ToString() == "registro_eliminado");
            var unknown = await client.PostAsJsonAsync("/formatos/A/reservas/actividad", renewal with { Removal = new("identificacion", "inexistente") });
            Check(unknown.StatusCode == HttpStatusCode.NotFound && (await Json(unknown))["code"]!.ToString() == "registro_desconocido");
            Check((await Live(client, input.TabId))["contenido"]!["identificacion"]!.AsArray().Count == 0);
            await database.Sql("DELETE FROM fixture_roles");
            Check((await client.PostAsJsonAsync("/formatos/A/reservas/actividad", renewal)).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Edición/etapa: histórico de otro ejercicio conserva respuestas originales y no mezcla inventario", async (app, client) =>
        {
            var content = Complete(app); content["identificacion"]![0]!["usuario"] = new JsonArray("Otro", "Externo", "Docentes");
            content["identificacion"]![0]!["usuarioOtro"] = "Detalle histórico";
            await SeedStage(database, content); await database.Sql("UPDATE formatos_ur SET ejercicio=2025,porcentaje=37");
            var original = await database.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f");
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var props = await Form(client); var live = await Live(client, tab);
            Check(!props["editable"]!.GetValue<bool>() && !live["editable"]!.GetValue<bool>() && live["porcentaje"]!.GetValue<int>() == 37);
            Check(JsonNode.DeepEquals(content, live["contenido"]) && JsonNode.DeepEquals(content, props["contenido"]));
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(tab, new BlockRequest("identificacion:inicial", 0)))).StatusCode == HttpStatusCode.Conflict);
            Check(Equals(original, await database.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f")));
        });
        Test("Edición/etapa: alias Nexo, entrada única, ruta y revocación efectiva", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            async Task CheckMenu()
            {
                var props = (await Json(await client.GetAsync("/inicio")))["props"]!;
                var modules = props["auth"]!["modules"]!.AsArray();
                Check(modules.Count(m => m!["key"]!.ToString() == ModuleAccess.Procedures) == 1);
                Check(modules[0]!["name"]!.ToString() == "Procedimientos Institucionales");
                Check((await Live(client, Guid.NewGuid()))["editable"]!.GetValue<bool>());
            }
            await CheckMenu();
            await database.Sql("UPDATE fixture_modules SET clave='procedimientos_institucionales' WHERE id=9"); await CheckMenu();
            await database.Sql("INSERT INTO fixture_modules VALUES(19,47,'procesos_operativos','Anterior','/inicio',null,2,null); INSERT INTO fixture_module_roles VALUES(30,19)"); await CheckMenu();
            await database.Sql("UPDATE fixture_modules SET ruta='/ajena' WHERE id IN(9,19)");
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("UPDATE fixture_modules SET ruta='/inicio',modulo_padre_id=50 WHERE id IN(9,19)");
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Edición/etapa: captura limitada, medios incluidos y revocación de privilegio administrativo", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var form = await Form(client); Check(!form["seccionesPosteriores"]!.GetValue<bool>());
            foreach (var key in new[] { "datos:inicial", "preguntas:0", "evaluaciones:PO-01:0", "acuerdos:inicial" })
                Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(tab, new BlockRequest(key, 0)))).StatusCode == HttpStatusCode.Forbidden);
            var media = await Lease(client, tab, "medios"); var values = FormBlocks.Split((await Live(client, tab))["contenido"]!.AsObject())["medios"]!;
            values["medioOtro"] = "Medio sintético";
            Check((await Patch(client, Edit(tab, media with { Value = values }))).IsSuccessStatusCode);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).StatusCode == HttpStatusCode.UnprocessableEntity);
            await database.Sql("UPDATE fixture_roles SET rol_clave='administrador'");
            Check((await Form(client))["seccionesPosteriores"]!.GetValue<bool>());
            var hidden = await Lease(client, tab, "datos:inicial"); var row = (await Live(client, tab))["contenido"]!["datos"]![0]!.DeepClone(); row["dato"] = "Privado";
            await database.Sql("UPDATE fixture_roles SET rol_clave='responsable_ur_supervisor'");
            Check((await Patch(client, Edit(tab, hidden with { Value = row }))).StatusCode == HttpStatusCode.Forbidden);
            Check(!(await Form(client))["seccionesPosteriores"]!.GetValue<bool>());
        });
        Test("Edición/etapa: representación y vista de prueba usan contexto efectivo sin sumar administrador", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client);
            Check((await Form(client))["seccionesPosteriores"]!.GetValue<bool>());
            Check((await StartRepresentation(client)).IsSuccessStatusCode); await Context(client);
            var props = await Form(client); Check(!props["seccionesPosteriores"]!.GetValue<bool>() && props["editable"]!.GetValue<bool>());
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(Guid.NewGuid(), new BlockRequest("preguntas:0", 0)))).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.DeleteAsync("/actuar-como-usuario")).IsSuccessStatusCode); await Context(client);
            Check((await StartPreview(client, "responsable", "A")).IsSuccessStatusCode); await Context(client);
            props = await Form(client); Check(!props["editable"]!.GetValue<bool>() && !props["seccionesPosteriores"]!.GetValue<bool>());
        });
        Test("Edición/etapa: destinatarios vigentes, borrador vacío, rechazo de cliente anterior e historia intacta", async (app, client) =>
        {
            var initial = Complete(app); initial["preguntas"]![0]!["respuesta"] = "Historia oculta";
            initial["identificacion"]![0]!["prioridad"] = "4";
            await SeedStage(database, initial); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var block = await Lease(client, tab, "identificacion:inicial"); var row = initial["identificacion"]![0]!.DeepClone();
            row["usuario"] = new JsonArray();
            Check((await Patch(client, Edit(tab, block with { Value = row }))).IsSuccessStatusCode);
            var live = await Live(client, tab); Check(live["porcentajeEtapa"]!.GetValue<int>() < 100);
            Check(live["revisionEtapa"]!["pendientes"]!.AsArray().Any(p => p!["campo"]!.ToString() == "usuario"));
            foreach (var retired in new[] { "Otro", "Externo" })
            {
                row["usuario"] = new JsonArray("Docentes", retired);
                var rejected = await Patch(client, Edit(tab, block with { Version = 1, Value = row }));
                Check(rejected.StatusCode == HttpStatusCode.UnprocessableEntity);
                Check((await Json(rejected))["validation"]![0]!["field"]!.ToString() == "usuario");
            }
            row["usuario"] = new JsonArray("Docentes"); row["usuarioOtro"] = "Cliente anterior";
            Check((await Patch(client, Edit(tab, block with { Version = 1, Value = row }))).StatusCode == HttpStatusCode.UnprocessableEntity);
            row.AsObject().Remove("usuarioOtro");
            row["usuario"] = new JsonArray("Docentes", "Docentes");
            Check((await Patch(client, Edit(tab, block with { Version = 1, Value = row }))).StatusCode == HttpStatusCode.UnprocessableEntity);
            row["usuario"] = new JsonArray("Público en general", "Instituciones públicas externas", "Empresas y organizaciones privadas");
            Check((await Patch(client, Edit(tab, block with { Version = 1, Value = row }))).IsSuccessStatusCode);
            live = await Live(client, tab); Check(live["porcentajeEtapa"]!.GetValue<int>() == 100 && live["revisionEtapa"]!["listo"]!.GetValue<bool>());
            var persisted = live["contenido"]!;
            Check(JsonNode.DeepEquals(row["usuario"], persisted["identificacion"]![0]!["usuario"]) && persisted["identificacion"]![0]!["prioridad"]!.ToString() == "4");
            foreach (var section in new[] { "datos", "evaluaciones", "preguntas", "acuerdos", "encabezado" }) Check(JsonNode.DeepEquals(initial[section], persisted[section]));
            Check(live["enviadoEn"] is null && live["puedeEnviar"]!.GetValue<bool>());
        });
        Test("Edición/etapa: cien habilita revisión de entrega sin requisitos de pestañas posteriores ni sesión", async (app, client) =>
        {
            var content = Complete(app); var blank = app.Services.GetRequiredService<FormSchema>().Blank(new("A", "100", "A", 2, null, 2026));
            foreach (var section in new[] { "datos", "evaluaciones", "preguntas", "acuerdos" }) content[section] = blank[section]!.DeepClone();
            await SeedStage(database, content); await Login(app, client);
            var live = await Live(client, Guid.NewGuid());
            Check(live["porcentajeEtapa"]!.GetValue<int>() == 100 && live["porcentaje"]!.GetValue<int>() == 100);
            Check(live["revisionEtapa"]!["listo"]!.GetValue<bool>() && live["revisionEnvio"]!["listo"]!.GetValue<bool>() && live["enviadoEn"] is null);
        });
        Test("Edición/etapa: códigos confirmados V/A, vínculos históricos corregibles y rechazo de referencia nueva D/N", async (app, client) =>
        {
            var content = Complete(app); content["identificacion"]![0]!["validacion"] = "N";
            var valid = content["identificacion"]![0]!.DeepClone(); valid["id"] = "validado"; valid["codigo"] = "PO-02"; valid["validacion"] = "A";
            content["identificacion"]!.AsArray().Add(valid); await SeedStage(database, content);
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var block = await Lease(client, tab, "sistemas:inicial");
            var row = content["sistemas"]![0]!.DeepClone(); row["fallas"] = "Conserva vínculo histórico N";
            Check((await Patch(client, Edit(tab, block with { Value = row }))).IsSuccessStatusCode);
            row["proceso"] = "PO-02"; Check((await Patch(client, Edit(tab, block with { Version = 1, Value = row }))).IsSuccessStatusCode);
            foreach (var invalid in new[] { "PO-01", "PO-99" })
            { row["proceso"] = invalid; Check((await Patch(client, Edit(tab, block with { Version = 2, Value = row }))).StatusCode == HttpStatusCode.UnprocessableEntity); }
            var process = valid.DeepClone(); process["id"] = "falso"; process["codigo"] = "PO-03";
            var newLease = await Lease(client, tab, "identificacion:falso");
            Check((await Patch(client, Edit(tab, newLease with { Value = process }))).StatusCode == HttpStatusCode.UnprocessableEntity);
        });
        Test("Edición/etapa: retiro ILDA atómico con relaciones ocultas, auditoría, reintento y no reaparición", async (app, client) =>
        {
            var content = Complete(app); content["identificacion"]![0]!["id"] = "ilda:17";
            await SeedStage(database, content);
            await database.Sql("INSERT INTO sincronizacion_catalogos VALUES('ilda',1,now()); INSERT INTO ilda_informacion_area(id_origen,ur2,informacion_generada,datos,sincronizado_en) VALUES('17','100','Origen ILDA sintético','{}',now())");
            await Login(app, client); await Csrf(client); var target = new RowRemoval("identificacion", "ilda:17");
            var input = await RemovalRequest(client, Guid.NewGuid(), target);
            var forged = input with { OperationId = Guid.NewGuid(), Blocks = input.Blocks.Select(b => b.Key.StartsWith("datos:")
                ? b with { Value = new JsonObject { ["id"] = "inicial", ["proceso"] = "", ["dato"] = "Sobrescritura" } } : b).ToArray() };
            Check((await Patch(client, forged)).StatusCode == HttpStatusCode.Conflict);
            // Una reserva auxiliar tampoco autoriza un parche sin la intención exacta de eliminación.
            Check((await Patch(client, input with { Removal = null, OperationId = Guid.NewGuid() })).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql(AuditFailureTrigger);
            Check(!(await Patch(client, input)).IsSuccessStatusCode);
            Check(Convert.ToInt32(await database.Scalar("SELECT count(*) FROM formato_exclusiones_ilda")) == 0);
            await database.Sql(RemoveAuditFailure);
            Check((await Patch(client, input)).IsSuccessStatusCode); Check((await Patch(client, input)).IsSuccessStatusCode);
            var live = await Live(client, input.TabId); var stored = live["contenido"]!;
            Check(stored["identificacion"]!.AsArray().Count == 0 && stored["evaluaciones"]!.AsObject().Count == 0);
            foreach (var section in new[] { "sistemas", "datos" }) { var expected = content[section]!.DeepClone(); expected[0]!["proceso"] = ""; Check(JsonNode.DeepEquals(expected, stored[section])); }
            Check(Convert.ToInt32(await database.Scalar("SELECT count(*) FROM formato_exclusiones_ilda")) == 1);
            Check(Convert.ToInt32(await database.Scalar("SELECT count(*) FROM formato_operaciones")) == 1);
            Check(Convert.ToInt32(await database.Scalar("SELECT count(*) FROM ilda_informacion_area WHERE id_origen='17'")) == 1);
            await database.Sql("UPDATE ilda_informacion_area SET informacion_generada='Actualizado por publicación sintética',sincronizado_en=now(),presente=true");
            Check((await Form(client))["contenido"]!["identificacion"]!.AsArray().Count == 0);
            Check((await Live(client, input.TabId))["contenido"]!["identificacion"]!.AsArray().Count == 0);
            using var scope = app.Services.CreateScope(); var unit = new Unit("A", "100", "A", 2, null, 2026);
            Check((await scope.ServiceProvider.GetRequiredService<ILocalCatalog>().Inventories([unit], default))["A"].Rows.Count == 0);
            Check(live["bloques"]!.AsArray().All(b => b!["reserva"] is null));
            app.Sources.Ilda.Rows.Clear(); app.Sources.Ilda.Rows.Add(17, "100", "Nueva publicación de la fuente sintética", null, "");
            Check(Completed(await RunSync(app, "ilda")));
            Check((await Live(client, input.TabId))["contenido"]!["identificacion"]!.AsArray().Count == 0);
            Check(Convert.ToInt32(await database.Scalar("SELECT count(*) FROM ilda_informacion_area WHERE id_origen='17' AND presente")) == 1);
            var rejectedRestore = await client.PostAsJsonAsync("/formatos/A/reservas", Edit(input.TabId, new BlockRequest("identificacion:ilda:17", 1)));
            Check(rejectedRestore.StatusCode == HttpStatusCode.Conflict && (await Json(rejectedRestore))["code"]!.ToString() == "registro_eliminado");
        });
        Test("Edición/etapa: retiro compite con reserva, versión y dependencia ocupada", async (app, client) =>
        {
            await SeedStage(database, Complete(app)); await Login(app, client); await Csrf(client);
            using var other = app.Client(); await Login(app, other); await Csrf(other); var tab = Guid.NewGuid();
            var target = new RowRemoval("identificacion", "inicial");
            var initial = await Live(client, tab); var plan = FormRemoval.Changes(initial["contenido"]!.AsObject(), target);
            var request = Edit(tab, plan.Keys.Select(k => new BlockRequest(k, 0)).ToArray()) with { Removal = target };
            var occupied = await Lease(other, Guid.NewGuid(), "sistemas:inicial");
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", request)).StatusCode == HttpStatusCode.Conflict);
            // El rechazo no deja una adquisición parcial del registro principal.
            Check(!(await Live(client, tab))["bloques"]!.AsArray().Any(b => b!["key"]!.ToString() == "identificacion:inicial" && b["reserva"] is not null));
            await database.Sql("UPDATE formato_bloques SET vence_en=clock_timestamp()-interval '1 second'");
            var contender = request with { TabId = Guid.NewGuid(), OperationId = Guid.NewGuid() };
            var responses = await Task.WhenAll(client.PostAsJsonAsync("/formatos/A/reservas", request), other.PostAsJsonAsync("/formatos/A/reservas", contender));
            Check(responses.Count(r => r.IsSuccessStatusCode) == 1 && responses.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1);
            await database.Sql("UPDATE formato_bloques SET vence_en=clock_timestamp()-interval '1 second',version=version+1 WHERE bloque='sistemas:inicial'");
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", request)).StatusCode == HttpStatusCode.Conflict);
            Check((await Live(client, tab))["contenido"]!["identificacion"]!.AsArray().Count == 1);
        });
        Test("Edición/etapa: enviados conservan contenido, instantánea y avance sin captura ni retiro", async (app, client) =>
        {
            await SeedStage(database, Complete(app));
            await database.Sql("UPDATE formatos_ur SET porcentaje=37,enviado_en=now(),enviado_como='enviador@example.test',instantanea_envio=jsonb_build_object('contenido',contenido)");
            var original = await database.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f");
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var live = await Live(client, tab); Check(!live["editable"]!.GetValue<bool>() && live["porcentaje"]!.GetValue<int>() == 37);
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(tab, new BlockRequest("identificacion:inicial", 0)))).StatusCode == HttpStatusCode.Conflict);
            Check(Equals(original, await database.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f")));
        });
    }
}
