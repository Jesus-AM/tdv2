using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Tdv2.Domain;
using Tdv2.Domain.Entities;
using Tdv2.Infrastructure;
using Tdv2.Integrations.Microsoft;
using Tdv2.Security;
using Tdv2.Web;

namespace Tdv2.Services;

public static class ParticipantIdentity
{
    // Misma identidad efectiva que las reservas existentes; el nombre nunca decide el color o la fotografía.
    public static string Key(string email) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(email.Trim().ToLowerInvariant()))));
}
public sealed record ParticipantPhoto(string? Photo, DateTimeOffset RenuevaEn);

/// <summary>Cachea sólo miniaturas, nunca permisos. Una petición verifica acceso antes de entrar aquí.</summary>
public sealed class MicrosoftPhotoCache(TimeProvider clock) : IDisposable
{
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 32 * 1024 * 1024 });
    private readonly SemaphoreSlim[] gates = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();
    public async Task<ParticipantPhoto> Get(string identity, Func<CancellationToken, Task<string?>> load, CancellationToken ct)
    {
        var gate = gates[Math.Abs(identity.GetHashCode() % gates.Length)];
        await gate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue<ParticipantPhoto>(identity, out var saved) && saved!.RenuevaEn > clock.GetUtcNow()) return saved;
            var photo = await load(ct);
            var ttl = photo is null ? TimeSpan.FromMinutes(2) : TimeSpan.FromMinutes(15);
            var result = new ParticipantPhoto(photo, clock.GetUtcNow() + ttl);
            cache.Set(identity, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl, Size = (photo?.Length ?? 0) * 2L + 512 });
            return result;
        }
        finally { gate.Release(); }
    }
    public void Dispose() { cache.Dispose(); foreach (var gate in gates) gate.Dispose(); }
}

public sealed class ParticipantPhotos(Tdv2DbContext db, RequestAccess access, MicrosoftPhotoCache cache,
    GraphTokens tokens, MicrosoftClient microsoft, IOptions<MicrosoftSettings> settings, ILogger<ParticipantPhotos> logger)
{
    public async Task<long?> VerifiedUser(string email, CancellationToken ct) =>
        await db.Users.AsNoTracking().Where(u => u.Email == email && u.MicrosoftTenantId == Guid.Parse(settings.Value.TenantId)
            && u.MicrosoftId != null).Select(u => (long?)u.Id).SingleOrDefaultAsync(ct);

    public async Task<ParticipantPhoto> Account(HttpContext http)
    {
        var profile = await access.Profile(http);
        // En representación se busca al objetivo: jamás reutilizar el identificador Microsoft del actor.
        if (access.Selection?.Kind == "preview") return new(null, DateTimeOffset.UtcNow.AddMinutes(2));
        var id = await VerifiedUser(profile.User.Email, http.RequestAborted);
        return id is null ? new(null, DateTimeOffset.UtcNow.AddMinutes(2)) : await Load(id.Value, http.RequestAborted);
    }
    public async Task<ParticipantPhoto> ForForm(HttpContext http, string unit, string participant)
    {
        var context = await access.Resolve(http);
        if (!context.Scopes.ContainsKey(unit)) throw new DomainProblem(403, "No tienes acceso al formato de esta UR.");
        if (participant.Length != 64 || participant.Any(c => !char.IsAsciiHexDigit(c))) throw Missing();
        var id = await ActiveUser(unit, participant, http.RequestAborted);
        if (id is null) throw Missing();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, http.RequestAborted);
        if (user is null || ParticipantIdentity.Key(user.Email) != participant) throw Missing();
        var result = await Load(id.Value, http.RequestAborted);
        // Una descarga lenta no amplía la ventana de la reserva ni entrega un participante ya retirado.
        if (await ActiveUser(unit, participant, http.RequestAborted) != id) throw Missing();
        return result;
    }
    private Task<long?> ActiveUser(string unit, string participant, CancellationToken ct) => db.FormBlocks.AsNoTracking()
        .Where(b => b.UnitId == unit && b.Participant == participant && b.LeaseId != null && b.ExpiresAt > DateTimeOffset.UtcNow)
        .Select(b => b.ParticipantUserId).Where(id => id != null).Distinct().SingleOrDefaultAsync(ct);
    private static DomainProblem Missing() => new(404, "La fotografía de este participante no está disponible.");
    private async Task<ParticipantPhoto> Load(long id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user?.MicrosoftId is null || user.MicrosoftTenantId != Guid.Parse(settings.Value.TenantId)) return new(null, DateTimeOffset.UtcNow.AddMinutes(2));
        return await cache.Get($"{user.Id}:{user.MicrosoftTenantId}:{user.MicrosoftId}", token => Fetch(user, token), ct);
    }
    private async Task<string?> Fetch(User user, CancellationToken ct)
    {
        try
        {
            var token = await tokens.Ensure(user.Id, ct);
            if (token is null) return null;
            async Task<(bool Unauthorized, string? Photo)> Verified(string credential)
            {
                // Conservar la renovación ante 401 del mecanismo previo. La miniatura no se
                // entrega hasta acreditar /me; una foto ausente no requiere otra consulta Graph.
                var photo = await microsoft.Photo(credential, ct);
                if (photo.Unauthorized || photo.Photo is null) return photo;
                var person = await microsoft.Me(credential, ct);
                if (person.ObjectId != user.MicrosoftId!.Value.ToString() || person.Email != user.Email) return (false, null);
                return photo;
            }
            var result = await Verified(token);
            if (result.Unauthorized && await tokens.Ensure(user.Id, ct, token) is { } renewed) result = await Verified(renewed);
            return result.Photo;
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogInformation("Miniatura Microsoft no disponible. Tipo: {Type}", error.GetType().Name);
            return null;
        }
    }
}
