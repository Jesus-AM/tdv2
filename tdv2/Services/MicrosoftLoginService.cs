using Tdv2.Integrations.Microsoft;
using System.Security.Claims;
using Tdv2.Infrastructure;
using Tdv2.Security;

namespace Tdv2.Services;

public sealed record MicrosoftLogin(ClaimsPrincipal Principal, string Email, string Name);

/// <summary>Confirma la identidad Microsoft. Los permisos se comprueban por aplicación al acceder.</summary>
public sealed class MicrosoftLoginService(MicrosoftClient microsoft, INexoProfiles nexo, PostgresIdentity identities, Tdv2.Competencias.IDigitalNexo digital)
{
    public string? VerifiedEmail { get; private set; }

    public async Task<MicrosoftLogin> Complete(string code, string verifier, CancellationToken ct, bool digitalApplication = false)
    {
        var credentials = await microsoft.Exchange(code, verifier, ct);
        var person = await microsoft.Me(credentials.Access, ct);
        var hint = await microsoft.ValidatedLoginHint(credentials.IdToken, verifier, person.ObjectId, ct);
        VerifiedEmail = person.Email;
        // Conservar el nombre institucional cuando esté disponible. Obtenerlo no concede
        // acceso: el ticket nunca contiene roles/módulos y cada destino revalida Nexo.
        Tdv2.Domain.Profile? profile = null;
        foreach (var source in digitalApplication ? new INexoProfiles[] { digital, nexo } : [nexo, digital])
        {
            try { profile = await source.ForEmail(person.Email, ct); break; }
            catch (Tdv2.Domain.DomainProblem e) when (e.Status is 403 or 503) { }
        }
        // Una cuenta institucional sin aplicaciones puede permanecer en el portal. No se
        // inventa adscripción, empleo ni autorización; el correo sirve solo como nombre alterno.
        profile ??= new(new(person.Email, person.Email, null, null, "sin_ur"), [], []);
        var principal = await identities.Save(person, profile, credentials, ct);
        // Sólo la cuenta Microsoft real posee este hint opaco; nunca se toma de la representación ni del correo.
        if (hint is not null) ((ClaimsIdentity)principal.Identity!).AddClaim(new("tdv2.microsoft_login_hint", hint));
        return new(principal, person.Email, profile.User.Name);
    }

}
