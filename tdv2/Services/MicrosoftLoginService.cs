using Tdv2.Integrations.Microsoft;
using System.Security.Claims;
using Tdv2.Infrastructure;
using Tdv2.Security;

namespace Tdv2.Services;

public sealed record MicrosoftLogin(ClaimsPrincipal Principal, string Email, string Name);

/// <summary>Confirma Microsoft y Nexo antes de persistir una identidad institucional.</summary>
public sealed class MicrosoftLoginService(MicrosoftClient microsoft, INexoProfiles nexo, PostgresIdentity identities)
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

}
