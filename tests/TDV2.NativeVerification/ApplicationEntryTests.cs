using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Tdv2.Security;
using Tdv2.Web;
using Tdv2.Competencias;

namespace Tdv2.NativeVerification;

internal sealed partial class DigitalTests
{
    private void AddApplicationEntryTests(List<(string Name, Func<NativeApplication, HttpClient, Task> Work)> cases)
    {
        cases.Add(("Entrada: restablecimiento de membresías conserva la sesión y los borradores confirmados", async (app, client) =>
        {
            await Login(app, client);
            var original = await Json(await client.GetAsync("/session/status"));
            var survey = await CompleteAndSave(client, 1);
            var input = Complete(survey, 2);
            var before = await Data("SELECT to_jsonb(s)::text FROM cd_surveys s");
            await Nexo("INSERT INTO fixture_suspensions VALUES(10)");
            Check(Convert.ToInt32(await Nexo("SELECT count(*) FROM nexo_usuarios WHERE usuario_id=20")) == 0);
            await Status(client.GetAsync(Base), 403);
            await Status(client.GetAsync(Base + "/api/encuesta"), 403);
            var refused = await Json(await client.PutAsJsonAsync(Base + "/api/encuesta", input), 403);
            Check(refused["message"]!.ToString() == ApplicationEntry.Denied(Base));
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/guardar", new DigitalManualSave(survey.Version, survey.Generation, Guid.NewGuid())), 403);
            await Status(client.PostAsJsonAsync(Base + "/api/encuesta/finalizar", new DigitalFinalize(survey.Version, survey.Generation)), 403);
            Check(Equals(before, await Data("SELECT to_jsonb(s)::text FROM cd_surveys s")));
            Check((await Json(await client.GetAsync("/accesos")))["redirect"]!.ToString() == "/inicio");
            var tab = Guid.NewGuid();
            var read = "/formatos/A/estado?tab=" + tab;
            var row = (await Json(await client.GetAsync(read)))["contenido"]!["identificacion"]![0]!.DeepClone();
            var key = "identificacion:" + row["id"];
            var leases = await Json(await client.PostAsJsonAsync("/formatos/A/reservas", new { tabId = tab, operationId = Guid.NewGuid(), blocks = new[] { new { key, version = 0 } } }));
            var lease = leases["bloques"]![0]!["reserva"]!["id"]!.ToString();
            Task<HttpResponseMessage> SaveForm(int version, string name)
            {
                row["tramite"] = name;
                return client.PatchAsJsonAsync("/formatos/A/bloques", new { tabId = tab, operationId = Guid.NewGuid(), blocks = new[] { new { key, version, leaseId = lease, value = row } } });
            }
            await Status(SaveForm(0, "Confirmado"), 200);
            var formBefore = await shared.Scalar("SELECT to_jsonb(f)::text FROM formatos_ur f WHERE id_ur='A'");
            await shared.Sql("INSERT INTO fixture_suspensions VALUES(10)");
            Check((await Json(await client.GetAsync("/accesos")))["props"]!["available"]!["options"]!.AsArray().Count == 0);
            await Nexo("DELETE FROM fixture_suspensions WHERE usuario_aplicacion_id=10");
            Check((await Json(await client.GetAsync("/accesos")))["redirect"]!.ToString() == Base);
            Check((await Survey(client)).Version == survey.Version);
            await Status(client.GetAsync("/inicio"), 403);
            await Status(client.GetAsync(read), 403);
            Check((await Json(await SaveForm(1, "Sin confirmar"), 403))["message"]!.ToString() == ApplicationEntry.Denied("/inicio"));
            await Status(client.PostAsJsonAsync("/formatos/A/enviar", new { tabId = tab, operationId = Guid.NewGuid(), version = 1 }), 403);
            Check(Equals(formBefore, await shared.Scalar("SELECT to_jsonb(f)::text FROM formatos_ur f WHERE id_ur='A'")));
            await shared.Sql("DELETE FROM fixture_suspensions WHERE usuario_aplicacion_id=10");
            Check((await Json(await client.GetAsync(read)))["contenido"]!["identificacion"]![0]!["tramite"]!.ToString() == "Confirmado");
            await Status(client.GetAsync("/formatos/B"), 403);
            await Status(client.GetAsync(Base + "/configuracion"), 403);
            var restored = await Json(await client.GetAsync("/session/status"));
            Check(original["id"]!.ToString() == restored["id"]!.ToString() && original["expires_at"]!.ToString() == restored["expires_at"]!.ToString() && app.Microsoft.Exchanges == 1);
        }));
        foreach (var previous in new[] { false, true })
        cases.Add(($"Entrada: primer vínculo tras restablecer un sitio, alias acreditado y ticket anterior={previous}", async (app, client) =>
        {
            await shared.Sql("DELETE FROM tdv2_identity_links; INSERT INTO fixture_suspensions VALUES(10)");
            await Nexo("INSERT INTO fixture_suspensions VALUES(10)");
            app.Microsoft.Mail = "alias@uacj.mx"; app.Microsoft.Email = "inicio@uacj.mx"; app.Microsoft.ProxyAddresses = ["smtp:persona@uacj.mx"];
            await Login(app, client);
            var original = await Json(await client.GetAsync("/session/status"));
            if (previous)
            {
                var crypto = app.Services.GetRequiredService<ProtectedValues>();
                var encrypted = (string)(await shared.Scalar("SELECT ticket FROM tdv2_sessions"))!;
                var ticket = TicketSerializer.Default.Deserialize(Convert.FromBase64String(crypto.Unprotect("session", encrypted)))!;
                var claims = (ClaimsIdentity)ticket.Principal.Identity!;
                claims.RemoveClaim(claims.FindFirst("tdv2.pending-microsoft-person"));
                await shared.Sql("UPDATE tdv2_sessions SET ticket=$1", crypto.Protect("session", Convert.ToBase64String(TicketSerializer.Default.Serialize(ticket))));
            }
            Check((await Json(await client.GetAsync("/accesos")))["props"]!["available"]!["options"]!.AsArray().Count == 0);
            await Nexo("DELETE FROM fixture_suspensions");
            Check((await Json(await client.GetAsync("/accesos")))["redirect"]!.ToString() == Base);
            var restored = await Json(await client.GetAsync("/session/status"));
            Check(original["id"]!.ToString() == restored["id"]!.ToString() && original["expires_at"]!.ToString() == restored["expires_at"]!.ToString());
            Check(Convert.ToInt64(await shared.Scalar("SELECT nexo_user_id FROM tdv2_identity_links")) == 20 && app.Microsoft.Exchanges == 1);
            await Accept(client);
            await Status(client.GetAsync("/inicio"), 403);
        }));
        cases.Add(("Entrada: sesión sin vínculo rechaza alias ambiguos y reintenta una publicación caída", async (app, client) =>
        {
            await shared.Sql("DELETE FROM tdv2_identity_links; INSERT INTO fixture_suspensions VALUES(10)");
            await Nexo("INSERT INTO fixture_suspensions VALUES(10),(11)");
            app.Microsoft.ProxyAddresses = ["smtp:otra@uacj.mx"];
            await Login(app, client);
            await shared.Sql("DELETE FROM fixture_suspensions");
            await Nexo("REVOKE SELECT ON nexo_usuarios FROM cd_native_nexo");
            try { await Status(client.GetAsync("/inicio"), 503); }
            finally { await Nexo("GRANT SELECT ON nexo_usuarios TO cd_native_nexo"); }
            await Nexo("DELETE FROM fixture_suspensions WHERE usuario_aplicacion_id=11");
            await Status(client.GetAsync("/inicio"), 403);
            Check(Convert.ToInt32(await shared.Scalar("SELECT count(*) FROM tdv2_identity_links")) == 0);
            await Nexo("INSERT INTO fixture_suspensions VALUES(11)");
            await Status(client.GetAsync("/inicio"), 200);
            Check(app.Microsoft.Exchanges == 1);
        }));
        foreach (var (td, cd) in new[] { (true, false), (false, true), (true, true), (false, false) })
            cases.Add(($"Entrada independiente: Procedimientos={td}, Competencias={cd}; sesión y portal conservados", async (app, client) =>
            {
                if (!td) await shared.Sql("INSERT INTO fixture_suspensions VALUES(10)");
                if (!cd) await Nexo("INSERT INTO fixture_suspensions VALUES(10)");
                await Status(client.GetAsync("/"), 200);
                await Login(app, client, ApplicationEntry.Choices);
                var result = await Json(await client.GetAsync(ApplicationEntry.Choices));
                if (td != cd) Check(result["redirect"]!.ToString() == (td ? "/inicio" : Base) && result["component"] is null);
                else
                {
                    var entry = result["props"]!["available"]!;
                    var paths = entry["options"]!.AsArray().Select(o => o!["path"]!.ToString()).ToArray();
                    Check(paths.Contains("/inicio") == td && paths.Contains(Base) == cd && !entry["unavailable"]!.GetValue<bool>());
                }
                using var html = new HttpRequestMessage(HttpMethod.Get, ApplicationEntry.Choices); html.Headers.Accept.ParseAdd("text/html");
                var document = await client.SendAsync(html);
                Check((int)document.StatusCode == (td != cd ? 302 : 200));
                if (td != cd) Check(document.Headers.Location?.OriginalString == (td ? "/inicio" : Base) && (await document.Content.ReadAsStringAsync()).Length == 0, "La entrada directa no renderiza el selector");
                foreach (var (path, granted) in new[] { ("/inicio", td), (Base + "/encuesta", cd) })
                {
                    var response = await client.GetAsync(path); Check((int)response.StatusCode == (granted ? 200 : 403));
                    if (!granted)
                    {
                        var body = await Json(response, 403); Check(body["message"]!.ToString() == ApplicationEntry.Denied(path));
                        var other = body["access"]!["options"]!.AsArray(); Check(other.Count == ((td || cd) ? 1 : 0));
                        Check(!body["access"]!.ToJsonString().Contains("Nexo", StringComparison.OrdinalIgnoreCase));
                    }
                }
                await Status(client.GetAsync(Base + "/api/encuesta"), cd ? 200 : 403);
                await Status(client.GetAsync("/"), 200); await Status(client.GetAsync(ApplicationEntry.Choices), 200);
                Check((await client.GetAsync("/connect")).Headers.Location?.OriginalString == ApplicationEntry.Choices);
            }));
        cases.Add(("Entrada: falla temporal distinta de denegación, sin quitar sesión ni acceso a otra aplicación", async (app, client) =>
        {
            await Login(app, client);
            await Nexo("REVOKE SELECT ON nexo_usuarios FROM cd_native_nexo");
            try
            {
                var denied = await Json(await client.GetAsync(Base + "/encuesta"), 503);
                Check(denied["message"]!.ToString() == ApplicationEntry.UnavailableFor("Competencias digitales"));
                Check(denied["access"]!["options"]![0]!["path"]!.ToString() == "/inicio");
                Check((await Json(await client.GetAsync(Base + "/api/encuesta"), 503))["message"]!.ToString() == ApplicationEntry.UnavailableFor("Competencias digitales"));
                var entry = (await Json(await client.GetAsync(ApplicationEntry.Choices)))["props"]!["available"]!;
                Check(entry["unavailable"]!.GetValue<bool>() && entry["options"]!.AsArray().Count == 1);
                await Status(client.GetAsync("/inicio"), 200); await Status(client.GetAsync("/"), 200);
            }
            finally { await Nexo("GRANT SELECT ON nexo_usuarios TO cd_native_nexo"); }
            await Status(client.GetAsync(Base + "/encuesta"), 200);
            await Nexo("DELETE FROM fixture_roles");
            Check((await Json(await client.GetAsync(Base + "/encuesta"), 403))["message"]!.ToString() == ApplicationEntry.Denied(Base));
        }));
        cases.Add(("Entrada: retorno local de ambas aplicaciones, genérico y público; rechaza destinos externos/codificados", async (app, _) =>
        {
            var allowed = new[] { "/inicio", "/formatos/A", Base + "/encuesta?paso=m365", "/", ApplicationEntry.Choices };
            var rejected = new[] { "https://external.test", "//external.test", "/\\external.test", "/%2fexternal.test", "/competencias-digitales/../connect", "/api/encuesta", "/formatos/A/estado", "" };
            foreach (var target in allowed) Check(ApplicationEntry.SafeReturn(target) == target);
            Check(ApplicationEntry.SafeReturn("/configuracion/pruebas-acceso/actuar-como-usuario") == "/configuracion/pruebas-acceso/actuar-como-usuario");
            foreach (var target in rejected) Check(ApplicationEntry.SafeReturn(target) == ApplicationEntry.Choices);
            // Nueve flujos completos dentro del límite real de 20 solicitudes/minuto.
            foreach (var target in allowed.Concat(rejected.Take(3)).Append(""))
            {
                using var client = app.Client();
                var start = await client.GetAsync("/connect?returnUrl=" + Uri.EscapeDataString(target));
                var query = QueryHelpers.ParseQuery(start.Headers.Location!.Query); app.Microsoft.ExpectedChallenge = query["code_challenge"];
                var callback = await client.GetAsync("/connect?code=synthetic-code&state=" + Uri.EscapeDataString(query["state"].ToString()) + "&returnUrl=https://external.test");
                Check(callback.Headers.Location?.OriginalString == ApplicationEntry.SafeReturn(target));
            }
        }));
        cases.Add(("Entrada: HTML anónimo inicia sesión por destino; API mantiene 401; cambiar cuenta conserva destino y CSRF", async (app, client) =>
        {
            foreach (var path in new[] { "/inicio", "/formatos/A", Base + "/encuesta?paso=m365", ApplicationEntry.Choices })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path); request.Headers.Accept.ParseAdd("text/html");
                var response = await client.SendAsync(request);
                Check(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location!.OriginalString == "/connect?returnUrl=" + Uri.EscapeDataString(path));
                await Status(client.GetAsync(path), 401);
            }
            await Login(app, client);
            using var missing = app.Client(); await Status(missing.PostAsync("/session/use-another-account", null), 419);
            var switched = await Json(await client.PostAsync("/session/use-another-account?returnUrl=" + Uri.EscapeDataString(Base + "/encuesta"), null));
            var url = switched["redirect"]!.ToString(); Check(url == "/connect?account=other&returnUrl=" + Uri.EscapeDataString(Base + "/encuesta"));
            await Status(client.GetAsync(Base + "/api/encuesta"), 401);
            var start = QueryHelpers.ParseQuery((await client.GetAsync(url)).Headers.Location!.Query);
            Check(start["prompt"] == "select_account" && !start.ContainsKey("login_hint"));
        }));
        cases.Add(("Entrada: revocación elimina accesos de la elección sin cerrar identidad institucional", async (app, client) =>
        {
            await Login(app, client); await Status(client.GetAsync(ApplicationEntry.Choices), 200);
            await shared.Sql("DELETE FROM fixture_roles");
            Check((await Json(await client.GetAsync(ApplicationEntry.Choices)))["redirect"]!.ToString() == Base);
            await Status(client.GetAsync("/inicio"), 403);
            await Nexo("DELETE FROM fixture_roles");
            var options = (await Json(await client.GetAsync(ApplicationEntry.Choices)))["props"]!["available"]!["options"]!.AsArray();
            Check(options.Count == 0); await Status(client.GetAsync("/inicio"), 403); await Status(client.GetAsync(Base), 403); await Status(client.GetAsync("/"), 200);
        }));
        cases.Add(("Entrada: varios módulos, roles y asignaciones conservan un solo sitio", async (app, client) =>
        {
            await Exclusive(); await Role("responsable_ur");
            await shared.Sql("UPDATE unidades_responsables_poa SET num_empleado='0001' WHERE id_ur='B'");
            await Nexo("INSERT INTO fixture_roles SELECT * FROM fixture_roles WHERE email='persona@uacj.mx'");
            await Login(app, client);
            var result = await Json(await client.GetAsync(ApplicationEntry.Choices)); Check(result["redirect"]!.ToString() == Base);
            var page = await Json(await client.GetAsync(Base)); Check(page["props"]!["digital"]!["navigation"]!.AsArray().Count == 4);
        }));
        cases.Add(("Entrada: registro extensible con cuatro sitios, orden, denegación y falla parcial", async (_, _) =>
        {
            foreach (var destination in new[] { "/ACCESOS", "/Connect", "//external.test", "/x/../accesos" })
            {
                var rejected = false;
                try { _ = new PortalSites([new("Inválido", destination, 1, _ => Task.FromResult(true), _ => true)]); }
                catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "El registro rechaza retornos de control y destinos inseguros");
            }
            var duplicate = new PortalSite("Duplicado", "/sitio-sintetico", 1, _ => Task.FromResult(true), _ => true);
            var duplicateRejected = false;
            try { _ = new PortalSites([duplicate, duplicate with { Destination = "/SITIO-SINTETICO" }]); }
            catch (InvalidOperationException) { duplicateRejected = true; }
            Check(duplicateRejected, "El registro no permite contar dos veces un destino");
            var third = true; var broken = false;
            await using var app = new NativeApplication(shared) { DigitalConfiguration = Configuration };
            app.AdditionalSites.Add(new("Sitio sintético tercero", "/sitio-sintetico/tercero", 30,
                _ => broken ? throw new Tdv2.Domain.DomainProblem(503, ApplicationEntry.Unavailable) : Task.FromResult(third), p => p.StartsWithSegments("/sitio-sintetico/tercero")));
            app.AdditionalSites.Add(new("Sitio sintético cuarto", "/sitio-sintetico/cuarto", 40, _ => Task.FromResult(true), p => p.StartsWithSegments("/sitio-sintetico/cuarto")));
            using var client = app.Client(); await Login(app, client, ApplicationEntry.Choices);
            var entry = (await Json(await client.GetAsync(ApplicationEntry.Choices)))["props"]!["available"]!;
            Check(entry["options"]!.AsArray().Select(s => s!["path"]!.ToString()).SequenceEqual(new[] { "/inicio", Base, "/sitio-sintetico/tercero", "/sitio-sintetico/cuarto" }));
            third = false;
            Check((await Json(await client.GetAsync(ApplicationEntry.Choices)))["props"]!["available"]!["options"]!.AsArray().Count == 3);
            await Status(client.GetAsync("/sitio-sintetico/tercero"), 403);
            using var anonymous = app.Client(); await Status(anonymous.GetAsync("/sitio-sintetico/cuarto"), 401);
            using var html = new HttpRequestMessage(HttpMethod.Get, "/sitio-sintetico/cuarto"); html.Headers.Accept.ParseAdd("text/html");
            Check((await anonymous.SendAsync(html)).Headers.Location?.OriginalString == "/connect?returnUrl=" + Uri.EscapeDataString("/sitio-sintetico/cuarto"));
            await shared.Sql("DELETE FROM fixture_roles"); await Nexo("DELETE FROM fixture_roles");
            Check((await Json(await client.GetAsync(ApplicationEntry.Choices)))["redirect"]!.ToString() == "/sitio-sintetico/cuarto");
            broken = true;
            entry = (await Json(await client.GetAsync(ApplicationEntry.Choices)))["props"]!["available"]!;
            Check(entry["unavailable"]!.GetValue<bool>() && entry["options"]!.AsArray().Count == 1, "Un error no se cuenta como denegación ni provoca entrada automática");
            using var specific = app.Client(); await Login(app, specific, "/sitio-sintetico/cuarto");
        }));
    }
}
