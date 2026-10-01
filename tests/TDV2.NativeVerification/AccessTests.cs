using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Tdv2.Domain;
using Tdv2.Security;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    internal static Task SetupAccess(NativeDatabase database) => database.Sql("""
        UPDATE fixture_roles SET rol_id=34,rol_clave='administrador',rol_nombre='Administrador' WHERE email='persona@uacj.mx';
        INSERT INTO fixture_modules VALUES(52,47,'pruebas_acceso','Pruebas de acceso','/configuracion/pruebas-acceso',null,12,50);
        INSERT INTO fixture_module_roles VALUES(34,9),(34,50),(34,52);
        INSERT INTO fixture_users VALUES(11,21,'encargada@uacj.mx','Encargada sintética','individual','0001','A','adscripcion'),
          (12,22,'ajena@uacj.mx','Persona ajena sintética','individual','0003','B','adscripcion');
        INSERT INTO fixture_roles VALUES(11,'encargada@uacj.mx',30,'responsable_ur','Responsable'),(12,'ajena@uacj.mx',30,'responsable_ur','Responsable');
        INSERT INTO fixture_delegation VALUES('encargada@uacj.mx','A','0001',2);
        INSERT INTO fixture_capability VALUES('persona@uacj.mx',true,true,'rama_ur','A',30);
        """);
    private static Task<HttpResponseMessage> AddCollaborator(HttpClient client, string kind = "local", string unit = "A", string email = "colaboradora@uacj.mx", string origin = "A4") =>
        client.PostAsJsonAsync("/colaboradores", new { ur = unit, email, id_ur = origin, tipo = kind, rol_id = 999, actor_email = "forjado@uacj.mx", num_empleado = "inventado", id_ur_alcance = "B" });
    private static async Task<long> LocalId(NativeDatabase database) => Convert.ToInt64(await database.Scalar("SELECT id FROM colaboraciones_ur ORDER BY id DESC LIMIT 1"));
    private static Task<HttpResponseMessage> StartRepresentation(HttpClient client, bool write = true, string email = "encargada@uacj.mx") =>
        client.PostAsJsonAsync("/actuar-como-usuario", new { email, motivo = "Verificación sintética de permisos", escritura = write, actor_email = "forjado@uacj.mx", duracion_minutos = 9000 });
    private static async Task<string> Context(HttpClient client)
    {
        var page = await Json(await client.GetAsync("/inicio")); var key = page["props"]!["contextoEdicion"]!.GetValue<string>();
        client.DefaultRequestHeaders.Remove("X-TDV2-Context"); client.DefaultRequestHeaders.Add("X-TDV2-Context", key); return key;
    }
    private static Task<HttpResponseMessage> StartPreview(HttpClient client, string role = "local", string unit = "A4") =>
        client.PostAsJsonAsync("/vista-prueba", new { mode = "escenario", role, ur = unit });
    private static async Task ExpireLocalContext(NativeDatabase database, NativeApplication app)
    {
        var crypto = app.Services.GetRequiredService<ProtectedValues>();
        var encrypted = (string)(await database.Scalar("SELECT selection FROM tdv2_access_contexts WHERE selection IS NOT NULL LIMIT 1"))!;
        var selection = JsonSerializer.Deserialize<AccessSelection>(crypto.Unprotect("access-context", encrypted))!;
        await database.Sql("UPDATE tdv2_access_contexts SET selection=$1", crypto.Protect("access-context", JsonSerializer.Serialize(selection with { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) })));
    }
    internal const string AuditFailureTrigger = """
        CREATE FUNCTION fixture_fail_audit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'SYNTHETIC_AUDIT_FAILURE_SECRET'; END $$;
        CREATE TRIGGER fail_audit BEFORE INSERT ON activity_logs FOR EACH ROW EXECUTE FUNCTION fixture_fail_audit();
        """;
    internal const string RemoveAuditFailure = "DROP TRIGGER fail_audit ON activity_logs; DROP FUNCTION fixture_fail_audit()";
    private static void RegisterAccessCases(NativeDatabase database, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Delegación SQL: búsqueda, vacío y alta local usa formato nivel 3 e identidad publicada", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            var found = await Json(await client.GetAsync("/colaboradores/personas?ur=A&q=colaboradora"));
            Check(found["personas"]!.AsArray().Count == 1 && found["personas"]![0]!["formato"]!.GetValue<string>() == "Área sintética A3");
            Check((await Json(await client.GetAsync("/colaboradores/personas?ur=A&q=nadie")))["personas"]!.AsArray().Count == 0);
            Check((await AddCollaborator(client)).StatusCode == HttpStatusCode.Created);
            Check((await database.Scalar("SELECT id_ur_alcance FROM colaboraciones_ur"))!.ToString() == "A3");
            Check((await database.Scalar("SELECT num_empleado FROM colaboraciones_ur"))!.ToString() == "0050");
            Check((await database.Scalar("SELECT otorgado_por FROM colaboraciones_ur"))!.ToString() == "persona@uacj.mx");
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM formatos_ur")) == 0);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM activity_logs WHERE entity='colaboracion' AND action='crear'")) == 1);
        });
        Test("Delegación: colaborador local subordinado edita sólo formato compartido, nunca nivel 4", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); Check((await AddCollaborator(client)).IsSuccessStatusCode);
            using var collaborator = app.Client(); app.Microsoft.Mail = app.Microsoft.Email = "colaboradora@uacj.mx"; app.Microsoft.Id = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
            await Login(app, collaborator); await Csrf(collaborator);
            var page = await Json(await collaborator.GetAsync("/formatos/A3")); var content = page["props"]!["contenido"]!;
            Check((await collaborator.PutAsJsonAsync("/formatos/A3", new { version = 0, contenido = content })).IsSuccessStatusCode);
            foreach (var unit in new[] { "A", "A4", "B" })
            {
                Check((await collaborator.GetAsync("/formatos/" + unit)).StatusCode == HttpStatusCode.Forbidden);
                Check((await collaborator.PutAsJsonAsync("/formatos/" + unit, new { version = 0, contenido = content })).StatusCode == HttpStatusCode.Forbidden);
            }
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM formatos_ur WHERE id_ur='A4'")) == 0);
        });
        Test("Delegación: áreas dependientes amplían sólo dentro de rama otorgante", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); Check((await AddCollaborator(client, "dependencias")).IsSuccessStatusCode);
            using var collaborator = app.Client(); app.Microsoft.Mail = app.Microsoft.Email = "colaboradora@uacj.mx"; app.Microsoft.Id = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
            await Login(app, collaborator); await Csrf(collaborator);
            foreach (var unit in new[] { "A", "A3" })
            {
                var content = (await Json(await collaborator.GetAsync("/formatos/" + unit)))["props"]!["contenido"]!;
                Check((await collaborator.PutAsJsonAsync("/formatos/" + unit, new { version = 0, contenido = content })).IsSuccessStatusCode);
            }
            Check((await collaborator.GetAsync("/formatos/B")).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Delegación: administrador sin publicación o encargado coincidente no ejecuta funciones", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client);
            await database.Sql("DELETE FROM fixture_delegation WHERE email='persona@uacj.mx'");
            Check((await client.GetAsync("/colaboradores")).IsSuccessStatusCode);
            Check((await AddCollaborator(client)).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("INSERT INTO fixture_delegation VALUES('persona@uacj.mx','A','0001',2); UPDATE fixture_users SET num_empleado='0999' WHERE email='persona@uacj.mx'");
            Check((await client.GetAsync("/colaboradores/personas?ur=A&q=colaboradora")).StatusCode == HttpStatusCode.Forbidden);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_calls")) == 0);
        });
        Test("Delegación: roles no asignables, rama ajena y nivel 3 se rechazan directamente", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            Check((await AddCollaborator(client, unit: "B", origin: "B4", email: "ajena@uacj.mx")).StatusCode == HttpStatusCode.Forbidden);
            Check((await AddCollaborator(client, origin: "B4")).StatusCode == HttpStatusCode.Forbidden);
            Check((await AddCollaborator(client, unit: "A3")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("DELETE FROM fixture_delegation_roles WHERE clave='colaborador_local'");
            Check((await AddCollaborator(client)).StatusCode == HttpStatusCode.Forbidden);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_calls")) == 0);
        });
        Test("Delegación: administrador combinado no delega en otra responsabilidad", async (app, client) =>
        {
            await SetupAccess(database);
            await database.Sql("INSERT INTO fixture_roles VALUES(10,'persona@uacj.mx',30,'responsable_ur','Responsable'); UPDATE unidades_responsables_poa SET num_empleado='0001' WHERE id_ur='B'; INSERT INTO fixture_delegation VALUES('persona@uacj.mx','B','0001',2)");
            await Login(app, client); await Csrf(client);
            Check((await AddCollaborator(client, unit: "B", origin: "B4", email: "ajena@uacj.mx")).StatusCode == HttpStatusCode.Forbidden);
        });
        Test("Delegación: Nexo rechaza persona/origen forjado y distingue error SQL de vacío", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            var rejected = await AddCollaborator(client, email: "ajena@uacj.mx"); Check((int)rejected.StatusCode == 422);
            Check(!(await rejected.Content.ReadAsStringAsync()).Contains("SYNTHETIC"));
            await database.Sql("INSERT INTO fixture_faults VALUES('buscar_personas','08006')");
            Check((await client.GetAsync("/colaboradores/personas?ur=A&q=nadie")).StatusCode == HttpStatusCode.ServiceUnavailable);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM colaboraciones_ur")) == 0);
        });
        Test("Delegación: identidad no publicada después del alta no crea vínculo; reintento converge", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await database.Sql("INSERT INTO fixture_faults VALUES('omit_identity','P0001')");
            Check((await AddCollaborator(client)).StatusCode == HttpStatusCode.ServiceUnavailable);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM colaboraciones_ur")) == 0);
            await database.Sql("DELETE FROM fixture_faults");
            Check((await AddCollaborator(client)).StatusCode == HttpStatusCode.Created);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_grants")) == 1);
        });
        Test("Delegación: dos altas concurrentes conservan un vínculo y concesión", async (app, client) =>
        {
            await Login(app, client); await Csrf(client);
            var result = await Task.WhenAll(AddCollaborator(client), AddCollaborator(client)); Check(result.All(r => r.StatusCode == HttpStatusCode.Created));
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM colaboraciones_ur")) == 1);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_grants")) == 1);
        });
        Test("Delegación: retiro local persiste ante falla central y reintento confirma auditoría", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await AddCollaborator(client); var id = await LocalId(database);
            await database.Sql("INSERT INTO fixture_faults VALUES('retirar_acceso','08006')");
            Check((await client.DeleteAsync("/colaboradores/" + id)).StatusCode == HttpStatusCode.Accepted);
            Check((bool)(await database.Scalar("SELECT revocada_en IS NOT NULL AND retiro_central_pendiente FROM colaboraciones_ur"))!);
            await database.Sql("DELETE FROM fixture_faults");
            Check((await client.DeleteAsync("/colaboradores/" + id)).IsSuccessStatusCode);
            Check(!(bool)(await database.Scalar("SELECT retiro_central_pendiente FROM colaboraciones_ur"))!);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_grants")) == 0);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM activity_logs WHERE action='retirar' AND meta->>'resultado'='confirmado'")) == 1);
        });
        Test("Auditoría: falla revierte alta local y revocación; nunca escribe sin bitácora", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await database.Sql(AuditFailureTrigger);
            try
            {
                Check((await AddCollaborator(client)).StatusCode == HttpStatusCode.ServiceUnavailable);
                Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM colaboraciones_ur")) == 0);
            }
            finally { await database.Sql(RemoveAuditFailure); }
            await AddCollaborator(client); var id = await LocalId(database); await database.Sql(AuditFailureTrigger);
            try
            {
                Check((await client.DeleteAsync("/colaboradores/" + id)).StatusCode == HttpStatusCode.ServiceUnavailable);
                Check((bool)(await database.Scalar("SELECT revocada_en IS NULL FROM colaboraciones_ur"))!);
            }
            finally { await database.Sql(RemoveAuditFailure); }
        });
        Test("Representación: administrador sin capacidad no busca ni inicia; sin escritura no la obtiene", async (app, client) =>
        {
            await SetupAccess(database); await database.Sql("UPDATE fixture_capability SET permitido=false"); await Login(app, client); await Csrf(client);
            Check((await client.GetAsync("/actuar-como-usuario/personas?q=encargada")).StatusCode == HttpStatusCode.Forbidden);
            Check((await StartRepresentation(client)).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("UPDATE fixture_capability SET permitido=true,escritura=false");
            Check((await StartRepresentation(client)).StatusCode == HttpStatusCode.Forbidden);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_representation")) == 0);
        });
        Test("Representación: búsqueda autorizada vacía/permitida y objetivo fuera de alcance", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client);
            Check((await Json(await client.GetAsync("/actuar-como-usuario/personas?q=encargada")))["personas"]!.AsArray().Count == 1);
            Check((await Json(await client.GetAsync("/actuar-como-usuario/personas?q=ajena")))["personas"]!.AsArray().Count == 0);
            Check((int)(await StartRepresentation(client, email: "ajena@uacj.mx")).StatusCode == 422);
            await database.Sql("INSERT INTO fixture_faults VALUES('representacion','08006')");
            Check((await client.GetAsync("/actuar-como-usuario/personas?q=encargada")).StatusCode == HttpStatusCode.ServiceUnavailable);
        });
        Test("Representación: identidad real intacta, token sólo cifrado y permisos efectivos", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); Check((await StartRepresentation(client)).IsSuccessStatusCode);
            var page = await Json(await client.GetAsync("/inicio")); var props = page["props"]!;
            Check(props["auth"]!["user"]!["email"]!.GetValue<string>() == "persona@uacj.mx" && props["representacion"]!["email"]!.GetValue<string>() == "encargada@uacj.mx");
            Check(!props["administrador"]!.GetValue<bool>() && props["auth"]!["roles"]![0]!["key"]!.GetValue<string>() == "responsable_ur");
            var secret = (await database.Scalar("SELECT token FROM fixture_representation"))!.ToString()!;
            Check(!page.ToJsonString().Contains(secret) && !page.ToJsonString().Contains("\"token\""));
            Check((await database.Scalar("SELECT selection FROM tdv2_access_contexts"))!.ToString()!.StartsWith("aspnet:v1:"));
            Check(!(await database.Scalar("SELECT string_agg(meta::text,'') FROM activity_logs"))!.ToString()!.Contains(secret));
        });
        Test("Representación: escritura y auditoría atómicas respetan alcance y actor real", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await StartRepresentation(client); await Context(client);
            var form = await Form(client); var content = form["contenido"]!;
            Check((await Save(client, 0, content)).IsSuccessStatusCode);
            Check((await client.PutAsJsonAsync("/formatos/A3", new { version = 0, contenido = content })).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.PutAsJsonAsync("/formatos/B", new { version = 0, contenido = content })).StatusCode == HttpStatusCode.Forbidden);
            Check((await database.Scalar("SELECT actualizado_por FROM formatos_ur"))!.ToString() == "persona@uacj.mx");
            Check((await database.Scalar("SELECT meta->>'representado_email' FROM activity_logs WHERE action='guardar'"))!.ToString() == "encargada@uacj.mx");
            await database.Sql(AuditFailureTrigger);
            try { Check((await Save(client, 1, content)).StatusCode == HttpStatusCode.ServiceUnavailable); Check(Convert.ToInt32(await database.Scalar("SELECT version FROM formatos_ur")) == 1); }
            finally { await database.Sql(RemoveAuditFailure); }
        });
        Test("Representación: sólo lectura rechaza endpoint directo y conserva permiso teórico visible", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await StartRepresentation(client, false); await Context(client);
            var form = await Form(client); Check(!form["editable"]!.GetValue<bool>() && form["permisoEdicion"]!.GetValue<bool>());
            Check((await Save(client, 0, form["contenido"]!)).StatusCode == HttpStatusCode.Conflict);
            Check((await AddCollaborator(client)).StatusCode == HttpStatusCode.Conflict);
        });
        Test("Representación: pestaña obsoleta y contextos anidados no ejecutan acciones centrales", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); var content = (await Form(client))["contenido"]!;
            await StartRepresentation(client); var before = Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_calls WHERE operation='validar'"));
            Check((await Save(client, 0, content)).StatusCode == HttpStatusCode.Conflict);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_calls WHERE operation='validar'")) == before);
            await Context(client); Check((await StartRepresentation(client)).StatusCode == HttpStatusCode.Conflict);
            Check((await StartPreview(client)).StatusCode == HttpStatusCode.Conflict);
            Check((await client.GetAsync("/configuracion")).StatusCode == HttpStatusCode.Conflict);
        });
        Test("Representación: dos inicios concurrentes no sustituyen el contexto ganador", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client);
            var starts = await Task.WhenAll(StartRepresentation(client), StartRepresentation(client));
            Check(starts.Count(r => r.IsSuccessStatusCode) == 1 && starts.Count(r => r.StatusCode == HttpStatusCode.Conflict) == 1);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_calls WHERE operation='iniciar'")) == 1);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_access_contexts WHERE selection IS NOT NULL")) == 1);
        });
        Test("Auditoría: rechazo CSRF identifica representación y no modifica datos", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await StartRepresentation(client); await Context(client);
            var content = (await Form(client))["contenido"]!; client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            Check((int)(await Save(client, 0, content)).StatusCode == 419);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM formatos_ur")) == 0);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM activity_logs WHERE user_email='persona@uacj.mx' AND meta->>'representado_email'='encargada@uacj.mx' AND meta->>'resultado'='rechazado' AND meta->'detalles'->>'status'='419'")) == 1);
        });
        Test("Logout representado: revocación y auditoría atómicas incluso con Nexo caído", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await StartRepresentation(client); await Context(client);
            await database.Sql(AuditFailureTrigger);
            try
            {
                Check((await client.PostAsJsonAsync("/logout", new { })).StatusCode == HttpStatusCode.ServiceUnavailable);
                Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_sessions")) == 1);
                Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_access_contexts WHERE selection IS NOT NULL")) == 1);
            }
            finally { await database.Sql(RemoveAuditFailure); }
            await database.Sql("INSERT INTO fixture_faults VALUES('representacion','08006')");
            Check((await client.PostAsJsonAsync("/logout", new { })).IsSuccessStatusCode);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_sessions")) == 0);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_access_contexts")) == 0);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM activity_logs WHERE action='logout' AND user_email='persona@uacj.mx' AND meta->>'representado_email'='encargada@uacj.mx' AND meta->'detalles'->>'cierre_central'='false'")) == 1);
        });
        Test("Representación: delegación utiliza transporte JSON con actor real y token servidor", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await StartRepresentation(client); await Context(client);
            Check((await client.GetAsync("/colaboradores/personas?ur=A&q=colaboradora")).IsSuccessStatusCode);
            Check((await AddCollaborator(client)).StatusCode == HttpStatusCode.Created);
            Check((await database.Scalar("SELECT otorgado_por FROM colaboraciones_ur"))!.ToString() == "persona@uacj.mx");
            Check((await client.DeleteAsync("/colaboradores/" + await LocalId(database))).IsSuccessStatusCode);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_calls WHERE transport='representation' AND actor='persona@uacj.mx' AND operation IN ('buscar_personas','conceder_acceso','retirar_acceso')")) == 3);
        });
        Test("Representación: revocación/vencimiento central bloquean sin restaurar administrador", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await StartRepresentation(client); await Context(client);
            await database.Sql("UPDATE fixture_representation SET revoked=true");
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Conflict);
            await database.Sql("UPDATE fixture_representation SET revoked=false,expira_en=now()-interval '1 minute'");
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Conflict);
            Check((bool)(await database.Scalar("SELECT selection IS NOT NULL FROM tdv2_access_contexts"))!);
        });
        Test("Representación: vencimiento local se verifica antes de llamar Nexo", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await StartRepresentation(client); await ExpireLocalContext(database, app);
            var before = Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_calls WHERE operation='validar'"));
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Conflict);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM fixture_calls WHERE operation='validar'")) == before);
        });
        Test("Representación: salida con Nexo caído restaura identidad propia explícitamente y audita", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await StartRepresentation(client); var key = await Context(client);
            await database.Sql("INSERT INTO fixture_faults VALUES('representacion','08006')");
            var blocked = await client.GetAsync("/inicio"); Check(blocked.StatusCode == HttpStatusCode.ServiceUnavailable);
            Check((await Json(blocked))["representacion"]!["id"]!.GetValue<string>() == key);
            Check((await client.DeleteAsync("/actuar-como-usuario")).IsSuccessStatusCode);
            await Context(client); Check((await Json(await client.GetAsync("/inicio")))["props"]!["administrador"]!.GetValue<bool>());
            Check((await database.Scalar("SELECT meta->'detalles'->>'cierre_central' FROM activity_logs WHERE action='finalizar'"))!.ToString() == "false");
        });
        Test("Representación: revocación de módulo actor o persona impide continuidad", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await StartRepresentation(client); await Context(client);
            await database.Sql("DELETE FROM fixture_module_roles WHERE rol_id=30");
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("INSERT INTO fixture_module_roles VALUES(30,9); DELETE FROM fixture_module_roles WHERE rol_id=34 AND modulo_id=52");
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.DeleteAsync("/actuar-como-usuario")).IsSuccessStatusCode);
        });
        Test("Auditoría: falla al iniciar representación revierte contexto y compensa Nexo", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await database.Sql(AuditFailureTrigger);
            try
            {
                Check((await StartRepresentation(client)).StatusCode == HttpStatusCode.ServiceUnavailable);
                Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_access_contexts WHERE selection IS NOT NULL")) == 0);
                Check((bool)(await database.Scalar("SELECT revoked FROM fixture_representation"))!);
            }
            finally { await database.Sql(RemoveAuditFailure); }
        });
        Test("Vista de prueba: sólo escenario y áreas válidas, sin modo usuario", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client);
            Check((int)(await client.PostAsJsonAsync("/vista-prueba", new { mode = "usuario", email = "encargada@uacj.mx" })).StatusCode == 422);
            Check((int)(await StartPreview(client, "responsable", "A4")).StatusCode == 422);
            Check((int)(await StartPreview(client, "superadmin", "A")).StatusCode == 422);
            Check((await StartPreview(client)).IsSuccessStatusCode);
            var page = (await Json(await client.GetAsync("/inicio")))["props"]!;
            Check(page["formatos"]!.AsArray().Count == 1 && page["formatos"]![0]!["id_ur"]!.GetValue<string>() == "A3");
            Check(!page["formatos"]![0]!["editable"]!.GetValue<bool>() && page["representacion"] is null && page["simulacion"] is not null);
        });
        Test("Vista de prueba: mutaciones/búsquedas/configuración bloqueadas y salida auditada", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); var form = await Form(client); await StartPreview(client, "administrador", "A");
            Check((await Save(client, 0, form["contenido"]!)).StatusCode == HttpStatusCode.Forbidden);
            Check((await AddCollaborator(client)).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.DeleteAsync("/colaboradores/1")).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.GetAsync("/colaboradores/personas?ur=A&q=persona")).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.GetAsync("/configuracion")).StatusCode == HttpStatusCode.Conflict);
            Check((await client.DeleteAsync("/vista-prueba")).IsSuccessStatusCode);
            Check((await Save(client, 0, form["contenido"]!)).IsSuccessStatusCode);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM activity_logs WHERE entity='vista_prueba'")) == 2);
        });
        Test("Vista de prueba: caducidad/revocación no restauran escritura y salida no depende de Nexo", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); await StartPreview(client); await ExpireLocalContext(database, app);
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Conflict);
            Check((await AddCollaborator(client)).StatusCode == HttpStatusCode.Forbidden);
            await database.Sql("REVOKE SELECT ON nexo_usuarios FROM tdv2_native_nexo");
            Check((await client.DeleteAsync("/vista-prueba")).IsSuccessStatusCode);
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.ServiceUnavailable);
        });
    }
}
