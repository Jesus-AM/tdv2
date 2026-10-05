using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Tdv2.Domain;
using Tdv2.Web;

namespace Tdv2.Security;

public sealed class InstitutionalAccessRequirement : IAuthorizationRequirement;

/// <summary>Política ASP.NET común a HTML y JSON. Nexo se revalida dentro de cada solicitud.</summary>
public sealed class InstitutionalAccessHandler : AuthorizationHandler<InstitutionalAccessRequirement>
{
    internal static readonly object Rejection = new();
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, InstitutionalAccessRequirement requirement)
    {
        if (context.Resource is not HttpContext http) return;
        // La política también cubre prefijos privados sin acción registrada. Las salidas de contexto
        // y logout siguen disponibles aunque Nexo falle; las mutaciones aún requieren CSRF.
        try
        {
            await AccessBoundary.Validate(http);
            context.Succeed(requirement);
        }
        catch (DomainProblem problem)
        {
            // Una denegación prevista es un resultado de autorización, no una excepción
            // que atraviese el framework y detenga Just My Code durante F5.
            http.Items[Rejection] = problem;
            context.Fail();
        }
    }
}

public sealed class InstitutionalAccessResultHandler(ReactShell shell) : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler fallback = new();

    public Task HandleAsync(RequestDelegate next, HttpContext http, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (!result.Succeeded && http.Items[InstitutionalAccessHandler.Rejection] is DomainProblem problem)
            return DomainProblemResponse.Write(http, problem, shell);
        return fallback.HandleAsync(next, http, policy, result);
    }
}
