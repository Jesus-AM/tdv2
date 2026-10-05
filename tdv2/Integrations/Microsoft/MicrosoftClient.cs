using Tdv2.Integrations.Nexo;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Tdv2.Security;
namespace Tdv2.Integrations.Microsoft;

public sealed record MicrosoftTokens(string Access, string Refresh, long Expires, string? IdToken = null);
public sealed record MicrosoftPerson(string ObjectId, string Email);

// Conserva OAuth y Graph /me de Laravel; un claim JWT sin verificar nunca autentica.
public sealed class MicrosoftClient(HttpClient http, IOptions<MicrosoftSettings> options)
{
    public const string Scopes = "openid profile offline_access User.Read";
    private MicrosoftSettings Settings { get { options.Value.Validate(); return options.Value; } }
    private static DomainProblem Failure() => new(503, "Microsoft no pudo completar la operación. Inicia sesión nuevamente.");
    public string Authorize(string state, string challenge, string? nonce = null, string? loginHint = null, bool otherAccount = false) => QueryHelpers.AddQueryString(Settings.Authority + "authorize", new Dictionary<string, string?>
    {
        ["client_id"] = Settings.ClientId,
        ["redirect_uri"] = Settings.Callback,
        ["response_type"] = "code",
        ["scope"] = Scopes,
        ["state"] = state,
        ["code_challenge"] = challenge,
        ["code_challenge_method"] = "S256"
        , ["nonce"] = nonce, ["login_hint"] = otherAccount ? null : loginHint,
        ["prompt"] = otherAccount ? "select_account" : null
    });
    public Task<MicrosoftTokens> Exchange(string code, string verifier, CancellationToken cancellation) => Tokens(new()
    { ["grant_type"] = "authorization_code", ["code"] = code, ["code_verifier"] = verifier, ["redirect_uri"] = Settings.Callback }, null, cancellation);
    public Task<MicrosoftTokens> Refresh(string refresh, CancellationToken cancellation) => Tokens(new()
    { ["grant_type"] = "refresh_token", ["refresh_token"] = refresh }, refresh, cancellation);
    private async Task<MicrosoftTokens> Tokens(Dictionary<string, string> data, string? previousRefresh, CancellationToken cancellation)
    {
        var settings = Settings;
        data["client_id"] = settings.ClientId; data["client_secret"] = settings.ClientSecret;
        var started = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var request = new HttpRequestMessage(HttpMethod.Post, settings.Authority + "token") { Content = new FormUrlEncodedContent(data) };
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation);
        if (!response.IsSuccessStatusCode) throw Failure();
        using var json = JsonDocument.Parse(await ReadLimited(response, 131072, cancellation));
        var root = json.RootElement;
        var access = String(root, "access_token"); var refresh = String(root, "refresh_token") ?? previousRefresh;
        var expiresText = root.TryGetProperty("expires_in", out var expires) ? expires.ToString() : "";
        if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh)
            || !string.Equals(String(root, "token_type"), "Bearer", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(expiresText, out var seconds) || seconds is < 1 or > 31_536_000 || started + seconds <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) throw Failure();
        return new(access, refresh, started + seconds, String(root, "id_token"));
    }
    public async Task<string?> ValidatedLoginHint(string? idToken, string verifier, string objectId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(idToken) || idToken.Length > 65536) throw Failure();
        var metadata = $"https://login.microsoftonline.com/{Settings.Tenant}/v2.0/.well-known/openid-configuration";
        var configuration = await OpenIdConnectConfigurationRetriever.GetAsync(metadata, new HttpDocumentRetriever(http) { RequireHttps = true }, ct);
        var result = await new JsonWebTokenHandler { MapInboundClaims = false }.ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            RequireSignedTokens = true, ValidateIssuerSigningKey = true, IssuerSigningKeys = configuration.SigningKeys,
            ValidateIssuer = true, ValidIssuer = $"https://login.microsoftonline.com/{Settings.Tenant}/v2.0",
            ValidateAudience = true, ValidAudience = Settings.ClientId, ValidateLifetime = true, RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1), ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        });
        if (!result.IsValid || result.ClaimsIdentity.FindFirst("nonce")?.Value != ProtectedValues.Hash("oidc:" + verifier)
            || result.ClaimsIdentity.FindFirst("tid")?.Value != Settings.Tenant || result.ClaimsIdentity.FindFirst("oid")?.Value != objectId)
            throw Failure();
        var hint = result.ClaimsIdentity.FindFirst("login_hint")?.Value;
        return hint is { Length: > 0 and <= 8192 } ? hint : null;
    }
    public async Task<MicrosoftPerson> Me(string token, CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me?$select=id,mail,userPrincipalName,displayName");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation);
        if (!response.IsSuccessStatusCode) throw Failure();
        using var document = JsonDocument.Parse(await ReadLimited(response, 65536, cancellation));
        var id = String(document.RootElement, "id");
        if (!Guid.TryParse(id, out var objectId)) throw Failure();
        var mail = String(document.RootElement, "mail");
        if (string.IsNullOrWhiteSpace(mail)) mail = String(document.RootElement, "userPrincipalName");
        return new(objectId.ToString(), PostgresNexoProfiles.Email(mail ?? ""));
    }
    public async Task<(bool Unauthorized, string? Photo)> Photo(string token, CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/photos/48x48/$value");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return (true, null);
        var type = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
        if (!response.IsSuccessStatusCode || type is not ("image/png" or "image/jpeg")) return (false, null);
        var bytes = await ReadLimited(response, 1_048_576, cancellation);
        return (false, bytes.Length == 0 ? null : $"data:{type};base64,{Convert.ToBase64String(bytes)}");
    }
    private static string? String(JsonElement json, string name) => json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static async Task<byte[]> ReadLimited(HttpResponseMessage response, int limit, CancellationToken cancellation)
    {
        if (response.Content.Headers.ContentLength > limit) throw Failure();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellation);
        using var output = new MemoryStream(); var buffer = new byte[8192]; int length;
        while ((length = await stream.ReadAsync(buffer, cancellation)) > 0)
        { if (output.Length + length > limit) throw Failure(); await output.WriteAsync(buffer.AsMemory(0, length), cancellation); }
        return output.ToArray();
    }
}
