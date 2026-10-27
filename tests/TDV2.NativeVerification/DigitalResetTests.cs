using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Npgsql;
using Tdv2.Competencias;

namespace Tdv2.NativeVerification;

internal sealed partial class DigitalTests
{
    private static Task<JsonObject> Prepare(HttpClient client, bool targets = false) => PrepareCore(client, targets);
    private static async Task<JsonObject> PrepareCore(HttpClient client, bool targets) =>
        await Json(await client.PostAsJsonAsync(Base + "/api/reinicio/resumen", new { includeTargets = targets }));
    private static Task<HttpResponseMessage> Confirm(HttpClient client, JsonObject summary, string phrase = "REINICIAR ENCUESTAS") =>
        client.PostAsJsonAsync(Base + "/api/reinicio", new { operation = summary["operation"]!.ToString(), confirmation = phrase });

    private void AddResetTests(List<(string Name, Func<NativeApplication, HttpClient, Task> Work)> cases)
    {
        void Test(string name, Func<NativeApplication, HttpClient, Task> work) => cases.Add(("Reinicio: " + name, work));
        Test("roles, representación y CSRF no pueden ejecutar la excepción", async (app, client) =>
        {
            await Login(app, client);
            foreach (var role in new[] { "participante", "responsable_ur", "responsable_ur_supervisor" })
            {
                await Role(role); await Status(client.PostAsJsonAsync(Base + "/api/reinicio/resumen", new { includeTargets = false }), 403);
                await Status(client.PostAsJsonAsync(Base + "/api/reinicio", new { operation = Guid.NewGuid(), confirmation = "REINICIAR ENCUESTAS" }), 403);
            }
            await Role("administrador"); var preview = await Prepare(client);
            await Status(Confirm(client, preview, "reiniciar encuestas"), 422);
            using (var noCsrf = app.Client()) { await Login(app, noCsrf); noCsrf.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); await Status(Confirm(noCsrf, preview), 419); }
            await Status(client.PostAsJsonAsync(Base + "/api/representacion", new { email = "otra@uacj.mx", motivo = "Verificación de reinicio prohibido" }), 200);
            var page = await Json(await client.GetAsync(Base)); client.DefaultRequestHeaders.Add("X-TDV2-Context", page["props"]!["contextoEdicion"]!.ToString());
            await Status(Confirm(client, preview), 403);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_reset_operations WHERE \"CompletedAt\" IS NOT NULL")) == 0);
        });
        Test("resumen cambiado, vencido o de otro administrador exige revisar", async (app, client) =>
        {
            await Role("administrador"); await Login(app, client); var preview = await Prepare(client);
            await Accept(client); await Status(Confirm(client, preview), 409);
            preview = await Prepare(client); await Data("UPDATE cd_reset_operations SET \"ExpiresAt\"=now()-interval '1 minute' WHERE \"Id\"=$1", Guid.Parse(preview["operation"]!.ToString()));
            await Status(Confirm(client, preview), 409);
            preview = await Prepare(client); await Data("UPDATE cd_reset_operations SET \"Actor\"='otro-administrador@uacj.mx' WHERE \"Id\"=$1", Guid.Parse(preview["operation"]!.ToString()));
            await Status(Confirm(client, preview), 403);
            preview = await Prepare(client); var generation = (await Survey(client)).Generation;
            await Status(client.PutAsJsonAsync(Base + "/api/metas/A", new DigitalTargetInput(9, 0, generation)), 200);
            await Status(Confirm(client, preview), 409);
        });
        Test("limpia datos propios, conserva metas por defecto, UUID, catálogos e historia", async (app, client) =>
        {
            await Role("administrador"); await Login(app, client); var view = await CompleteAndSave(client);
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new DigitalFinalize(view.Version, view.Generation)), 200);
            await Seed("0099", "A3", 0, false);
            await Status(client.PutAsJsonAsync(Base + "/api/metas/A", new DigitalTargetInput(8, 0, view.Generation)), 200);
            var catalogs = await shared.Scalar("SELECT jsonb_agg(to_jsonb(u) ORDER BY id_ur)::text FROM unidades_responsables_poa u");
            var forms = await shared.Scalar("SELECT count(*) FROM formatos_ur");
            var identities = await Nexo("SELECT jsonb_agg(to_jsonb(u) ORDER BY usuario_id)::text FROM fixture_users u");
            var history = await Data("SELECT count(*) FROM \"__CDMigrationsHistory\"");
            var summary = await Prepare(client);
            Check(summary["counts"]!["drafts"]!.GetValue<int>() == 1 && summary["counts"]!["completed"]!.GetValue<int>() == 1);
            Check(summary["counts"]!["reports"]!.GetValue<int>() == 1 && summary["counts"]!["consents"]!.GetValue<int>() == 1);
            var result = await Json(await Confirm(client, summary)); Check(result["completedAt"] is not null);
            Check(Convert.ToInt64(await Data("SELECT (SELECT count(*) FROM cd_surveys)+(SELECT count(*) FROM cd_consents)")) == 0);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_targets")) == 1);
            Check(Equals(catalogs, await shared.Scalar("SELECT jsonb_agg(to_jsonb(u) ORDER BY id_ur)::text FROM unidades_responsables_poa u")) && Equals(forms, await shared.Scalar("SELECT count(*) FROM formatos_ur")));
            Check(Equals(identities, await Nexo("SELECT jsonb_agg(to_jsonb(u) ORDER BY usuario_id)::text FROM fixture_users u")) && Equals(history, await Data("SELECT count(*) FROM \"__CDMigrationsHistory\"")));
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM pg_class WHERE relnamespace='public'::regnamespace AND relkind='S'")) == 0);
            Check((string)(await Data("SELECT data_type FROM information_schema.columns WHERE table_name='cd_surveys' AND column_name='Id'"))! == "uuid");
            Check((string)(await Data("SELECT \"Actor\"||':'||\"Employee\" FROM cd_reset_operations WHERE \"CompletedAt\" IS NOT NULL"))! == "persona@uacj.mx:0001");
            var empty = await Survey(client); Check(empty.Generation != view.Generation && empty.Version == 0 && !empty.Accepted && empty.Profile.Count == 0 && empty.Answers.Count == 0 && empty.Navigation.Current == "perfil");
            Check(empty.Institution["employee"]!.ToString() == "0001" && empty.Institution["unit"]!["id"]!.ToString() == "A4");
        });
        Test("doble envío y reintento no borran la nueva captura; metas opcionales", async (app, client) =>
        {
            await Role("administrador"); await Login(app, client); var old = await Accept(client);
            await Status(client.PutAsJsonAsync(Base + "/api/metas/A", new DigitalTargetInput(12, 0, old.Generation)), 200);
            var preview = await Prepare(client, true); var responses = await Task.WhenAll(Confirm(client, preview), Confirm(client, preview));
            var first = await Json(responses[0]); var second = await Json(responses[1]); Check(first.ToJsonString() == second.ToJsonString());
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_targets")) == 0);
            var fresh = await Accept(client); await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(fresh.Version, Complete(fresh).Profile, new(), fresh.Generation)), 200);
            var snapshot = await Data("SELECT row_to_json(s)::text FROM cd_surveys s"); await Status(Confirm(client, preview), 200);
            Check(Equals(snapshot, await Data("SELECT row_to_json(s)::text FROM cd_surveys s")));
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", Complete(old)), 409);
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new DigitalFinalize(1, old.Generation)), 409);
            await Status(client.PostAsJsonAsync(Base + "/api/aviso", new DigitalAccept(old.Instrument["notice"]!["version"]!.ToString(), old.Generation)), 409);
            await Status(client.PutAsJsonAsync(Base + "/api/metas/A", new DigitalTargetInput(9, 0, old.Generation)), 409);
            Check(Equals(snapshot, await Data("SELECT row_to_json(s)::text FROM cd_surveys s")));
            var newId = await Data("SELECT \"Id\" FROM cd_surveys"); Check(newId is Guid);
        });
        Test("rollback de limpieza, generación y auditoría ante fallo SQL", async (app, client) =>
        {
            await Role("administrador"); await Login(app, client); var view = await Accept(client); await Seed("0098", "A3", 1, true);
            var preview = await Prepare(client, true); var before = await Data("SELECT jsonb_agg(to_jsonb(s) ORDER BY \"Employee\")::text FROM cd_surveys s");
            await Data("CREATE FUNCTION public.cd_test_fail() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'synthetic rollback'; END $$; CREATE TRIGGER cd_test_rollback AFTER TRUNCATE ON public.cd_consents FOR EACH STATEMENT EXECUTE FUNCTION public.cd_test_fail()");
            try
            {
                using var response = await Confirm(client, preview); Check(!response.IsSuccessStatusCode);
                Check(Equals(before, await Data("SELECT jsonb_agg(to_jsonb(s) ORDER BY \"Employee\")::text FROM cd_surveys s")));
                Check((await Survey(client)).Generation == view.Generation && Convert.ToInt64(await Data("SELECT count(*) FROM cd_reset_operations WHERE \"CompletedAt\" IS NOT NULL")) == 0);
            }
            finally { await Data("DROP TRIGGER cd_test_rollback ON cd_consents; DROP FUNCTION cd_test_fail()"); }
            await Status(Confirm(client, preview), 200);
        });
        Test("permisos mínimos, search_path seguro y definitivas siguen protegidas", async (app, client) =>
        {
            await Role("administrador"); await Login(app, client); var view = await CompleteAndSave(client);
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new DigitalFinalize(view.Version, view.Generation)), 200);
            foreach (var sql in new[] { "TRUNCATE public.cd_surveys", "DELETE FROM public.cd_surveys", "UPDATE public.cd_control SET \"Generation\"=gen_random_uuid()", "DELETE FROM public.cd_reset_operations", "UPDATE public.cd_surveys SET \"Name\"='cambio'" })
            { try { await Sql(appConnection, sql); throw new InvalidOperationException("Permiso excesivo: " + sql); } catch (PostgresException) { } }
            Check(!(bool)(await Data("SELECT has_function_privilege('cd_native_nexo','public.cd_execute_reset(uuid,text,text)','EXECUTE')"))!);
            Check((bool)(await Data("SELECT prosecdef AND 'search_path=pg_catalog, pg_temp'=ANY(proconfig) FROM pg_proc WHERE oid='public.cd_execute_reset(uuid,text,text)'::regprocedure"))!);
            var preview = await Prepare(client); await Status(Confirm(client, preview), 200);
            view = await CompleteAndSave(client); await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new DigitalFinalize(view.Version, view.Generation)), 200);
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", Complete(view)), 409);
        });
        Test("guardados y finalizaciones esperan el reinicio y rechazan generación antigua", async (app, client) =>
        {
            await Role("administrador"); await Login(app, client); var old = await CompleteAndSave(client); var preview = await Prepare(client);
            await using var connection = new NpgsqlConnection(appConnection); await connection.OpenAsync(); await using var tx = await connection.BeginTransactionAsync();
            await using (var command = new NpgsqlCommand("SELECT public.cd_execute_reset($1,$2,$3)", connection, tx))
            { command.Parameters.AddWithValue(Guid.Parse(preview["operation"]!.ToString())); command.Parameters.AddWithValue("persona@uacj.mx"); command.Parameters.AddWithValue("0001"); await command.ExecuteScalarAsync(); }
            var save = client.PutAsJsonAsync(Base + "/api/encuesta", Complete(old));
            var final = client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new DigitalFinalize(old.Version, old.Generation));
            await Task.Delay(150); Check(!save.IsCompleted && !final.IsCompleted); await tx.CommitAsync();
            await Status(save, 409); await Status(final, 409); Check((await Survey(client)).Version == 0);
        });
        Test("un guardado en curso termina primero e invalida el resumen confirmado", async (app, client) =>
        {
            await Role("administrador"); await Login(app, client); var view = await Accept(client); var preview = await Prepare(client);
            await using var connection = new NpgsqlConnection(diagnosis); await connection.OpenAsync(); await using var tx = await connection.BeginTransactionAsync();
            await using (var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('0001',27102026))", connection, tx)) await command.ExecuteNonQueryAsync();
            var save = client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, Complete(view).Profile, new(), view.Generation));
            var waiting = false;
            for (var i = 0; i < 120; i++)
            {
                waiting = (bool)(await Data("SELECT EXISTS(SELECT 1 FROM pg_locks WHERE locktype='advisory' AND classid=0 AND objid=2710202601 AND mode='ShareLock' AND granted)"))!;
                if (waiting) break; await Task.Delay(25);
            }
            Check(waiting && !save.IsCompleted, "Guardado debe conservar bloqueo global mientras espera al empleado");
            var reset = Confirm(client, preview); await Task.Delay(150); Check(!reset.IsCompleted);
            await tx.CommitAsync(); await Status(save, 200); await Status(reset, 409);
            var saved = await Survey(client); Check(saved.Generation == view.Generation && saved.Profile.Count == 6);
        });
    }
}
