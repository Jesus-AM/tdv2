using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
namespace Tdv2.Web;

public static class AuthenticationEndpoints
{
    private const string BrowserCookie = "__Host-tdv2.oauth";
    private static CookieOptions BrowserOptions() => new() { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = "/", MaxAge = TimeSpan.FromMinutes(10), IsEssential = true };
    public static void MapMicrosoft(this WebApplication app)
    {
        app.MapGet("/connect", async (HttpContext http, IOptions<MicrosoftSettings> settings, PostgresOAuthAttempts attempts,
            MicrosoftClient microsoft, INexoProfiles nexo, PostgresIdentity identities, AuthenticationAudit audit) =>
        {
            if (http.User.Identity?.IsAuthenticated == true) return (IResult)Results.Redirect("/inicio");
            settings.Value.Validate();
            if (!http.Request.Query.ContainsKey("code") && !http.Request.Query.ContainsKey("error"))
            {
                var attempt = await attempts.Create(http.RequestAborted);
                http.Response.Cookies.Append(BrowserCookie, attempt.Browser, BrowserOptions());
                return Results.Redirect(microsoft.Authorize(attempt.State, attempt.Challenge));
            }
            string? email = null;
            try
            {
                var state = http.Request.Query["state"];
                if (state.Count != 1) return Results.Redirect("/?authError=invalid");
                var verifier = await attempts.Consume(state.ToString(), http.Request.Cookies[BrowserCookie] ?? "", http.RequestAborted);
                if (verifier is null) return Results.Redirect("/?authError=invalid");
                http.Response.Cookies.Delete(BrowserCookie, BrowserOptions());
                var code = http.Request.Query["code"];
                if (http.Request.Query.ContainsKey("error") || code.Count != 1 || string.IsNullOrWhiteSpace(code) || code.ToString().Length > 16384)
                    return Results.Redirect("/?authError=invalid");
                var tokens = await microsoft.Exchange(code.ToString(), verifier, http.RequestAborted);
                var person = await microsoft.Me(tokens.Access, http.RequestAborted); email = person.Email;
                var profile = await nexo.ForEmail(person.Email, http.RequestAborted);
                var principal = await identities.Save(person, profile, tokens, http.RequestAborted);
                await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties { IsPersistent = false, IssuedUtc = DateTimeOffset.UtcNow, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2) });
                http.Response.Cookies.Delete("tdv2.aspnet.csrf");
                await audit.Record("login", person.Email, profile.User.Name, http.RequestAborted);
                return Results.Redirect("/inicio");
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                app.Logger.LogWarning("No se pudo completar el inicio Microsoft. Tipo: {Type}", error.GetType().Name);
                await audit.Record("access_denied", email, null, http.RequestAborted);
                return Results.Redirect(error is DomainProblem { Status: 403 } ? "/?authError=denied" : "/?authError=unavailable");
            }
        }).RequireRateLimiting("microsoft-login");
        app.MapPost("/logout", async (HttpContext http, AuthenticationAudit audit, OperationAudit operations,
            AccessState state, DatabaseConnections connections, NexoOperations nexo) =>
        {
            var email = http.User.FindFirstValue(ClaimTypes.Email); var name = http.User.Identity?.Name;
            if (state.SessionHash is not null)
            {
                var snapshot = await state.Load(http.RequestAborted);
                await using var connection = await connections.Open("Tdv2", http.RequestAborted);
                await using var transaction = await connection.BeginTransactionAsync(http.RequestAborted);
                await state.Guard(connection, transaction, http.RequestAborted, true);
                var closed = false;
                if (snapshot.Selection is { Kind: "representation" } selection && selection.ActorEmail == email)
                {
                    try { await nexo.Represent("finalizar", email!, new() { ["token"] = selection.Token }, http.RequestAborted); closed = true; }
                    catch (DomainProblem) { /* Local session revocation must remain available when Nexo fails. */ }
                }
                await using var command = new Npgsql.NpgsqlCommand("DELETE FROM tdv2_sessions WHERE id_hash=$1", connection, transaction);
                command.Parameters.AddWithValue(state.SessionHash); await command.ExecuteNonQueryAsync(http.RequestAborted);
                await operations.Write(connection, transaction, "logout", "auth", "sesion", "confirmado", http.RequestAborted,
                    snapshot.Selection, new { cierre_central = closed });
                await transaction.CommitAsync(http.RequestAborted);
            }
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            http.Response.Cookies.Delete(BrowserCookie, BrowserOptions());
            http.Response.Cookies.Delete("tdv2.aspnet.csrf");
            if (email is not null && state.SessionHash is null) await audit.Record("logout", email, name, http.RequestAborted);
            var target = email is null ? "/" : "/session/microsoft-logout";
            return http.Request.Headers.Accept.Any(v => v?.Contains("text/html") == true)
                ? (IResult)Results.Redirect(target) : Results.Json(new { redirect = target });
        });
        app.MapGet("/session/microsoft-logout", (IOptions<MicrosoftSettings> settings) =>
        {
            settings.Value.Validate();
            return Results.Redirect(QueryHelpers.AddQueryString(settings.Value.Authority + "logout", "post_logout_redirect_uri", settings.Value.PublicOrigin.TrimEnd('/') + "/"));
        });
        app.MapGet("/user/photo", async (HttpContext http, RequestAccess access, GraphTokens tokens, MicrosoftClient microsoft) =>
        {
            await access.Profile(http);
            string? photo = null;
            try
            {
                var id = long.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var token = await tokens.Ensure(id, http.RequestAborted);
                if (token is not null)
                {
                    var result = await microsoft.Photo(token, http.RequestAborted);
                    if (result.Unauthorized && await tokens.Ensure(id, http.RequestAborted, token) is { } renewed) result = await microsoft.Photo(renewed, http.RequestAborted);
                    photo = result.Photo;
                }
            }
            catch (Exception error) when (error is not OperationCanceledException)
            { app.Logger.LogInformation("Foto Microsoft no disponible. Tipo: {Type}", error.GetType().Name); }
            return Results.Json(new { photo });
        }).RequireRateLimiting("photo");
    }
}
