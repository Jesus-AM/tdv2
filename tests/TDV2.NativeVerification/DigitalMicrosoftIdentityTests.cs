using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Tdv2.Competencias;

namespace Tdv2.NativeVerification;

internal sealed partial class DigitalTests
{
    private void AddMicrosoftIdentityTests(List<(string Name, Func<NativeApplication, HttpClient, Task> Work)> cases)
    {
        void Test(string name, Func<NativeApplication, HttpClient, Task> work) => cases.Add(("Identidad Microsoft: " + name, work));
        Test("alias y acceso exclusivo conservan borrador, consentimiento, foto y auditoría canónica", async (app, client) =>
        {
            await Exclusive(); await shared.Sql("DELETE FROM tdv2_identity_links");
            app.Microsoft.Mail = "alias@uacj.mx"; app.Microsoft.Email = "inicio@uacj.mx"; app.Microsoft.ProxyAddresses = ["SMTP:alias@uacj.mx", "smtp:persona@uacj.mx"];
            await Login(app, client); var view = await CompleteAndSave(client, 0);
            Check(await QueuedSaves() == 0 && await ReceivedSaves() == 0);
            var before = await Data("SELECT row_to_json(s)::text FROM cd_surveys s"); var identity = await shared.Scalar("SELECT id FROM users");
            var requests = app.Microsoft.MeRequests;
            await Status(client.GetAsync("/inicio"), 403);
            Check((await Json(await client.GetAsync("/accesos")))["redirect"]!.ToString() == Base);
            var manual = new DigitalManualSave(view.Version, view.Generation, Guid.NewGuid());
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/guardar", manual), 200);
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/guardar", manual), 200);
            Check(app.Microsoft.MeRequests == requests, "Captura y autorización no consultan Graph");
            Check(await QueuedSaves() == 1 && await ReceivedSaves() == 1);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE categoria='encuesta' AND accion='guardar' AND actor_email='persona@uacj.mx' AND actor_usuario_id=20")) == 1);
            using var second = app.Client(); app.Microsoft.Mail = "nuevo@uacj.mx";
            await Login(app, second); var restored = await Survey(second);
            Check(restored.Accepted && restored.Answers["m-word"].Level == 0 && restored.Version == view.Version);
            Check(Equals(before, await Data("SELECT row_to_json(s)::text FROM cd_surveys s")) && Equals(identity, await shared.Scalar("SELECT id FROM users")));
            app.Microsoft.PhotoAvailable = true; second.DefaultRequestHeaders.Add("X-TDV2-Application", "competencias-digitales");
            Check((await Json(await second.GetAsync("/user/photo")))["photo"] is not null, "Foto del mismo oid aunque mail difiera del correo canónico");
        });
        Test("cambio de correo Microsoft no modifica informe definitivo ni crea otra encuesta", async (app, client) =>
        {
            await Exclusive(); await Login(app, client); var view = await CompleteAndSave(client);
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new DigitalFinalize(view.Version, view.Generation)), 200);
            var before = await Data("SELECT row_to_json(s)::text FROM cd_surveys s");
            using var second = app.Client(); app.Microsoft.Mail = "alias-nuevo@uacj.mx"; await Login(app, second);
            var restored = await Survey(second); Check(restored.CompletedAt is not null);
            await Status(second.GetAsync(Base + "/api/informe"), 200);
            await Status(second.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(restored.Version, restored.Profile, restored.Answers, restored.Generation)), 409);
            Check(Equals(before, await Data("SELECT row_to_json(s)::text FROM cd_surveys s")) && Convert.ToInt64(await Data("SELECT count(*) FROM cd_surveys")) == 1);
        });
        Test("sin acceso no sustituye identidad por otra cuenta autorizada entre alias", async (app, client) =>
        {
            await Exclusive(); await Nexo("DELETE FROM fixture_roles WHERE email='persona@uacj.mx'");
            app.Microsoft.Mail = "otra@uacj.mx"; app.Microsoft.ProxyAddresses = ["smtp:persona@uacj.mx"];
            var start = await client.GetAsync("/connect?returnUrl=" + Base);
            var query = QueryHelpers.ParseQuery(start.Headers.Location!.Query); app.Microsoft.ExpectedChallenge = query["code_challenge"];
            var callback = await client.GetAsync("/connect?code=synthetic-code&state=" + Uri.EscapeDataString(query["state"].ToString()));
            Check(callback.Headers.Location?.OriginalString == "/?authError=denied");
            await Status(client.GetAsync(Base + "/api/encuesta"), 401);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_surveys")) == 0);
        });
        Test("representación conserva actor canónico y objetivo sin escrituras", async (app, client) =>
        {
            await Exclusive(); await Role("administrador"); app.Microsoft.Mail = "admin-alias@uacj.mx"; await Login(app, client);
            await Status(client.PostAsJsonAsync(Base + "/api/representacion", new { email = "otra@uacj.mx", motivo = "Verificación sintética de identidad" }), 200);
            var page = await Json(await client.GetAsync(Base + "/encuesta"));
            client.DefaultRequestHeaders.Add("X-TDV2-Context", page["props"]!["contextoEdicion"]!.ToString());
            var view = await Survey(client); Check(page["props"]!["digital"]!["readOnly"]!.GetValue<bool>());
            await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, view.Profile, view.Answers, view.Generation)), 403);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE actor_email='persona@uacj.mx' AND usuario_afectado_email='otra@uacj.mx'")) > 0);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE actor_email='admin-alias@uacj.mx'")) == 0);
        });
    }
}
