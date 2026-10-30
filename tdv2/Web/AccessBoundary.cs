using Tdv2.Domain;
using Tdv2.Security;
using Microsoft.Extensions.Options;
using Tdv2.Integrations.Microsoft;
namespace Tdv2.Web;

public static class AccessBoundary
{
    // Protege tanto HTML como JSON/escrituras, incluidas rutas privadas todavía no implementadas.
    public static async Task Validate(HttpContext http)
    {
        var path = http.Request.Path;
        if(path=="/session/status" || path=="/session/expire")
        {
            if(http.User.Identity?.IsAuthenticated!=true) throw new DomainProblem(401,"Inicia sesión con tu cuenta institucional.");
            SessionLifetime.Require(http.User,http.RequestServices.GetRequiredService<TimeProvider>()); return;
        }
        if (path == ApplicationEntry.Choices) { await ApplicationEntry.RequireIdentity(http); return; }
        if (path.StartsWithSegments(Tdv2.Competencias.DigitalRoutes.Base))
        {
            if (path == "/competencias-digitales/api/representacion" && HttpMethods.IsDelete(http.Request.Method)) return;
            var digital = http.RequestServices.GetRequiredService<Tdv2.Competencias.DigitalAccess>();
            var slug = path.Value!.TrimEnd('/').Split('/').Last();
            var module = Array.IndexOf(Tdv2.Competencias.DigitalRoutes.Slugs, slug);
            if (Tdv2.Competencias.DigitalRoutes.Page(path) && module >= 0) await digital.Require(http, module);
            else await digital.Resolve(http);
            return;
        }
        if (path == "/vista-prueba" && HttpMethods.IsDelete(http.Request.Method)
            || path == "/actuar-como-usuario" && HttpMethods.IsDelete(http.Request.Method)) return;
        var access = http.RequestServices.GetRequiredService<RequestAccess>();
        if (path.StartsWithSegments("/configuracion") || path.StartsWithSegments("/actuar-como-usuario") || path.StartsWithSegments("/vista-prueba"))
            await access.EnsureOwn(http);
        if (path.StartsWithSegments("/configuracion/procesos")) await access.Profile(http, "configuracion_procesos");
        else if (path.StartsWithSegments("/configuracion/sincronizaciones")) await access.Profile(http, "sincronizaciones");
        else if (path.StartsWithSegments("/configuracion/pruebas-acceso") || path.StartsWithSegments("/actuar-como-usuario") || path.StartsWithSegments("/vista-prueba"))
        {
            await access.Profile(http, "pruebas_acceso");
            if (path == "/configuracion/pruebas-acceso/rol-area" || path.StartsWithSegments("/vista-prueba")) await access.Profile(http, "procesos_operativos");
        }
        else if (path.StartsWithSegments("/configuracion")) await access.Profile(http, "configuracion");
        else if (path == "/inicio" || path.StartsWithSegments("/colaboradores") || path.StartsWithSegments("/formatos"))
        {
            var context = await access.Resolve(http);
            if (path.StartsWithSegments("/formatos", out var remaining) && remaining.HasValue)
            {
                var unit = Uri.UnescapeDataString(remaining.Value!.TrimStart('/').Split('/')[0]);
                if (!context.Scopes.TryGetValue(unit, out var scope) || !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method) && (!scope.Edit || context.ReadOnly))
                    throw new DomainProblem(403, "No tienes acceso al formato de esta UR.");
            }
        }
        else if (path.StartsWithSegments("/form-events"))
        {
            var origin = http.Request.Headers.Origin.ToString();
            // El proxy puede terminar TLS; el origen permitido es configuración del servidor,
            // nunca Host ni X-Forwarded-* proporcionados por el cliente.
            var allowedOrigin = http.RequestServices.GetRequiredService<IOptions<MicrosoftSettings>>().Value.PublicOrigin.TrimEnd('/');
            if (origin.Length > 0 && !string.Equals(origin, allowedOrigin, StringComparison.OrdinalIgnoreCase))
                throw new DomainProblem(403, "Origen no autorizado.");
            await access.Resolve(http);
        }
        else if (path == "/user/photo" && http.Request.Headers["X-TDV2-Application"] == "competencias-digitales")
            await http.RequestServices.GetRequiredService<Tdv2.Competencias.DigitalAccess>().Resolve(http);
        else if (path.StartsWithSegments("/user")) await access.Profile(http);
        else if (http.RequestServices.GetRequiredService<PortalSites>().ForPath(path) is { } site)
        {
            // Un sitio futuro conserva la comprobación en URL directa, no sólo en el selector.
            // Sus endpoints deben aplicar además los alcances y reglas propios del recurso.
            await ApplicationEntry.RequireIdentity(http);
            if (!await site.CanOpen(http)) throw new DomainProblem(403, $"Tu cuenta no tiene acceso a {site.Name}.");
        }
    }
}
