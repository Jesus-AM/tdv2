using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

public sealed class ParticipantPhotos(Tdv2DbContext db, RequestAccess access, MicrosoftPhotoCache cache,
    GraphTokens tokens, MicrosoftClient microsoft, IOptions<MicrosoftSettings> settings, ILogger<ParticipantPhotos> logger, AccessState state)
{
    public async Task<long?> VerifiedUser(string email, CancellationToken ct) =>
        await db.Users.AsNoTracking().Where(u => u.Email == email && u.MicrosoftTenantId == Guid.Parse(settings.Value.TenantId)
            && u.MicrosoftId != null).Select(u => (long?)u.Id).SingleOrDefaultAsync(ct);

    public async Task<ParticipantPhoto> Account(HttpContext http)
    {
        var profile = await access.Profile(http);
        var context = PhotoContext.Key(http, state, profile.User.Email);
        if (http.Request.Headers["X-TDV2-Photo-Context"] is { Count: > 0 } sent && sent.ToString() != context)
            throw new DomainProblem(409, "El usuario activo cambió. Recarga para consultar su fotografía.");
        // En representación se busca al objetivo: jamás reutilizar el identificador Microsoft del actor.
        if (access.Selection?.Kind == "preview") {
            await state.CheckRead(http.RequestAborted);
            return new(null, DateTimeOffset.UtcNow.AddMinutes(2), Contexto: context);
        }
        var id = await VerifiedUser(profile.User.Email, http.RequestAborted);
        var result = id is null ? new ParticipantPhoto(null, DateTimeOffset.UtcNow.AddMinutes(2)) : await Load(id.Value, http.RequestAborted);
        await state.CheckRead(http.RequestAborted);
        return result with { Contexto = context };
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
        await state.CheckRead(http.RequestAborted);
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
        return await cache.Get($"{user.Id}:{user.MicrosoftTenantId}:{user.MicrosoftId}:{user.Email}", token => Fetch(user, token), ct);
    }
    private async Task<MicrosoftPhotoResult> Fetch(User user, CancellationToken ct)
    {
        // HttpClient con ResponseHeadersRead no limita la lectura del cuerpo. Acotar toda
        // la renovación evita retener indefinidamente la exclusión por identidad si Graph se cuelga.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        var operation = timeout.Token;
        try
        {
            var token = await tokens.Ensure(user.Id, operation);
            if (token is null) return Report(new(MicrosoftPhotoStatus.Unavailable, Cause: "tokens_missing"));
            async Task<MicrosoftPhotoResult> Verified(string credential)
            {
                try
                {
                    var photo = await microsoft.Photo(credential, operation);
                    if (photo.Status is not (MicrosoftPhotoStatus.Found or MicrosoftPhotoStatus.Missing)) return photo;
                    // Acreditar también la ausencia: un token de otra identidad no puede invalidar esta foto.
                    var person = await microsoft.PhotoIdentity(credential, operation);
                    if (person.ObjectId != user.MicrosoftId!.Value.ToString() || person.Email != user.Email)
                        return new(MicrosoftPhotoStatus.Unavailable, Cause: "identity_mismatch");
                    return photo;
                }
                catch (MicrosoftPhotoFailure failure) { return failure.Result; }
            }
            var result = await Verified(token);
            if (result.Status == MicrosoftPhotoStatus.Unauthorized && await tokens.Ensure(user.Id, operation, token) is { } renewed) result = await Verified(renewed);
            return Report(result);
        }
        catch (MicrosoftPhotoFailure failure) { return Report(failure.Result); }
        catch (HttpRequestException) { return Report(new(MicrosoftPhotoStatus.Temporary, Cause: "network")); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return Report(new(MicrosoftPhotoStatus.Temporary, Cause: "timeout")); }
        catch (JsonException) { return Report(new(MicrosoftPhotoStatus.Unavailable, Cause: "invalid_response")); }
        catch (DomainProblem) { return Report(new(MicrosoftPhotoStatus.Unavailable, Cause: "invalid_response")); }
    }
    private MicrosoftPhotoResult Report(MicrosoftPhotoResult result)
    {
        if (result.Cause is not null) logger.LogWarning("Miniatura Microsoft no disponible. Causa: {Cause}", result.Cause);
        return result;
    }
}
