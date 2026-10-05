using Tdv2.Web;

namespace Tdv2.Hosting;

public static class ApplicationPipeline
{
    public static void UseTdv2(this WebApplication app)
    {
        app.UseMiddleware<ExceptionBoundaryMiddleware>();
        if (app.Services.GetRequiredService<ReactShell>().UsesVite)
        {
            // Sólo assets de desarrollo y WebSocket HMR. API, portada y OAuth siguen en ASP.NET.
            app.MapWhen(context => context.Request.Path.StartsWithSegments("/__vite"), branch =>
                branch.UseSpa(spa => spa.UseProxyToSpaDevelopmentServer(ReactShell.ViteOrigin)));
        }
        app.UseStaticFiles();
        app.UseWebSockets();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseMiddleware<RequestBoundaryMiddleware>();
        app.UseAuthorization();
        app.UseMiddleware<ReactPageMiddleware>();
        app.MapControllers();
        app.MapHub<Tdv2.Controllers.FormHub>("/form-events");
        app.MapFallback(() => Results.Json(new { message = "La ruta aún no está implementada en la migración ASP.NET." }, statusCode: 501));
    }
}
