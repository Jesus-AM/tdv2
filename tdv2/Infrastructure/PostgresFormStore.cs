using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Domain.Entities;
using Tdv2.Security;
namespace Tdv2.Infrastructure;

/// <summary>EF para consultas/guardado; SQL únicamente para los bloqueos y la auditoría compartida.</summary>
public sealed class PostgresFormStore(Tdv2DbContext db, AccessState state) : IFormStore
{
    private DateTime? catalogReadAt;
    private bool unitsRead;

    public async Task<IReadOnlyList<Unit>> Units(CancellationToken cancellation)
    {
        // Una sola sentencia toma la misma instantánea para áreas y fecha del catálogo.
        var rows = await db.ResponsibleUnits.AsNoTracking().Where(u => u.Present)
            .Select(u => new { Unit = u, CatalogDate = db.CatalogSynchronizations
                .Where(c => c.Id == "sii").Select(c => (DateTime?)c.CompletedAt).FirstOrDefault() })
            .ToListAsync(cancellation);
        if (rows.Count > 0) { catalogReadAt = rows[0].CatalogDate; unitsRead = true; }
        return rows.Select(r => new Unit(r.Unit.Id, r.Unit.Code, r.Unit.Description, r.Unit.Level ?? 0,
            r.Unit.ParentId, r.Unit.Year, r.Unit.EmployeeNumber, r.Unit.Present, r.Unit.Status ?? "", r.Unit.Kind)).ToArray();
    }

    public async Task<IReadOnlyList<Collaboration>> Collaborations(string email, CancellationToken cancellation) =>
        await db.UnitCollaborations.AsNoTracking().Where(c => c.Email == email && c.RevokedAt == null)
            .Select(c => new Collaboration(c.Email, c.EmployeeNumber, c.OriginUnitId, c.ScopeUnitId,
                c.Kind, c.NexoGrantId, c.NexoRoleId, c.GrantorUnitId)).ToListAsync(cancellation);

    public async Task<StoredForm?> Get(string unit, CancellationToken cancellation)
    {
        var form = await db.UnitForms.AsNoTracking().SingleOrDefaultAsync(f => f.UnitId == unit, cancellation);
        return form is null ? null : Stored(form);
    }

    private static StoredForm Stored(UnitForm form) => new(form.UnitId, JsonNode.Parse(form.Content)!.AsObject(),
        form.Version, form.Progress, new DateTimeOffset(DateTime.SpecifyKind(form.UpdatedAt!.Value, DateTimeKind.Utc)), form.UpdatedBy,
        form.SubmittedAt, form.SubmissionSnapshot, form.Year);

    public async Task LockUnit(string unit, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellation)
    {
        await state.Guard(connection, transaction, cancellation);
        await using var command = new NpgsqlCommand("SELECT id_ur FROM unidades_responsables_poa WHERE id_ur=$1 AND presente AND lower(trim(estatus_ur))='activo' FOR UPDATE", connection, transaction);
        command.Parameters.AddWithValue(unit);
        if (await command.ExecuteScalarAsync(cancellation) is null) throw new DomainProblem(403, "La UR ya no está activa.");
        var catalog = await db.CatalogSynchronizations.AsNoTracking().Where(c => c.Id == "sii").Select(c => (DateTime?)c.CompletedAt).FirstOrDefaultAsync(cancellation);
        if (!unitsRead || catalog != catalogReadAt) throw new DomainProblem(409, "El catálogo cambió. Conserva tu propuesta y actualiza el estado.");
    }

}
