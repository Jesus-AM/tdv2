using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Tdv2.Web;
namespace Tdv2.NativeVerification;
internal static partial class NativeTests
{
    private static async Task VerifyDevelopmentStartup(NativeDatabase database)
    {
        // Usa los puertos y el comando del perfil compartido, con identidad/fuentes exclusivamente sintéticas.
        // No inicia Visual Studio ni accede al almacén de User Secrets del operador.
        var viteStart = new ProcessStartInfo("node") { WorkingDirectory = Path.Combine(database.Workspace, "ClientApp"),
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "node_modules/vite/bin/vite.js", "--host", "127.0.0.1", "--port", "5173", "--strictPort" }) viteStart.ArgumentList.Add(argument);
        viteStart.Environment.Remove("TDV2_TEST_CONNECTION"); viteStart.Environment.Remove("PGPASSWORD");
        using var vite = Process.Start(viteStart)!;
        var viteOut = vite.StandardOutput.ReadToEndAsync(); var viteErrors = vite.StandardError.ReadToEndAsync();
        try
        {
            using var check = new HttpClient();
            var ready = false;
            for (var attempt = 0; attempt < 80 && !vite.HasExited; attempt++) {
                try { ready = (await check.GetAsync("http://127.0.0.1:5173/__vite/index.html")).IsSuccessStatusCode; } catch (HttpRequestException) { }
                if (ready) break;
                await Task.Delay(250);
            }
            Check(ready && !vite.HasExited, "Vite no inició en 5173; no se detienen procesos ajenos.");
            using var certificate = NativeApplication.Certificate();
            await using var app = new NativeApplication(database) { Vite = true, PublicOrigin = "https://localhost:7136" };
            app.UseKestrel(options => { options.ListenLocalhost(7136, listen => listen.UseHttps(certificate)); options.ListenLocalhost(5064); });
            using var client = app.CreateClient(new() { AllowAutoRedirect = false });
            Check(app.Services.GetRequiredService<ReactShell>().UsesVite, "El host no habilitó el proxy Vite.");
            app.Reads.Count = 0;
            var run = new ProcessStartInfo("node") { WorkingDirectory = Path.Combine(database.Workspace, "ClientApp"),
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            run.ArgumentList.Add("tests/browser/visual-studio-startup.mjs");
            run.Environment.Remove("TDV2_TEST_CONNECTION"); run.Environment.Remove("PGPASSWORD");
            using var browser = Process.Start(run)!;
            var output = browser.StandardOutput.ReadToEndAsync(); var errors = browser.StandardError.ReadToEndAsync();
            await browser.WaitForExitAsync(); Console.Write(await output); Console.Write(await errors);
            Check(browser.ExitCode == 0 && app.Reads.Count == 0, "Arranque/HMR debe pasar sin consultas de datos.");
        }
        finally { if (!vite.HasExited) vite.Kill(entireProcessTree: true); await vite.WaitForExitAsync(); await Task.WhenAll(viteOut, viteErrors); }
    }
}
