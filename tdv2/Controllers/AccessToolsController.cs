using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tdv2.Infrastructure;
using Tdv2.Services;
using Tdv2.Web;
namespace Tdv2.Controllers;

public sealed class AccessToolsController(AccessToolService service) : ControllerBase
{
    [HttpGet("/configuracion/pruebas-acceso")]
    public async Task<IResult> Index()
    {
        return PageResponse.Page(HttpContext, await service.Index());
    }

    [HttpGet("/configuracion/pruebas-acceso/actuar-como-usuario")]
    public async Task<IResult> Representation()
    {
        return PageResponse.Page(HttpContext, await service.Representation());
    }

    [HttpGet("/configuracion/pruebas-acceso/rol-area")]
    public async Task<IResult> Preview()
    {
        return PageResponse.Page(HttpContext, await service.Preview());
    }

    [HttpGet("/actuar-como-usuario/personas")]
    [EnableRateLimiting("representation-search")]
    public async Task<IResult> Search()
    {
        return Results.Json(await service.Search(Inputs.Query(HttpContext, "q", 2, 150), Inputs.Page(HttpContext, "pagina")));
    }

    [HttpPost("/actuar-como-usuario")]
    [EnableRateLimiting("representation-start")]
    public async Task<IResult> StartRepresentation([FromBody] JsonObject input)
    {
        await service.StartRepresentation(input);
        return Results.Json(new { redirect = "/inicio" });
    }

    [HttpPost("/vista-prueba")]
    [EnableRateLimiting("preview-start")]
    public async Task<IResult> StartPreview([FromBody] JsonObject input)
    {
        await service.StartPreview(input);
        return Results.Json(new { redirect = "/inicio" });
    }

    [HttpGet("/actuar-como-usuario")]
    public IResult RedirectRepresentation() => Results.Redirect("/configuracion/pruebas-acceso/actuar-como-usuario");

    [HttpDelete("/actuar-como-usuario")]
    public async Task<IResult> ExitRepresentation()
    {
        await service.Exit("representation");
        return Results.Json(new { redirect = "/inicio" });
    }

    [HttpGet("/vista-prueba")]
    public IResult RedirectPreview() => Results.Redirect("/configuracion/pruebas-acceso/rol-area");

    [HttpDelete("/vista-prueba")]
    public async Task<IResult> ExitPreview()
    {
        await service.Exit("preview");
        return Results.Json(new { redirect = "/inicio" });
    }

}
