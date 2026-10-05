namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en tdv2_oauth_attempts; no se envía directamente a React.</summary>
public sealed class OAuthAttempt
{
    public string Id { get; set; } = "";
    public string BrowserHash { get; set; } = "";
    public string Verifier { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}
