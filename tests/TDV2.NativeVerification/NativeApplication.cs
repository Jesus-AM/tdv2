using Tdv2.Integrations.Microsoft;
using Tdv2.Integrations.Catalogs;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tdv2.Security;
using Tdv2.Synchronization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
namespace Tdv2.NativeVerification;

internal sealed class FakeMicrosoft : HttpMessageHandler
{
    internal const string Tenant = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    internal const string ObjectId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    internal string Email = "persona@uacj.mx";
    internal string Id = ObjectId;
    internal string Mail = "persona@uacj.mx";
    internal bool TokenFailure, PhotoFailure, RotateRefresh = true;
    internal int Exchanges, Refreshes;
    internal string? ExpectedChallenge;
    internal string LastVerifier = "";
    internal string? LoginHint = "opaque-synthetic-real-account";
    internal bool InvalidNonce, InvalidSignature;
    private readonly RsaSecurityKey signingKey = new(RSA.Create(2048)) { KeyId = "synthetic-key" };
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri!.AbsolutePath.EndsWith("/.well-known/openid-configuration"))
            return Json(new { issuer = $"https://login.microsoftonline.com/{Tenant}/v2.0", jwks_uri = $"https://login.microsoftonline.com/{Tenant}/discovery/v2.0/keys" });
        if (request.RequestUri.AbsolutePath.EndsWith("/discovery/v2.0/keys"))
        {
            var key = signingKey.Rsa.ExportParameters(false);
            return Json(new { keys = new[] { new { kty = "RSA", use = "sig", kid = signingKey.KeyId, alg = "RS256",
                n = WebEncoders.Base64UrlEncode(key.Modulus!), e = WebEncoders.Base64UrlEncode(key.Exponent!) } } });
        }
        if (request.RequestUri!.Host == "login.microsoftonline.com" && request.RequestUri.AbsolutePath == $"/{Tenant}/oauth2/v2.0/token")
        {
            var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(cancellationToken));
            if (form["client_id"] != "cccccccc-cccc-4ccc-8ccc-cccccccccccc" || form["client_secret"] != "synthetic-only") throw new InvalidOperationException("Unexpected synthetic provider credentials.");
            if (form["grant_type"] == "authorization_code")
            {
                Exchanges++; LastVerifier = form["code_verifier"].ToString();
                if (ExpectedChallenge is not null && ProtectedValues.Challenge(LastVerifier) != ExpectedChallenge) throw new InvalidOperationException("PKCE did not match the authorization request.");
            }
            else if (form["grant_type"] == "refresh_token") { Refreshes++; }
            else throw new InvalidOperationException("Unexpected grant type.");
            if (TokenFailure) return Json(new { error = "synthetic_provider_error_secret" }, HttpStatusCode.BadRequest);
            var payload = new Dictionary<string, object> { ["token_type"] = "Bearer", ["access_token"] = "synthetic-access-" + Refreshes, ["expires_in"] = 3600 };
            if (form["grant_type"] == "authorization_code" || RotateRefresh) payload["refresh_token"] = "synthetic-refresh-" + Refreshes;
            if (form["grant_type"] == "authorization_code")
            {
                var claims = new Dictionary<string, object> { ["oid"] = Id, ["tid"] = Tenant, ["nonce"] = InvalidNonce ? "invalid" : ProtectedValues.Hash("oidc:" + LastVerifier) };
                if (LoginHint is not null) claims["login_hint"] = LoginHint;
                payload["id_token"] = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
                    Issuer = $"https://login.microsoftonline.com/{Tenant}/v2.0", Audience = "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
                    Expires = DateTime.UtcNow.AddHours(1), Claims = claims,
                    SigningCredentials = new(InvalidSignature ? new RsaSecurityKey(RSA.Create(2048)) : signingKey, SecurityAlgorithms.RsaSha256) });
            }
            return Json(payload);
        }
        if (request.RequestUri.Host == "graph.microsoft.com")
        {
            if (request.Headers.Authorization?.Scheme != "Bearer") throw new InvalidOperationException("Graph must authenticate the request.");
            if (request.RequestUri.AbsolutePath == "/v1.0/me") return Json(new { id = Id, mail = Mail, userPrincipalName = Email, displayName = "Untrusted Graph display name" });
            if (request.RequestUri.AbsolutePath == "/v1.0/me/photos/48x48/$value") return new HttpResponseMessage(PhotoFailure ? HttpStatusCode.Unauthorized : HttpStatusCode.NotFound);
        }
        throw new InvalidOperationException("Unexpected outbound request; real services are forbidden in this suite.");
    }
    private static HttpResponseMessage Json(object value, HttpStatusCode code = HttpStatusCode.OK) => new(code) { Content = new StringContent(JsonSerializer.Serialize(value), System.Text.Encoding.UTF8, "application/json") };
}

