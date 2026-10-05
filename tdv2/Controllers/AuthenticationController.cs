using Tdv2.Integrations.Microsoft;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Tdv2.Domain;
using Tdv2.Security;
using Tdv2.Services;
using Tdv2.Web;

namespace Tdv2.Controllers;

public sealed class AuthenticationController(IOptions<MicrosoftSettings> settings, PostgresOAuthAttempts attempts,
    MicrosoftClient microsoft, MicrosoftLoginService login, SessionTerminationService termination,
    AuthenticationAudit audit, AccessState state, RequestAccess access, ProtectedValues crypto, ILogger<AuthenticationController> logger) : ControllerBase
{
    private const string BrowserCookie = "__Host-tdv2.oauth";
    private const string HintCookie = "__Host-tdv2.microsoft-hint";
    private const string LogoutCookie = "__Host-tdv2.microsoft-logout";
    private string? Hint(string cookie)
    {
        try
        {
            var stored = Request.Cookies[cookie];
            if (stored is null) return null;
            var parts = crypto.Unprotect(cookie, stored).Split('|', 2);
            return parts.Length == 2 && long.TryParse(parts[0], out var expires) && expires > DateTimeOffset.UtcNow.ToUnixTimeSeconds() ? parts[1] : null;
        }
        catch (System.Security.Cryptography.CryptographicException) { return null; }
    }
    private void StoreHint(string cookie, string hint) => Response.Cookies.Append(cookie,
        crypto.Protect(cookie, DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds() + "|" + hint), BrowserOptions());
    private static CookieOptions BrowserOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = TimeSpan.FromMinutes(10),
        IsEssential = true
    };

    [HttpGet("/connect")]
    [EnableRateLimiting("microsoft-login")]
    public async Task<IResult> Connect()
    {
        if (User.Identity?.IsAuthenticated == true) return Results.Redirect("/inicio");
        settings.Value.Validate();
        var ct = HttpContext.RequestAborted;
        if (!Request.Query.ContainsKey("code") && !Request.Query.ContainsKey("error"))
        {
            var attempt = await attempts.Create(ct);
            Response.Cookies.Append(BrowserCookie, attempt.Browser, BrowserOptions());
            return Results.Redirect(microsoft.Authorize(attempt.State, attempt.Challenge, attempt.Nonce,
                Hint(HintCookie), Request.Query["account"] == "other"));
        }
        string? email = null;
        try
        {
            var stateValue = Request.Query["state"];
            if (stateValue.Count != 1) return Results.Redirect("/?authError=invalid");
            // State de un uso + cookie del navegador + PKCE impiden canjear un retorno ajeno.
            var verifier = await attempts.Consume(stateValue.ToString(), Request.Cookies[BrowserCookie] ?? "", ct);
            if (verifier is null) return Results.Redirect("/?authError=invalid");
            Response.Cookies.Delete(BrowserCookie, BrowserOptions());
            var code = Request.Query["code"];
            if (Request.Query.ContainsKey("error") || code.Count != 1 || string.IsNullOrWhiteSpace(code) || code.ToString().Length > 16384)
                return Results.Redirect("/?authError=invalid");
            var authenticated = await login.Complete(code.ToString(), verifier, ct);
            email = authenticated.Email;
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticated.Principal,
                new AuthenticationProperties { IsPersistent = false, IssuedUtc = DateTimeOffset.UtcNow, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2) });
            Response.Cookies.Delete("tdv2.aspnet.csrf");
            StoreHint(HintCookie, authenticated.Principal.FindFirstValue("tdv2.microsoft_login_hint") ?? authenticated.Email);
            await audit.Record("login", email, authenticated.Name, ct);
            return Results.Redirect("/inicio");
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning("No se pudo completar el inicio Microsoft. Tipo: {Type}", error.GetType().Name);
            await audit.Record("access_denied", email ?? login.VerifiedEmail, null, ct);
            return Results.Redirect(error is DomainProblem { Status: 403 } ? "/?authError=denied" : "/?authError=unavailable");
        }
    }

    [HttpPost("/logout")]
    public async Task<IResult> Logout()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (User.FindFirstValue("tdv2.microsoft_login_hint") is { } hint)
        { StoreHint(LogoutCookie, hint); StoreHint(HintCookie, hint); }
        else
        {
            // El correo real puede orientar el próximo login, nunca el logout.
            Response.Cookies.Delete(LogoutCookie, BrowserOptions());
            if (email is not null) StoreHint(HintCookie, email);
        }
        await termination.Revoke(email, HttpContext.RequestAborted);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete(BrowserCookie, BrowserOptions());
        Response.Cookies.Delete("tdv2.aspnet.csrf");
        if (email is not null && state.SessionHash is null)
            await audit.Record("logout", email, User.Identity?.Name, HttpContext.RequestAborted);
        var target = email is null ? "/" : "/session/microsoft-logout";
        return Request.Headers.Accept.Any(v => v?.Contains("text/html") == true)
            ? Results.Redirect(target) : Results.Json(new { redirect = target });
    }

    [HttpGet("/session/microsoft-logout")]
    public IResult MicrosoftLogout()
    {
        settings.Value.Validate();
        var hint = Hint(LogoutCookie);
        Response.Cookies.Delete(LogoutCookie, BrowserOptions());
        return Results.Redirect(QueryHelpers.AddQueryString(settings.Value.Authority + "logout", new Dictionary<string, string?>
        { ["post_logout_redirect_uri"] = settings.Value.PublicOrigin.TrimEnd('/') + "/", ["logout_hint"] = hint }));
    }

    [HttpPost("/session/use-another-account")]
    public async Task<IResult> OtherAccount()
    {
        await termination.Revoke(User.FindFirstValue(ClaimTypes.Email), HttpContext.RequestAborted);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete(HintCookie, BrowserOptions()); Response.Cookies.Delete(LogoutCookie, BrowserOptions());
        Response.Cookies.Delete(BrowserCookie, BrowserOptions()); Response.Cookies.Delete("tdv2.aspnet.csrf");
        return Results.Json(new { redirect = "/connect?account=other" });
    }

    [HttpGet("/user/photo")]
    [EnableRateLimiting("photo")]
    public async Task<IResult> Photo()
    {
        await access.Profile(HttpContext);
        var photo = await login.Photo(long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), HttpContext.RequestAborted);
        return Results.Json(new { photo });
    }
}
