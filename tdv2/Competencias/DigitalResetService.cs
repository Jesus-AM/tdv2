using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;

namespace Tdv2.Competencias;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DigitalResetPreview(bool IncludeTargets);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DigitalResetConfirm(Guid Operation, string Confirmation);

public static class DigitalGeneration
{
    // Mismo bloqueo en las funciones SQL. Lecturas y escrituras comparten; reinicio exclusivo.
    public const long LockKey = 2710202601;
    public static async Task<Guid> Shared(DigitalDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("SELECT pg_catalog.pg_advisory_xact_lock_shared(2710202601)", ct);
        return await db.Control.AsNoTracking().Where(c => c.Id == 1).Select(c => c.Generation).SingleAsync(ct);
    }
    public static void Require(Guid current, Guid requested)
    {
        if (requested != current) throw new DomainProblem(409, "Las encuestas fueron reiniciadas. Recarga para comenzar desde Perfil; esta captura anterior ya no se puede guardar.", new { reset = true });
    }
}

public sealed class DigitalResetService(DigitalDbContext db, DatabaseConnections connections, AccessState state, DigitalActivity activity, DigitalActivityDelivery delivery)
{
    public Task<JsonObject> Preview(DigitalScope scope, DigitalResetPreview input, CancellationToken ct) =>
        Execute(scope, "SELECT public.cd_prepare_reset($1,$2,$3,$4)", [Guid.NewGuid(), scope.Profile.User.Email, UnitDirectory.Employee(scope.Profile.User)!, input.IncludeTargets], ct);

    public Task<JsonObject> Confirm(DigitalScope scope, DigitalResetConfirm input, CancellationToken ct)
    {
        if (input.Confirmation != "REINICIAR ENCUESTAS") throw new DomainProblem(422, "Escribe exactamente REINICIAR ENCUESTAS para confirmar.");
        return Execute(scope, "SELECT public.cd_execute_reset($1,$2,$3)", [input.Operation, scope.Profile.User.Email, UnitDirectory.Employee(scope.Profile.User)!], ct);
    }
    private async Task<JsonObject> Execute(DigitalScope scope, string sql, object[] parameters, CancellationToken ct)
    {
        if (!scope.ConfigurationModule(DigitalRoutes.Management) || state.Loaded?.Selection is not null)
            throw new DomainProblem(403, "Solo el administrador real de Competencias digitales puede reiniciar encuestas, fuera de representación.");
        await using var shared = await connections.Open("Tdv2", ct);
        await using var session = await shared.BeginTransactionAsync(ct);
        await state.Guard(shared, session, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)tx.GetDbTransaction());
            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter);
            var result = JsonNode.Parse((string)(await command.ExecuteScalarAsync(ct))!)!.AsObject();
            var action = sql.Contains("cd_execute_reset", StringComparison.Ordinal) ? "reiniciar_encuestas" : "preparar_reinicio";
            await activity.Queue(scope, action + ":" + parameters[0], "administracion", action, "reinicio_global", parameters[0].ToString()!, ct);
            await state.Guard(shared, session, ct);
            await tx.CommitAsync(ct); await session.CommitAsync(ct);
            await tx.DisposeAsync();
            await delivery.TryFlush();
            return result;
        }
        catch (PostgresException e) when (e.SqlState == "CD001")
        { throw new DomainProblem(409, "El resumen cambió o venció. Revisa nuevamente los conteos y confirma una nueva operación."); }
        catch (PostgresException e) when (e.SqlState == "CD002")
        { throw new DomainProblem(403, "La confirmación no corresponde a este administrador."); }
    }
}
