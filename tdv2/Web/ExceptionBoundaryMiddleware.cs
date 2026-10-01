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

namespace Tdv2.Web;

/// <summary>Traduce fallos a mensajes públicos; nunca registra SQL, tokens ni excepciones completas.</summary>
public sealed class ExceptionBoundaryMiddleware(RequestDelegate next, ReactShell shell, ILogger<ExceptionBoundaryMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
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
                await shell.Write(context);
                return;
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
            logger.LogError("No se pudo completar la operación TDV2. Tipo: {Type}", error.GetType().Name);
            context.Response.StatusCode = 503;
            await context.Response.WriteAsJsonAsync(new { message = "No fue posible completar la operación. Inténtalo más tarde." });
        }
    }
}
