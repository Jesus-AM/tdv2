using Microsoft.Extensions.Options;

namespace Tdv2.Web;

public sealed class ReactDevelopmentOptions
{
    public bool UseVite { get; set; }
}

/// <summary>Sirve la misma interfaz desde Vite en depuración y desde wwwroot al publicar.</summary>
public sealed class ReactShell(IWebHostEnvironment environment, IOptions<ReactDevelopmentOptions> options, IHttpClientFactory clients, ILogger<ReactShell> logger)
{
    // Vite sólo escucha en loopback. El navegador mantiene HTTPS, cookies y CSRF en 7136.
    public const string ViteOrigin = "http://127.0.0.1:5173";
    public bool UsesVite => environment.IsDevelopment() && options.Value.UseVite;

    public async Task Write(HttpContext http)
    {
        http.Response.ContentType = "text/html; charset=utf-8";
        try
        {
            if (UsesVite)
            {
                using var response = await clients.CreateClient("vite").GetAsync(ViteOrigin + "/__vite/index.html", http.RequestAborted);
                response.EnsureSuccessStatusCode();
                await response.Content.CopyToAsync(http.Response.Body, http.RequestAborted);
                return;
            }
            var file = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "index.html");
            if (!File.Exists(file)) { await Unavailable(http); return; }
            await http.Response.SendFileAsync(file, http.RequestAborted);
        }
        catch (Exception error) when (!http.Response.HasStarted && !http.RequestAborted.IsCancellationRequested)
        {
            // También se llama desde el manejador de un rechazo: una caída de Vite no debe
            // provocar otra excepción ni exponer detalles de conexión al intentar mostrarlo.
            logger.LogWarning("Interfaz TDV2 no disponible. Tipo: {Type}", error.GetType().Name);
            await Unavailable(http);
        }
    }

    private static Task Unavailable(HttpContext http)
    {
        http.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        http.Response.ContentType = "application/json; charset=utf-8";
        return http.Response.WriteAsJsonAsync(new { message = "La interfaz de TDV2 no está disponible." });
    }
}
