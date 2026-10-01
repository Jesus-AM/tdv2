using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Web;
using Tdv2.Security;
using Tdv2.Synchronization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection("Synchronization"));
builder.Services.AddSingleton<ISourceConnections,SourceConnections>();
builder.Services.AddScoped<ICatalogSource,CatalogSource>();
builder.Services.AddScoped<CatalogPublication>();
builder.Services.AddScoped<SyncCoordinator>();
builder.Services.AddScoped<ILocalCatalog,LocalCatalog>();
builder.Services.Configure<MicrosoftSettings>(builder.Configuration.GetSection("Microsoft"));
var protection = builder.Services.AddDataProtection().SetApplicationName("TDV2.AspNet.v1")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, ".runtime", "keys")));
if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
builder.Services.AddSingleton<DatabaseConnections>();
builder.Services.AddSingleton<ProtectedValues>();
builder.Services.AddSingleton<PostgresTicketStore>();
builder.Services.AddScoped<PostgresOAuthAttempts>();
builder.Services.AddScoped<PostgresIdentity>();
builder.Services.AddScoped<ISessionIdentity>(services => services.GetRequiredService<PostgresIdentity>());
builder.Services.AddScoped<GraphTokens>();
builder.Services.AddScoped<AuthenticationAudit>();
builder.Services.AddScoped<AccessState>();
builder.Services.AddScoped<OperationAudit>();
builder.Services.AddScoped<NexoOperations>();
builder.Services.AddScoped<CollaboratorAccess>();
builder.Services.AddHttpClient<MicrosoftClient>(client => client.Timeout = TimeSpan.FromSeconds(15))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_600_000);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "tdv2.aspnet.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<PostgresTicketStore>((options, store) => options.SessionStore = store);
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "tdv2.aspnet.csrf";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddRateLimiter(options =>
{
    foreach (var (name,limit) in new[] { ("sync-config",20),("sync-start",10) })
        options.AddPolicy(name,context => RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit=limit,Window=TimeSpan.FromMinutes(1),QueueLimit=0 }));
    options.RejectionStatusCode = 429;
    foreach (var (name, limit) in new[] { ("collaborator-search", 60), ("collaborator-writes", 30), ("representation-search", 30), ("representation-start", 10), ("preview-start", 20) })
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("microsoft-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("photo", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("form-writes", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddScoped<IFormStore, PostgresFormStore>();
builder.Services.AddScoped<PostgresNexoProfiles>();
builder.Services.AddScoped<INexoProfiles>(services => services.GetRequiredService<PostgresNexoProfiles>());
builder.Services.AddScoped<RequestAccess>();
builder.Services.AddSingleton(new FormSchema(File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "Contracts", "procesos_operativos.json"))));
var app = builder.Build();
if (args.Any(a => a is "--sync-worker" or "--sync-once" || a.StartsWith("--sync-check=",StringComparison.Ordinal)))
{
    Environment.ExitCode = await SyncCommands.Run(app.Services,args);
    await app.DisposeAsync(); return;
}
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "private, no-store";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    try { await next(context); }
    catch (DomainProblem problem)
    {
        await context.RequestServices.GetRequiredService<OperationAudit>().Failure(context, problem.Status);
        context.Response.StatusCode = problem.Status;
        if (PagePaths.IsPage(context.Request.Path) && context.Request.Headers.Accept.Any(v => v?.Contains("text/html") == true))
        {
            var shell = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html");
            if (File.Exists(shell))
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.SendFileAsync(shell); return;
            }
        }
        var payload = problem.Details is null ? new System.Text.Json.Nodes.JsonObject()
            : System.Text.Json.JsonSerializer.SerializeToNode(problem.Details)!.AsObject();
        payload["message"] = problem.Message;
        var snapshot = context.RequestServices.GetRequiredService<AccessState>().Loaded;
        payload["representacion"] = System.Text.Json.JsonSerializer.SerializeToNode(snapshot?.Selection?.Representation());
        payload["simulacion"] = System.Text.Json.JsonSerializer.SerializeToNode(snapshot?.Selection?.Preview());
        payload["contextoEdicion"] = snapshot?.Key ?? "own";
        await context.Response.WriteAsJsonAsync(payload);
    }
    catch (AntiforgeryValidationException)
    {
        await context.RequestServices.GetRequiredService<OperationAudit>().Failure(context, 419);
        context.Response.StatusCode = 419;
        await context.Response.WriteAsJsonAsync(new { message = "La sesión de captura venció. Recarga antes de continuar." });
    }
    catch (BadHttpRequestException)
    {
        await context.RequestServices.GetRequiredService<OperationAudit>().Failure(context, 400);
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { message = "La solicitud no es válida." });
    }
    catch (Exception error) when (error is not OperationCanceledException)
    {
        await context.RequestServices.GetRequiredService<OperationAudit>().Failure(context, 503);
        app.Logger.LogError("No se pudo completar la operación TDV2. Tipo: {Type}", error.GetType().Name);
        context.Response.StatusCode = 503;
        await context.Response.WriteAsJsonAsync(new { message = "No fue posible completar la operación. Inténtalo más tarde." });
    }
});
app.UseStaticFiles();
app.UseAuthentication();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
        await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
    await AccessBoundary.Validate(context);
    if (HttpMethods.IsGet(context.Request.Method) && context.Request.Headers.Accept.Any(v => v?.Contains("text/html") == true)
        && !context.Request.Headers.ContainsKey("X-TDV2-Page") && PagePaths.IsPage(context.Request.Path))
    {
        var index = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");
        if (!File.Exists(index)) throw new DomainProblem(503, "Compila el frontend para abrir la interfaz de TDV2.");
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(index);
        return;
    }
    await next(context);
});
app.MapGet("/health/live", () => Results.Json(new { status = "ok", databaseChecked = false }));
app.MapGet("/session/csrf", (HttpContext http, IAntiforgery csrf) => Results.Json(new { token = csrf.GetAndStoreTokens(http).RequestToken }));
app.MapMicrosoft();
app.MapGet("/user/modules", async (HttpContext http, RequestAccess access) =>
{
    var profile = await access.Profile(http);
    if (access.Selection is not null) profile = profile with { Modules = profile.Modules.Where(m => m.Key is not ("configuracion" or "sincronizaciones" or "pruebas_acceso")).ToArray() };
    return ModuleAccess.Navigation(profile);
});
app.MapGet("/configuracion", async (HttpContext http, RequestAccess access) =>
{
    var profile = await access.Profile(http, "configuracion");
    return PageResponse.Page(http, "Configuracion", new() { ["secciones"] = new { sincronizaciones = ModuleAccess.Allows(profile, "sincronizaciones"), pruebas_acceso = ModuleAccess.Allows(profile, "pruebas_acceso") } }, profile);
});
app.MapGet("/acceso-restringido", async (HttpContext http, AccessState state) =>
{
    await state.Load(http.RequestAborted);
    return PageResponse.Page(http, "AccesoRestringido", new() { ["access"] = new { message = "No tienes acceso a este recurso.", status = 403 } });
});
app.MapForms();
app.MapAccessTools();
app.MapCollaborators();
app.MapSynchronizations();
app.MapControllers();
app.MapFallback(() => Results.Json(new { message = "La ruta aún no está implementada en la migración ASP.NET." }, statusCode: 501));
app.Run();

public partial class Program;
public static class PagePaths
{
    public static bool IsPage(PathString path) => path == "/inicio" || path.StartsWithSegments("/formatos")
        || path == "/colaboradores" || path.StartsWithSegments("/configuracion") || path == "/vista-prueba"
        || path == "/actuar-como-usuario" || path == "/acceso-restringido";
}
