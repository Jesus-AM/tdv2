using Tdv2.Competencias;
using Tdv2.Domain;

namespace Tdv2.Web;

// Registro de integración del servidor. Nunca se recibe desde el navegador ni concede permisos.
public sealed record PortalSite(string Name, string Destination, int Order,
    Func<HttpContext, Task<bool>> CanOpen, Func<PathString, bool> OwnsRoute);

public sealed class PortalSites
{
    private readonly PortalSite[] sites;
    public PortalSites(IEnumerable<PortalSite> registrations)
    {
        sites = registrations.OrderBy(s => s.Order).ThenBy(s => s.Destination, StringComparer.Ordinal).ToArray();
        if (sites.Any(s => string.IsNullOrWhiteSpace(s.Name) || !ValidDestination(s.Destination))
            || sites.Select(s => s.Destination).Distinct(StringComparer.OrdinalIgnoreCase).Count() != sites.Length)
            throw new InvalidOperationException("El registro de sitios contiene destinos inválidos o duplicados.");
    }
    private static bool ValidDestination(string path) => path.Length > 1 && path.Length <= 2048
        && path.StartsWith('/') && !path.EndsWith('/') && !path.Contains("//")
        && !path.Any(c => char.IsControl(c) || c is '\\' or '%' or '?' or '#')
        && !path.Split('/').Any(s => s is "." or "..")
        && !path.Equals(ApplicationEntry.Choices, StringComparison.OrdinalIgnoreCase) && !path.Equals("/connect", StringComparison.OrdinalIgnoreCase)
        && !new PathString(path).StartsWithSegments("/session");

    public string SafeReturn(string? value) => ApplicationEntry.SafeReturn(value, sites.Select(s => s.Destination));
    public PortalSite? ForPath(PathString path) => sites.FirstOrDefault(s => s.OwnsRoute(path));
    public bool IsLanding(PathString path) => sites.Any(s => path == s.Destination);

    public async Task<ApplicationEntry.Available> Find(HttpContext http, PathString? exclude = null)
    {
        await ApplicationEntry.RequireIdentity(http);
        var options = new List<ApplicationEntry.Option>(); var unavailable = false;
        // Secuencial: los verificadores comparten servicios/DbContexts de esta solicitud.
        foreach (var site in sites)
        {
            if (exclude is { } path && site.OwnsRoute(path)) continue;
            try
            {
                if (await site.CanOpen(http)) options.Add(new(site.Name, site.Destination));
            }
            catch (DomainProblem e) when (e.Status is 403 or 409) { /* Denegado o contexto incompatible. */ }
            catch (DomainProblem e) when (e.Status != 401) { unavailable = true; }
            catch (Exception e) when (e is not OperationCanceledException && e is not DomainProblem)
            {
                http.RequestServices.GetRequiredService<ILogger<PortalSites>>()
                    .LogWarning("No se pudo comprobar un sitio. Tipo: {Type}", e.GetType().Name);
                unavailable = true;
            }
        }
        return new(options, unavailable);
    }

    public static void Register(IServiceCollection services)
    {
        services.AddScoped<PortalSites>();
        services.AddSingleton(new PortalSite("Procedimientos institucionales", "/inicio", 10,
            async http => { await http.RequestServices.GetRequiredService<RequestAccess>().Resolve(http); return true; },
            path => new[] { "/inicio", "/formatos", "/colaboradores", "/configuracion", "/vista-prueba", "/actuar-como-usuario", "/user", "/form-events" }.Any(p => path.StartsWithSegments(p))));
        services.AddSingleton(new PortalSite("Competencias digitales", DigitalRoutes.Base, 20,
            async http => (await http.RequestServices.GetRequiredService<DigitalAccess>().Resolve(http)).MenuPages.Any(),
            path => path.StartsWithSegments(DigitalRoutes.Base)));
    }
}
