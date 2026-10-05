using Tdv2.Integrations.Nexo;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Tdv2.Domain;
using Tdv2.Security;
using Tdv2.Infrastructure;
namespace Tdv2.Web;

public static class PageResponse
{
    public static IResult Page(HttpContext http, Tdv2.Services.PageData page) => Page(http, page.Component, page.Props, page.Profile);

    public static IResult Page(HttpContext http, string component, Dictionary<string, object?> props, Profile? profile = null)
    {
        var token = http.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(http);
        var access = http.RequestServices.GetRequiredService<RequestAccess>();
        var state = http.RequestServices.GetRequiredService<AccessState>();
        var selection = access.Selection ?? state.Loaded?.Selection;
        var real = access.Real ?? profile;
        var shared = selection?.Kind == "preview" ? real : profile;
        if (selection is not null && shared is not null) shared = shared with { Modules = shared.Modules.Where(m => m.Key is not ("configuracion" or "pruebas_acceso" or "sincronizaciones")).ToArray() };
        props["name"] = "Transformación Digital";
        props["auth"] = new
        {
            user = real is null ? null : new { id = long.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : 0, name = real.User.Name, email = http.User.FindFirstValue(ClaimTypes.Email) },
            roles = shared?.Roles.Select(r => new { id = r.Id, key = r.Key, name = r.Name }).ToArray() ?? [],
            modules = shared is null ? [] : ModuleAccess.Navigation(shared),
            canPreview = selection is null && real is not null && ModuleAccess.Allows(real, "pruebas_acceso") && ModuleAccess.Allows(real, "procesos_operativos"),
            canRepresent = selection is null && access.Capability is not null && NexoOperations.Flag(access.Capability, "permitido")
        };
        props["simulacion"] = selection?.Preview(access.Directory);
        props["representacion"] = selection?.Representation();
        props["contextoEdicion"] = state.Loaded?.Key ?? "own";
        props["session"] = new { lifetime_ms = 7_200_000 };
        props["routes"] = new { inicio = "/inicio", logout = "/logout", home = "/", connect = "/connect" };
        props["csrfToken"] = token.RequestToken;
        return Results.Json(new { component, props, url = http.Request.Path + http.Request.QueryString });
    }
}
