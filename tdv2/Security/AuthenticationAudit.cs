using Npgsql;
using Tdv2.Infrastructure;
namespace Tdv2.Security;

public sealed class AuthenticationAudit(DatabaseConnections connections, ILogger<AuthenticationAudit> logger)
{
    public async Task Record(string action, string? email, string? name, CancellationToken cancellation)
    {
        try
        {
            await using var connection = await connections.Open("Tdv2", cancellation);
            await using var command = new NpgsqlCommand("""
                INSERT INTO activity_logs(user_email,user_name,entity,action,created_at,updated_at)
                VALUES($1,$2,'auth',$3,timezone('UTC',now()),timezone('UTC',now()))
                """, connection);
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text, Value = (object?)email ?? DBNull.Value });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text, Value = (object?)name ?? DBNull.Value });
            command.Parameters.AddWithValue(action); await command.ExecuteNonQueryAsync(cancellation);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        { logger.LogWarning("No se pudo registrar autenticación. Tipo: {Type}", error.GetType().Name); }
    }
}
