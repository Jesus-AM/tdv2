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
            "denied" => "Tu cuenta no tiene un acceso y un rol vigentes para Transformación Digital en Nexo.",
            "unavailable" => "No se pudo iniciar sesión con Microsoft o comprobar tu acceso. Inténtalo más tarde.",
            _ => null
        };
        return View();
    }
}
