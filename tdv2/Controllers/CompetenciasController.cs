using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Tdv2.Competencias;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
using Tdv2.Web;

namespace Tdv2.Controllers;

[Route("competencias-digitales")]
public sealed class CompetenciasController(DigitalAccess access, AccessState state) : ControllerBase
{
    private CancellationToken Ct => HttpContext.RequestAborted;
    private T Service<T>() where T : notnull => HttpContext.RequestServices.GetRequiredService<T>();
    private async Task<IResult> Saved(DigitalScope scope, Guid generation)
    {
        var view = await Service<DigitalSurveyService>().Read(scope, Ct);
        DigitalGeneration.Require(view.Generation, generation);
        return Results.Json(view);
    }
    [HttpGet("")]
    [HttpGet("avance-dependencia")]
    [HttpGet("resultados-generales")]
    [HttpGet("encuesta")]
    [HttpGet("mi-informe-ruta")]
    [HttpGet("configuracion")]
    public async Task<IResult> Page([FromQuery] string? unit, [FromQuery] int? level, [FromQuery] string? position, [FromQuery] string? paso)
    {
        var slug = Request.Path.Value!.TrimEnd('/').Split('/').Last(); var index = Array.IndexOf(DigitalRoutes.Slugs, slug);
        var scope = index < 0 ? await access.Resolve(HttpContext) : await access.Require(HttpContext, index);
        var pageGeneration = await Service<DigitalDbContext>().Control.AsNoTracking().Select(c => c.Generation).SingleAsync(Ct);
        object? content = null;
        if (index is 0 or 1) content = await Service<DigitalAggregates>().Read(scope, unit, level, Ct, position, finalizedOnly: index == 1);
        else if (index == 4)
        {
            var catalog = await Service<Tdv2DbContext>().CatalogSynchronizations.AsNoTracking().SingleOrDefaultAsync(c => c.Id == "sii", Ct);
            var digitalDb = Service<DigitalDbContext>();
            await using var read = await digitalDb.Database.BeginTransactionAsync(Ct);
            var generation = await DigitalGeneration.Shared(digitalDb, Ct);
            content = new { generation, units = scope.Allowed.OrderBy(u => u.Code).Select(DigitalUnitOption.From), targets = await Service<DigitalDbContext>().Targets.AsNoTracking().Select(t => new { t.UnitId, t.Value, t.Version, t.UpdatedAt }).ToArrayAsync(Ct),
                catalog = new { source = "SII · catálogo local compartido de Transformación Digital", available = catalog is not null,
                    completedAt = catalog is null ? (DateTimeOffset?)null : new DateTimeOffset(DateTime.SpecifyKind(catalog.CompletedAt, DateTimeKind.Utc)) },
                nexoRoles = scope.Profile.Roles.Select(r => new { r.Key, r.Name }), nexoModules = scope.Profile.Modules.Select(m => new { m.Key, m.Name, m.Path }) };
        }
        else if (index < 0)
        {
            if (!Enumerable.Range(0, 5).Any(scope.Module)) throw new DomainProblem(403, "No tienes módulos autorizados en Competencias digitales.");
            // La entrada solo ofrece el siguiente destino; no evita los permisos de encuesta/informe.
            content = await Service<DigitalDbContext>().Surveys.AsNoTracking().Where(s => s.Employee == UnitDirectory.Employee(scope.Profile.User))
                .Select(s => new { version = s.Version, completedAt = s.CompletedAt }).SingleOrDefaultAsync(Ct)
                ?? new { version = 0, completedAt = (DateTimeOffset?)null };
        }
        else content = await Service<DigitalSurveyService>().Read(scope, Ct, paso);
        var csrf = Service<IAntiforgery>().GetAndStoreTokens(HttpContext);
        return Results.Json(new { component = "CompetenciasDigitales", url = Request.Path + Request.QueryString,
            props = new { name = "Competencias digitales UACJ", csrfToken = csrf.RequestToken, contextoEdicion = state.Loaded?.Key ?? "own",
                photoContext = PhotoContext.Key(HttpContext, state, scope.Profile.User.Email, "competencias-digitales"), photoApplication = "competencias-digitales",
                auth = new { user = new { id = User.FindFirstValue(ClaimTypes.NameIdentifier), name = access.Real!.User.Name, email = access.Real.User.Email }, roles = scope.Profile.Roles, modules = Array.Empty<object>(), canPreview = false, canRepresent = false },
                representacion = access.Selection?.Representation(), simulacion = (object?)null, session = new { lifetime_ms = 7200000 },
                routes = new { inicio = DigitalRoutes.Base, logout = "/logout", home = "/", connect = "/connect" },
                digital = new { generation = pageGeneration, page = index, user = scope.Profile.User.Name, actor = access.Real.User.Name, readOnly = access.Selection is not null,
                    notice = Service<DigitalInstrument>().Resource["notice"],
                    navigation = scope.MenuPages.Select(i => new { path = DigitalRoutes.Base + "/" + DigitalRoutes.Slugs[i], name = DigitalRoutes.Names[i] }), content } } });
    }
    [HttpGet("api/avance")]
    public async Task<IResult> Progress([FromQuery] string? unit, [FromQuery] int? level, [FromQuery] string? position) => Results.Json(await Service<DigitalAggregates>().Read(await access.Require(HttpContext, 0), unit, level, Ct, position));
    [HttpGet("api/resultados")]
    public async Task<IResult> ResultsData([FromQuery] string? unit, [FromQuery] int? level, [FromQuery] string? position) => Results.Json(await Service<DigitalAggregates>().Read(await access.Require(HttpContext, 1), unit, level, Ct, position, finalizedOnly: true));
    [HttpGet("api/resultados/descarga")]
    public async Task<IResult> Export([FromQuery] string? unit, [FromQuery] int? level, [FromQuery] string? position) => Results.File(DigitalAggregates.Csv(await Service<DigitalAggregates>().Read(await access.Require(HttpContext, 1), unit, level, Ct, position, finalizedOnly: true)), "text/csv; charset=utf-8", "competencias-resultados-agregados.csv");
    [HttpGet("api/encuesta")]
    public async Task<IResult> Survey([FromQuery] string? paso) => Results.Json(await Service<DigitalSurveyService>().Read(await access.Require(HttpContext, 2), Ct, paso));
    [HttpPost("api/aviso")]
    [EnableRateLimiting("form-writes")]
    public async Task<IResult> Accept([FromBody] DigitalAccept input)
    { var scope = await access.Require(HttpContext, 2); await Service<DigitalSurveyService>().Accept(scope, input, Ct); return await Saved(scope, input.Generation); }
    [HttpPut("api/encuesta")]
    [EnableRateLimiting("form-writes")]
    public async Task<IResult> Save([FromBody] DigitalSave input)
    { var scope = await access.Require(HttpContext, 2); await Service<DigitalSurveyService>().Save(scope, input, Ct); return await Saved(scope, input.Generation); }
    [HttpPost("api/encuesta/finalizar")]
    [EnableRateLimiting("form-writes")]
    public async Task<IResult> Finish([FromBody] DigitalFinalize input)
    { var scope = await access.Require(HttpContext, 2); await Service<DigitalSurveyService>().Finalize(scope, input, Ct); return await Saved(scope, input.Generation); }
    [HttpGet("api/informe")]
    public async Task<IResult> Report() => Results.Json(await Service<DigitalSurveyService>().Read(await access.Require(HttpContext, 3), Ct));
    [HttpPut("api/metas/{unit}")]
    public async Task<IResult> Target(string unit, [FromBody] DigitalTargetInput input)
    { await Service<DigitalSurveyService>().Target(await access.Require(HttpContext, 4), unit, input, Ct); return Results.Json(new { saved = true }); }
    [HttpGet("api/generacion")]
    public async Task<IResult> Generation()
    { var scope = await access.Resolve(HttpContext); if (!Enumerable.Range(0, 5).Any(scope.Module)) throw new DomainProblem(403, "No tienes módulos autorizados."); return Results.Json(new { generation = await Service<DigitalDbContext>().Control.AsNoTracking().Select(c => c.Generation).SingleAsync(Ct) }); }
    [HttpPost("api/reinicio/resumen")]
    [EnableRateLimiting("form-writes")]
    public async Task<IResult> ResetPreview([FromBody] DigitalResetPreview input) => Results.Json(await Service<DigitalResetService>().Preview(await access.Require(HttpContext, 4), input, Ct));
    [HttpPost("api/reinicio")]
    [EnableRateLimiting("form-writes")]
    public async Task<IResult> Reset([FromBody] DigitalResetConfirm input) => Results.Json(await Service<DigitalResetService>().Confirm(await access.Require(HttpContext, 4), input, Ct));
    [HttpGet("api/representacion/personas")]
    [EnableRateLimiting("representation-search")]
    public async Task<IResult> Search([FromQuery] string q = "", [FromQuery] int page = 1) => Results.Json(await Service<DigitalRepresentation>().Search(HttpContext, q, page));
    [HttpPost("api/representacion")]
    [EnableRateLimiting("representation-start")]
    public async Task<IResult> Represent([FromBody] JsonObject input)
    { await Service<DigitalRepresentation>().Start(HttpContext, input); return Results.Json(new { redirect = DigitalRoutes.Base }); }
    [HttpDelete("api/representacion")]
    public async Task<IResult> Exit()
    { await Service<DigitalRepresentation>().Exit(HttpContext); return Results.Json(new { redirect = DigitalRoutes.Base }); }
}
