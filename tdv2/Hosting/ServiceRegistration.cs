using Tdv2.Integrations.Microsoft;
using Tdv2.Integrations.Nexo;
using Tdv2.Integrations.Catalogs;
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

using Tdv2.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
namespace Tdv2.Hosting;

public static class ServiceRegistration
{
    public static void AddTdv2(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithViews(options => options.Filters.Add<RequestModelValidation>());
        builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .AddRequirements(new InstitutionalAccessRequirement()).Build());
        builder.Services.AddScoped<IAuthorizationHandler, InstitutionalAccessHandler>();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, InstitutionalAccessResultHandler>();
        builder.Services.AddDbContext<Tdv2DbContext>(options => Tdv2DatabaseOptions.Configure(options, builder.Configuration));
        // Los errores públicos se traducen en ExceptionBoundaryMiddleware; EF no registra SQL ni excepciones del proveedor.
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);
        builder.Services.AddHttpContextAccessor();
        builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection("Synchronization"));
        builder.Services.AddSingleton<ISourceConnections, SourceConnections>();
        builder.Services.AddScoped<ICatalogSource, CatalogSource>();
        builder.Services.AddScoped<CatalogPublication>();
        builder.Services.AddScoped<SyncCoordinator>();
        builder.Services.AddScoped<ILocalCatalog, LocalCatalog>();
        builder.Services.Configure<MicrosoftSettings>(builder.Configuration.GetSection("Microsoft"));
        // User Secrets sólo es configuración de desarrollo. Estas llaves protegen sesiones/tokens
        // del servidor y necesitan un directorio persistente propio al publicar en Ubuntu.
        var protection = builder.Services.AddDataProtection().SetApplicationName("TDV2.AspNet.v1")
            .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeyDirectory"] ?? Path.Combine(builder.Environment.ContentRootPath, ".runtime", "keys")));
        if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi();
        builder.Services.AddSingleton<DatabaseConnections>();
        builder.Services.AddSingleton<ProtectedValues>();
        builder.Services.AddSingleton<PostgresTicketStore>();
        builder.Services.AddScoped<PostgresOAuthAttempts>();
        builder.Services.AddScoped<PostgresIdentity>();
        builder.Services.AddScoped<ISessionIdentity>(services => services.GetRequiredService<PostgresIdentity>());
        builder.Services.AddScoped<GraphTokens>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<MicrosoftPhotoCache>();
        builder.Services.AddScoped<ParticipantPhotos>();
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
            foreach (var (name, limit) in new[] { ("sync-config", 20), ("sync-start", 10) })
                options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
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
        builder.Services.AddScoped<PostgresFormStore>();
        builder.Services.AddScoped<IFormStore>(services => services.GetRequiredService<PostgresFormStore>());
        builder.Services.AddScoped<FormEditingService>();
        builder.Services.AddSingleton<FormNotifications>();
        builder.Services.AddSignalR(options => { options.MaximumReceiveMessageSize = 4096; options.EnableDetailedErrors = false; });
        builder.Services.AddScoped<PostgresNexoProfiles>();
        builder.Services.AddScoped<INexoProfiles>(services => services.GetRequiredService<PostgresNexoProfiles>());
        builder.Services.AddScoped<RequestAccess>();
        builder.Services.AddSingleton(new FormSchema(File.ReadAllText(Path.Combine(builder.Environment.ContentRootPath, "Contracts", "procesos_operativos.json"))));
        builder.Services.AddScoped<FormService>();
        builder.Services.AddScoped<ProcessConfigurationService>();
        builder.Services.AddScoped<MicrosoftLoginService>();
        builder.Services.AddScoped<SessionTerminationService>();
        builder.Services.AddScoped<PostgresCollaborationStore>();
        builder.Services.AddScoped<SynchronizationQueries>();
        builder.Services.AddScoped<CollaboratorService>();
        builder.Services.AddScoped<AccessToolService>();
        builder.Services.Configure<ReactDevelopmentOptions>(builder.Configuration.GetSection("ReactDevelopment"));
        builder.Services.AddSingleton<ReactShell>();
        builder.Services.AddHttpClient("vite", client => client.Timeout = TimeSpan.FromSeconds(30));
    }
}
