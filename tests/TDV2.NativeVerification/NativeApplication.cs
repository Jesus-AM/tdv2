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
using Microsoft.EntityFrameworkCore;
using Tdv2.Infrastructure;
using Tdv2.Security;
using Tdv2.Synchronization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
namespace Tdv2.NativeVerification;

internal sealed class SyntheticClock : TimeProvider
{
    internal TimeSpan Offset;
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + Offset;
}

internal sealed class FakeMicrosoft : HttpMessageHandler
{
    internal const string Tenant = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    internal const string ObjectId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    internal string Email = "persona@uacj.mx";
    internal string Id = ObjectId;
    internal string Mail = "persona@uacj.mx";
    internal bool TokenFailure, PhotoFailure, PhotoAvailable, RotateRefresh = true;
    internal int Exchanges, Refreshes, PhotoRequests, MeRequests;
    internal HttpStatusCode? PhotoStatus, RefreshStatus;
    internal string? PhotoError;
    internal int RetrySeconds;
    internal string PhotoPixel = Pixel;
    internal TaskCompletionSource? PhotoEntered, PhotoContinue;
    internal string? MeObjectIdOverride;
    private sealed record Account(string Id, string Email, string Mail);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Account> accounts = new();
    internal const string Pixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl6r0sAAAAASUVORK5CYII=";
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
            if (RefreshStatus is { } refreshStatus && form["grant_type"] == "refresh_token") return Json(new { error = "synthetic_failure" }, refreshStatus);
            var account = form["grant_type"] == "refresh_token" && accounts.TryGetValue(form["refresh_token"].ToString(), out var previous)
                ? previous : new Account(Id, Email, Mail);
            var suffix = account.Id == ObjectId ? "" : "-" + account.Id;
            var accessToken = "synthetic-access-" + Refreshes + suffix;
            var refreshToken = "synthetic-refresh-" + Refreshes + suffix;
            accounts[accessToken] = account; accounts[refreshToken] = account;
            var payload = new Dictionary<string, object> { ["token_type"] = "Bearer", ["access_token"] = accessToken, ["expires_in"] = 3600 };
            if (form["grant_type"] == "authorization_code" || RotateRefresh) payload["refresh_token"] = refreshToken;
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
            var account = accounts.GetValueOrDefault(request.Headers.Authorization.Parameter!) ?? new Account(Id, Email, Mail);
            if (request.RequestUri.AbsolutePath == "/v1.0/me") { Interlocked.Increment(ref MeRequests); return Json(new { id = MeObjectIdOverride ?? account.Id, mail = account.Mail, userPrincipalName = account.Email, displayName = "Untrusted Graph display name" }); }
            if (request.RequestUri.AbsolutePath == "/v1.0/me/photos/48x48/$value") {
                Interlocked.Increment(ref PhotoRequests);
                if (PhotoEntered is { } entered) { entered.TrySetResult(); await PhotoContinue!.Task.WaitAsync(cancellationToken); }
                if (PhotoError == "network") throw new HttpRequestException("SYNTHETIC_NETWORK_SECRET");
                if (PhotoError == "timeout") throw new TaskCanceledException("SYNTHETIC_TIMEOUT_SECRET");
                if (PhotoStatus is { } photoStatus) {
                    var response = new HttpResponseMessage(photoStatus);
                    if (RetrySeconds > 0) response.Headers.RetryAfter = new(TimeSpan.FromSeconds(RetrySeconds));
                    return response;
                }
                if (PhotoFailure || !PhotoAvailable) return new HttpResponseMessage(PhotoFailure ? HttpStatusCode.Unauthorized : HttpStatusCode.NotFound);
                var photo = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Convert.FromBase64String(PhotoPixel)) };
                photo.Content.Headers.ContentType = new("image/png"); return photo;
            }
        }
        throw new InvalidOperationException("Unexpected outbound request; real services are forbidden in this suite.");
    }
    private static HttpResponseMessage Json(object value, HttpStatusCode code = HttpStatusCode.OK) => new(code) { Content = new StringContent(JsonSerializer.Serialize(value), System.Text.Encoding.UTF8, "application/json") };
}