internal sealed class NativeApplication(NativeDatabase database) : WebApplicationFactory<global::Program>
{
    internal FakeMicrosoft Microsoft { get; } = new();
    internal SyntheticSources Sources { get; } = new();
    internal string ControlToken { get; } = ProtectedValues.Random();
    internal string PublicOrigin = "https://localhost";
    internal bool Browser;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => { logging.ClearProviders(); logging.AddProvider(new SyntheticDiagnostics()); });
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Tdv2"] = database.AppConnection, ["ConnectionStrings:Nexo"] = database.NexoConnection,
            ["Microsoft:TenantId"] = FakeMicrosoft.Tenant, ["Microsoft:ClientId"] = "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
            ["Microsoft:ClientSecret"] = "synthetic-only", ["Microsoft:PublicOrigin"] = PublicOrigin,
            ["Synchronization:IldaEnabled"] = "true"
        }));
        builder.ConfigureTestServices(services =>
        {
            // Production auth, Nexo queries, cookie sessions and stores stay registered. Only Microsoft's HTTPS peer is simulated.
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddSingleton<ISourceConnections>(Sources);
            services.AddHttpClient<MicrosoftClient>().ConfigurePrimaryHttpMessageHandler(() => Microsoft);
            if (Browser) services.AddSingleton<IStartupFilter>(new BrowserControls(database, ControlToken, Sources));
        });
    }
    internal HttpClient Client() => CreateClient(new() { BaseAddress = new Uri(PublicOrigin), AllowAutoRedirect = false, HandleCookies = true });
    internal static X509Certificate2 Certificate()
    {
        if (OperatingSystem.IsWindows())
        {
            // Schannel cannot serve TLS with ephemeral keys. Read an existing ASP.NET development
            // certificate; never create/import/trust a certificate or modify the Windows key store.
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            return store.Certificates.Cast<X509Certificate2>()
                .Where(c => c.HasPrivateKey && c.NotBefore <= DateTime.Now && c.NotAfter > DateTime.Now
                    && c.Extensions.Any(e => e.Oid?.Value == "1.3.6.1.4.1.311.84.1.1"))
                .OrderByDescending(c => c.NotAfter).FirstOrDefault()
                ?? throw new InvalidOperationException("Browser tests require an existing ASP.NET development HTTPS certificate; the suite does not install certificates.");
        }
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder(); san.AddDnsName("localhost"); san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
}

