using System.Text.Json;
using Tdv2.Domain;
using Tdv2.Integrations.Nexo;

namespace Tdv2.Integrations.Microsoft;

public sealed record MicrosoftSmtpAddress(string Address, bool Primary);
public sealed record MicrosoftPerson(string ObjectId, string Email, string? Mail = null, string? UserPrincipalName = null,
    string? DisplayName = null, IReadOnlyList<MicrosoftSmtpAddress>? SmtpAddresses = null)
{
    public string[] Addresses => new[] { Email, Institutional(UserPrincipalName) }.Where(x => x is not null)
        .Select(x => x!).Concat((SmtpAddresses ?? []).Select(x => x.Address)).Distinct(StringComparer.Ordinal).ToArray();
    // Para acreditar un vínculo nuevo se exige UPN o SMTP del perfil /me validado.
    // mail participa en la detección de contradicciones, pero no basta por sí solo.
    public string[] VerifiedAddresses => new[] { Institutional(UserPrincipalName) }.Where(x => x is not null)
        .Select(x => x!).Concat((SmtpAddresses ?? []).Select(x => x.Address)).Distinct(StringComparer.Ordinal).ToArray();

    private static string? Institutional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return PostgresNexoProfiles.Email(value); } catch (DomainProblem) { return null; }
    }
    public static MicrosoftPerson Parse(JsonElement root)
    {
        static string? Text(JsonElement node, string key) => node.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (!Guid.TryParse(Text(root, "id"), out var id) || id == Guid.Empty
            || !root.TryGetProperty("proxyAddresses", out var proxies) || proxies.ValueKind != JsonValueKind.Array || proxies.GetArrayLength() > 512)
            throw new DomainProblem(503, "No fue posible comprobar tu identidad institucional.");
        var mail = Text(root, "mail"); var upn = Text(root, "userPrincipalName");
        var addresses = new List<MicrosoftSmtpAddress>();
        foreach (var item in proxies.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String) throw new DomainProblem(503, "No fue posible comprobar tu identidad institucional.");
            var value = item.GetString()!;
            if ((value.StartsWith("SMTP:", StringComparison.Ordinal) || value.StartsWith("smtp:", StringComparison.Ordinal))
                && Institutional(value[5..]) is { } address) addresses.Add(new(address, value.StartsWith("SMTP:", StringComparison.Ordinal)));
        }
        var smtp = addresses.GroupBy(a => a.Address, StringComparer.Ordinal).Select(g => new MicrosoftSmtpAddress(g.Key, g.Any(a => a.Primary))).ToArray();
        if (smtp.Count(a => a.Primary) > 1) throw new DomainProblem(503, "No fue posible comprobar tu identidad institucional.");
        // Un mail ajeno al catálogo institucional o vacío no impide comprobar UPN/SMTP.
        // Esta dirección alternativa es presentación; el vínculo exige la resolución canónica.
        var email = Institutional(mail) ?? Institutional(upn) ?? smtp.FirstOrDefault()?.Address
            ?? throw new DomainProblem(503, "No fue posible comprobar tu identidad institucional.");
        return new(id.ToString(), email, mail, upn, Text(root, "displayName"), smtp);
    }
}
