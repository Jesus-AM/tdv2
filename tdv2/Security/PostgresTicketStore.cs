using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
using Tdv2.Infrastructure;
namespace Tdv2.Security;

public sealed class PostgresTicketStore(DatabaseConnections connections, ProtectedValues crypto, TimeProvider clock, IHttpContextAccessor accessor) : ITicketStore
{
    public static async Task Revoke(NpgsqlConnection connection, NpgsqlTransaction transaction, string sessionHash, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("DELETE FROM tdv2_sessions WHERE id_hash=$1", connection, transaction);
        command.Parameters.AddWithValue(sessionHash);
        await command.ExecuteNonQueryAsync(ct);
    }

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
        if(renew)
        {
            var previous=await RetrieveAsync(key);
            if(previous is null || SessionLifetime.Item(previous.Properties,SessionLifetime.StartKey)!=SessionLifetime.Item(ticket.Properties,SessionLifetime.StartKey)) return;
            ticket.Properties.ExpiresUtc=previous.Properties.ExpiresUtc;
        }
        if(!SessionLifetime.Valid(ticket,clock)) throw new InvalidOperationException("La sesión no conserva su inicio original.");
        ticket.Properties.AllowRefresh=false;
        await using var connection = await connections.Open("Tdv2");
        await using var command = new NpgsqlCommand(renew
            ? "UPDATE tdv2_sessions SET ticket=$2,expires_at=LEAST(expires_at,$3) WHERE id_hash=$1 AND expires_at>clock_timestamp()"
            : "INSERT INTO tdv2_sessions(id_hash,ticket,expires_at) VALUES($1,$2,$3)", connection);
        command.Parameters.AddWithValue(ProtectedValues.Hash(key));
        command.Parameters.AddWithValue(crypto.Protect("session", Convert.ToBase64String(TicketSerializer.Default.Serialize(ticket))));
        command.Parameters.AddWithValue(SessionLifetime.Deadline(ticket));
        await command.ExecuteNonQueryAsync(); // La renovación no recrea una sesión revocada por logout.
    }
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        await using var connection = await connections.Open("Tdv2");
        await using var command = new NpgsqlCommand("SELECT ticket,expires_at FROM tdv2_sessions WHERE id_hash=$1", connection);
        command.Parameters.AddWithValue(ProtectedValues.Hash(key));
        await using var reader=await command.ExecuteReaderAsync();
        if(!await reader.ReadAsync()) return null;
        var ticket=TicketSerializer.Default.Deserialize(Convert.FromBase64String(crypto.Unprotect("session",reader.GetString(0))));
        if(!SessionLifetime.Valid(ticket,clock) || reader.GetFieldValue<DateTimeOffset>(1)<=clock.GetUtcNow())
        { if(accessor.HttpContext is { } http) http.Items[SessionLifetime.ExpiredTicketKey]=ticket; return null; }
        if(accessor.HttpContext is { } active) active.Items[SessionLifetime.TicketKey]=ticket;
        return ticket;
    }
    public async Task RemoveAsync(string key)
    {
        await using var connection = await connections.Open("Tdv2");
        await using var command = new NpgsqlCommand("DELETE FROM tdv2_sessions WHERE id_hash=$1", connection);
        command.Parameters.AddWithValue(ProtectedValues.Hash(key)); await command.ExecuteNonQueryAsync();
    }
}
