using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Tdv2.Services;

namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    internal static async Task CentralCollaborator(NativeDatabase db, string role = "colaborador_local")
    {
        await db.Sql("UPDATE fixture_roles SET rol_id=31,rol_clave=$1,rol_nombre='Colaboración central sintética' WHERE email='persona@uacj.mx'", role);
        await db.Sql("INSERT INTO fixture_grants VALUES(101,'persona@uacj.mx',31,'A4','central')");
        // El escenario puede ejecutarse después de una prueba que retiró todos los módulos.
        // Publicar el módulo es parte del fixture central, igual que en la configuración real requerida.
        await db.Sql("INSERT INTO fixture_module_roles SELECT 31,9 WHERE NOT EXISTS(SELECT 1 FROM fixture_module_roles WHERE rol_id=31 AND modulo_id=9)");
    }
    private static async Task<string[]> VisibleForms(HttpClient client)
    {
        var response = await client.GetAsync("/inicio"); Check(response.IsSuccessStatusCode, "Listado: HTTP " + (int)response.StatusCode);
        return (await Json(response))["props"]!["formatos"]!.AsArray().Select(r => r!["id_ur"]!.ToString()).Order().ToArray();
    }
    private static void RegisterCentralCollaborationCases(NativeDatabase database, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Central/local sin vínculo en nivel 2, 3 e inferior; reserva, guardado y eliminación", async (app, client) =>
        {
            await CentralCollaborator(database); await Login(app, client); await Csrf(client);
            foreach (var (origin, unit) in new[] { ("A", "A"), ("A3", "A3"), ("A4", "A3") })
            {
                await database.Sql("UPDATE fixture_users SET \"ID_UR\"=$1", origin);
                Check((await VisibleForms(client)).SequenceEqual(new[] { unit }));
                var tab = Guid.NewGuid(); var live = await Live(client, tab, unit);
                Check(live["editable"]!.GetValue<bool>() && !live["puedeEnviar"]!.GetValue<bool>());
                var version = live["bloques"]!.AsArray().SingleOrDefault(b => b!["key"]!.ToString() == "contexto")?["version"]?.GetValue<int>() ?? 0;
                var lease = await Lease(client, tab, version: version, unit: unit);
                Check((await Patch(client, Edit(tab, lease with { Value = Header(origin) }) with { Release = true }, unit)).IsSuccessStatusCode);
            }
            var deleteTab = Guid.NewGuid(); var removal = await RemovalRequest(client, deleteTab, new("identificacion", "inicial"), "A3");
            Check((await Patch(client, removal, "A3")).IsSuccessStatusCode);
            Check((await Live(client, deleteTab, "A3"))["contenido"]!["identificacion"]!.AsArray().Count == 0);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM colaboraciones_ur")) == 0);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM activity_logs WHERE action='guardar_bloques'")) > 0);
        });
        Test("Central/dependencias limita rama y todos los endpoints rechazan UR ajena y nivel 4", async (app, client) =>
        {
            await CentralCollaborator(database, "colaborador_dependencias"); await Login(app, client); await Csrf(client);
            Check((await VisibleForms(client)).SequenceEqual(new[] { "A", "A3" }));
            foreach (var unit in new[] { "A", "A3" })
            {
                var tab = Guid.NewGuid(); var lease = await Lease(client, tab, unit: unit);
                Check((await Patch(client, Edit(tab, lease with { Value = Header("Permitida") }) with { Release = true }, unit)).IsSuccessStatusCode);
                Check((await client.PostAsJsonAsync($"/formatos/{unit}/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).StatusCode == HttpStatusCode.Forbidden);
            }
            foreach (var unit in new[] { "B", "A4", "100" })
            {
                Check((await client.GetAsync($"/formatos/{unit}")).StatusCode == HttpStatusCode.Forbidden);
                Check((await client.GetAsync($"/formatos/{unit}/estado?tab={Guid.NewGuid()}")).StatusCode == HttpStatusCode.Forbidden);
                Check((await client.PostAsJsonAsync($"/formatos/{unit}/reservas", Edit(Guid.NewGuid(), new BlockRequest("contexto", 0)))).StatusCode == HttpStatusCode.Forbidden);
                Check((await Patch(client, Edit(Guid.NewGuid(), new BlockRequest("identificacion:inicial", 0, Guid.NewGuid(), null)), unit)).StatusCode == HttpStatusCode.Forbidden);
            }
            foreach (var path in new[] { "/colaboradores", "/configuracion", "/configuracion/pruebas-acceso" })
                Check((await client.GetAsync(path)).StatusCode == HttpStatusCode.Forbidden);
            Check((await StartRepresentation(client)).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Central/traslado revoca edición anterior; retiro no cae a rol efectivo ni concesión delegada", async (app, client) =>
        {
            await CentralCollaborator(database); await Login(app, client); await Csrf(client);
            var tab = Guid.NewGuid(); var lease = await Lease(client, tab, unit: "A3");
            await database.Sql("UPDATE fixture_users SET \"ID_UR\"='B'");
            Check((await VisibleForms(client)).SequenceEqual(new[] { "B" }));
            Check((await Patch(client, Edit(tab, lease with { Value = Header("Traslado") }), "A3")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("DELETE FROM fixture_grants; INSERT INTO fixture_grants VALUES(102,'persona@uacj.mx',31,'B','aplicacion')");
            Check((await VisibleForms(client)).Length == 0);
            Check((await client.GetAsync("/formatos/B")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("UPDATE fixture_grants SET origen='central'"); Check((await VisibleForms(client)).SequenceEqual(new[] { "B" }));
            await database.Sql("DELETE FROM fixture_roles"); Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Central/cuenta suspendida, módulo retirado o concesiones no disponibles deniegan", async (app, client) =>
        {
            await CentralCollaborator(database); await Login(app, client); await Csrf(client);
            var tab = Guid.NewGuid(); var lease = await Lease(client, tab, unit: "A3");
            await database.Sql("DELETE FROM fixture_module_roles WHERE rol_id=31");
            Check((await Patch(client, Edit(tab, lease with { Value = Header("Sin módulo") }), "A3")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("INSERT INTO fixture_module_roles VALUES(31,9); REVOKE SELECT ON nexo_concesiones FROM tdv2_native_nexo");
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.ServiceUnavailable);
            await database.Sql("GRANT SELECT ON nexo_concesiones TO tdv2_native_nexo; DELETE FROM fixture_users"); // Publicación sin acceso efectivo.
            Check((await Patch(client, Edit(tab, lease with { Value = Header("Suspendida") }), "A3")).StatusCode == HttpStatusCode.Forbidden);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM formato_operaciones")) == 0);
        });
        Test("Central/concesión sin rol no habilita; formato enviado continúa inmutable", async (app, client) =>
        {
            await SeedEditing(database, Complete(app)); await Login(app, client); await Csrf(client);
            var tab = Guid.NewGuid(); Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).IsSuccessStatusCode);
            var before = await database.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f WHERE id_ur='A'");
            await CentralCollaborator(database, "colaborador_dependencias");
            await database.Sql("INSERT INTO fixture_grants VALUES(102,'persona@uacj.mx',NULL,'A4','central')");
            Check((await VisibleForms(client)).SequenceEqual(new[] { "A", "A3" }));
            Check(!(await Live(client, tab))["editable"]!.GetValue<bool>());
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(tab, new BlockRequest("identificacion:inicial", 1)))).StatusCode == HttpStatusCode.Conflict);
            Check((await Patch(client, Edit(tab, new BlockRequest("identificacion:inicial", 1, Guid.NewGuid(), null)))).StatusCode == HttpStatusCode.Conflict);
            Check(Equals(before, await database.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f WHERE id_ur='A'")));
            await database.Sql("DELETE FROM fixture_grants WHERE rol_id IS NOT NULL"); Check((await VisibleForms(client)).Length == 0);
        });
        Test("Central/identidad y jerarquía inválidas explican vacío; tipo N auxiliar y tipo 0 elegible", async (app, client) =>
        {
            await CentralCollaborator(database); await Login(app, client);
            foreach (var sql in new[] { "UPDATE fixture_users SET num_empleado=NULL", "UPDATE fixture_users SET num_empleado='0001',\"ID_UR\"='110'", "UPDATE fixture_users SET \"ID_UR\"='A4'; UPDATE unidades_responsables_poa SET id_ur_pertenece='missing' WHERE id_ur='A4'" })
            {
                await database.Sql(sql); Check((await VisibleForms(client)).Length == 0);
                Check(!string.IsNullOrWhiteSpace((await Json(await client.GetAsync("/inicio")))["props"]!["sinFormatosMotivo"]!.ToString()));
            }
            await database.Sql("UPDATE unidades_responsables_poa SET id_ur_pertenece='A3' WHERE id_ur='A4'; UPDATE unidades_responsables_poa SET tipo_ur=' n ' WHERE id_ur='A3'; UPDATE unidades_responsables_poa SET tipo_ur='0',cve_ur='06000' WHERE id_ur='A'");
            Check((await VisibleForms(client)).SequenceEqual(new[] { "A" }));
            Check((await client.GetAsync("/formatos/A3")).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Central/revocación local pendiente no se transforma en central; ambas vías independientes", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); Check((await AddCollaborator(client, "dependencias")).IsSuccessStatusCode);
            using var collaborator = app.Client(); app.Microsoft.Mail = app.Microsoft.Email = "colaboradora@uacj.mx"; app.Microsoft.Id = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
            await Login(app, collaborator); await Csrf(collaborator);
            await database.Sql("INSERT INTO fixture_grants VALUES(101,'colaboradora@uacj.mx',31,'A4','central'); INSERT INTO fixture_roles SELECT usuario_aplicacion_id,email,31,'colaborador_local','Local' FROM fixture_users WHERE email='colaboradora@uacj.mx'");
            Check((await VisibleForms(collaborator)).SequenceEqual(new[] { "A", "A3" }));
            await database.Sql("INSERT INTO fixture_faults VALUES('retirar_acceso','08006')");
            await client.DeleteAsync("/colaboradores/" + await LocalId(database));
            Check((bool)(await database.Scalar("SELECT revocada_en IS NOT NULL AND retiro_central_pendiente FROM colaboraciones_ur"))!);
            Check((await VisibleForms(collaborator)).SequenceEqual(new[] { "A3" })); // Únicamente la central local independiente.
            await database.Sql("DELETE FROM fixture_grants WHERE origen='central'");
            Check((await VisibleForms(collaborator)).Length == 0);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_grants WHERE origen='aplicacion'")) == 1);
            await database.Sql("UPDATE colaboraciones_ur SET revocada_en=NULL,retiro_central_pendiente=false"); // Reactivación sólo en fixture.
            Check((await VisibleForms(collaborator)).SequenceEqual(new[] { "A", "A3" }));
        });
        Test("Central/representación usa sólo concesiones del representado; preview permanece lectura", async (app, client) =>
        {
            await SetupAccess(database);
            await database.Sql("UPDATE fixture_roles SET rol_id=31,rol_clave='colaborador_local' WHERE email='encargada@uacj.mx'; UPDATE fixture_users SET \"ID_UR\"='A4' WHERE email='encargada@uacj.mx'; INSERT INTO fixture_grants VALUES(101,'encargada@uacj.mx',31,'A4','central')");
            await Login(app, client); await Csrf(client); Check((await StartRepresentation(client)).IsSuccessStatusCode); await Context(client);
            Check((await VisibleForms(client)).SequenceEqual(new[] { "A3" }));
            var tab = Guid.NewGuid(); var lease = await Lease(client, tab, unit: "A3");
            Check((await Patch(client, Edit(tab, lease with { Value = Header("Representada") }) with { Release = true }, "A3")).IsSuccessStatusCode);
            Check((await client.GetAsync("/formatos/A")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("DELETE FROM fixture_grants"); Check((await VisibleForms(client)).Length == 0);
            Check((await client.DeleteAsync("/actuar-como-usuario")).IsSuccessStatusCode); await Context(client);
            Check((await StartPreview(client)).IsSuccessStatusCode); await Context(client);
            Check((await VisibleForms(client)).SequenceEqual(new[] { "A3" }));
            Check((await client.PostAsJsonAsync("/formatos/A3/reservas", Edit(tab, new BlockRequest("contexto", 1)))).StatusCode == HttpStatusCode.Forbidden);
        });
    }
}
