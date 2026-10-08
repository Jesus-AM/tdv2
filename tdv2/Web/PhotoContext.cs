using System.Security.Claims;
using Tdv2.Security;

namespace Tdv2.Web;

public static class PhotoContext
{
    // Identificador opaco, no credencial. Separa sesión, cuenta Microsoft y revisión de representación.
    public static string? Key(HttpContext http, AccessState state, string? effectiveEmail) =>
        state.SessionHash is null || effectiveEmail is null ? null : ProtectedValues.Hash(string.Join(':',
            "photo", state.SessionHash, http.User.FindFirstValue("tdv2.tenant"), http.User.FindFirstValue("tdv2.object"),
            state.Loaded?.Key ?? "own", state.Loaded?.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0", effectiveEmail));
}
