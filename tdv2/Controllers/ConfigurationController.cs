using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tdv2.Domain;
using Tdv2.Synchronization;
using Tdv2.Web;

namespace Tdv2.Controllers;

public sealed class ConfigurationController(RequestAccess access, SynchronizationQueries queries, SyncCoordinator sync, ManualSyncDispatcher dispatcher) : ControllerBase
{
    [HttpGet("/configuracion")]
    public async Task<IResult> Index()
    {
        var profile = await access.Profile(HttpContext, "configuracion");
        return PageResponse.Page(HttpContext, "Configuracion", new()
        {
            ["secciones"] = new
            {
                configuracion_procesos = ModuleAccess.Allows(profile, "configuracion_procesos"),
                sincronizaciones = ModuleAccess.Allows(profile, "sincronizaciones"),
                pruebas_acceso = ModuleAccess.Allows(profile, "pruebas_acceso")
            }
        }, profile);
    }

    [HttpGet("/configuracion/sincronizaciones")]
    public async Task<IResult> Synchronizations()
    {
        var profile = await access.Profile(HttpContext, "sincronizaciones");
        return PageResponse.Page(HttpContext, "Sincronizaciones", await queries.Snapshot(HttpContext.RequestAborted), profile);
    }

    [HttpPut("/configuracion/sincronizaciones/programacion")]
    [EnableRateLimiting("sync-config")]
    public async Task<IResult> Configure([FromBody] JsonObject input)
    {
        static int Integer(JsonObject value, string name) => value[name] is JsonValue v && v.TryGetValue<int>(out var number) && number > 0
            ? number : throw Inputs.Invalid(name, "Revisa el valor indicado.");
        var schedule = new SyncSchedule(Integer(input, "version"), Inputs.Bool(input, "activa"), Integer(input, "intervalo_minutos"),
            Inputs.Text(input, "hora", 5, 5), Inputs.Text(input, "zona_horaria", 1, 64), Inputs.Bool(input, "incluir_ilda"));
        try { await sync.Configure(schedule, access.Actor(HttpContext), true, HttpContext.RequestAborted); }
        catch (SyncProblem error) { throw new DomainProblem(422, error.Message); }
        return Results.Json(new { message = "Programación guardada." });
    }

    [HttpPost("/configuracion/sincronizaciones/ejecutar")]
    [EnableRateLimiting("sync-start")]
    public async Task<IResult> Enqueue([FromBody] JsonObject input)
    {
        dispatcher.RequireAvailable();
        var id = await sync.Enqueue(Inputs.Text(input, "fuentes", 3, 5), "manual", access.Actor(HttpContext), true, HttpContext.RequestAborted);
        dispatcher.Wake();
        return Results.Json(new { message = "Solicitud aceptada. Confirmando el inicio de la sincronización.", id }, statusCode: 202);
    }
}
