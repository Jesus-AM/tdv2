using System.Text.Json;
using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Security;

namespace Tdv2.Web;

/// <summary>Contrato público común de errores de dominio y denegaciones de autorización.</summary>
public static class DomainProblemResponse
{
    public static async Task Write(HttpContext context, DomainProblem problem, ReactShell shell)
    {
        await context.RequestServices.GetRequiredService<OperationAudit>().Failure(context, problem.Status);
        var sites = context.RequestServices.GetRequiredService<PortalSites>();
        var page = PagePaths.IsPage(context.Request.Path) || sites.IsLanding(context.Request.Path);
        if (problem.Status == 401 && page
            && context.Request.Headers.Accept.Any(v => v?.Contains("text/html") == true))
        {
            context.Response.Redirect("/connect?returnUrl=" + Uri.EscapeDataString(sites.SafeReturn(context.Request.Path + context.Request.QueryString)));
            return;
        }
        context.Response.StatusCode = problem.Status;
        if (page && context.Request.Headers.Accept.Any(v => v?.Contains("text/html") == true))
        {
            await shell.Write(context);
            return;
        }
        var payload = problem.Details is null ? new JsonObject() : JsonSerializer.SerializeToNode(problem.Details)!.AsObject();
        payload["message"] = problem.Message;
        if (problem.Status == 403 && problem.Message == ApplicationEntry.NoApplications)
            payload["message"] = ApplicationEntry.Denied(context.Request.Path);
        if ((ApplicationEntry.IsDestination(context.Request.Path) || sites.IsLanding(context.Request.Path)) && problem.Status is 403 or 503)
        {
            var message = problem.Status == 503 ? ApplicationEntry.Unavailable : sites.ForPath(context.Request.Path) is { } site
                ? $"Tu cuenta no tiene acceso a {site.Name}." : ApplicationEntry.Denied(context.Request.Path);
            IReadOnlyList<ApplicationEntry.Option> options = [];
            try { options = (await ApplicationEntry.Find(context, context.Request.Path)).Options; }
            catch (DomainProblem) { /* La sesión tampoco pudo validarse: no ofrecer accesos. */ }
            catch (Exception e) when (e is not OperationCanceledException) { /* Error al verificar: no inferir autorizaciones. */ }
            payload["message"] = message;
            payload["access"] = JsonSerializer.SerializeToNode(new { message, status = problem.Status, options = options.Select(o => new { name = o.Name, path = o.Path }) });
        }
        var snapshot = context.RequestServices.GetRequiredService<AccessState>().Loaded;
        payload["representacion"] = JsonSerializer.SerializeToNode(snapshot?.Selection?.Representation());
        payload["simulacion"] = JsonSerializer.SerializeToNode(snapshot?.Selection?.Preview());
        payload["contextoEdicion"] = snapshot?.Key ?? "own";
        await context.Response.WriteAsJsonAsync(payload);
    }
}
