using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tdv2.Services;
namespace Tdv2.Controllers;

public sealed class ParticipantPhotosController(ParticipantPhotos photos) : ControllerBase
{
    [HttpGet("/formatos/{ur}/participantes/{participant}/foto")]
    [EnableRateLimiting("photo")]
    public async Task<IResult> Photo(string ur, string participant)
    {
        Response.Headers.CacheControl = "private, no-store";
        return Results.Json(await photos.ForForm(HttpContext, ur, participant));
    }
}
