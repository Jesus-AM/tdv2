using System.Security.Cryptography;
using System.Text.Json;

namespace Tdv2.Verification;

internal static class PublicAssetsTests
{
    // HTTP real del pipeline ASP.NET mediante TestServer; dobles de negocio, sin base.
    internal static async Task Run(string destination)
    {
        await using var app = new Application();
        using var client = app.Client(authenticated: false);
        string[] paths = ["/", "/quiz/microsoft-365/index.html", "/quiz/microsoft-365/quiz.css",
            "/quiz/microsoft-365/quiz.js", "/quiz/microsoft-365/quiz-controls.js",
            "/fonts/inter/inter.css", "/fonts/inter/InterVariable.woff2", "/fonts/inter/InterVariable-Italic.woff2",
            "/fonts/inter/LICENSE.txt", "/css/public-quiz.css", "/js/public-quiz.js", "/js/public-navigation.js",
            "/fonts/roboto/roboto.css", "/fonts/roboto/RobotoVariable.ttf", "/fonts/roboto/RobotoVariable-Italic.ttf", "/fonts/roboto/OFL.txt"];
        var results = new List<object>();
        foreach (var path in paths)
        {
            using var response = await client.GetAsync(path);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length == 0 || response.Headers.GetValues("X-Frame-Options").Single() != "DENY"
                || response.Headers.GetValues("X-Content-Type-Options").Single() != "nosniff")
                throw new Exception("Recurso público o cabeceras incorrectos: " + path);
            if (path.EndsWith(".woff2") && (response.Content.Headers.ContentType?.MediaType != "font/woff2"
                || !bytes.AsSpan(0, 4).SequenceEqual("wOF2"u8))) throw new Exception("Fuente WOFF2 inválida.");
            if (path.EndsWith(".ttf") && (response.Content.Headers.ContentType?.MediaType != "application/x-font-ttf"
                || !bytes.AsSpan(0, 4).SequenceEqual(new byte[] { 0, 1, 0, 0 }))) throw new Exception("Fuente TTF inválida.");
            results.Add(new { path, status = (int)response.StatusCode, contentType = response.Content.Headers.ContentType?.MediaType,
                sha256 = Convert.ToHexString(SHA256.HashData(bytes)), frameOptions = "DENY", bytes = bytes.Length });
        }
        // También exporta la rama autenticada de Razor con la identidad sintética de TestServer.
        using var authenticated = app.Client();
        using var authenticatedResponse = await authenticated.GetAsync("/");
        authenticatedResponse.EnsureSuccessStatusCode();
        var authenticatedHtml = await authenticatedResponse.Content.ReadAsStringAsync();
        if (!authenticatedHtml.Contains("Ir a mi espacio") || !authenticatedHtml.Contains("action=\"/logout\""))
            throw new Exception("La navegación autenticada no conserva sus acciones.");
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(destination)!, "public-authenticated.html"), authenticatedHtml);
        await File.WriteAllTextAsync(destination, JsonSerializer.Serialize(new { passed = true, postgres = false, anonymous = true, syntheticAuthenticatedPage = true, results }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
