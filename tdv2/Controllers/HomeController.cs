using Microsoft.AspNetCore.Mvc;
namespace Tdv2.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        ViewData["Error"] = Request.Query["authError"].ToString() switch
        {
            "invalid" => "La solicitud de autenticación venció o no es válida. Inténtalo nuevamente.",
            "denied" => Tdv2.Web.ApplicationEntry.NoApplications,
            "unavailable" => Tdv2.Web.ApplicationEntry.Unavailable,
            _ => null
        };
        return View();
    }
}
