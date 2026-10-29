using System.Globalization;
using System.Security.Claims;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Web;

namespace Tdv2.Security;

public static class NexoActorIdentity
{
    public const string Claim = "tdv2.nexo_user";
    public static long? Id(ClaimsPrincipal principal) => long.TryParse(principal.FindFirstValue(Claim), NumberStyles.None,
        CultureInfo.InvariantCulture, out var id) && id > 0 ? id : null;
    public static async Task<Profile> ForActor(this INexoProfiles source, ClaimsPrincipal principal, CancellationToken ct)
    {
        if (Id(principal) is not { } id) throw new DomainProblem(403, ApplicationEntry.NoApplications);
        var profile = await source.ForUser(id, ct);
        if (profile.NexoUserId != id || profile.User.Email != principal.FindFirstValue(ClaimTypes.Email))
            throw new DomainProblem(503, ApplicationEntry.Unavailable);
        return profile;
    }
}