// Available ONLY in this test executable, on its loopback HTTPS Kestrel host. Never in the application assembly.
internal sealed class BrowserControls(NativeDatabase database, string token, SyntheticSources sources) : IStartupFilter
{
    private Task<System.Text.Json.Nodes.JsonObject?>? worker;
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (http, rest) =>
        {
            if (!http.Request.Path.StartsWithSegments("/__fixture", out var action))
            {
                await rest();
                if (http.Request.Path == "/form-events" && http.Response.StatusCode >= 400)
                    Console.WriteLine("SYNTHETIC SignalR transport HTTP " + http.Response.StatusCode);
                return;
            }
            if (http.Request.Headers["X-Fixture-Key"] != token) { http.Response.StatusCode = 403; return; }
            switch (action.Value)
            {
                case "/scope-level3":
                    await database.Sql("""
                        UPDATE fixture_roles SET rol_clave='responsable_ur_supervisor',rol_nombre='Responsable de UR con supervisión' WHERE email='persona@uacj.mx';
                        UPDATE fixture_users SET num_empleado='0002',"ID_UR"='A3' WHERE email='persona@uacj.mx';
                        DELETE FROM fixture_delegation WHERE email='persona@uacj.mx';
                        INSERT INTO fixture_delegation VALUES('persona@uacj.mx','A3','0002',3);
                        INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,desc_ur,id_ur_pertenece,nivel_ur,tipo_ur,estatus_ur,presente)
                        VALUES('aux',2026,'00000','Nodo auxiliar excluido','A',3,'0','Activo',true);
                        UPDATE unidades_responsables_poa SET cve_ur='06000',id_ur_pertenece='aux',tipo_ur='1' WHERE id_ur='A3';
                        """); break;
                case "/sync-tick":
                    using (var scope=http.RequestServices.CreateScope()) await scope.ServiceProvider.GetRequiredService<SyncCoordinator>().Tick(http.RequestAborted);
                    break;
                case "/sync-hold": sources.Block="ilda"; sources.Entered=new(TaskCreationOptions.RunContinuationsAsynchronously); sources.Continue=new(TaskCreationOptions.RunContinuationsAsynchronously); break;
                case "/sync-worker-start":
                    var services=http.RequestServices.GetRequiredService<IServiceScopeFactory>();
                    worker=Task.Run(async () => { using var scope=services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<SyncCoordinator>().Tick(CancellationToken.None); });
                    await sources.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15)); break;
                case "/sync-worker-release": sources.Continue.TrySetResult(); if(worker is not null) await worker; sources.Block=null; break;
                case "/sync-fail-ilda": sources.Failure="ilda"; break;
                case "/sync-restore-source": sources.Failure=null; break;
                case "/sync-change-source":
                    sources.Ilda.Rows.RemoveAt(0); sources.Ilda.Rows.Add(6,"100","Nuevo trámite ILDA",null,"");
                    sources.Sii.Rows[0]["DESC_UR"]="Área sintética A actualizada"; break;
                case "/sync-due": await database.Sql("UPDATE sincronizacion_configuracion SET proxima_en=timezone('UTC',clock_timestamp())-interval '2 days'"); break;
                case "/sync-interrupted": await database.Sql("UPDATE sincronizacion_ejecuciones SET estado='ejecutando',iniciada_en=timezone('UTC',clock_timestamp()) WHERE id=(SELECT ejecucion_activa FROM sincronizacion_configuracion); UPDATE sincronizacion_configuracion SET propietario=gen_random_uuid(),reserva_hasta=timezone('UTC',clock_timestamp())-interval '1 minute'"); break;
                case "/sync-access-off": await database.Sql("DELETE FROM fixture_module_roles WHERE rol_id=34 AND modulo_id=51"); break;
                case "/sync-access-on": await database.Sql("INSERT INTO fixture_module_roles VALUES(34,51)"); break;
                case "/sync-fail-sql": await database.Sql("CREATE FUNCTION fixture_browser_fail_sync() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'SYNTHETIC_CATALOG_SECRET'; END $$; CREATE TRIGGER browser_fail_sync BEFORE INSERT ON ilda_informacion_area FOR EACH ROW EXECUTE FUNCTION fixture_browser_fail_sync()"); break;
                case "/sync-recover-sql": await database.Sql("DROP TRIGGER IF EXISTS browser_fail_sync ON ilda_informacion_area; DROP FUNCTION IF EXISTS fixture_browser_fail_sync()"); break;
                case "/sync-report":
                    var syncReport=await database.Scalar("""
                        SELECT json_build_object('ilda',(SELECT coalesce(json_agg(r ORDER BY id_origen),'[]') FROM ilda_informacion_area r),
                          'runs',(SELECT coalesce(json_agg(r ORDER BY solicitada_en DESC),'[]') FROM sincronizacion_ejecuciones r),
                          'forms',(SELECT coalesce(json_agg(json_build_object('ur',id_ur,'version',version,'contenido',contenido)),'[]') FROM formatos_ur),
                          'audits',(SELECT count(*) FROM activity_logs WHERE entity='sincronizacion'),
                          'leaks',(SELECT count(*) FROM activity_logs WHERE meta::text LIKE '%SECRET%'))::text
                        """);
                    var payload=System.Text.Json.Nodes.JsonNode.Parse(syncReport!.ToString()!)!.AsObject(); payload["remoteCommands"]=sources.Queries.Count;
                    await http.Response.WriteAsJsonAsync(payload); return;
                case "/module-off": await database.Sql("DELETE FROM fixture_module_roles"); break;
                case "/module-on": await database.Sql("INSERT INTO fixture_module_roles VALUES(30,9)"); break;
                case "/user-off": await database.Sql("DELETE FROM fixture_users"); break;
                case "/user-on": await database.Sql("INSERT INTO fixture_users VALUES(10,20,'persona@uacj.mx','Persona sintética','individual','0001','A4','adscripcion')"); break;
                case "/app-off": await database.Sql("DELETE FROM fixture_app"); break;
                case "/app-on": await database.Sql("INSERT INTO fixture_app VALUES(47,'tdv2','TDV2 sintético',2)"); break;
                case "/outage": await database.Sql("REVOKE SELECT ON nexo_usuarios FROM tdv2_native_nexo"); break;
                case "/restore": await database.Sql("GRANT SELECT ON nexo_usuarios TO tdv2_native_nexo"); break;
                case "/fail-save": await database.Sql(NativeTests.FailureTrigger); break;
                case "/recover-save": await database.Sql("DROP TRIGGER fail_save ON formatos_ur; DROP FUNCTION fixture_fail_save()"); break;
                case "/expire-editing": await database.Sql("UPDATE formato_bloques SET vence_en=clock_timestamp()-interval '1 second'"); break;
                case "/ready-to-submit":
                    // Preparación exclusivamente sintética: el envío se realiza luego por la UI y el backend reales.
                    var complete = NativeTests.Complete(http.RequestServices.GetRequiredService<Tdv2.Domain.FormSchema>()).ToJsonString();
                    await database.Sql("UPDATE formatos_ur SET contenido=$1::json,porcentaje=100,version=version+1 WHERE id_ur='A'", complete);
                    break;
                case "/stored":
                    var json = await database.Scalar("SELECT json_build_object('version',version,'porcentaje',porcentaje,'contenido',contenido)::text FROM formatos_ur WHERE id_ur='A'");
                    http.Response.ContentType = "application/json"; await http.Response.WriteAsync(json?.ToString() ?? "null"); return;
                case "/delegation-off": await database.Sql("DELETE FROM fixture_delegation WHERE email='persona@uacj.mx'"); break;
                case "/delegation-on": await database.Sql("INSERT INTO fixture_delegation VALUES('persona@uacj.mx','A','0001',2)"); break;
                case "/search-outage": await database.Sql("INSERT INTO fixture_faults VALUES('buscar_personas','08006') ON CONFLICT(operation) DO UPDATE SET code=EXCLUDED.code"); break;
                case "/search-restore": await database.Sql("DELETE FROM fixture_faults WHERE operation='buscar_personas'"); break;
                case "/retire-outage": await database.Sql("INSERT INTO fixture_faults VALUES('retirar_acceso','08006')"); break;
                case "/retire-restore": await database.Sql("DELETE FROM fixture_faults WHERE operation='retirar_acceso'"); break;
                case "/rep-deny": await database.Sql("UPDATE fixture_capability SET permitido=false"); break;
                case "/rep-allow": await database.Sql("UPDATE fixture_capability SET permitido=true,escritura=true"); break;
                case "/rep-revoke": await database.Sql("UPDATE fixture_representation SET revoked=true"); break;
                case "/rep-expire": await database.Sql("UPDATE fixture_representation SET expira_en=now()-interval '1 minute'"); break;
                case "/rep-outage": await database.Sql("INSERT INTO fixture_faults VALUES('representacion','08006')"); break;
                case "/rep-restore": await database.Sql("DELETE FROM fixture_faults WHERE operation='representacion'"); break;
                case "/access-report":
                    var report = await database.Scalar("""
                        SELECT json_build_object(
                          'links',(SELECT coalesce(json_agg(json_build_object('id',id,'scope',id_ur_alcance,'kind',tipo,'revoked',revocada_en IS NOT NULL,'pending',retiro_central_pendiente)),'[]'::json) FROM colaboraciones_ur),
                          'audits',(SELECT coalesce(json_agg(json_build_object('actor',user_email,'entity',entity,'action',action,'meta',meta)),'[]'::json) FROM activity_logs WHERE entity<>'auth'),
                          'forms',(SELECT count(*) FROM formatos_ur),
                          'activeContexts',(SELECT count(*) FROM tdv2_access_contexts WHERE selection IS NOT NULL),
                          'secretLeaks',(SELECT count(*) FROM activity_logs a JOIN fixture_representation r ON position(r.token in a.meta::text)>0))::text
                        """);
                    http.Response.ContentType = "application/json"; await http.Response.WriteAsync(report!.ToString()!); return;
                default: http.Response.StatusCode = 404; return;
            }
            await http.Response.WriteAsJsonAsync(new { ok = true });
        });
        next(app);
    };
}
