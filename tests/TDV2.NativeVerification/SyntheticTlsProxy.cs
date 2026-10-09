using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Tdv2.NativeVerification;

// Opt-in Windows test transport: TLS stays on loopback, with no certificate/key store writes.
internal sealed class SyntheticTlsProxy(Process process, string origin) : IAsyncDisposable
{
    internal string Origin { get; } = origin;
    internal static async Task<SyntheticTlsProxy> Start(string workspace, string upstream, string token, X509Certificate2 certificate)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("TDV2_TEST_NODE") ?? "node") {
            WorkingDirectory = Path.Combine(workspace, "ClientApp"), UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add("tests/browser/synthetic-tls-proxy.mjs");
        start.Environment.Remove("TDV2_TEST_CONNECTION"); start.Environment.Remove("PGPASSWORD");
        var process = Process.Start(start)!;
        try {
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new {
                upstream, token, pfx = Convert.ToBase64String(certificate.Export(X509ContentType.Pfx))
            }));
            await process.StandardInput.FlushAsync();
            var origin = await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
            if (origin is null || !origin.StartsWith("https://127.0.0.1:", StringComparison.Ordinal))
                throw new InvalidOperationException("Synthetic loopback TLS startup failed.");
            return new(process, origin);
        } catch { if (!process.HasExited) process.Kill(true); process.Dispose(); throw; }
    }
    public async ValueTask DisposeAsync()
    {
        process.StandardInput.Close();
        try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (TimeoutException) { process.Kill(true); await process.WaitForExitAsync(); }
        process.Dispose();
    }
}
