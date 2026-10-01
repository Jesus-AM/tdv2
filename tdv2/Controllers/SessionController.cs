using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Tdv2.Domain;
using Tdv2.Security;
using Tdv2.Web;

namespace Tdv2.Controllers;

public sealed class SessionController(RequestAccess access, AccessState state) : ControllerBase
{
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
        return PageResponse.Page(HttpContext, "AccesoRestringido", new() { ["access"] = new { message = "No tienes acceso a este recurso.", status = 403 } });
    }
}
