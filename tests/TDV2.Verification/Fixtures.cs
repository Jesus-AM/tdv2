using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;

namespace Tdv2.Verification;

internal static class Fixtures
{
    internal static readonly Unit[] Units = [
        new("A", "100", "Área sintética A", 2, null, 2026, "0001"),
        new("A3", "110", "Área sintética A3", 3, "A", 2026, "0002"),
        new("A4", "111", "Área sintética A4", 4, "A3", 2026),
        new("B", "200", "Área sintética B", 2, null, 2026, "0003"),
        new("B3", "210", "Área sintética B3", 3, "B", 2026, "0001"),
        new("inactive", "900", "Inactiva", 2, null, 2026, "0001", true, "Inactivo"),
        new("absent", "901", "Ausente", 2, null, 2026, "0001", false)
    ];
    internal static Profile Profile(params string[] roles) => new(new("persona@example.test", "Persona sintética", "0001", "A4"),
        roles.Select((r, i) => new Role(i + 1, r, r)).ToArray(), [new(1, "procesos_operativos", "Procesos operativos", "/inicio"),
        new(2, "configuracion", "Configuración", "/configuracion"), new(3, "sincronizaciones", "Sincronizaciones", "/configuracion/sincronizaciones", 2)]);
}

internal sealed class FakeNexo : INexoProfiles
{
    internal Profile Current = Fixtures.Profile("responsable_ur");
    internal bool Outage;
    internal IReadOnlyList<Grant> CurrentGrants = [];
    public Task<Profile> ForEmail(string email, CancellationToken cancellation) => Outage
        ? throw new InvalidOperationException("SYNTHETIC_SECRET_MUST_NEVER_ESCAPE") : Task.FromResult(Current);
    public Task<IReadOnlyList<Grant>> Grants(string email, CancellationToken cancellation) => Outage
        ? throw new InvalidOperationException("SYNTHETIC_SECRET_MUST_NEVER_ESCAPE") : Task.FromResult(CurrentGrants);
}
internal sealed class MemoryForms : IFormStore
{
    private readonly Dictionary<string, StoredForm> forms = [];
    private readonly object gate = new();
    internal IReadOnlyList<Collaboration> Links = [];
    internal bool Outage;
    public Task<IReadOnlyList<Unit>> Units(CancellationToken cancellation) => Outage
        ? throw new InvalidOperationException("SYNTHETIC_DATABASE_SECRET") : Task.FromResult<IReadOnlyList<Unit>>(Fixtures.Units);
    public Task<IReadOnlyList<Collaboration>> Collaborations(string email, CancellationToken cancellation) => Task.FromResult(Links);
    public Task<StoredForm?> Get(string unit, CancellationToken cancellation)
    {
        lock (gate) return Task.FromResult(forms.TryGetValue(unit, out var value) ? value with { Content = (JsonObject)value.Content.DeepClone() } : null);
    }
    public Task<StoredForm> Save(string unit, int expectedVersion, JsonObject content, string actor, CancellationToken cancellation)
    {
        lock (gate)
        {
            if ((forms.GetValueOrDefault(unit)?.Version ?? 0) != expectedVersion) throw new DomainProblem(409, "Conflicto de versión.");
            var saved = new StoredForm(unit, (JsonObject)content.DeepClone(), expectedVersion + 1, FormSchema.Progress(content), DateTimeOffset.UtcNow, actor);
            forms[unit] = saved;
            return Task.FromResult(saved);
        }
    }
}

// This scheme and its header exist ONLY in the verification executable, never in the web app.
internal sealed class SyntheticAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Synthetic-Session")) return Task.FromResult(AuthenticateResult.NoResult());
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "synthetic-id"),
            new Claim(ClaimTypes.Email, "persona@example.test"), new Claim(ClaimTypes.Name, "Persona sintética")], Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
internal sealed class Application : WebApplicationFactory<global::Program>
{
    internal FakeNexo Nexo { get; } = new();
    internal MemoryForms Store { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Tdv2"] = "" }));
        builder.ConfigureTestServices(services =>
        {
            services.Remove(services.Single(d => d.ImplementationType == typeof(Tdv2.Synchronization.ManualSyncWorker)));
            services.RemoveAll<IFormStore>(); services.AddSingleton<IFormStore>(Store);
            services.AddSingleton<Tdv2.Synchronization.ILocalCatalog,EmptyLocalCatalog>();
            services.RemoveAll<INexoProfiles>(); services.AddSingleton<INexoProfiles>(Nexo);
            services.RemoveAll<ISessionIdentity>(); services.AddSingleton<ISessionIdentity, SyntheticIdentity>();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddAuthentication(options => { options.DefaultAuthenticateScheme = "Synthetic"; options.DefaultChallengeScheme = "Synthetic"; })
                .AddScheme<AuthenticationSchemeOptions, SyntheticAuthentication>("Synthetic", _ => { });
        });
    }
    internal HttpClient Client(bool authenticated = true)
    {
        var client = CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
        if (authenticated) client.DefaultRequestHeaders.Add("X-Synthetic-Session", "yes");
        return client;
    }
}
internal sealed class SyntheticIdentity : ISessionIdentity
{
    public Task<bool> Matches(ClaimsPrincipal principal, CancellationToken cancellation) => Task.FromResult(principal.Identity?.AuthenticationType == "Synthetic");
}
internal sealed class EmptyLocalCatalog : Tdv2.Synchronization.ILocalCatalog
{
    public Task<Tdv2.Synchronization.UnitInventory> Inventory(Unit unit, CancellationToken ct) => Task.FromResult(new Tdv2.Synchronization.UnitInventory("pendiente",[],null));
    public Task<DateTimeOffset?> LastSii(CancellationToken ct) => Task.FromResult<DateTimeOffset?>(null);
}
