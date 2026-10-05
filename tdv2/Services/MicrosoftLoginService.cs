using Tdv2.Integrations.Microsoft;
using System.Security.Claims;
using Tdv2.Infrastructure;
using Tdv2.Security;

namespace Tdv2.Services;

public sealed record MicrosoftLogin(ClaimsPrincipal Principal, string Email, string Name);

/// <summary>Confirma Microsoft y Nexo antes de persistir una identidad institucional.</summary>
public sealed class MicrosoftLoginService(MicrosoftClient microsoft, INexoProfiles nexo, PostgresIdentity identities,
    GraphTokens tokens, ILogger<MicrosoftLoginService> logger)
{
    public string? VerifiedEmail { get; private set; }

    public async Task<MicrosoftLogin> Complete(string code, string verifier, CancellationToken ct)
    {
        var credentials = await microsoft.Exchange(code, verifier, ct);
        var person = await microsoft.Me(credentials.Access, ct);
        var hint = await microsoft.ValidatedLoginHint(credentials.IdToken, verifier, person.ObjectId, ct);
        VerifiedEmail = person.Email;
        // Microsoft acredita la cuenta real; sólo Nexo decide el perfil y sus permisos vigentes.
        var profile = await nexo.ForEmail(person.Email, ct);
        var principal = await identities.Save(person, profile, credentials, ct);
        // Sólo la cuenta Microsoft real posee este hint opaco; nunca se toma de la representación ni del correo.
        if (hint is not null) ((ClaimsIdentity)principal.Identity!).AddClaim(new("tdv2.microsoft_login_hint", hint));
        return new(principal, person.Email, profile.User.Name);
    }

    public async Task<string?> Photo(long userId, CancellationToken ct)
    {
        try
        {
            var token = await tokens.Ensure(userId, ct);
            if (token is null) return null;
            var result = await microsoft.Photo(token, ct);
            if (result.Unauthorized && await tokens.Ensure(userId, ct, token) is { } renewed)
                result = await microsoft.Photo(renewed, ct);
            return result.Photo;
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // La foto es opcional; fallar Graph no concede permisos ni invalida por sí solo la sesión.
            logger.LogInformation("Foto Microsoft no disponible. Tipo: {Type}", error.GetType().Name);
            return null;
        }
    }
}