internal sealed class NativeApplication(NativeDatabase database) : WebApplicationFactory<global::Program>
{
    internal FakeMicrosoft Microsoft { get; } = new();
    internal SyntheticSources Sources { get; } = new();
    internal ReadCounter Reads { get; } = new();
    internal SyntheticClock Clock { get; } = new();
    internal SyntheticDiagnostics Diagnostics { get; } = new();
    internal string ControlToken { get; } = ProtectedValues.Random();
    internal string PublicOrigin = "https://localhost";
    internal bool Browser, Vite, ManualProcessing;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => { logging.ClearProviders(); logging.AddProvider(Diagnostics); });
        builder.ConfigureAppConfiguration((host, configuration) => {
            // El builder se crea en Testing y nunca carga User Secrets. Sólo esta prueba habilita
            // el proxy de desarrollo después de sustituir la configuración por fuentes sintéticas.
            if (Vite) host.HostingEnvironment.EnvironmentName = "Development";
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Tdv2"] = database.AppConnection, ["ConnectionStrings:Nexo"] = database.NexoConnection,
            ["Microsoft:TenantId"] = FakeMicrosoft.Tenant, ["Microsoft:ClientId"] = "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
            ["Microsoft:ClientSecret"] = "synthetic-only", ["Microsoft:PublicOrigin"] = PublicOrigin,
            ["Synchronization:IldaEnabled"] = "true", ["ReactDevelopment:UseVite"] = Vite.ToString()
        }); });
        builder.ConfigureTestServices(services =>
        {
            if (!ManualProcessing)
            {
                // Los escenarios históricos avanzan el coordinador explícitamente.
                // Las pruebas Sync/web y sync-manual conservan el hosted service real.
                var worker = services.Single(d => d.ImplementationType == typeof(ManualSyncWorker));
                services.Remove(worker);
                services.AddSingleton(_ => { var dispatcher = new ManualSyncDispatcher(); dispatcher.Started(); return dispatcher; });
            }
            services.AddSingleton(Reads);
            services.AddSingleton<TimeProvider>(Clock);
            services.AddDbContext<Tdv2DbContext>(options => options.AddInterceptors(Reads));
            // Production auth, Nexo queries, cookie sessions and stores stay registered. Only Microsoft's HTTPS peer is simulated.
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddSingleton<ISourceConnections>(Sources);
            services.AddHttpClient<MicrosoftClient>().ConfigurePrimaryHttpMessageHandler(() => Microsoft);
            if (Browser) {
                Microsoft.PhotoAvailable = true;
                services.AddSingleton<IStartupFilter>(new BrowserControls(database, ControlToken, Sources, Microsoft, Clock));
            }
        });
    }
    internal HttpClient Client() => CreateClient(new() { BaseAddress = new Uri(PublicOrigin), AllowAutoRedirect = false, HandleCookies = true });
    internal static X509Certificate2 Certificate(bool ephemeral = false)
    {
        if (OperatingSystem.IsWindows() && !ephemeral)
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
internal sealed class BrowserControls(NativeDatabase database, string token, SyntheticSources sources, FakeMicrosoft microsoft, SyntheticClock clock) : IStartupFilter
{
    private Task<System.Text.Json.Nodes.JsonObject?>? worker;
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (http, rest) =>
        {
            // Only the test host accepts the loopback TLS proxy's private transport marker.
            if (IPAddress.IsLoopback(http.Connection.RemoteIpAddress ?? IPAddress.None)
                && http.Request.Headers["X-Synthetic-Tls"] == token) http.Request.Scheme = "https";
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
                case "/photo-state":
                    await http.Response.WriteAsJsonAsync(new { requests = microsoft.PhotoRequests, identities = microsoft.MeRequests }); return;
                case "/photo-control":
                    if (int.TryParse(http.Request.Query["minutes"], out var minutes) && minutes is >= 0 and <= 120) clock.Offset = TimeSpan.FromMinutes(minutes);
                    microsoft.PhotoStatus = http.Request.Query["mode"].ToString() switch { "temporary" => HttpStatusCode.ServiceUnavailable, "missing" => HttpStatusCode.NotFound, _ => null };
                    if (http.Request.Query["changed"] == "true") microsoft.PhotoPixel = Convert.ToBase64String(Convert.FromBase64String(microsoft.PhotoPixel).Concat(new byte[] { 0 }).ToArray());
                    if (http.Request.Query["target"] == "true") { microsoft.Id = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee"; microsoft.Email = microsoft.Mail = "encargada@uacj.mx"; }
                    break;
                case "/photo-hold": microsoft.PhotoEntered = new(TaskCreationOptions.RunContinuationsAsynchronously); microsoft.PhotoContinue = new(TaskCreationOptions.RunContinuationsAsynchronously); break;
                case "/photo-release": microsoft.PhotoContinue?.TrySetResult(); microsoft.PhotoEntered = null; break;
                case "/performance-seed":
                    var count = int.TryParse(http.Request.Query["rows"], out var requested) ? requested : 200;
                    if (count is < 1 or > 200) { http.Response.StatusCode = 400; return; }
                    var large = http.RequestServices.GetRequiredService<Tdv2.Domain.FormSchema>().Blank(new("A", "100", "Área sintética A", 2, null, 2026));
                    foreach (var section in new[] { "identificacion", "sistemas", "datos", "acuerdos" })
                    {
                        var template = large[section]![0]!.DeepClone(); var rows = large[section]!.AsArray(); rows.Clear();
                        var sectionCount = http.Request.Query["single"] == "true" && section != "identificacion" ? 0 : count;
                        for (var i = 0; i < sectionCount; i++) { var row = template.DeepClone(); row["id"] = "synthetic-" + i; rows.Add(row); }
                    }
                    await database.Sql("DELETE FROM formato_bloques WHERE id_ur='A'; DELETE FROM formato_operaciones WHERE id_ur='A'; DELETE FROM formato_posiciones WHERE id_ur='A'; DELETE FROM formatos_ur WHERE id_ur='A'");
                    await database.Sql("INSERT INTO formatos_ur(id_ur,contenido,version,porcentaje,actualizado_por,created_at,updated_at,ejercicio) VALUES('A',$1::json,0,0,'persona@uacj.mx',now(),now(),2026)", large.ToJsonString());
                    break;
                case "/capture-metrics":
                    var reads = http.RequestServices.GetRequiredService<ReadCounter>();
                    if (HttpMethods.IsPost(http.Request.Method)) reads.Count = 0;
                    await http.Response.WriteAsJsonAsync(new { efCommands = reads.Count }); return;
                case "/configuration-admin": await NativeTests.ConfigurationAdmin(database); break;
                case "/stage-ilda-seed":
                    var seeded = NativeTests.Complete(http.RequestServices.GetRequiredService<Tdv2.Domain.FormSchema>());
                    seeded["identificacion"]![0]!["id"] = "ilda:17";
                    await database.Sql("DELETE FROM formato_bloques WHERE id_ur='A'");
                    await database.Sql("UPDATE formatos_ur SET contenido=$1::json,version=version+1 WHERE id_ur='A'", seeded.ToJsonString());
                    await database.Sql("INSERT INTO sincronizacion_catalogos VALUES('ilda',1,now()); INSERT INTO ilda_informacion_area(id_origen,ur2,informacion_generada,datos,sincronizado_en) VALUES('17','100','Inventario ILDA sintético','{}',now())");
                    break;
                case "/stage-ilda-republish":
                    await database.Sql("UPDATE ilda_informacion_area SET informacion_generada='Nueva publicación sintética',presente=true,sincronizado_en=now()"); break;
                case "/recipients-history":
                    var history = NativeTests.Complete(http.RequestServices.GetRequiredService<Tdv2.Domain.FormSchema>());
                    history["identificacion"]![0]!["usuario"] = new System.Text.Json.Nodes.JsonArray("Docentes", "Otro", "Externo");
                    history["identificacion"]![0]!["usuarioOtro"] = "Detalle histórico sintético";
                    await database.Sql("UPDATE formatos_ur SET ejercicio=2025,contenido=$1::json,version=version+1 WHERE id_ur='A'", history.ToJsonString()); break;
                case "/recipients-submitted":
                    await database.Sql("UPDATE formatos_ur SET enviado_en=clock_timestamp(),enviado_como='persona@uacj.mx',instantanea_envio=jsonb_build_object('contenido',contenido) WHERE id_ur='A'"); break;
                case "/scope-level3":
                    await database.Sql("""
                        UPDATE fixture_roles SET rol_clave='responsable_ur_supervisor',rol_nombre='Responsable de UR con supervisión' WHERE email='persona@uacj.mx';
                        UPDATE fixture_users SET num_empleado='0002',"ID_UR"='A3' WHERE email='persona@uacj.mx';
                        DELETE FROM fixture_delegation WHERE email='persona@uacj.mx';
                        INSERT INTO fixture_delegation VALUES('persona@uacj.mx','A3','0002',3);
                        INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,desc_ur,id_ur_pertenece,nivel_ur,tipo_ur,estatus_ur,presente)
                        VALUES('aux',2026,'00000','Nodo auxiliar excluido','A',3,' n ','Activo',true);
                        UPDATE unidades_responsables_poa SET cve_ur='06000',id_ur_pertenece='aux',tipo_ur='0' WHERE id_ur='A3';
                        UPDATE unidades_responsables_poa SET tipo_ur=' 0 ' WHERE id_ur='A';
                        """); break;
                case "/sync-tick":
                    using (var scope=http.RequestServices.CreateScope()) await scope.ServiceProvider.GetRequiredService<SyncCoordinator>().Tick(http.RequestAborted);
                    break;
                case "/systems-seed":
                    var systemsSeed = NativeTests.Complete(http.RequestServices.GetRequiredService<Tdv2.Domain.FormSchema>());
                    foreach (var field in new[] { "sistema", "uso", "estado", "sistemaOtro", "usoOtro", "moduloSiiId", "moduloSiiDescripcion" }) systemsSeed["sistemas"]![0]![field] = "";
                    await NativeTests.SeedStage(database, systemsSeed);
                    break;
                case "/delivery-seed":
                    var delivery = NativeTests.StageOnly(http.RequestServices.GetRequiredService<Tdv2.Domain.FormSchema>());
                    var p2 = delivery["identificacion"]![0]!.DeepClone(); p2["id"] = "segundo"; p2["codigo"] = "PO-02"; p2["tramite"] = "Procedimiento pendiente"; p2["resultado"] = "";
                    var p3 = delivery["identificacion"]![0]!.DeepClone(); p3["id"] = "tercero"; p3["codigo"] = "PO-03"; p3["tramite"] = "Procedimiento prescindible";
                    delivery["identificacion"]!.AsArray().Add(p2); delivery["identificacion"]!.AsArray().Add(p3);
                    delivery["evaluaciones"]!["PO-02"] = delivery["evaluaciones"]!["PO-01"]!.DeepClone();
                    var s1 = delivery["sistemas"]![0]!; s1["sistema"] = "excel"; s1["uso"] = "consultar"; s1["estado"] = "bien"; s1["fallas"] = "";
                    foreach (var detail in Tdv2.Domain.SystemAnswers.Details) s1[detail] = "";
                    var s2 = s1.DeepClone(); s2["id"] = "segunda"; delivery["sistemas"]!.AsArray().Add(s2);
                    await NativeTests.SeedStage(database, delivery);
                    await database.Sql("""
                        INSERT INTO fixture_users VALUES(50,50,'colaboradora@uacj.mx','Colaboradora sintética','individual','0050','A','adscripcion');
                        INSERT INTO fixture_roles VALUES(50,'colaboradora@uacj.mx',31,'colaborador_local','Colaboración central sintética');
                        INSERT INTO fixture_grants VALUES(101,'colaboradora@uacj.mx',31,'A','central');
                        """); break;
                case "/validation-history":
                    var validations = NativeTests.RetiredValidationFixture(http.RequestServices.GetRequiredService<Tdv2.Domain.FormSchema>());
                    await database.Sql("DELETE FROM formato_bloques WHERE id_ur='A'");
                    await database.Sql("UPDATE formatos_ur SET contenido=$1::json,version=version+1 WHERE id_ur='A'", validations.ToJsonString());
                    break;
                case "/delivery-identity":
                    var helperIdentity = http.Request.Query["who"] == "helper";
                    microsoft.Email = microsoft.Mail = helperIdentity ? "colaboradora@uacj.mx" : "persona@uacj.mx";
                    microsoft.Id = helperIdentity ? "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee" : FakeMicrosoft.ObjectId;
                    break;
                case "/systems-rename": sources.Modules.Rows[2]["DESC_MODULO"] = "Descripción posterior"; goto case "/systems-sync";
                case "/systems-remove": sources.Modules.Rows.RemoveAt(2); goto case "/systems-sync";
                case "/systems-sync":
                    using (var scope = http.RequestServices.CreateScope())
                    {
                        var coordinator = scope.ServiceProvider.GetRequiredService<SyncCoordinator>();
                        await coordinator.Enqueue("sii", "comando", "prueba_sintetica", false, http.RequestAborted);
                        await coordinator.Tick(http.RequestAborted);
                    }
                    break;
                case "/sync-hold": sources.Block="ilda"; sources.Entered=new(TaskCreationOptions.RunContinuationsAsynchronously); sources.Continue=new(TaskCreationOptions.RunContinuationsAsynchronously); break;
                case "/sync-worker-start":
                    var services=http.RequestServices.GetRequiredService<IServiceScopeFactory>();
                    worker=Task.Run(async () => { using var scope=services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<SyncCoordinator>().Tick(CancellationToken.None); });
                    await sources.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15)); break;
                case "/sync-worker-release": sources.Continue.TrySetResult(); if(worker is not null) await worker; sources.Block=null; break;
                case "/sync-fail-ilda": sources.Failure="ilda"; break;
                case "/sync-claim-fail":
                    await database.Sql("CREATE FUNCTION fixture_browser_claim() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.propietario IS NOT NULL THEN RAISE EXCEPTION 'SYNTHETIC_CLAIM_SECRET'; END IF; RETURN NEW; END $$; CREATE TRIGGER fail_browser_claim BEFORE UPDATE ON sincronizacion_configuracion FOR EACH ROW EXECUTE FUNCTION fixture_browser_claim()"); break;
                case "/sync-claim-recover":
                    await database.Sql("DROP TRIGGER fail_browser_claim ON sincronizacion_configuracion; DROP FUNCTION fixture_browser_claim()"); break;
                case "/sync-fail-modules": sources.Failure="sii_modulos"; break;
                case "/sync-restore-source": sources.Failure=null; break;
                case "/sync-change-source":
                    sources.Ilda.Rows.RemoveAt(0); sources.Ilda.Rows.Add(6,"100","Nuevo trámite ILDA",null,"");
                    sources.Sii.Rows[0]["DESC_UR"]="Área sintética A actualizada"; break;
                case "/sync-due": await database.Sql("UPDATE sincronizacion_configuracion SET proxima_en=timezone('UTC',clock_timestamp())-interval '2 days'"); break;
                case "/sync-interrupted": await database.Sql("UPDATE sincronizacion_ejecuciones SET estado='ejecutando',iniciada_en=timezone('UTC',clock_timestamp()) WHERE id=(SELECT ejecucion_activa FROM sincronizacion_configuracion); UPDATE sincronizacion_configuracion SET propietario=gen_random_uuid(),reserva_hasta=timezone('UTC',clock_timestamp())-interval '1 minute'"); break;
                case "/sync-access-off": await database.Sql("DELETE FROM fixture_module_roles WHERE rol_id=34 AND modulo_id=51"); break;
                case "/sync-access-on": await database.Sql("INSERT INTO fixture_module_roles VALUES(34,51)"); break;
                case "/sync-schema-missing": await database.Sql("ALTER TABLE public.sii_modulos RENAME TO fixture_missing_modules"); break;
                case "/sync-schema-restore": await database.Sql("ALTER TABLE public.fixture_missing_modules RENAME TO sii_modulos"); break;
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
                case "/central-collaborator": await NativeTests.CentralCollaborator(database); break;
                case "/central-remove": await database.Sql("DELETE FROM fixture_grants WHERE origen='central'"); break;
                case "/central-no-employee": await database.Sql("UPDATE fixture_users SET num_empleado=NULL WHERE email='persona@uacj.mx'"); break;
                case "/central-restore-responsible":
                    await database.Sql("UPDATE fixture_roles SET rol_id=30,rol_clave='responsable_ur',rol_nombre='Responsable'; UPDATE fixture_users SET num_empleado='0001' WHERE email='persona@uacj.mx'; DELETE FROM fixture_grants WHERE origen='central'"); break;
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
