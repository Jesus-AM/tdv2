using Tdv2.Domain;
namespace Tdv2.Integrations.Microsoft;

public sealed class MicrosoftSettings
{
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string PublicOrigin { get; set; } = "";
    public string Tenant => Guid.Parse(TenantId).ToString();
    public string Authority => "https://login.microsoftonline.com/" + Tenant + "/oauth2/v2.0/";
    public string Callback => PublicOrigin.TrimEnd('/') + "/connect";
    public void Validate()
    {
        if (!Guid.TryParse(TenantId, out _) || !Guid.TryParse(ClientId, out _) || string.IsNullOrWhiteSpace(ClientSecret)
            || !Uri.TryCreate(PublicOrigin, UriKind.Absolute, out var origin) || origin.Scheme != "https"
            || origin.AbsolutePath != "/" || origin.Query != "" || origin.Fragment != "" || origin.UserInfo != "")
            throw new DomainProblem(503, "El inicio de sesión institucional no está configurado correctamente.");
    }
}
