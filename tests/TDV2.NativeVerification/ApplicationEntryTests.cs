using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Tdv2.Web;

namespace Tdv2.NativeVerification;

internal sealed partial class DigitalTests
{
    private void AddApplicationEntryTests(List<(string Name, Func<NativeApplication, HttpClient, Task> Work)> cases)
    {
        foreach (var (td, cd) in new[] { (true, false), (false, true), (true, true), (false, false) })
            cases.Add(($"Entrada independiente: Procedimientos={td}, Competencias={cd}; sesión y portal conservados", async (app, client) =>
            {
                if (!td) await shared.Sql("DELETE FROM fixture_roles");
                if (!cd) await Nexo("DELETE FROM fixture_roles");
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
                Check(denied["message"]!.ToString() == ApplicationEntry.Unavailable);
                Check(denied["access"]!["options"]![0]!["path"]!.ToString() == "/inicio");
                Check((await Json(await client.GetAsync(Base + "/api/encuesta"), 503))["message"]!.ToString() == ApplicationEntry.Unavailable);
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
