using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tdv2.Infrastructure;
using Tdv2.Services;
using Tdv2.Web;
namespace Tdv2.Controllers;

public sealed class FormsController(FormService service) : ControllerBase
{
    [HttpGet("/inicio")]
    public async Task<IResult> Index()
    {
        return PageResponse.Page(HttpContext, await service.Index());
    }

    [HttpGet("/formatos/{ur}")]
    public async Task<IResult> Show(string ur)
    {
        return PageResponse.Page(HttpContext, await service.Show(ur));
    }

    [HttpPut("/formatos/{ur}")]
    [EnableRateLimiting("form-writes")]
    public async Task<IResult> Save(string ur, [FromBody] SaveForm input)
    {
        await Task.CompletedTask;
        throw new Tdv2.Domain.DomainProblem(428, "Actualiza el editor para guardar con reservas por bloque.");
    }

    [HttpGet("/formatos/{ur}/estado")]
    public async Task<IResult> State(string ur, [FromQuery] Guid tab, [FromServices] FormEditingService editing) => Results.Json(await editing.Read(ur, tab));

    [HttpPost("/formatos/{ur}/reservas")]
    [EnableRateLimiting("form-writes")]
    public async Task<IResult> Reserve(string ur, [FromBody] EditRequest input, [FromServices] FormEditingService editing) => Results.Json(await editing.Reserve(ur, input));

    [HttpPost("/formatos/{ur}/reservas/actividad")]
    [EnableRateLimiting("form-writes")]
    public async Task<IResult> Renew(string ur, [FromBody] EditRequest input, [FromServices] FormEditingService editing) => Results.Json(await editing.Reserve(ur, input, true));

    [HttpPost("/formatos/{ur}/reservas/liberar")]
    public async Task<IResult> Release(string ur, [FromBody] EditRequest input, [FromServices] FormEditingService editing) => Results.Json(await editing.Release(ur, input));

    [HttpPatch("/formatos/{ur}/bloques")]
    [EnableRateLimiting("form-writes")]
    public async Task<IResult> Blocks(string ur, [FromBody] EditRequest input, [FromServices] FormEditingService editing) => Results.Json(await editing.Save(ur, input));

    [HttpPost("/formatos/{ur}/enviar")]
    [EnableRateLimiting("form-writes")]
    public async Task<IResult> Submit(string ur, [FromBody] SubmitRequest input, [FromServices] FormEditingService editing) => Results.Json(await editing.Submit(ur, input));

}
