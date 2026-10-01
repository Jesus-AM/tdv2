using Microsoft.AspNetCore.Mvc.Filters;

namespace Tdv2.Web;

/// <summary>Conserva el 400 del transporte anterior sin devolver detalles del model binder.</summary>
public sealed class RequestModelValidation : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid) throw new BadHttpRequestException("La solicitud no es válida.");
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
