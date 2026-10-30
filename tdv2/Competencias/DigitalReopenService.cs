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
public sealed record DigitalReopen(Guid Operation, Guid Survey, int Version, Guid Generation);

public sealed class DigitalReopenService(DigitalDbContext db, DatabaseConnections connections, AccessState state, DigitalActivity activity, DigitalActivityDelivery delivery)
{
    private void Authorize(DigitalScope scope)
    {
        if (!scope.ConfigurationModule(DigitalRoutes.Management) || state.Loaded?.Selection is not null)
            throw new DomainProblem(403, "Esta acción requiere al administrador autorizado, fuera de representación.");
    }
    public async Task<object> Search(DigitalScope scope, string query, CancellationToken ct)
    {
        Authorize(scope); query = query.Trim();
        if (query.Length is < 2 or > 100) throw new DomainProblem(422, "Escribe al menos dos caracteres del nombre o correo.");
        await using var read = await db.Database.BeginTransactionAsync(ct);
        var generation = await DigitalGeneration.Shared(db, ct);
        var text = "%" + query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var rows = await db.Surveys.AsNoTracking().Where(s => (s.CompletedAt != null || s.ReopenedAt != null)
            && (EF.Functions.ILike(s.Name, text) || EF.Functions.ILike(s.Email, text)))
            .OrderBy(s => s.Name).ThenBy(s => s.Id).Take(26)
            .Select(s => new { s.Id, s.Name, s.Email, s.InstitutionJson, s.Version, s.Revision, s.CompletedAt }).ToArrayAsync(ct);
        var ids = rows.Where(s => s.CompletedAt == null).Select(s => s.Id).ToArray();
        var previous = await db.Submissions.AsNoTracking().Where(s => ids.Contains(s.SurveyId)).GroupBy(s => s.SurveyId)
            .Select(g => new { Id = g.Key, Date = g.Max(s => s.CompletedAt) }).ToDictionaryAsync(s => s.Id, s => s.Date, ct);
        return new { generation, more = rows.Length > 25, people = rows.Take(25).Select(s => new {
            s.Id, s.Name, s.Email, s.Version, s.Revision, s.CompletedAt, lastSubmittedAt = s.CompletedAt ?? previous.GetValueOrDefault(s.Id),
            unit = JsonNode.Parse(s.InstitutionJson)?["unit"]?.DeepClone(), status = s.CompletedAt is null ? "Reabierta · En curso" : "Finalizada" }) };
    }
    public async Task<JsonObject> Reopen(DigitalScope scope, DigitalReopen input, CancellationToken ct)
    {
        Authorize(scope);
        if (input.Operation == Guid.Empty || input.Survey == Guid.Empty || input.Version < 1) throw new DomainProblem(422, "Selecciona una encuesta y confirma la reapertura.");
        await using var shared = await connections.Open("Tdv2", ct);
        await using var session = await shared.BeginTransactionAsync(ct); await state.Guard(shared, session, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await using var command = new NpgsqlCommand("SELECT public.cd_reopen_survey($1,$2,$3,$4,$5,$6)", (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)tx.GetDbTransaction());
            foreach (var value in new object[] { input.Operation, scope.Profile.User.Email, UnitDirectory.Employee(scope.Profile.User)!, input.Survey, input.Version, input.Generation }) command.Parameters.AddWithValue(value);
            var result = JsonNode.Parse((string)(await command.ExecuteScalarAsync(ct))!)!.AsObject();
            await activity.Queue(scope, "reopen:" + input.Operation, "administracion", "reabrir_encuesta", "reapertura", input.Operation.ToString(), ct, result["email"]!.ToString());
            await state.Guard(shared, session, ct); await tx.CommitAsync(ct); await session.CommitAsync(ct);
            await tx.DisposeAsync(); await delivery.TryFlush(); result.Remove("email"); return result;
        }
        catch (PostgresException e) when (e.SqlState == "CD003")
        { throw new DomainProblem(409, "La encuesta o su generación cambió. Actualiza la búsqueda antes de reabrir."); }
        catch (PostgresException e) when (e.SqlState == "CD002")
        { throw new DomainProblem(403, "La operación no corresponde a este administrador o encuesta."); }
    }
}
