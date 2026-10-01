using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
namespace Tdv2.Security;

public sealed class ProtectedValues(IDataProtectionProvider provider)
{
    public string Protect(string purpose, string value) => "aspnet:v1:" + provider.CreateProtector("TDV2", purpose, "v1").Protect(value);
    public string Unprotect(string purpose, string value)
    {
        if (!value.StartsWith("aspnet:v1:", StringComparison.Ordinal)) throw new CryptographicException("Se requiere un nuevo inicio de sesión.");
        return provider.CreateProtector("TDV2", purpose, "v1").Unprotect(value[10..]);
    }
    public static string Random() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static string Challenge(string verifier) => WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
}
