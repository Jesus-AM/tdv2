using Microsoft.AspNetCore.Mvc;
using Tdv2.Services;
using Tdv2.Web;
namespace Tdv2.Controllers;

public sealed class ProcessConfigurationController(RequestAccess access, ProcessConfigurationService settings) : ControllerBase
{
    private async Task<Tdv2.Domain.Profile> Authorize()
    {
        await access.EnsureOwn(HttpContext);
        return await access.Profile(HttpContext, "configuracion_procesos");
    }
    [HttpGet("/configuracion/procesos")]
    public async Task<IResult> Index()
    {
        var profile = await Authorize();
        return PageResponse.Page(HttpContext, "ConfiguracionProcesos", await settings.Read(HttpContext.RequestAborted), profile);
    }
    [HttpPost("/configuracion/procesos/vista-previa")]
    public async Task<IResult> Preview([FromBody] ParticipationChange input)
    {
        await Authorize();
        return Results.Json(await settings.PreviewOrSave(input, false, HttpContext.RequestAborted));
    }
    [HttpPut("/configuracion/procesos")]
    public async Task<IResult> Save([FromBody] ParticipationChange input)
    {
        await Authorize();
        return Results.Json(await settings.PreviewOrSave(input, true, HttpContext.RequestAborted));
    }
}
