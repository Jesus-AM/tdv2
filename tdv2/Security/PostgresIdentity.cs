using Tdv2.Integrations.Microsoft;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Tdv2.Domain;
using Tdv2.Infrastructure;
namespace Tdv2.Security;

public interface ISessionIdentity { Task<bool> Matches(ClaimsPrincipal principal, CancellationToken cancellation); }
public sealed record MicrosoftBinding(string? Email, long? NexoUserId);
public sealed class PostgresIdentity(DatabaseConnections connections, Tdv2DbContext db, ProtectedValues crypto, IOptions<MicrosoftSettings> settings) : ISessionIdentity
{
    // Las sesiones anteriores no resolvían el ID canónico. Reautenticar una vez,
    // sin presentar la ausencia de ese claim antiguo como una denegación de sitio.
    public const string SessionVersion = "aspnet-2";
    public async Task<MicrosoftBinding> Find(MicrosoftPerson person, CancellationToken ct)
    {
        var tenant = Guid.Parse(settings.Value.TenantId); var oid = Guid.Parse(person.ObjectId);
        var email = await db.Users.AsNoTracking().Where(u => u.MicrosoftTenantId == tenant && u.MicrosoftId == oid).Select(u => u.Email).SingleOrDefaultAsync(ct);
        var canonical = await db.IdentityLinks.AsNoTracking().Where(l => l.TenantId == tenant && l.ObjectId == oid).Select(l => (long?)l.NexoUserId).SingleOrDefaultAsync(ct);
        return new(email, canonical);
    }
    public async Task<ClaimsPrincipal> Save(MicrosoftPerson microsoft, Profile profile, MicrosoftTokens? tokens, long? expectedLink, CancellationToken cancellation, AuthenticationTicket? pending = null)
    {
        var tenant = Guid.Parse(settings.Value.TenantId); var objectId = Guid.Parse(microsoft.ObjectId);
        var email = Tdv2.Integrations.Nexo.PostgresNexoProfiles.Email(profile.User.Email);
        await using var connection = await connections.Open("Tdv2", cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        // El bloqueo transaccional serializa primeros inicios simultáneos antes de que exista el usuario.
        await using (var guard = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended($1,0))", connection, transaction))
        { guard.Parameters.AddWithValue(tenant + ":" + objectId); await guard.ExecuteNonQueryAsync(cancellation); }
        await using (var guard = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended($1,0))", connection, transaction))
        { guard.Parameters.AddWithValue(email); await guard.ExecuteNonQueryAsync(cancellation); }
        if (pending is not null)
        {
            await using var readTicket = new NpgsqlCommand("SELECT ticket FROM tdv2_sessions WHERE id_hash=$1 AND expires_at>clock_timestamp() FOR UPDATE", connection, transaction);
            readTicket.Parameters.AddWithValue(pending.Principal.FindFirstValue(AccessState.Claim)!);
            if (await readTicket.ExecuteScalarAsync(cancellation) is not string encrypted) throw new DomainProblem(401, "La sesión terminó.");
            var current = TicketSerializer.Default.Deserialize(Convert.FromBase64String(crypto.Unprotect("session", encrypted)))!;
            if (NexoActorIdentity.Id(current.Principal) is not null)
                return current.Principal; // Otra pestaña ya confirmó este ticket. El destino valida de nuevo el vínculo.
        }
        // Sólo el flujo de identidad acreditado puede insertar un vínculo, nunca cambiarlo.
        // La sesión revalida el vínculo
        // en cada solicitud; una revocación concurrente nunca reutiliza la autorización.
        long? existingLink;
        await using (var proof = new NpgsqlCommand("SELECT nexo_user_id FROM public.tdv2_identity_links WHERE microsoft_tenant_id=$1 AND microsoft_id=$2", connection, transaction))
        {
            proof.Parameters.AddWithValue(tenant); proof.Parameters.AddWithValue(objectId);
            var value = await proof.ExecuteScalarAsync(cancellation);
            existingLink = value is null ? null : Convert.ToInt64(value);
            if (existingLink != expectedLink && !(expectedLink is null && existingLink == profile.NexoUserId))
                throw new DomainProblem(409, "La correspondencia institucional cambió. Inicia sesión nuevamente.");
        }
        var verifiedMatch = profile.NexoUserId is not null && microsoft.VerifiedAddresses.Contains(email);
        if (existingLink is null && profile.NexoUserId is not null && !verifiedMatch)
            throw new DomainProblem(403, "No fue posible confirmar la correspondencia de tu cuenta institucional.");
        long? id = null;
        await using (var read = new NpgsqlCommand("SELECT id,microsoft_tenant_id,microsoft_id,email FROM users WHERE (microsoft_tenant_id=$1 AND microsoft_id=$2) OR lower(email)=ANY($3) FOR UPDATE", connection, transaction))
        {
            read.Parameters.AddWithValue(tenant); read.Parameters.AddWithValue(objectId);
            read.Parameters.AddWithValue(microsoft.Addresses.Append(email).Distinct().ToArray());
            await using var reader = await read.ExecuteReaderAsync(cancellation);
            while (await reader.ReadAsync(cancellation))
            {
                var stable = !reader.IsDBNull(1) && !reader.IsDBNull(2) && reader.GetGuid(1) == tenant && reader.GetGuid(2) == objectId;
                var legacyVerified = reader.IsDBNull(1) && reader.IsDBNull(2) && verifiedMatch && reader.GetString(3) == email;
                if (id is not null || !stable && !legacyVerified)
                    throw new DomainProblem(403, "La identidad Microsoft no coincide con la cuenta local.");
                id = reader.GetInt64(0);
            }
        }
        if (existingLink is null && profile.NexoUserId is { } canonical)
        {
            await using var link = new NpgsqlCommand("INSERT INTO public.tdv2_identity_links(microsoft_tenant_id,microsoft_id,nexo_user_id,verified_at,verified_by,verification_reference) VALUES($1,$2,$3,now(),'oidc-login','Graph /me UPN/SMTP + nexo_usuarios: coincidencia única')", connection, transaction);
            link.Parameters.AddWithValue(tenant); link.Parameters.AddWithValue(objectId); link.Parameters.AddWithValue(canonical);
            try { await link.ExecuteNonQueryAsync(cancellation); }
            catch (PostgresException e) when (e.SqlState == "23505")
            { throw new DomainProblem(403, "No fue posible confirmar la correspondencia de tu cuenta institucional."); }
        }
        var name = string.IsNullOrWhiteSpace(profile.User.Name) ? email : profile.User.Name;
        await using (var write = new NpgsqlCommand(id is null ? """
            INSERT INTO users(email,name,password,microsoft_tenant_id,microsoft_id,created_at,updated_at)
            VALUES($1,$2,$3,$4,$5,timezone('UTC',now()),timezone('UTC',now())) RETURNING id
            """ : """
            UPDATE users SET email=$1,name=$2,microsoft_tenant_id=$3,microsoft_id=$4,updated_at=timezone('UTC',now())
            WHERE id=$5 RETURNING id
            """, connection, transaction))
        {
            write.Parameters.AddWithValue(email); write.Parameters.AddWithValue(name);
            if (id is null) write.Parameters.AddWithValue(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
            write.Parameters.AddWithValue(tenant); write.Parameters.AddWithValue(objectId);
            if (id is not null) write.Parameters.AddWithValue(id.Value);
            id = Convert.ToInt64(await write.ExecuteScalarAsync(cancellation));
        }
        if (tokens is not null)
        {
            await using var write = new NpgsqlCommand("""
            INSERT INTO ms_graph_tokens(user_id,email,access_token,refresh_token,expires,created_at,updated_at)
            VALUES($1,$2,$3,$4,$5,timezone('UTC',now()),timezone('UTC',now()))
            ON CONFLICT(user_id) DO UPDATE SET email=EXCLUDED.email,access_token=EXCLUDED.access_token,
              refresh_token=EXCLUDED.refresh_token,expires=EXCLUDED.expires,updated_at=EXCLUDED.updated_at
            """, connection, transaction);
            write.Parameters.AddWithValue(id!.Value); write.Parameters.AddWithValue(email);
            write.Parameters.AddWithValue(crypto.Protect("graph-access", tokens.Access)); write.Parameters.AddWithValue(crypto.Protect("graph-refresh", tokens.Refresh));
            write.Parameters.AddWithValue(tokens.Expires.ToString(System.Globalization.CultureInfo.InvariantCulture));
            await write.ExecuteNonQueryAsync(cancellation);
        }
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, id!.Value.ToString()), new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name), new("tdv2.tenant", tenant.ToString()), new("tdv2.object", objectId.ToString()), new("tdv2.session_version", SessionVersion)], CookieAuthenticationDefaults.AuthenticationScheme));
        if (profile.NexoUserId is { } nexo) ((ClaimsIdentity)principal.Identity!).AddClaim(new(NexoActorIdentity.Claim, nexo.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        if (pending is not null)
        {
            var hash = pending.Principal.FindFirstValue(AccessState.Claim)!;
            if (pending.Principal.FindFirstValue(ClaimTypes.NameIdentifier) != id.Value.ToString() || !verifiedMatch)
                throw new DomainProblem(403, Web.ApplicationEntry.NoApplications);
            // Sólo se cambia el ticket original todavía vivo. Preservar inicio, caducidad, hash y CSRF.
            var identity = (ClaimsIdentity)principal.Identity!;
            foreach (var claim in pending.Principal.Claims.Where(c => c.Type == AccessState.Claim || c.Type == SessionLifetime.StartClaim || c.Type == "tdv2.microsoft_login_hint"))
                identity.AddClaim(claim);
            var ticket = new AuthenticationTicket(principal, pending.Properties, pending.AuthenticationScheme);
            await using var update = new NpgsqlCommand("UPDATE tdv2_sessions SET ticket=$2 WHERE id_hash=$1 AND expires_at>clock_timestamp()", connection, transaction);
            update.Parameters.AddWithValue(hash);
            update.Parameters.AddWithValue(crypto.Protect("session", Convert.ToBase64String(TicketSerializer.Default.Serialize(ticket))));
            if (await update.ExecuteNonQueryAsync(cancellation) != 1)
                throw new DomainProblem(401, "La sesión terminó.");
            await using var emailUpdate = new NpgsqlCommand("UPDATE ms_graph_tokens SET email=$2 WHERE user_id=$1", connection, transaction);
            emailUpdate.Parameters.AddWithValue(id.Value); emailUpdate.Parameters.AddWithValue(email);
            await emailUpdate.ExecuteNonQueryAsync(cancellation);
        }
        await transaction.CommitAsync(cancellation);
        return principal;
    }
    public Task<bool> Matches(ClaimsPrincipal principal, CancellationToken cancellation) => Matches(principal, false, cancellation);

    // Sólo el flujo de primera vinculación usa esta prueba del objeto Microsoft original. Puede que otra sesión
    // del mismo objeto ya haya confirmado su correo canónico. La política de acceso sigue exigiendo Matches.
    public Task<bool> MatchesPendingMicrosoft(ClaimsPrincipal principal, CancellationToken cancellation) =>
        NexoActorIdentity.Id(principal) is null ? Matches(principal, true, cancellation) : Task.FromResult(false);

    private async Task<bool> Matches(ClaimsPrincipal principal, bool pending, CancellationToken cancellation)
    {
        if (principal.Identity?.IsAuthenticated != true || principal.FindFirstValue("tdv2.session_version") != SessionVersion
            || principal.FindFirstValue(AccessState.Claim)?.Length != 64
            || !long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || id <= 0
            || !Guid.TryParse(principal.FindFirstValue("tdv2.tenant"), out var tenant) || !Guid.TryParse(settings.Value.TenantId, out var configured) || tenant != configured
            || !Guid.TryParse(principal.FindFirstValue("tdv2.object"), out var objectId)) return false;
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? "";
        var canonical = NexoActorIdentity.Id(principal);
        var account = db.Users.AsNoTracking().Where(u => u.Id == id && u.MicrosoftTenantId == tenant && u.MicrosoftId == objectId);
        if (pending) return await account.AnyAsync(cancellation);
        return await account.AnyAsync(u => u.Email == email && db.IdentityLinks.Where(l => l.TenantId == tenant && l.ObjectId == objectId)
            .Select(l => (long?)l.NexoUserId).FirstOrDefault() == canonical, cancellation);
    }
}
