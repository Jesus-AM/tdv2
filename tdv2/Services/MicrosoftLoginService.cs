using Tdv2.Integrations.Microsoft;
using System.Security.Claims;
using Tdv2.Infrastructure;
using Tdv2.Security;

namespace Tdv2.Services;

public sealed record MicrosoftLogin(ClaimsPrincipal Principal, string Email, string Name);

/// <summary>Confirma la identidad Microsoft. Los permisos se comprueban por aplicación al acceder.</summary>
public sealed class MicrosoftLoginService(MicrosoftClient microsoft, INexoProfiles nexo, PostgresIdentity identities, Tdv2.Competencias.IDigitalNexo digital, IConfiguration configuration)
{
    public string? VerifiedEmail { get; private set; }

    public async Task<MicrosoftLogin> Complete(string code, string verifier, CancellationToken ct, bool digitalApplication = false)
    {
        var credentials = await microsoft.Exchange(code, verifier, ct);
        var person = await microsoft.Me(credentials.Access, ct);
        var hint = await microsoft.ValidatedLoginHint(credentials.IdToken, verifier, person.ObjectId, ct);
        VerifiedEmail = person.Email;
        var binding = await identities.Find(person, ct);
        var sources = new List<INexoProfiles>();
        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString("Nexo"))) sources.Add(nexo);
        if (!string.IsNullOrWhiteSpace(configuration.GetConnectionString("CompetenciasDigitalesNexo"))) sources.Add(digital);
        if (digitalApplication) sources.Reverse();
        var matches = new List<Tdv2.Domain.NexoIdentity>();
        foreach (var source in sources)
        {
            try { matches.AddRange(await source.IdentitiesForAddresses(person.Addresses, ct)); }
            catch (Tdv2.Domain.DomainProblem e) when (e.Status == 403) { /* Publicación sin aplicación disponible. */ }
            // Un vínculo ya acreditado no se sustituye por alias ante una fuente caída.
            // Sin vínculo previo, no afirmar unicidad si no se pudo comprobar una fuente configurada.
            catch (Tdv2.Domain.DomainProblem e) when (e.Status == 503 && binding.NexoUserId is not null) { }
        }
        var ids = matches.Select(m => m.Id).Distinct().ToArray();
        if (ids.Length > 1 || binding.NexoUserId is { } pinned && ids.Any(id => id != pinned))
            throw new Tdv2.Domain.DomainProblem(403, "No fue posible confirmar la correspondencia de tu cuenta institucional.");
        var matched = matches.FirstOrDefault(m => m.AccountType == "individual" && person.VerifiedAddresses.Contains(m.Email));
        var canonical = binding.NexoUserId ?? matched?.Id;
        // Conservar el nombre institucional cuando esté disponible. Obtenerlo no concede
        // acceso: el ticket nunca contiene roles/módulos y cada destino revalida Nexo.
        Tdv2.Domain.Profile? profile = null;
        foreach (var source in canonical is null ? [] : sources)
        {
            Tdv2.Domain.Profile candidate;
            try { candidate = await source.ForUser(canonical!.Value, ct); }
            catch (Tdv2.Domain.DomainProblem e) when (e.Status is 403 or 503) { continue; }
            if (candidate.NexoUserId != canonical)
                throw new Tdv2.Domain.DomainProblem(403, "No fue posible confirmar la correspondencia de tu cuenta institucional.");
            if (profile is not null && profile.User.Email != candidate.User.Email)
                throw new Tdv2.Domain.DomainProblem(503, Tdv2.Web.ApplicationEntry.Unavailable);
            profile ??= candidate;
        }
        // Una cuenta institucional sin aplicaciones puede permanecer en el portal. No se
        // inventa adscripción, empleo ni autorización; el correo sirve solo como nombre alterno.
        var email = matched?.Email ?? binding.Email ?? person.Email;
        profile ??= new(new(email, matched?.Name ?? email, null, null, "sin_ur"), [], [], canonical);
        VerifiedEmail = profile.User.Email;
        var principal = await identities.Save(person, profile, credentials, binding.NexoUserId, ct);
        // Sólo la cuenta Microsoft real posee este hint opaco; nunca se toma de la representación ni del correo.
        if (hint is not null) ((ClaimsIdentity)principal.Identity!).AddClaim(new("tdv2.microsoft_login_hint", hint));
        return new(principal, profile.User.Email, profile.User.Name);
    }

}
