using Microsoft.AspNetCore.Antiforgery;

namespace Tdv2.Web;

/// <summary>Aplica las mismas reglas a documentos HTML, datos JSON y escrituras.</summary>
public sealed class RequestBoundaryMiddleware(RequestDelegate next, ReactShell shell)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery csrf)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
            await csrf.ValidateRequestAsync(context);
        // Autorizar antes de servir React evita eludir permisos solicitando text/html.
        await AccessBoundary.Validate(context);
        if (HttpMethods.IsGet(context.Request.Method) && context.Request.Headers.Accept.Any(v => v?.Contains("text/html") == true)
            && !context.Request.Headers.ContainsKey("X-TDV2-Page") && PagePaths.IsPage(context.Request.Path))
        {
            await shell.Write(context);
            return;
        }
        await next(context);
    }
}
