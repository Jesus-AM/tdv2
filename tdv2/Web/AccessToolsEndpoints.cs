using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
namespace Tdv2.Web;

public static class AccessToolsEndpoints
{
    public static void MapAccessTools(this WebApplication app)
    {
        app.MapGet("/configuracion/pruebas-acceso", async (HttpContext http, RequestAccess access, NexoOperations nexo) =>
        {
            var real = await access.Profile(http, "pruebas_acceso");
            return PageResponse.Page(http, "PruebasAcceso", new() { ["puedeVistaPrueba"] = ModuleAccess.Allows(real, "procesos_operativos"),
                ["capacidad"] = access.Capability ?? await nexo.Capability(access.Actor(http), http.RequestAborted) }, real);
        });
        app.MapGet("/configuracion/pruebas-acceso/actuar-como-usuario", async (HttpContext http, RequestAccess access, NexoOperations nexo) =>
        {
            var real = await access.Profile(http, "pruebas_acceso");
            return PageResponse.Page(http, "ActuarComoUsuario", new() { ["capacidad"] = access.Capability ?? await nexo.Capability(access.Actor(http), http.RequestAborted) }, real);
        });
        app.MapGet("/actuar-como-usuario", () => Results.Redirect("/configuracion/pruebas-acceso/actuar-como-usuario"));
        app.MapGet("/vista-prueba", () => Results.Redirect("/configuracion/pruebas-acceso/rol-area"));
        app.MapGet("/configuracion/pruebas-acceso/rol-area", async (HttpContext http, RequestAccess access) =>
        {
            var real = await access.Profile(http, "pruebas_acceso");
            var units = await access.Units(http);
            return PageResponse.Page(http, "VistaPrueba", new() { ["unidades"] = units.Units.Values.OrderBy(u => u.Code).Select(u => u.Public()).ToArray(),
                ["roles"] = ScenarioRoles.All.Select(r => new { value = r.Key, title = r.Value.Label }).ToArray(), ["seleccion"] = null }, real);
        });
        app.MapGet("/actuar-como-usuario/personas", async (HttpContext http, RequestAccess access, NexoOperations nexo, OperationAudit audit) =>
        {
            await access.Profile(http, "pruebas_acceso");
            var capability = access.Capability ?? await nexo.Capability(access.Actor(http), http.RequestAborted);
            RequireCapability(capability);
            var query = Inputs.Query(http, "q", 2, 150); var page = Inputs.Page(http, "pagina");
            var response = (await nexo.Represent("buscar", access.Actor(http), new() { ["q"] = query, ["pagina"] = page }, http.RequestAborted)).AsObject();
            if (response["personas"] is not JsonArray people) throw new DomainProblem(503, "Nexo no confirmó el resultado de la búsqueda.");
            var safe = people.Take(25).Select(p =>
            {
                var row = p!.AsObject();
                return new { usuario_id = NexoOperations.Number(row, "usuario_id"), email = NexoOperations.Text(row, "email"),
                    nombre = NexoOperations.Text(row, "nombre"), id_ur = NexoOperations.Text(row, "id_ur"), desc_ur = NexoOperations.Text(row, "desc_ur"),
                    roles = (row["roles"] as JsonArray ?? []).Select(r => new { clave = NexoOperations.Text(r!.AsObject(), "clave"), nombre = NexoOperations.Text(r!.AsObject(), "nombre") }).ToArray() };
            }).ToArray();
            await audit.Record("buscar", "representacion", "personas", "confirmado", http.RequestAborted, new { cantidad = safe.Length });
            return Results.Json(new { personas = safe, hayMas = people.Count > 25 });
        }).RequireRateLimiting("representation-search");
        app.MapPost("/actuar-como-usuario", async (JsonObject input, HttpContext http, RequestAccess access, NexoOperations nexo,
            INexoProfiles profiles, AccessState state, DatabaseConnections connections, OperationAudit audit) =>
        {
            await access.Profile(http, "pruebas_acceso");
            var capability = access.Capability ?? await nexo.Capability(access.Actor(http), http.RequestAborted); RequireCapability(capability);
            var email = PostgresNexoProfiles.Email(Inputs.Text(input, "email", 3, 254)); var reason = Inputs.Text(input, "motivo", 5, 300); var write = Inputs.Bool(input, "escritura");
            if (write && !NexoOperations.Flag(capability, "escritura")) throw new DomainProblem(403, "Nexo no autorizó representación con escritura para tu cuenta.");
            var profile = await profiles.ForEmail(email, http.RequestAborted);
            if (!ModuleAccess.Allows(profile, "procesos_operativos")) throw new DomainProblem(403, "La persona no tiene acceso a Procesos operativos.");
            AccessSelection? selection = null;
            await using var connection = await connections.Open("Tdv2", http.RequestAborted);
            await using var transaction = await connection.BeginTransactionAsync(http.RequestAborted);
            try
            {
                await state.Guard(connection, transaction, http.RequestAborted, true);
                var response = (await nexo.Represent("iniciar", access.Actor(http), new() { ["email"] = email, ["motivo"] = reason, ["escritura"] = write }, http.RequestAborted)).AsObject();
                selection = RequestAccess.ParseRepresentation(response, access.Actor(http), email, true);
                var duration = NexoOperations.Number(capability, "duracion_minutos");
                if (duration <= 0 || (selection.ExpiresAt - DateTimeOffset.UtcNow).TotalMinutes > duration + 0.1 || selection.Write && !write)
                    throw new DomainProblem(503, "Nexo devolvió una duración o permiso diferente al autorizado.");
                selection = selection with { Reason = reason };
                audit.Effective = profile;
                await state.Set(connection, transaction, selection, http.RequestAborted);
                await audit.Write(connection, transaction, "iniciar", "representacion", selection.Id, "confirmado", http.RequestAborted, selection);
                await transaction.CommitAsync(http.RequestAborted);
            }
            catch
            {
                // Nexo and TDV2 are distinct databases. Compensate an uncommitted local start where possible.
                if (selection?.Token is { } token)
                    try { await nexo.Represent("finalizar", access.Actor(http), new() { ["token"] = token }, CancellationToken.None); } catch (DomainProblem) { }
                throw;
            }
            return Results.Json(new { redirect = "/inicio" });
        }).RequireRateLimiting("representation-start");
        app.MapPost("/vista-prueba", async (JsonObject input, HttpContext http, RequestAccess access, AccessState state, DatabaseConnections connections, OperationAudit audit) =>
        {
            var real = await access.Profile(http, "pruebas_acceso");
            if (Inputs.Text(input, "mode", 1, 20) != "escenario" || input.ContainsKey("email"))
                throw Inputs.Invalid("mode", "La vista de prueba sólo admite un escenario de rol y área, sin seleccionar una persona.");
            var role = Inputs.Text(input, "role", 1, 30); var unit = Inputs.Text(input, "ur", 1, 32);
            var selection = new AccessSelection("preview", Guid.NewGuid().ToString(), access.Actor(http), "", "Vista de prueba", unit, false, DateTimeOffset.UtcNow.AddMinutes(30), Role: role);
            ScenarioRoles.Resolve(selection, await access.Units(http), real);
            await using var connection = await connections.Open("Tdv2", http.RequestAborted);
            await using var transaction = await connection.BeginTransactionAsync(http.RequestAborted);
            await state.Guard(connection, transaction, http.RequestAborted, true);
            await state.Set(connection, transaction, selection, http.RequestAborted);
            await audit.Write(connection, transaction, "iniciar", "vista_prueba", selection.Id, "confirmado", http.RequestAborted, selection);
            await transaction.CommitAsync(http.RequestAborted);
            return Results.Json(new { redirect = "/inicio" });
        }).RequireRateLimiting("preview-start");
        app.MapDelete("/actuar-como-usuario", (HttpContext http, AccessState state, DatabaseConnections connections, NexoOperations nexo, OperationAudit audit) => Exit("representation", http, state, connections, nexo, audit));
        app.MapDelete("/vista-prueba", (HttpContext http, AccessState state, DatabaseConnections connections, NexoOperations nexo, OperationAudit audit) => Exit("preview", http, state, connections, nexo, audit));
    }
    private static void RequireCapability(JsonObject capability)
    {
        if (NexoOperations.Flag(capability, "no_disponible")) throw new DomainProblem(503, "No fue posible comprobar Actuar como usuario con Nexo.");
        if (!NexoOperations.Flag(capability, "permitido")) throw new DomainProblem(403, "Nexo no autorizó Actuar como usuario para tu cuenta.");
    }
    private static async Task<IResult> Exit(string kind, HttpContext http, AccessState state, DatabaseConnections connections, NexoOperations nexo, OperationAudit audit)
    {
        var snapshot = await state.Load(http.RequestAborted);
        if (snapshot.Selection is not { } selection) return Results.Json(new { redirect = "/inicio" });
        if (selection.Kind != kind) throw new DomainProblem(409, "Utiliza la salida del contexto activo.");
        await using var connection = await connections.Open("Tdv2", http.RequestAborted);
        await using var transaction = await connection.BeginTransactionAsync(http.RequestAborted);
        await state.Guard(connection, transaction, http.RequestAborted, true);
        var closed = false;
        if (kind == "representation")
        {
            try
            {
                var actor = http.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (actor == selection.ActorEmail) { await nexo.Represent("finalizar", actor, new() { ["token"] = selection.Token }, http.RequestAborted); closed = true; }
            }
            catch (DomainProblem) { /* Explicit local exit remains available if Nexo rejects or is unavailable. */ }
        }
        await state.Set(connection, transaction, null, http.RequestAborted);
        await audit.Write(connection, transaction, kind == "preview" ? "salir" : "finalizar", kind == "preview" ? "vista_prueba" : "representacion",
            selection.Id, "confirmado", http.RequestAborted, selection, new { cierre_central = closed });
        await transaction.CommitAsync(http.RequestAborted);
        return Results.Json(new { redirect = "/inicio" });
    }
}
