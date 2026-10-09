using Tdv2.Hosting;
using Tdv2.Synchronization;

var builder = WebApplication.CreateBuilder(args);
builder.AddTdv2();
var app = builder.Build();

// El sitio atiende la cola manual mediante un servicio hospedado; nunca migra ni
// crea ejecuciones programadas al arrancar. Los comandos conservan su ciclo explícito.
if (args.Any(a => a is "--sync-worker" or "--sync-once" || a.StartsWith("--sync-check=", StringComparison.Ordinal)))
{
    Environment.ExitCode = await SyncCommands.Run(app.Services, args);
    await app.DisposeAsync();
    return;
}

app.UseTdv2();
app.Run();

public partial class Program;
