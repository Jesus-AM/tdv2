using Tdv2.Domain;
using Microsoft.Extensions.Options;
using Tdv2.Integrations.Microsoft;
namespace Tdv2.Web;

public static class AccessBoundary
{
    // Protege tanto HTML como JSON/escrituras, incluidas rutas privadas todavía no implementadas.
    public static async Task Validate(HttpContext http)
    {
        var path = http.Request.Path;
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
        else if (path.StartsWithSegments("/user")) await access.Profile(http);
    }
}
