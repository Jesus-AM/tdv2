using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Tdv2.Domain;
using Tdv2.Security;
using Tdv2.Web;

namespace Tdv2.Controllers;

public sealed class SessionController(RequestAccess access, AccessState state) : ControllerBase
{
    [HttpGet("/session/status")]
    public IResult Status()=>User.Identity?.IsAuthenticated==true ? Results.Json(SessionLifetime.Client(HttpContext)) : Results.Json(new{sessionExpired=true,redirect="/"},statusCode:401);

    public sealed record ExpiryCheck(string Id);
    [HttpPost("/session/expire")]
    public IResult Expire([FromBody] ExpiryCheck input)
    {
        var ticket=HttpContext.Items[SessionLifetime.TicketKey] as Microsoft.AspNetCore.Authentication.AuthenticationTicket;
        if(ticket is null || SessionLifetime.Item(ticket.Properties,SessionLifetime.IdKey)!=input.Id)
            return Results.Json(new{sessionChanged=true,redirect="/"},statusCode:409);
        // Si llegó unos milisegundos antes, devuelve el reloj/deadline real. Nunca
        // cierra una sesión nueva iniciada desde otra pestaña.
        return Results.Json(SessionLifetime.Client(HttpContext));
    }
    [HttpGet("/health/live")]
    public IResult Live() => Results.Json(new { status = "ok", databaseChecked = false });

    [HttpGet("/session/csrf")]
    public IResult Csrf([FromServices] IAntiforgery csrf) => Results.Json(new { token = csrf.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpGet("/user/modules")]
    public async Task<IResult> Modules()
    {
        var profile = await access.Profile(HttpContext);
        if (access.Selection is not null)
            profile = profile with { Modules = profile.Modules.Where(m => m.Key is not ("configuracion" or "sincronizaciones" or "pruebas_acceso")).ToArray() };
        return Results.Json(ModuleAccess.Navigation(profile));
    }

    [HttpGet("/acceso-restringido")]
    public async Task<IResult> Restricted()
    {
        await state.Load(HttpContext.RequestAborted);
        return PageResponse.Page(HttpContext, "AccesoRestringido", new() { ["access"] = new { message = ApplicationEntry.NoApplications, status = 403 } });
    }

    [HttpGet("/accesos")]
    public async Task<IResult> Choices([FromServices] ReactShell shell)
    {
        var available = await ApplicationEntry.Find(HttpContext);
        var html = Request.Headers.Accept.Any(v => v?.Contains("text/html") == true) && !Request.Headers.ContainsKey("X-TDV2-Page");
        if (available.DirectDestination is { } destination)
            return html ? Results.LocalRedirect(destination) : Results.Json(new { redirect = destination });
        if (html) { await shell.Write(HttpContext); return Results.Empty; }
        return PageResponse.Page(HttpContext, "Accesos", new() { ["available"] = available });
    }
}
