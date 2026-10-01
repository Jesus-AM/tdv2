using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tdv2.Infrastructure;
using Tdv2.Services;
using Tdv2.Web;
namespace Tdv2.Controllers;

public sealed class CollaboratorsController(CollaboratorService service) : ControllerBase
{
    [HttpGet("/colaboradores")]
    public async Task<IResult> Index()
    {
        return PageResponse.Page(HttpContext, await service.Index());
    }

    [HttpGet("/colaboradores/personas")]
    [EnableRateLimiting("collaborator-search")]
    public async Task<IResult> Search()
    {
        return Results.Json(await service.Search(Inputs.Query(HttpContext, "ur", 1, 32), Inputs.Query(HttpContext, "q", 2, 150), Inputs.Page(HttpContext, "page")));
    }

    [HttpPost("/colaboradores")]
    [EnableRateLimiting("collaborator-writes")]
    public async Task<IResult> Add([FromBody] JsonObject input)
    {
        return Results.Json(await service.Add(input), statusCode: 201);
    }

    [HttpDelete("/colaboradores/{collaboration:long}")]
    [EnableRateLimiting("collaborator-writes")]
    public async Task<IResult> Remove(long collaboration)
    {
        var pending = await service.Remove(collaboration);
        return Results.Json(new { message = pending ? "El acceso a este formato ya está retirado. Quedó pendiente el retiro en Nexo; puedes reintentarlo desde esta lista." : "Colaboración retirada." }, statusCode: pending ? 202 : 200);
    }

}
