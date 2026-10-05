using Microsoft.AspNetCore.Antiforgery;

namespace Tdv2.Web;

/// <summary>Aplica las mismas reglas a documentos HTML, datos JSON y escrituras.</summary>
public sealed class RequestBoundaryMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery csrf)
    {
        // WebSocket sobre HTTP/2 utiliza CONNECT. Este canal sólo entrega consultas y valida
        // Origin, sesión y alcance; las mutaciones siguen siendo HTTP con antiforgery obligatorio.
        var collaborationSocket = context.Request.Path == "/form-events" && context.WebSockets.IsWebSocketRequest;
        if (!collaborationSocket && !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
            await csrf.ValidateRequestAsync(context);
        await next(context);
    }
}

/// <summary>Se ejecuta después de UseAuthorization: solicitar HTML nunca evita la política.</summary>
public sealed class ReactPageMiddleware(RequestDelegate next, ReactShell shell)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) && context.Request.Headers.Accept.Any(v => v?.Contains("text/html") == true)
            && !context.Request.Headers.ContainsKey("X-TDV2-Page") && PagePaths.IsPage(context.Request.Path))
        {
            await shell.Write(context);
            return;
        }
        await next(context);
    }
}
