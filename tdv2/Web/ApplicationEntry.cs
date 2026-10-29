using System.Security.Claims;
using Tdv2.Competencias;
using Tdv2.Domain;
using Tdv2.Security;

namespace Tdv2.Web;

// Destinos de interfaz, nunca API, URLs externas ni concesiones enviadas por el navegador.
public static class ApplicationEntry
{
    public const string Choices = "/accesos";
    public const string Unavailable = "No fue posible comprobar tu acceso. Inténtalo de nuevo más tarde.";
    public const string NoApplications = "Tu cuenta no tiene acceso a las secciones privadas.";
    public static string Denied(PathString path) => path.StartsWithSegments(DigitalRoutes.Base)
        ? "Tu cuenta no tiene acceso a Competencias digitales."
        : "Tu cuenta no tiene acceso a Procedimientos institucionales.";

    public static string SafeReturn(string? value, IEnumerable<string>? siteDestinations = null)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 2048 || !value.StartsWith('/') || value.StartsWith("//")
            || value.Contains('\\') || value.Any(char.IsControl)) return Choices;
        var path = value.Split('?', '#')[0];
        // No aceptar formas codificadas o segmentos que el navegador normalice hacia otra ruta.
        if (path.Contains('%') || path.Contains("//") || path.Split('/').Any(s => s is "." or "..")) return Choices;
        return IsDestination(path) || siteDestinations?.Contains(path, StringComparer.OrdinalIgnoreCase) == true ? value : Choices;
    }
    public static bool IsDestination(PathString path) => path == "/" || path == Choices || DigitalRoutes.Page(path)
        || path == "/inicio" || path == "/colaboradores" || path == "/configuracion"
        || path == "/configuracion/procesos" || path == "/configuracion/sincronizaciones"
        || path == "/configuracion/pruebas-acceso" || path == "/configuracion/pruebas-acceso/rol-area"
        || path == "/configuracion/pruebas-acceso/actuar-como-usuario"
        || path == "/vista-prueba" || path == "/actuar-como-usuario"
        || path.StartsWithSegments("/formatos", out var rest) && rest.Value is { Length: > 1 } unit
            && !unit[1..].Contains('/') && unit[1..].All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    public static async Task RequireIdentity(HttpContext http)
    {
        if (http.User.FindFirstValue(ClaimTypes.Email) is null
            || !await http.RequestServices.GetRequiredService<ISessionIdentity>().Matches(http.User, http.RequestAborted))
            throw new DomainProblem(401, "Inicia sesión con tu cuenta institucional.");
    }
    public sealed record Option(string Name, string Path);
    public sealed record Available(IReadOnlyList<Option> Options, bool Unavailable)
    {
        // Una consulta fallida no demuestra que exista un único sitio autorizado.
        [System.Text.Json.Serialization.JsonIgnore]
        public string? DirectDestination => !Unavailable && Options.Count == 1 ? Options[0].Path : null;
    }

    public static Task<Available> Find(HttpContext http, PathString? exclude = null)
        => http.RequestServices.GetRequiredService<PortalSites>().Find(http, exclude);
}
