using System.Security.Claims;
using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Integrations.Nexo;
using Tdv2.Security;
using Tdv2.Web;

namespace Tdv2.Competencias;

public sealed class DigitalRepresentation(DigitalAccess access, IDigitalNexo nexo, AccessState state, DatabaseConnections connections, OperationAudit audit, DigitalActivity activity, DigitalActivityDelivery delivery)
{
    private async Task<JsonObject> Capability(HttpContext http)
    {
        var scope = await access.Require(http, 4);
        if (state.Loaded?.Selection is not null) throw new DomainProblem(409, "Sal de la representación antes de iniciar otra.");
        var result = (await nexo.Represent("capacidad", scope.Profile.User.Email, null, http.RequestAborted)).AsObject();
        if (!NexoOperations.Flag(result, "permitido")) throw new DomainProblem(403, "Nexo no autorizó Actuar como usuario en Competencias digitales.");
        return result;
    }
    public async Task<object> Search(HttpContext http, string query, int page)
    {
        await Capability(http);
        if (query.Length < 2 || query.Length > 100 || page < 1 || page > 1000) throw new DomainProblem(422, "Escribe al menos dos caracteres para buscar.");
        var data = (await nexo.Represent("buscar", access.Real!.User.Email, new() { ["q"] = query, ["pagina"] = page }, http.RequestAborted)).AsObject();
        if (data["personas"] is not JsonArray people) throw new DomainProblem(503, "No fue posible comprobar el resultado de la búsqueda.");
        return new { people = people.Take(25).Select(p => new { email = NexoOperations.Text(p!.AsObject(), "email"), name = NexoOperations.Text(p.AsObject(), "nombre") }).ToArray(), more = people.Count > 25 };
    }
    public async Task Start(HttpContext http, JsonObject input)
    {
        var capability = await Capability(http);
        if (input.Any(p => p.Key is not ("email" or "motivo"))) throw new DomainProblem(422, "La representación solo admite consultas.");
        var email = PostgresNexoProfiles.Email(Inputs.Text(input, "email", 3, 254)); var reason = Inputs.Text(input, "motivo", 5, 300);
        var profile = await nexo.ForEmail(email, http.RequestAborted); DigitalAccess.ValidateEmployee(profile, email);
        var actor = access.Real!.User.Email; AccessSelection? selection = null;
        await using var connection = await connections.Open("Tdv2", http.RequestAborted);
        await using var transaction = await connection.BeginTransactionAsync(http.RequestAborted);
        try
        {
            await state.Guard(connection, transaction, http.RequestAborted, true);
            var result = (await nexo.Represent("iniciar", actor, new() { ["email"] = email, ["motivo"] = reason, ["escritura"] = false }, http.RequestAborted)).AsObject();
            selection = RequestAccess.ParseRepresentation(result, actor, email, true) with { Application = "competencias-digitales", Reason = reason };
            var duration = NexoOperations.Number(capability, "duracion_minutos");
            if (selection.Write || duration <= 0 || (selection.ExpiresAt - DateTimeOffset.UtcNow).TotalMinutes > duration + .1)
                throw new DomainProblem(503, "Nexo devolvió una representación distinta a la autorizada.");
            audit.Effective = profile;
            await state.Set(connection, transaction, selection, http.RequestAborted);
            await audit.Write(connection, transaction, "iniciar", "representacion_competencias", selection.Id, "confirmado", http.RequestAborted, selection);
            await transaction.CommitAsync(http.RequestAborted);
            await activity.BestEffort(() => activity.Representation(selection, false, CancellationToken.None));
            await delivery.TryFlush();
        }
        catch
        {
            if (selection?.Token is { } token) try { await nexo.Represent("finalizar", actor, new() { ["token"] = token }, CancellationToken.None); } catch (DomainProblem) { }
            throw;
        }
    }
    public async Task Exit(HttpContext http)
    {
        var snapshot = await state.Load(http.RequestAborted);
        if (snapshot.Selection is not { } selection) return;
        if (selection.Application != "competencias-digitales" || selection.Kind != "representation") throw new DomainProblem(409, "Usa la salida del contexto activo.");
        var actor = http.User.FindFirstValue(ClaimTypes.Email);
        if (actor != selection.ActorEmail) throw new DomainProblem(403, "El contexto no corresponde a esta cuenta.");
        await using var connection = await connections.Open("Tdv2", http.RequestAborted);
        await using var transaction = await connection.BeginTransactionAsync(http.RequestAborted);
        await state.Guard(connection, transaction, http.RequestAborted, true);
        var closed = false;
        try { await nexo.Represent("finalizar", actor, new() { ["token"] = selection.Token }, http.RequestAborted); closed = true; }
        catch (DomainProblem) { /* La salida local se mantiene disponible durante una caída de Nexo. */ }
        await state.Set(connection, transaction, null, http.RequestAborted);
        await audit.Write(connection, transaction, "finalizar", "representacion_competencias", selection.Id, "confirmado", http.RequestAborted, selection, new { cierre_central = closed });
        await transaction.CommitAsync(http.RequestAborted);
        await activity.BestEffort(() => activity.Representation(selection, true, CancellationToken.None));
        await delivery.TryFlush();
    }
}
