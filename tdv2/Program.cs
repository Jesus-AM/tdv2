using Tdv2.Hosting;
using Tdv2.Synchronization;

var builder = WebApplication.CreateBuilder(args);
builder.AddTdv2();
var app = builder.Build();

// Entrega explícita de actividad pendiente; no aplica DDL ni activa un proceso remoto al arrancar el sitio.
if (args.Contains("--deliver-digital-activity"))
{
    using var scope = app.Services.CreateScope();
    using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    try
    {
        var delivery = scope.ServiceProvider.GetRequiredService<Tdv2.Competencias.DigitalActivityDelivery>();
        var delivered = await delivery.Flush(100, deadline.Token);
        var pending = await delivery.Pending(deadline.Token);
        Console.WriteLine($"Actividad de Competencias: {delivered} recibos confirmados en este lote; {pending} eventos pendientes, incluidos los que esperan reintento.");
        Environment.ExitCode = pending == 0 ? 0 : 2;
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"No se confirmó la entrega de actividad ({e.GetType().Name}). Revisar configuración y disponibilidad; los eventos persistidos se conservan.");
        Environment.ExitCode = 1;
    }
    await app.DisposeAsync();
    return;
}

// El sitio atiende la cola manual mediante un servicio hospedado; nunca migra ni
// crea ejecuciones programadas al arrancar. Los comandos conservan su ciclo explícito.
if (args.Any(a => a is "--sync-worker" or "--sync-once" || a.StartsWith("--sync-check=", StringComparison.Ordinal)))
{
    Environment.ExitCode = await SyncCommands.Run(app.Services, args);
    await app.DisposeAsync();
    return;
}

if (args.Contains("--import-applications") || args.Contains("--check-applications"))
{
    Environment.ExitCode = await ApplicationListCommands.Run(app.Services, args.Contains("--check-applications"));
    await app.DisposeAsync();
    return;
}

app.UseTdv2();
app.Run();

public partial class Program;
