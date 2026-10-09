using Tdv2.Domain;

namespace Tdv2.Synchronization;

// La señal sólo adelanta la consulta. PostgreSQL conserva el trabajo aunque se pierda
// una señal, se cierre el navegador o se reinicie cualquiera de los procesos.
public sealed class ManualSyncDispatcher : IDisposable
{
    private readonly SemaphoreSlim wake = new(0, 1);
    private volatile bool running;
    private volatile string? failure;
    public object Status => new { disponible = running && failure is null, error = Error };
    public string? Error => !running
        ? "El procesamiento manual de esta instancia no está disponible. Actualiza el estado y vuelve a intentarlo."
        : failure;
    public void Started() { running = true; failure = null; }
    public void Stopped() => running = false;
    public void Healthy() => failure = null;
    public void Failed() => failure = "No se pudo acceder a la cola local de sincronizaciones. Se conservaron las solicitudes; el servidor volverá a comprobarla. Revisa PostgreSQL y sus migraciones si el problema continúa.";
    public void RequireAvailable() { if (Error is { } error) throw new DomainProblem(503, error); }
    public void Wake() { try { wake.Release(); } catch (SemaphoreFullException) { } }
    public Task<bool> Wait(CancellationToken ct) => wake.WaitAsync(TimeSpan.FromSeconds(2), ct);
    public void Dispose() => wake.Dispose();
}

public sealed class ManualSyncWorker(IServiceScopeFactory scopes, ManualSyncDispatcher dispatcher,
    ILogger<ManualSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        dispatcher.Started();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    // Mismo coordinador, publicación y exclusión que el CLI; no crea horarios.
                    await scope.ServiceProvider.GetRequiredService<SyncCoordinator>().Tick(stoppingToken, manualOnly: true);
                    dispatcher.Healthy();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception error)
                {
                    dispatcher.Failed();
                    logger.LogWarning("No se pudo procesar la cola manual local. Tipo {Type}", error.GetType().Name);
                }
                await dispatcher.Wait(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally { dispatcher.Stopped(); }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        dispatcher.Stopped();
        return base.StopAsync(cancellationToken);
    }
}
