using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
using Tdv2.Infrastructure;
namespace Tdv2.Security;

public sealed class PostgresTicketStore(DatabaseConnections connections, ProtectedValues crypto) : ITicketStore
{
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = ProtectedValues.Random();
        var identity = (System.Security.Claims.ClaimsIdentity)ticket.Principal.Identity!;
        foreach (var previous in identity.FindAll(AccessState.Claim).ToArray()) identity.RemoveClaim(previous);
        identity.AddClaim(new(AccessState.Claim, ProtectedValues.Hash(key)));
        await Write(key, ticket, false); return key;
    }
    public Task RenewAsync(string key, AuthenticationTicket ticket) => Write(key, ticket, true);
    private async Task Write(string key, AuthenticationTicket ticket, bool renew)
    {
        await using var connection = await connections.Open("Tdv2");
        await using var command = new NpgsqlCommand(renew
            ? "UPDATE tdv2_sessions SET ticket=$2,expires_at=$3 WHERE id_hash=$1 AND expires_at>clock_timestamp()"
            : "INSERT INTO tdv2_sessions(id_hash,ticket,expires_at) VALUES($1,$2,$3)", connection);
        command.Parameters.AddWithValue(ProtectedValues.Hash(key));
        command.Parameters.AddWithValue(crypto.Protect("session", Convert.ToBase64String(TicketSerializer.Default.Serialize(ticket))));
        command.Parameters.AddWithValue(ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddHours(2));
        await command.ExecuteNonQueryAsync(); // Renewal never recreates a session removed by logout.
    }
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        await using var connection = await connections.Open("Tdv2");
        await using var command = new NpgsqlCommand("SELECT ticket FROM tdv2_sessions WHERE id_hash=$1 AND expires_at>clock_timestamp()", connection);
        command.Parameters.AddWithValue(ProtectedValues.Hash(key));
        if (await command.ExecuteScalarAsync() is not string value) return null;
        return TicketSerializer.Default.Deserialize(Convert.FromBase64String(crypto.Unprotect("session", value)));
    }
    public async Task RemoveAsync(string key)
    {
        await using var connection = await connections.Open("Tdv2");
        await using var command = new NpgsqlCommand("DELETE FROM tdv2_sessions WHERE id_hash=$1", connection);
        command.Parameters.AddWithValue(ProtectedValues.Hash(key)); await command.ExecuteNonQueryAsync();
    }
}
