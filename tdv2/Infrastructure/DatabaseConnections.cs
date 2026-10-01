using Npgsql;
using Tdv2.Domain;
namespace Tdv2.Infrastructure;

public sealed class DatabaseConnections(IConfiguration configuration)
{
    public async Task<NpgsqlConnection> Open(string name, CancellationToken cancellation = default)
    {
        var value = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(value)) throw new DomainProblem(503, "La conexión requerida no está configurada.");
        var settings = new NpgsqlConnectionStringBuilder(value) { IncludeErrorDetail = false, LogParameters = false };
        var connection = new NpgsqlConnection(settings.ConnectionString);
        try { await connection.OpenAsync(cancellation); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
}
