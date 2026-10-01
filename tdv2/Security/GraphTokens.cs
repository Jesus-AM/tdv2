using Npgsql;
using Tdv2.Infrastructure;
namespace Tdv2.Security;

public sealed class GraphTokens(DatabaseConnections connections, ProtectedValues crypto, MicrosoftClient microsoft)
{
    public async Task<string?> Ensure(long user, CancellationToken cancellation, string? rejected = null)
    {
        await using var connection = await connections.Open("Tdv2", cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        string access, refresh; long expires;
        await using (var command = new NpgsqlCommand("SELECT access_token,refresh_token,expires FROM ms_graph_tokens WHERE user_id=$1 FOR UPDATE", connection, transaction))
        {
            command.Parameters.AddWithValue(user);
            await using var reader = await command.ExecuteReaderAsync(cancellation);
            if (!await reader.ReadAsync(cancellation)) return null;
            access = crypto.Unprotect("graph-access", reader.GetString(0));
            refresh = crypto.Unprotect("graph-refresh", reader.GetString(1));
            expires = long.Parse(reader.GetString(2), System.Globalization.CultureInfo.InvariantCulture);
        }
        if (expires > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60 && (rejected is null || access != rejected)) return access;
        var tokens = await microsoft.Refresh(refresh, cancellation); // No reintentar la transacción: canjear dos veces un refresh token puede invalidarlo.
        await using (var command = new NpgsqlCommand("UPDATE ms_graph_tokens SET access_token=$2,refresh_token=$3,expires=$4,updated_at=timezone('UTC',now()) WHERE user_id=$1", connection, transaction))
        {
            command.Parameters.AddWithValue(user); command.Parameters.AddWithValue(crypto.Protect("graph-access", tokens.Access));
            command.Parameters.AddWithValue(crypto.Protect("graph-refresh", tokens.Refresh)); command.Parameters.AddWithValue(tokens.Expires.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync(cancellation);
        }
        await transaction.CommitAsync(cancellation); return tokens.Access;
    }
}
