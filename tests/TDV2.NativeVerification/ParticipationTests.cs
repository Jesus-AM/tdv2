using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Tdv2.Services;
namespace Tdv2.NativeVerification;
internal static partial class NativeTests
{
    internal static async Task ConfigurationAdmin(NativeDatabase db)
    {
        await db.Sql("""
            UPDATE fixture_roles SET rol_clave='administrador',rol_nombre='Administrador' WHERE email='persona@uacj.mx';
            INSERT INTO fixture_modules VALUES(52,47,'configuracion_procesos','Configuración procesos','/configuracion/procesos','mdi-tune',12,50);
            INSERT INTO fixture_module_roles VALUES(30,50),(30,52);
            """);
    }
    private static async Task<ParticipationChange> Preview(HttpClient client, int version, int[] levels, string[] types)
    {
        var input = new ParticipationChange(version, levels, types);
        var response = await client.PostAsJsonAsync("/configuracion/procesos/vista-previa", input);
        Check(response.IsSuccessStatusCode, "Vista previa: " + (int)response.StatusCode);
        return input with { Confirmation = (await Json(response))["confirmation"]!.ToString() };
    }
    private static Task<HttpResponseMessage> Configure(HttpClient client, ParticipationChange input) => client.PutAsJsonAsync("/configuracion/procesos", input);
    private static void RegisterParticipationCases(NativeDatabase database, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Participación/vista de prueba y representación no modifican ni previsualizan configuración", async (app, client) =>
        {
            await SetupAccess(database);
            await database.Sql("INSERT INTO fixture_modules VALUES(53,47,'configuracion_procesos','Configuración procesos','/configuracion/procesos','mdi-tune',13,50); INSERT INTO fixture_module_roles VALUES(34,53)");
            await Login(app, client); await Csrf(client);
            Check((await StartPreview(client)).IsSuccessStatusCode); await Context(client);
            Check(!(await client.PostAsJsonAsync("/configuracion/procesos/vista-previa", new ParticipationChange(1, [3], []))).IsSuccessStatusCode);
            Check(!(await Configure(client, new(1, [3], []))).IsSuccessStatusCode);
            Check((await client.DeleteAsync("/vista-prueba")).IsSuccessStatusCode); await Context(client);
            Check((await StartRepresentation(client)).IsSuccessStatusCode); await Context(client);
            Check(!(await client.GetAsync("/configuracion/procesos")).IsSuccessStatusCode);
            Check(!(await Configure(client, new(1, [3], []))).IsSuccessStatusCode);
            Check(Convert.ToInt32(await database.Scalar("SELECT version FROM configuracion_procesos")) == 1);
        });
        Test("Participación/administrador, dos módulos y padre correctos; CSRF y confirmación obligatorios", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            Check((await client.GetAsync("/configuracion/procesos")).StatusCode == HttpStatusCode.Forbidden);
            await ConfigurationAdmin(database);
            Check((await client.GetAsync("/configuracion/procesos")).IsSuccessStatusCode);
            await database.Sql("UPDATE fixture_modules SET modulo_padre_id=9 WHERE id=52");
            Check((await client.GetAsync("/configuracion/procesos")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("UPDATE fixture_modules SET modulo_padre_id=50 WHERE id=52");
            Check((await Configure(client, new(1, [2], ["N"]))).StatusCode == HttpStatusCode.Conflict);
            var preview = await Preview(client, 1, [2], ["N"]);
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            Check(!(await Configure(client, preview)).IsSuccessStatusCode);
            Check(Convert.ToInt32(await database.Scalar("SELECT version FROM configuracion_procesos")) == 1);
        });
        Test("Participación/vista previa obsoleta por catálogo o formato no confirma", async (app, client) =>
        {
            await ConfigurationAdmin(database); await Login(app, client); await Csrf(client);
            var preview = await Preview(client, 1, [3], [" n "]);
            await database.Sql("UPDATE unidades_responsables_poa SET desc_ur='Catálogo modificado sintético' WHERE id_ur='B'");
            Check((await Configure(client, preview)).StatusCode == HttpStatusCode.Conflict);
            preview = await Preview(client, 1, [3], ["N"]);
            await Lease(client, Guid.NewGuid());
            Check((await Configure(client, preview)).StatusCode == HttpStatusCode.Conflict);
            Check((await Configure(client, await Preview(client, 1, [3], ["N"]))).IsSuccessStatusCode);
            Check((await Configure(client, preview)).StatusCode == HttpStatusCode.Conflict);
        });
        Test("Participación/excluir revoca todas las escrituras sin borrar; reactivar recupera identidad y respuestas", async (app, client) =>
        {
            await ConfigurationAdmin(database); await Login(app, client); await Csrf(client);
            var tab = Guid.NewGuid(); var lease = await Lease(client, tab, unit: "A3");
            Check((await Patch(client, Edit(tab, lease with { Value = Header("Histórico sintético") }), "A3")).IsSuccessStatusCode);
            var original = await database.Scalar("SELECT id::text||contenido::text FROM formatos_ur WHERE id_ur='A3'");
            Check((await Configure(client, await Preview(client, 1, [2], ["N"]))).IsSuccessStatusCode);
            foreach (var path in new[] { "/formatos/A3", $"/formatos/A3/estado?tab={tab}" }) Check((await client.GetAsync(path)).StatusCode == HttpStatusCode.Forbidden);
            Check((await Patch(client, Edit(tab, lease with { Version = 1, Value = Header("No debe guardar") }), "A3")).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.PostAsJsonAsync("/formatos/A3/reservas", Edit(tab, new Tdv2.Services.BlockRequest("medios", 0)))).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.PostAsJsonAsync("/formatos/A3/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).StatusCode == HttpStatusCode.Forbidden);
            Check((await Configure(client, await Preview(client, 2, [2,3], ["N"]))).IsSuccessStatusCode);
            Check((await client.GetAsync("/formatos/A3")).IsSuccessStatusCode);
            Check(Equals(original, await database.Scalar("SELECT id::text||contenido::text FROM formatos_ur WHERE id_ur='A3'")));
            Check(Convert.ToInt32(await database.Scalar("SELECT count(*) FROM activity_logs WHERE action='participacion_actualizada' AND meta->'detalles'->'anterior' IS NOT NULL AND meta->'detalles'->'nuevo' IS NOT NULL")) == 2);
        });
        Test("Participación/auditoría fallida revierte configuración", async (app, client) =>
        {
            await ConfigurationAdmin(database); await Login(app, client); await Csrf(client);
            var preview = await Preview(client, 1, [3], []);
            await database.Sql(AuditFailureTrigger);
            try { Check(!(await Configure(client, preview)).IsSuccessStatusCode); }
            finally { await database.Sql(RemoveAuditFailure); }
            Check(Convert.ToInt32(await database.Scalar("SELECT version FROM configuracion_procesos")) == 1);
        });
        Test("Participación/tipos normalizados, nulos y cero; conserva opciones guardadas ausentes del catálogo", async (app, client) =>
        {
            await ConfigurationAdmin(database);
            await database.Sql("""
                UPDATE unidades_responsables_poa SET tipo_ur=' n ' WHERE id_ur='A3';
                UPDATE unidades_responsables_poa SET tipo_ur=' 0 ' WHERE id_ur='A';
                INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,nivel_ur,estatus_ur,presente,tipo_ur)
                VALUES('nivel9',2026,'99',9,'Activo',true,'Z');
                """);
            await Login(app, client); await Csrf(client);
            Check((await VisibleForms(client)).Contains("A") && !(await VisibleForms(client)).Contains("A3"));
            Check((await Configure(client, await Preview(client, 1, [2,3,9], [" z "]))).IsSuccessStatusCode);
            Check((await VisibleForms(client)).Contains("A3") && !(await VisibleForms(client)).Contains("nivel9"));
            await database.Sql("UPDATE unidades_responsables_poa SET presente=false WHERE id_ur='nivel9'");
            var settings = (await Json(await client.GetAsync("/configuracion/procesos")))["props"]!;
            Check(settings["levels"]!.AsArray().Any(x => x!.GetValue<int>() == 9));
            Check(settings["types"]!.AsArray().Any(x => x!.ToString() == "Z"));
            Check((await Configure(client, await Preview(client, 2, [3,9], ["Z"]))).IsSuccessStatusCode);
        });
        Test("Participación/nuevo nivel permite responsabilidad propia sin generalizar la delegación", async (app, client) =>
        {
            await ConfigurationAdmin(database);
            await database.Sql("UPDATE unidades_responsables_poa SET num_empleado='0001' WHERE id_ur='A4'");
            await Login(app, client); await Csrf(client);
            Check((await Configure(client, await Preview(client, 1, [2,3,4], ["N"]))).IsSuccessStatusCode);
            await database.Sql("""
                UPDATE fixture_roles SET rol_clave='responsable_ur',rol_nombre='Responsable' WHERE email='persona@uacj.mx';
                UPDATE unidades_responsables_poa SET num_empleado='otro' WHERE id_ur IN ('A','A3','B','B3');
                """);
            var page = (await Json(await client.GetAsync("/inicio")))["props"]!;
            Check(page["formatos"]!.AsArray().Select(f => f!["id_ur"]!.ToString()).SequenceEqual(["A4"]));
            Check(!page["puedeColaboradores"]!.GetValue<bool>());
            Check((await client.GetAsync("/formatos/A4")).IsSuccessStatusCode);
            Check((await client.GetAsync("/colaboradores")).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Participación/enviado conserva instantánea tras exclusión y reactivación", async (app, client) =>
        {
            await ConfigurationAdmin(database); await Login(app, client); await Csrf(client);
            Check((await Save(client, 0, Complete(app))).IsSuccessStatusCode);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(Guid.NewGuid(), Guid.NewGuid(), 1))).IsSuccessStatusCode);
            var before = await database.Scalar("SELECT instantanea_envio::text FROM formatos_ur WHERE id_ur='A'");
            Check((await Configure(client, await Preview(client, 1, [3], ["N"]))).IsSuccessStatusCode);
            Check((await Configure(client, await Preview(client, 2, [2,3], ["N"]))).IsSuccessStatusCode);
            Check(!(await Live(client, Guid.NewGuid()))["editable"]!.GetValue<bool>());
            Check(Equals(before, await database.Scalar("SELECT instantanea_envio::text FROM formatos_ur WHERE id_ur='A'")));
        });
        Test("Participación/reservas y cambios concurrentes se serializan sin escrituras posteriores a exclusión", async (app, client) =>
        {
            await ConfigurationAdmin(database); await Login(app, client); await Csrf(client);
            var tab = Guid.NewGuid(); var lease = await Lease(client, tab);
            var preview = await Preview(client, 1, [3], ["N"]);
            var results = await Task.WhenAll(Configure(client, preview), Patch(client, Edit(tab, lease with { Value = Header("Carrera sintética") })));
            Check(results.All(r => r.IsSuccessStatusCode || r.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden));
            if (!results[0].IsSuccessStatusCode) Check((await Configure(client, await Preview(client, 1, [3], ["N"]))).IsSuccessStatusCode);
            var version = await database.Scalar("SELECT version FROM formatos_ur WHERE id_ur='A'");
            Check((await Patch(client, Edit(tab, lease with { Value = Header("Tardía") }))).StatusCode == HttpStatusCode.Forbidden);
            Check(Equals(version, await database.Scalar("SELECT version FROM formatos_ur WHERE id_ur='A'")));
        });
        Test("Presencia/color consistente entre observadores y pestañas; no confunde titularidad", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            var a = Guid.NewGuid(); var b = Guid.NewGuid(); await Lease(client, a); await Lease(client, b, "medios");
            var first = (await Live(client, a))["bloques"]!.AsArray(); var second = (await Live(client, b))["bloques"]!.AsArray();
            Check(first.Select(x => x!["reserva"]!["color"]!.GetValue<int>()).Distinct().Count() == 1);
            Check(first[0]!["reserva"]!["color"]!.ToString() == second[0]!["reserva"]!["color"]!.ToString());
            Check(first.Any(x => x!["reserva"]!["otraPestana"]!.GetValue<bool>()));
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(b, new BlockRequest("contexto", 0)))).StatusCode == HttpStatusCode.Conflict);
        });
        Test("Presencia/colisión de color coordinada entre identidades y sólo una reserva por bloque", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            await database.Sql("""
                INSERT INTO fixture_users VALUES(11,21,'color24@uacj.mx','Otra persona sintética','individual','0001','A','adscripcion');
                INSERT INTO fixture_roles VALUES(11,'color24@uacj.mx',30,'responsable_ur','Responsable');
                """);
            // Ambas identidades sintéticas prefieren el índice 1: PostgreSQL debe resolver la colisión.
            app.Microsoft.Email = "color24@uacj.mx"; app.Microsoft.Mail = "color24@uacj.mx"; app.Microsoft.Id = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
            using var other = app.Client(); await Login(app, other); await Csrf(other);
            var a = Guid.NewGuid(); var b = Guid.NewGuid();
            await Task.WhenAll(Lease(client, a), Lease(other, b, "medios"));
            var blocks = (await Live(client, a))["bloques"]!.AsArray();
            Check(blocks.Select(x => x!["reserva"]!["color"]!.GetValue<int>()).Distinct().Count() == 2);
            var race = await Task.WhenAll(client.PostAsJsonAsync("/formatos/A/reservas", Edit(a, new BlockRequest("preguntas:0", 0))),
                other.PostAsJsonAsync("/formatos/A/reservas", Edit(b, new BlockRequest("preguntas:0", 0))));
            Check(race.Count(r => r.IsSuccessStatusCode) == 1 && race.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1);
        });
    }
}
