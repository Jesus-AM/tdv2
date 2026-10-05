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
        context.Response.StatusCode = problem.Status;
        if (PagePaths.IsPage(context.Request.Path) && context.Request.Headers.Accept.Any(v => v?.Contains("text/html") == true))
        {
            await shell.Write(context);
            return;
        }
        var payload = problem.Details is null ? new JsonObject() : JsonSerializer.SerializeToNode(problem.Details)!.AsObject();
        payload["message"] = problem.Message;
        var snapshot = context.RequestServices.GetRequiredService<AccessState>().Loaded;
        payload["representacion"] = JsonSerializer.SerializeToNode(snapshot?.Selection?.Representation());
        payload["simulacion"] = JsonSerializer.SerializeToNode(snapshot?.Selection?.Preview());
        payload["contextoEdicion"] = snapshot?.Key ?? "own";
        await context.Response.WriteAsJsonAsync(payload);
    }
}
