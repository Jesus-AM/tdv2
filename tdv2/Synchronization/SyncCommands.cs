using System.Runtime.InteropServices;
namespace Tdv2.Synchronization;

public static class SyncCommands
{
    private static async Task Pulse(IServiceProvider services, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<SyncCoordinator>().Heartbeat(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception error) { Console.Error.WriteLine("No se pudo actualizar el estado del procesador. Tipo: " + error.GetType().Name); }
        }
    }
    public static async Task<int> Run(IServiceProvider services, string[] args)
    {
        using var cancel = new CancellationTokenSource();
        using var terminate = OperatingSystem.IsWindows() ? null : PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => { context.Cancel = true; cancel.Cancel(); });
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cancel.Cancel(); }; Console.CancelKeyPress += handler;
        var pulse = args.Contains("--sync-worker") ? Pulse(services, cancel.Token) : Task.CompletedTask;
        try
        {
            var check = args.FirstOrDefault(a => a.StartsWith("--sync-check=", StringComparison.Ordinal));
            if (check is not null)
            {
                var name = check[13..]; if (name is not ("sii" or "ilda" or "ambas")) { Console.Error.WriteLine("Usa --sync-check=sii|ilda|ambas."); return 2; }
                using var scope = services.CreateScope(); var sync = scope.ServiceProvider.GetRequiredService<SyncCoordinator>();
                foreach (var source in name == "ambas" ? new[] { "sii", "ilda" } : [name]) Console.WriteLine(source + ": " + (await sync.Check(source, cancel.Token)).ToJsonString());
                return 0;
            }
            var loop = args.Contains("--sync-worker");
            do
            {
                try
                {
                    using var scope = services.CreateScope(); var run = await scope.ServiceProvider.GetRequiredService<SyncCoordinator>().Tick(cancel.Token);
                    Console.WriteLine(run is null ? "Sin ejecuciones pendientes." : "Ejecución " + run["id"] + ": " + run["estado"]);
                    if (!loop) return run?["estado"]?.ToString() is "fallida" or "parcial" ? 1 : 0;
                }
                catch (OperationCanceledException) when (cancel.IsCancellationRequested) { break; }
                catch (Exception error)
                {
                    Console.Error.WriteLine("No se pudo procesar la sincronización. Tipo: " + error.GetType().Name);
                    if (!loop) return 1;
                }
                await Task.Delay(TimeSpan.FromMinutes(1), cancel.Token);
            } while (!cancel.IsCancellationRequested);
            return 0;
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested) { return 0; }
        catch (Exception error) { Console.Error.WriteLine("La comprobación no se completó. Tipo: " + error.GetType().Name); return 1; }
        finally
        {
            cancel.Cancel();
            try { await pulse; } catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
            Console.CancelKeyPress -= handler;
        }
    }
}
