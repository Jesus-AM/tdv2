using System.Net.Http.Json;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Tdv2.Integrations.Microsoft;
using Tdv2.Security;

namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    private static void AddMicrosoftAliasTests(NativeDatabase database, List<(string Name, Func<NativeApplication, HttpClient, Task> Work)> cases)
    {
        void Test(string name, Func<NativeApplication, HttpClient, Task> work) => cases.Add(("Microsoft alias: " + name, work));
        Test("cambio de mail conserva la identidad ya vinculada", async (app, client) =>
        {
            await Login(app, client); var id = await database.Scalar("SELECT id FROM users");
            await Csrf(client); await client.PostAsJsonAsync("/logout", new { });
            app.Microsoft.Mail = "alias@uacj.mx";
            await Login(app, client);
            Check((await client.GetAsync("/inicio")).IsSuccessStatusCode, "El vínculo estable debe conservar el acceso vigente");
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 1 && Equals(id, await database.Scalar("SELECT id FROM users")), "Sin cuenta duplicada");
        });
        Test("registro antiguo sin identidad no se vincula por mail sin UPN o SMTP coincidente", async (app, client) =>
        {
            await database.Sql("DELETE FROM tdv2_identity_links"); app.Microsoft.Email = "sin-correspondencia@uacj.mx";
            await database.Sql("INSERT INTO users(email,name,password) VALUES('persona@uacj.mx','Cuenta antigua sintética','no-login')");
            Check(DeniedLogin(await Callback(client, await Start(app, client))), "El correo no acredita una vinculación histórica");
            Check(await database.Scalar("SELECT microsoft_id FROM users") is DBNull or null, "No atribuir identidad a una cuenta antigua");
        });
        Test("perfil solicita explícitamente proxyAddresses", async (app, _) =>
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<MicrosoftClient>().Me("synthetic-profile", CancellationToken.None);
            Check(app.Microsoft.LastProfileSelect?.Split(',').Contains("proxyAddresses") == true, "Falta proxyAddresses en $select");
        });
        Test("UPN, mail y alias SMTP no cambian el usuario canónico acreditado", async (app, client) =>
        {
            app.Microsoft.Mail = "principal@uacj.mx"; app.Microsoft.Email = "inicio@uacj.mx";
            app.Microsoft.ProxyAddresses = ["SMTP:Principal@uacj.mx", "smtp:PERSONA@UACJ.MX", "smtp:persona@uacj.mx", "SIP:ajena@uacj.mx", "X500:/o=synthetic", "smtp:externa@example.test"];
            await Login(app, client);
            Check((await client.GetAsync("/inicio")).IsSuccessStatusCode);
            Check((await database.Scalar("SELECT email FROM users"))?.ToString() == "persona@uacj.mx");
            using var scope = app.Services.CreateScope();
            var person = await scope.ServiceProvider.GetRequiredService<MicrosoftClient>().Me("synthetic-profile", CancellationToken.None);
            Check(person.SmtpAddresses!.Count == 2 && person.SmtpAddresses.Single(a => a.Primary).Address == "principal@uacj.mx");
            Check(person.Addresses.Length == 3 && !person.Addresses.Contains("ajena@uacj.mx"));
            Check(app.Microsoft.MeRequests == 2, "Sólo login y consulta explícita de prueba; sin Graph en la página");
        });
        Test("sin correspondencia acreditada no se obtiene autorización por correo ni alias", async (app, client) =>
        {
            await database.Sql("DELETE FROM tdv2_identity_links"); app.Microsoft.Email = app.Microsoft.Mail = "sin-correspondencia@uacj.mx";
            await Login(app, client);
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Forbidden);
            var choices = await Json(await client.GetAsync("/accesos"));
            Check(choices["props"]!["available"]!["options"]!.AsArray().Count == 0);
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_identity_links")) == 0);
        });
        Test("alias que contradice otra identidad se rechaza sin combinar permisos", async (app, client) =>
        {
            await database.Sql("INSERT INTO fixture_users VALUES(11,21,'ajena@uacj.mx','Otra persona','individual','0002','A3','adscripcion')");
            app.Microsoft.ProxyAddresses = ["SMTP:persona@uacj.mx", "smtp:ajena@uacj.mx"];
            Check(DeniedLogin(await Callback(client, await Start(app, client))));
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 0);
        });
        Test("cambio del correo canónico conserva ID local y audita la identidad acreditada", async (app, client) =>
        {
            await Login(app, client); var id = await database.Scalar("SELECT id FROM users");
            await Csrf(client); await client.PostAsJsonAsync("/logout", new { });
            await database.Sql("UPDATE fixture_users SET email='vigente@uacj.mx'; UPDATE fixture_roles SET email='vigente@uacj.mx'");
            app.Microsoft.Mail = "otro-alias@uacj.mx";
            await Login(app, client); Check((await client.GetAsync("/inicio")).IsSuccessStatusCode);
            Check(Equals(id, await database.Scalar("SELECT id FROM users")) && (await database.Scalar("SELECT email FROM users"))?.ToString() == "vigente@uacj.mx");
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM activity_logs WHERE user_email='vigente@uacj.mx' AND action='login'")) == 1);
        });
        Test("fallo Graph y colección incompleta no crean identidad ni amplían acceso", async (app, client) =>
        {
            app.Microsoft.ProfileStatus = HttpStatusCode.ServiceUnavailable;
            Check((await Callback(client, await Start(app, client))).Headers.Location?.OriginalString == "/?authError=unavailable");
            app.Microsoft.ProfileStatus = null; app.Microsoft.MissingProxyAddresses = true;
            Check(DeniedLogin(await Callback(client, await Start(app, client))));
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 0);
        });
        Test("revocar correspondencia invalida sesión sin consultar Graph ni recurrir a alias", async (app, client) =>
        {
            await Login(app, client); var requests = app.Microsoft.MeRequests;
            await database.Sql("DELETE FROM tdv2_identity_links");
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Unauthorized);
            Check(app.Microsoft.MeRequests == requests);
        });
        Test("la cuenta web puede leer e insertar vínculos pero no cambiarlos ni borrarlos", async (app, _) =>
        {
            Check((bool)(await database.Scalar("SELECT has_table_privilege('tdv2_native_app','tdv2_identity_links','SELECT')"))!);
            Check((bool)(await database.Scalar("SELECT has_table_privilege('tdv2_native_app','tdv2_identity_links','INSERT')"))!);
            foreach (var permission in new[] { "UPDATE", "DELETE", "TRUNCATE" })
                Check(!(bool)(await database.Scalar("SELECT has_table_privilege('tdv2_native_app','tdv2_identity_links',$1)", permission))!);
        });
        Test("id de Graph y tenant deben corresponder a los claims firmados", async (app, client) =>
        {
            app.Microsoft.MeObjectIdOverride = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
            Check(DeniedLogin(await Callback(client, await Start(app, client))));
            app.Microsoft.MeObjectIdOverride = null; app.Microsoft.IdTokenTenant = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
            Check(DeniedLogin(await Callback(client, await Start(app, client))));
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 0);
        });
        foreach (var source in new[] { "UPN", "SMTP", "SIP" })
        Test("correspondencia inicial por " + source + " con mail diferente", async (app, client) =>
        {
            await database.Sql("DELETE FROM tdv2_identity_links");
            await database.Sql("INSERT INTO users(email,name,password) VALUES('persona@uacj.mx','Persona histórica sintética','no-login')");
            var original = await database.Scalar("SELECT id FROM users");
            app.Microsoft.Mail = "trabajo@uacj.mx"; app.Microsoft.Email = source == "UPN" ? "persona@uacj.mx" : "inicio@uacj.mx";
            app.Microsoft.ProxyAddresses = source == "SMTP" ? ["SMTP:trabajo@uacj.mx", "smtp:persona@uacj.mx", "smtp:PERSONA@UACJ.MX"] : ["SIP:persona@uacj.mx"];
            if (source == "SIP")
            {
                await Login(app, client); Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Forbidden);
                Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_identity_links")) == 0);
                Check(await database.Scalar("SELECT microsoft_id FROM users WHERE id=$1", original!) is DBNull);
            }
            else
            {
                await Login(app, client); Check((await client.GetAsync("/inicio")).IsSuccessStatusCode);
                Check(Equals(original, await database.Scalar("SELECT id FROM users")) && Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 1);
                Check(Convert.ToInt64(await database.Scalar("SELECT nexo_user_id FROM tdv2_identity_links")) == 20);
                // Un cambio posterior sin el alias original conserva la acreditación estable.
                using var second = app.Client(); app.Microsoft.Email = app.Microsoft.Mail = "posterior@uacj.mx"; app.Microsoft.ProxyAddresses = [];
                await Login(app, second); Check((await second.GetAsync("/inicio")).IsSuccessStatusCode);
                Check(Equals(original, await database.Scalar("SELECT id FROM users")));
            }
        });
        Test("ambigüedad inicial sin vínculo rechaza en vez de elegir el usuario con permisos", async (app, client) =>
        {
            await database.Sql("DELETE FROM tdv2_identity_links; INSERT INTO fixture_users VALUES(11,21,'ajena@uacj.mx','Otra persona','individual','0002','A3','adscripcion')");
            app.Microsoft.ProxyAddresses = ["smtp:ajena@uacj.mx"];
            Check(DeniedLogin(await Callback(client, await Start(app, client))));
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 0 && Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_identity_links")) == 0);
        });
        foreach (var source in new[] { "UPN", "SMTP" })
        Test("mail fuera del catálogo no impide comprobar " + source + " institucional", async (app, client) =>
        {
            await database.Sql("DELETE FROM tdv2_identity_links");
            app.Microsoft.Mail = "trabajo@example.test";
            app.Microsoft.Email = source == "UPN" ? "persona@uacj.mx" : "inicio@example.test";
            app.Microsoft.ProxyAddresses = source == "SMTP" ? ["smtp:persona@uacj.mx"] : [];
            await Login(app, client);
            Check((await client.GetAsync("/inicio")).IsSuccessStatusCode);
            Check((await database.Scalar("SELECT email FROM users"))?.ToString() == "persona@uacj.mx");
            Check(Convert.ToInt64(await database.Scalar("SELECT nexo_user_id FROM tdv2_identity_links")) == 20);
        });
        Test("fallo de consulta inicial no acredita unicidad ni crea vínculo", async (app, client) =>
        {
            await database.Sql("DELETE FROM tdv2_identity_links; REVOKE SELECT ON nexo_usuarios FROM tdv2_native_nexo");
            try
            {
                Check((await Callback(client, await Start(app, client))).Headers.Location?.OriginalString == "/?authError=unavailable");
                Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 0 && Convert.ToInt64(await database.Scalar("SELECT count(*) FROM tdv2_identity_links")) == 0);
            }
            finally { await database.Sql("GRANT SELECT ON nexo_usuarios TO tdv2_native_nexo"); }
        });
        Test("usuario canónico ya vinculado a otro objeto no admite un segundo vínculo", async (app, client) =>
        {
            app.Microsoft.Id = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
            Check((await Callback(client, await Start(app, client))).Headers.Location?.OriginalString == "/?authError=denied");
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM users")) == 0);
            Check((await database.Scalar("SELECT microsoft_id::text FROM tdv2_identity_links"))!.ToString() == FakeMicrosoft.ObjectId);
        });
        Test("sin vínculo ni conexión de Competencias el ID diferido no bloquea Procedimientos", async (_, _) =>
        {
            await database.Sql("DELETE FROM tdv2_identity_links");
            await using var app = new NativeApplication(database) { DigitalConfiguration = new() { ["CompetenciasDigitales:Nexo:ApplicationId"] = "87" } };
            using var client = app.Client(); await Login(app, client);
            Check((await client.GetAsync("/inicio")).IsSuccessStatusCode);
            Check((await client.GetAsync("/competencias-digitales")).StatusCode == HttpStatusCode.ServiceUnavailable);
        });
        Test("ticket anterior exige una autenticación y recupera acceso sin ciclo 401", async (app, client) =>
        {
            await Login(app, client); var id = await database.Scalar("SELECT id FROM users");
            var crypto = app.Services.GetRequiredService<ProtectedValues>();
            var ticket = TicketSerializer.Default.Deserialize(Convert.FromBase64String(crypto.Unprotect("session", (string)(await database.Scalar("SELECT ticket FROM tdv2_sessions"))!)))!;
            var claims = (ClaimsIdentity)ticket.Principal.Identity!;
            claims.RemoveClaim(claims.FindFirst("tdv2.session_version")); claims.AddClaim(new("tdv2.session_version", "aspnet-1"));
            claims.RemoveClaim(claims.FindFirst(NexoActorIdentity.Claim));
            await database.Sql("UPDATE tdv2_sessions SET ticket=$1", crypto.Protect("session", Convert.ToBase64String(TicketSerializer.Default.Serialize(ticket))));
            Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Unauthorized);
            await Login(app, client); Check((await client.GetAsync("/inicio")).IsSuccessStatusCode);
            Check(Equals(id, await database.Scalar("SELECT id FROM users")) && app.Microsoft.Exchanges == 2);
        });
        Test("alias asociado a otra identidad local no se fusiona con un vínculo existente", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await client.PostAsJsonAsync("/logout", new { });
            await database.Sql("INSERT INTO users(email,name,password,microsoft_tenant_id,microsoft_id) VALUES('conflicto@uacj.mx','Otra identidad sintética','no-login',$1,$2)", Guid.Parse(FakeMicrosoft.Tenant), Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"));
            var before = await database.Scalar("SELECT json_agg(row_to_json(u) ORDER BY id)::text FROM users u");
            app.Microsoft.ProxyAddresses = ["smtp:conflicto@uacj.mx"];
            Check(DeniedLogin(await Callback(client, await Start(app, client))));
            Check(Equals(before, await database.Scalar("SELECT json_agg(row_to_json(u) ORDER BY id)::text FROM users u")));
        });
    }
}
