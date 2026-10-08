using System.Net;

namespace Tdv2.Integrations.Microsoft;

public enum MicrosoftPhotoStatus { Found, Missing, Unauthorized, Temporary, Unavailable }
public sealed record MicrosoftPhotoResult(MicrosoftPhotoStatus Status, string? Photo = null, string? Cause = null, TimeSpan? RetryAfter = null)
{
    public static MicrosoftPhotoResult Http(HttpResponseMessage response, bool photograph = true)
    {
        var code = response.StatusCode;
        var retry = response.Headers.RetryAfter?.Delta
            ?? (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : (TimeSpan?)null);
        return code switch
        {
            HttpStatusCode.NotFound when photograph => new(MicrosoftPhotoStatus.Missing),
            HttpStatusCode.Unauthorized => new(MicrosoftPhotoStatus.Unauthorized, Cause: "microsoft_unauthorized"),
            HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests => new(MicrosoftPhotoStatus.Temporary, Cause: "microsoft_" + (int)code, RetryAfter: retry),
            _ when (int)code >= 500 => new(MicrosoftPhotoStatus.Temporary, Cause: "microsoft_unavailable", RetryAfter: retry),
            _ => new(MicrosoftPhotoStatus.Unavailable, Cause: "microsoft_rejected")
        };
    }
}

// Sólo códigos internos: nunca adjuntar cuerpo HTTP, tokens ni excepciones criptográficas originales.
public sealed class MicrosoftPhotoFailure(MicrosoftPhotoResult result) : Exception(result.Cause)
{
    public MicrosoftPhotoResult Result { get; } = result;
}
