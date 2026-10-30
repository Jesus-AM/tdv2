using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Tdv2.Security;
using Tdv2.Services;

namespace Tdv2.Web;

public sealed class SessionExpiryMiddleware(RequestDelegate next)
{
    public const string Marker="tdv2.session-ended";
    public static void Mark(HttpContext http)=>http.Response.Cookies.Append(Marker,"1",new(){HttpOnly=true,Secure=true,SameSite=SameSiteMode.Lax,Path="/",IsEssential=true});
    public static async Task End(HttpContext http)
    {
        if(http.Items[SessionLifetime.ExpiredTicketKey] is AuthenticationTicket old) http.User=old.Principal;
        try
        {
            await http.RequestServices.GetRequiredService<SessionTerminationService>().Expire(http.User.FindFirstValue(ClaimTypes.Email),http.RequestAborted);
            if(http.User.FindFirstValue(AccessState.Claim) is not null)
                await Tdv2.Competencias.DigitalActivityHooks.Logout(http.RequestServices);
        }
        finally
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            http.Response.Cookies.Delete("tdv2.aspnet.csrf"); Mark(http);
            http.User=new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
    public static async Task Reply(HttpContext http)
    {
        http.Response.Headers["X-TDV2-Session-Expired"]="1";
        var page=PagePaths.IsPage(http.Request.Path) || http.RequestServices.GetRequiredService<PortalSites>().IsLanding(http.Request.Path);
        if(page && HttpMethods.IsGet(http.Request.Method) && http.Request.Headers.Accept.Any(v=>v?.Contains("text/html")==true) && !http.Request.Headers.ContainsKey("X-TDV2-Page"))
            http.Response.Redirect("/");
        else { http.Response.StatusCode=401; await http.Response.WriteAsJsonAsync(new{message="La sesión terminó.",sessionExpired=true,redirect="/"}); }
    }
    public async Task InvokeAsync(HttpContext http)
    {
        var expired=http.Items.ContainsKey(SessionLifetime.ExpiredTicketKey)
            || http.Request.Cookies.ContainsKey("tdv2.aspnet.session") && http.User.Identity?.IsAuthenticated!=true;
        var ended=http.User.Identity?.IsAuthenticated!=true && http.Request.Cookies.ContainsKey(Marker);
        if(expired) await End(http);
        // Portada y acceso explícito siguen públicos. El marcador impide que Atrás
        // o una URL privada inicien automáticamente otro OAuth tras vencer.
        var path=http.Request.Path;
        if((expired || ended) && path!="/" && path!="/connect" && path!="/health/live" && !path.StartsWithSegments("/quiz") && !path.StartsWithSegments("/documentos"))
        { await Reply(http); return; }
        if(http.Items[SessionLifetime.TicketKey] is AuthenticationTicket ticket)
        {
            var id=SessionLifetime.Item(ticket.Properties,SessionLifetime.IdKey)!;
            http.Response.Headers["X-TDV2-Session-Id"]=id;
            if(http.Request.Headers["X-TDV2-Session"].FirstOrDefault() is { Length:>0 } expected && expected!=id)
            {
                http.Response.StatusCode=409;
                await http.Response.WriteAsJsonAsync(new{message="La sesión cambió.",sessionChanged=true,redirect="/"});return;
            }
        }
        await next(http);
    }
}
