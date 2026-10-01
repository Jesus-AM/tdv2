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
        return Results.Json(await service.Save(ur, input));
    }

}
