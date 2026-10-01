using Npgsql;
using Tdv2.Infrastructure;
namespace Tdv2.Security;

public sealed class PostgresOAuthAttempts(DatabaseConnections connections, ProtectedValues crypto)
{
    public async Task<(string State, string Browser, string Challenge)> Create(CancellationToken cancellation)
    {
        var state = ProtectedValues.Random(); var browser = ProtectedValues.Random(); var verifier = ProtectedValues.Random();
        await using var connection = await connections.Open("Tdv2", cancellation);
        await using var command = new NpgsqlCommand("""
            INSERT INTO tdv2_oauth_attempts(state_hash,browser_hash,verifier,expires_at)
            VALUES ($1,$2,$3,clock_timestamp() + interval '10 minutes')
            """, connection);
        command.Parameters.AddWithValue(ProtectedValues.Hash(state)); command.Parameters.AddWithValue(ProtectedValues.Hash(browser));
        command.Parameters.AddWithValue(crypto.Protect("oauth-pkce", verifier));
        await command.ExecuteNonQueryAsync(cancellation);
        return (state, browser, ProtectedValues.Challenge(verifier));
    }
    public async Task<string?> Consume(string state, string browser, CancellationToken cancellation)
    {
        if (state.Length is < 32 or > 128 || browser.Length is < 32 or > 128) return null;
        await using var connection = await connections.Open("Tdv2", cancellation);
        await using var command = new NpgsqlCommand("""
            DELETE FROM tdv2_oauth_attempts WHERE state_hash=$1 AND browser_hash=$2 AND expires_at>clock_timestamp()
            RETURNING verifier
            """, connection);
        command.Parameters.AddWithValue(ProtectedValues.Hash(state)); command.Parameters.AddWithValue(ProtectedValues.Hash(browser));
        return await command.ExecuteScalarAsync(cancellation) is string value ? crypto.Unprotect("oauth-pkce", value) : null;
    }
}
