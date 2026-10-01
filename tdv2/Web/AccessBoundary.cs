using Tdv2.Domain;
namespace Tdv2.Web;

public static class AccessBoundary
{
    // Covers both HTML documents and JSON/mutations, including not-yet-implemented private routes.
    public static async Task Validate(HttpContext http)
    {
        var path = http.Request.Path;
        if (path == "/vista-prueba" && HttpMethods.IsDelete(http.Request.Method)
            || path == "/actuar-como-usuario" && HttpMethods.IsDelete(http.Request.Method)) return;
        var access = http.RequestServices.GetRequiredService<RequestAccess>();
        if (path.StartsWithSegments("/configuracion") || path.StartsWithSegments("/actuar-como-usuario") || path.StartsWithSegments("/vista-prueba"))
            await access.EnsureOwn(http);
        if (path.StartsWithSegments("/configuracion/sincronizaciones")) await access.Profile(http, "sincronizaciones");
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
                var unit = Uri.UnescapeDataString(remaining.Value!.TrimStart('/'));
                if (!context.Scopes.TryGetValue(unit, out var scope) || !HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method) && (!scope.Edit || context.ReadOnly))
                    throw new DomainProblem(403, "No tienes acceso al formato de esta UR.");
            }
        }
        else if (path.StartsWithSegments("/user")) await access.Profile(http);
    }
}
