using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Infrastructure;
namespace Tdv2.Security;

public interface ISessionIdentity { Task<bool> Matches(ClaimsPrincipal principal, CancellationToken cancellation); }
public sealed class PostgresIdentity(DatabaseConnections connections, ProtectedValues crypto, IOptions<MicrosoftSettings> settings) : ISessionIdentity
{
    public const string SessionVersion = "aspnet-1";
    public async Task<ClaimsPrincipal> Save(MicrosoftPerson microsoft, Profile profile, MicrosoftTokens tokens, CancellationToken cancellation)
    {
        var tenant = Guid.Parse(settings.Value.TenantId); var objectId = Guid.Parse(microsoft.ObjectId);
        if (profile.User.Email != microsoft.Email) throw new DomainProblem(403, "La identidad institucional no coincide.");
        await using var connection = await connections.Open("Tdv2", cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        // El bloqueo transaccional serializa primeros inicios simultáneos antes de que exista el usuario.
        await using (var guard = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended($1,0))", connection, transaction))
        { guard.Parameters.AddWithValue(microsoft.Email); await guard.ExecuteNonQueryAsync(cancellation); }
        long? id = null;
        await using (var read = new NpgsqlCommand("SELECT id,microsoft_tenant_id,microsoft_id FROM users WHERE lower(email)=$1 FOR UPDATE", connection, transaction))
        {
            read.Parameters.AddWithValue(microsoft.Email);
            await using var reader = await read.ExecuteReaderAsync(cancellation);
            if (await reader.ReadAsync(cancellation))
            {
                id = reader.GetInt64(0);
                if (!reader.IsDBNull(1) && reader.GetGuid(1) != tenant || !reader.IsDBNull(2) && reader.GetGuid(2) != objectId || await reader.ReadAsync(cancellation))
                    throw new DomainProblem(403, "La identidad Microsoft no coincide con la cuenta local.");
            }
        }
        var name = string.IsNullOrWhiteSpace(profile.User.Name) ? microsoft.Email : profile.User.Name;
        await using (var write = new NpgsqlCommand(id is null ? """
            INSERT INTO users(email,name,password,microsoft_tenant_id,microsoft_id,created_at,updated_at)
            VALUES($1,$2,$3,$4,$5,timezone('UTC',now()),timezone('UTC',now())) RETURNING id
            """ : """
            UPDATE users SET email=$1,name=$2,microsoft_tenant_id=$3,microsoft_id=$4,updated_at=timezone('UTC',now())
            WHERE id=$5 RETURNING id
            """, connection, transaction))
        {
            write.Parameters.AddWithValue(microsoft.Email); write.Parameters.AddWithValue(name);
            if (id is null) write.Parameters.AddWithValue(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
            write.Parameters.AddWithValue(tenant); write.Parameters.AddWithValue(objectId);
            if (id is not null) write.Parameters.AddWithValue(id.Value);
            id = Convert.ToInt64(await write.ExecuteScalarAsync(cancellation));
        }
        await using (var write = new NpgsqlCommand("""
            INSERT INTO ms_graph_tokens(user_id,email,access_token,refresh_token,expires,created_at,updated_at)
            VALUES($1,$2,$3,$4,$5,timezone('UTC',now()),timezone('UTC',now()))
            ON CONFLICT(user_id) DO UPDATE SET email=EXCLUDED.email,access_token=EXCLUDED.access_token,
              refresh_token=EXCLUDED.refresh_token,expires=EXCLUDED.expires,updated_at=EXCLUDED.updated_at
            """, connection, transaction))
        {
            write.Parameters.AddWithValue(id!.Value); write.Parameters.AddWithValue(microsoft.Email);
            write.Parameters.AddWithValue(crypto.Protect("graph-access", tokens.Access)); write.Parameters.AddWithValue(crypto.Protect("graph-refresh", tokens.Refresh));
            write.Parameters.AddWithValue(tokens.Expires.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await write.ExecuteNonQueryAsync(cancellation);
        }
        await transaction.CommitAsync(cancellation);
        return new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, id!.Value.ToString()), new(ClaimTypes.Email, microsoft.Email),
            new(ClaimTypes.Name, name), new("tdv2.tenant", tenant.ToString()), new("tdv2.object", objectId.ToString()), new("tdv2.session_version", SessionVersion)], CookieAuthenticationDefaults.AuthenticationScheme));
    }
    public async Task<bool> Matches(ClaimsPrincipal principal, CancellationToken cancellation)
    {
        if (principal.Identity?.IsAuthenticated != true || principal.FindFirstValue("tdv2.session_version") != SessionVersion
            || principal.FindFirstValue(AccessState.Claim)?.Length != 64
            || !long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || id <= 0
            || !Guid.TryParse(principal.FindFirstValue("tdv2.tenant"), out var tenant) || !Guid.TryParse(settings.Value.TenantId, out var configured) || tenant != configured
            || !Guid.TryParse(principal.FindFirstValue("tdv2.object"), out var objectId)) return false;
        await using var connection = await connections.Open("Tdv2", cancellation);
        await using var command = new NpgsqlCommand("SELECT 1 FROM users WHERE id=$1 AND email=$2 AND microsoft_tenant_id=$3 AND microsoft_id=$4", connection);
        command.Parameters.AddWithValue(id); command.Parameters.AddWithValue(principal.FindFirstValue(ClaimTypes.Email) ?? "");
        command.Parameters.AddWithValue(tenant); command.Parameters.AddWithValue(objectId);
        return await command.ExecuteScalarAsync(cancellation) is not null;
    }
}
