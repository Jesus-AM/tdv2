using Tdv2.Integrations.Nexo;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;

namespace Tdv2.Services;

/// <summary>Revoca la sesión y audita ambas identidades, incluso si Nexo está caído.</summary>
public sealed class SessionTerminationService(AccessState state, DatabaseConnections connections, NexoOperations nexo, OperationAudit audit)
{
    public async Task Revoke(string? email, CancellationToken ct)
    {
        if (state.SessionHash is null) return;
        var snapshot = await state.Load(ct);
        await using var connection = await connections.Open("Tdv2", ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await state.Guard(connection, transaction, ct, true);
        var closed = false;
        if (snapshot.Selection is { Kind: "representation" } selection && selection.ActorEmail == email)
        {
            try { await nexo.Represent("finalizar", email!, new() { ["token"] = selection.Token }, ct); closed = true; }
            catch (DomainProblem) { /* El cierre local sigue disponible ante revocación o caída central. */ }
        }
        await PostgresTicketStore.Revoke(connection, transaction, state.SessionHash, ct);
        await audit.Write(connection, transaction, "logout", "auth", "sesion", "confirmado", ct,
            snapshot.Selection, new { cierre_central = closed });
        await transaction.CommitAsync(ct);
    }
}
