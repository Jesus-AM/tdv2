using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Tdv2.Domain;

namespace Tdv2.Security;

// Ambos límites son iguales: el absoluto siempre vence antes o a la vez que
// dos horas de inactividad. No se necesita un latido que renueve la sesión.
public static class SessionLifetime
{
    public static readonly TimeSpan Duration=TimeSpan.FromHours(2);
    public const string StartKey="tdv2.original-login", IdKey="tdv2.session-coordination", StartClaim="tdv2.original-login";
    public const string ExpiredTicketKey="tdv2.expired-ticket", TicketKey="tdv2.active-ticket";
    public static string? Item(AuthenticationProperties properties,string key)=>properties.Items.TryGetValue(key,out var value) ? value : null;
    public static AuthenticationProperties Start(ClaimsPrincipal principal,TimeProvider clock)
    {
        var now=clock.GetUtcNow();
        var identity=(ClaimsIdentity)principal.Identity!;
        foreach(var claim in identity.FindAll(StartClaim).ToArray()) identity.RemoveClaim(claim);
        var stamp=now.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        identity.AddClaim(new(StartClaim,stamp));
        // Las propiedades de cookie se serializan a segundos. Su envoltura se
        // redondea hacia arriba; el ticket SQL y la política validan el milisegundo exacto.
        var expires=now.ToUnixTimeMilliseconds()+7200000;
        var props=new AuthenticationProperties { IsPersistent=false,IssuedUtc=now,ExpiresUtc=DateTimeOffset.FromUnixTimeSeconds((expires+999)/1000),AllowRefresh=false };
        props.Items[StartKey]=stamp; props.Items[IdKey]=Guid.NewGuid().ToString("N");
        return props;
    }
    public static DateTimeOffset Deadline(AuthenticationTicket ticket)=>DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(Item(ticket.Properties,StartKey)!,CultureInfo.InvariantCulture))+Duration;
    public static bool Valid(AuthenticationTicket? ticket,TimeProvider clock)
    {
        if(ticket is null) return false;
        var props=ticket.Properties;
        return props.Items.TryGetValue(StartKey,out var raw) && raw==ticket.Principal.FindFirstValue(StartClaim)
            && long.TryParse(raw,NumberStyles.None,CultureInfo.InvariantCulture,out var stamp)
            && props.Items.TryGetValue(IdKey,out var id) && Guid.TryParseExact(id,"N",out _)
            && stamp<=clock.GetUtcNow().ToUnixTimeMilliseconds()
            && clock.GetUtcNow()<DateTimeOffset.FromUnixTimeMilliseconds(stamp)+Duration
            && props.ExpiresUtc is { } expires && clock.GetUtcNow()<expires;
    }
    public static void Require(ClaimsPrincipal principal,TimeProvider clock)
    {
        if(!long.TryParse(principal.FindFirstValue(StartClaim),out var stamp)
            || clock.GetUtcNow().ToUnixTimeMilliseconds()>=stamp+(long)Duration.TotalMilliseconds)
            throw new DomainProblem(401,"La sesión terminó.");
    }
    public static object Client(HttpContext http)
    {
        var clock=http.RequestServices.GetRequiredService<TimeProvider>();
        var ticket=http.Items[TicketKey] as AuthenticationTicket;
        var start=long.TryParse(http.User.FindFirstValue(StartClaim),out var stamp) ? stamp : clock.GetUtcNow().ToUnixTimeMilliseconds();
        return new { lifetime_ms=7200000,id=ticket is null ? null : Item(ticket.Properties,IdKey),
            started_at=start,expires_at=start+7200000,server_now=clock.GetUtcNow().ToUnixTimeMilliseconds(),idle_ms=7200000,absolute_ms=7200000 };
    }
}
