using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Domain.Entities;
using Tdv2.Infrastructure;
using Tdv2.Security;
using Tdv2.Synchronization;
using Tdv2.Web;
namespace Tdv2.Services;

public sealed record BlockRequest(string Key, [property: JsonRequired] int Version, Guid? LeaseId = null, JsonNode? Value = null);
public sealed record EditRequest(Guid TabId, Guid OperationId, BlockRequest[] Blocks, string Section = "contexto", bool Release = false);
public sealed record SubmitRequest(Guid TabId, Guid OperationId, [property: JsonRequired] int Version);
public sealed record LeaseView(Guid? id, bool propia, string? titular, DateTimeOffset? venceEn);
public sealed record BlockView(string key, int version, LeaseView? reserva);

/// <summary>PostgreSQL arbitra reservas, escrituras parciales y envío; la red sólo transporta propuestas.</summary>
public sealed class FormEditingService(Tdv2DbContext db, PostgresFormStore store, RequestAccess access,
    AccessState state, FormSchema schema, ILocalCatalog catalogs, OperationAudit audit, IHttpContextAccessor accessor,
    FormNotifications notifications)
{
    private HttpContext Http => accessor.HttpContext!;
    private CancellationToken Ct => Http.RequestAborted;
    private string Actor => access.Actor(Http);
    private long Revision => state.Loaded!.Revision;
    private async Task<(Tdv2.Web.AccessContext Context, Scope Scope)> Authorize(string ur, bool write)
    {
        var context = await access.Resolve(Http);
        if (!context.Scopes.TryGetValue(ur, out var scope) || write && (!scope.Edit || context.ReadOnly))
            throw new DomainProblem(403, "No tienes permiso para esta operación en el formato.");
        return (context, scope);
    }
    private async Task<DateTimeOffset> Now(NpgsqlConnection connection, NpgsqlTransaction? transaction)
    {
        await using var command = new NpgsqlCommand("SELECT clock_timestamp()", connection, transaction);
        return new DateTimeOffset((DateTime)(await command.ExecuteScalarAsync(Ct))!);
    }
    private static DomainProblem Conflict(string message) => new(409, message);
    private static void Validate(EditRequest input)
    {
        if (input.TabId == Guid.Empty || input.OperationId == Guid.Empty || input.Blocks is null || input.Blocks.Length is < 1 or > 2400
            || input.Blocks.Any(b => b is null || b.Version < 0 || string.IsNullOrEmpty(b.Key) || !FormBlocks.ValidKey(b.Key))
            || input.Blocks.Select(b => b.Key).Distinct(StringComparer.Ordinal).Count() != input.Blocks.Length
            || !FormBlocks.Sections.Contains(input.Section)) throw new DomainProblem(422, "Revisa los bloques y la sesión de edición.");
    }
    private bool Owner(FormBlock block, Guid tab, Guid? lease) => block.LeaseId is not null && block.LeaseId == lease
        && block.SessionHash == state.SessionHash && block.TabId == tab && block.ContextRevision == Revision;
    private static void Editable(UnitForm form, Unit unit)
    {
        if (form.SubmittedAt is not null) throw Conflict("El formato ya fue enviado y es de solo consulta.");
        if (form.Year != 0 && form.Year != unit.Year) throw Conflict("El ejercicio del catálogo cambió. El formato histórico se conserva y no puede sobrescribirse.");
    }
    private async Task<UnitForm> Load(Unit unit, Profile effective)
    {
        var form = await db.UnitForms.SingleOrDefaultAsync(f => f.UnitId == unit.Id, Ct);
        if (form is not null)
        {
            if (form.SubmittedAt is null)
            {
                var existing = JsonNode.Parse(form.Content)!.AsObject();
                LocalCatalog.Merge(existing, await catalogs.Inventory(unit, Ct));
                if (!JsonNode.DeepEquals(existing, JsonNode.Parse(form.Content)))
                { form.Content = existing.ToJsonString(); form.Version++; await db.SaveChangesAsync(Ct); }
            }
            return form;
        }
        var content = schema.Blank(unit);
        LocalCatalog.Merge(content, await catalogs.Inventory(unit, Ct));
        form = new UnitForm { UnitId = unit.Id, Year = unit.Year, Content = content.ToJsonString(), Version = 0,
            UpdatedBy = effective.User.Email, CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified) };
        db.UnitForms.Add(form);
        await db.SaveChangesAsync(Ct);
        return form;
    }
    public async Task<object> Read(string ur, Guid tab)
    {
        var (context, scope) = await Authorize(ur, false);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, Ct);
        var now = await Now((NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)transaction.GetDbTransaction());
        var form = await db.UnitForms.AsNoTracking().SingleOrDefaultAsync(f => f.UnitId == ur, Ct);
        var content = form is null ? schema.Blank(scope.Unit) : JsonNode.Parse(form.Content)!.AsObject();
        if (form?.SubmittedAt is null) LocalCatalog.Merge(content, await catalogs.Inventory(scope.Unit, Ct));
        var blocks = await db.FormBlocks.AsNoTracking().Where(b => b.UnitId == ur).ToListAsync(Ct);
        var section = await db.FormPositions.AsNoTracking().Where(p => p.UnitId == ur && p.Actor == Actor && p.Effective == context.Profile.User.Email).Select(p => p.Section).SingleOrDefaultAsync(Ct);
        var result = new { contenido = content, version = form?.Version ?? 0, porcentaje = form?.Progress ?? 0,
            actualizadoEn = form?.UpdatedAt is { } updated ? new DateTimeOffset(DateTime.SpecifyKind(updated, DateTimeKind.Utc)) : (DateTimeOffset?)null,
            actualizadoPor = form?.UpdatedBy, enviadoEn = form?.SubmittedAt,
            editable = scope.Edit && !context.ReadOnly && form?.SubmittedAt is null,
            puedeEnviar = scope.Edit && !context.ReadOnly && form?.SubmittedAt is null && new FormAccess(context.Directory).CanSubmit(context.Profile, scope.Unit),
            seccion = section ?? "contexto", servidorEn = now,
            bloques = blocks.Select(b => Public(b, tab, now)).ToArray() };
        await transaction.CommitAsync(Ct);
        return result;
    }
    private BlockView Public(FormBlock block, Guid tab, DateTimeOffset now) => new(block.Key, block.Version,
        block.ExpiresAt > now && block.LeaseId is not null ? new(Owner(block, tab, block.LeaseId) ? block.LeaseId : null,
            Owner(block, tab, block.LeaseId), block.Holder, block.ExpiresAt) : null);
    public async Task<object> Reserve(string ur, EditRequest input, bool renew = false)
    {
        Validate(input); var (context, scope) = await Authorize(ur, true);
        await using var tx = await db.Database.BeginTransactionAsync(Ct);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection(); var pg = (NpgsqlTransaction)tx.GetDbTransaction();
        // Orden común: sesión → UR → formato. Adquirir, guardar y enviar se serializan en la UR,
        // nunca se mantiene una transacción abierta mientras la persona llena un campo.
        await store.LockUnit(ur, connection, pg, Ct);
        var form = await Load(scope.Unit, context.Profile); Editable(form, scope.Unit);
        var all = await db.FormBlocks.Where(b => b.UnitId == ur).ToDictionaryAsync(b => b.Key, Ct);
        var now = await Now(connection, pg);
        var result = new List<FormBlock>();
        foreach (var request in input.Blocks)
        {
            if (!all.TryGetValue(request.Key, out var block))
            {
                block = new() { UnitId = ur, Key = request.Key }; db.FormBlocks.Add(block);
            }
            if (block.Version != request.Version) throw Conflict("El bloque cambió. Recupera el estado vigente antes de aplicar tu propuesta.");
            var active = block.LeaseId is not null && block.ExpiresAt > now;
            if (renew && (!active || !Owner(block, input.TabId, request.LeaseId))) throw Conflict("La reserva venció o pertenece a otra sesión. Conserva tu propuesta.");
            if (active && (block.SessionHash != state.SessionHash || block.TabId != input.TabId || block.ContextRevision != Revision))
                throw Conflict("Otra sesión está editando este bloque. Conserva tu propuesta y espera a que termine.");
            if (!active) block.LeaseId = Guid.NewGuid(); // Testigo nuevo: el titular vencido nunca recupera autoridad con el anterior.
            block.SessionHash = state.SessionHash; block.TabId = input.TabId; block.ContextRevision = Revision;
            block.Holder = context.Profile.User.Name; block.ExpiresAt = now.AddSeconds(45); result.Add(block);
        }
        await db.SaveChangesAsync(Ct); await tx.CommitAsync(Ct);
        notifications.Changed(ur);
        return new { bloques = result.Select(b => Public(b, input.TabId, now)), servidorEn = now };
    }
    private async Task<JsonNode?> Receipt(string ur, Guid operation, Guid tab, string fingerprint)
    {
        var receipt = await db.FormMutations.AsNoTracking().SingleOrDefaultAsync(m => m.UnitId == ur && m.Id == operation, Ct);
        if (receipt is null) return null;
        if (receipt.SessionHash != state.SessionHash || receipt.TabId != tab || receipt.ContextRevision != Revision || receipt.Fingerprint != fingerprint)
            throw Conflict("El identificador de operación ya se utilizó con otra propuesta.");
        return JsonNode.Parse(receipt.Response);
    }
    private void Remember(string ur, Guid operation, Guid tab, string fingerprint, object response, DateTimeOffset now) =>
        db.FormMutations.Add(new() { UnitId = ur, Id = operation, TabId = tab, SessionHash = state.SessionHash!, ContextRevision = Revision,
            Fingerprint = fingerprint, Response = JsonSerializer.Serialize(response), CreatedAt = now });
    private static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));

    public async Task<object> Save(string ur, EditRequest input)
    {
        Validate(input); var (context, scope) = await Authorize(ur, true); var fingerprint = Fingerprint(input);
        await using var tx = await db.Database.BeginTransactionAsync(Ct);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection(); var pg = (NpgsqlTransaction)tx.GetDbTransaction();
        await store.LockUnit(ur, connection, pg, Ct);
        var form = await Load(scope.Unit, context.Profile);
        // Un reintento idéntico confirma el resultado original, incluso si después se envió el formato.
        if (await Receipt(ur, input.OperationId, input.TabId, fingerprint) is { } prior) return prior;
        Editable(form, scope.Unit);
        var all = await db.FormBlocks.Where(b => b.UnitId == ur).ToDictionaryAsync(b => b.Key, Ct);
        var now = await Now(connection, pg);
        foreach (var request in input.Blocks)
            if (!all.TryGetValue(request.Key, out var block) || !Owner(block, input.TabId, request.LeaseId)
                || block.ExpiresAt <= now || block.Version != request.Version)
                throw Conflict("La versión o la reserva del bloque cambió. Tu propuesta no se ha sobrescrito ni guardado.");
        var deadlines = input.Blocks.Select(b => all[b.Key].ExpiresAt!.Value).ToArray();
        var old = JsonNode.Parse(form.Content)!.AsObject();
        var candidate = FormBlocks.Apply(old, input.Blocks.ToDictionary(b => b.Key, b => b.Value, StringComparer.Ordinal));
        var canonical = FormBlocks.Split(schema.Validate(candidate, scope.Unit));
        // Normaliza sólo los bloques de esta operación. Jamás sustituye otros bloques con una copia del cliente.
        var changes = input.Blocks.ToDictionary(b => b.Key, b => canonical.GetValueOrDefault(b.Key), StringComparer.Ordinal);
        var content = FormBlocks.Apply(old, changes);
        form.Content = content.ToJsonString(); form.Year = scope.Unit.Year; form.Version++;
        form.Progress = checked((short)FormSchema.Progress(content)); form.UpdatedBy = context.Profile.User.Email;
        form.UpdatedAt = DateTime.SpecifyKind(now.UtcDateTime, DateTimeKind.Unspecified);
        foreach (var request in input.Blocks)
        {
            var block = all[request.Key]; block.Version++;
            if (input.Release) { block.LeaseId = null; block.ExpiresAt = null; }
        }
        var position = await db.FormPositions.SingleOrDefaultAsync(p => p.UnitId == ur && p.Actor == Actor && p.Effective == context.Profile.User.Email, Ct);
        if (position is null) { position = new() { UnitId = ur, Actor = Actor, Effective = context.Profile.User.Email }; db.FormPositions.Add(position); }
        position.Section = input.Section;
        var response = new { version = form.Version, porcentaje = form.Progress, actualizadoEn = now, actualizadoPor = form.UpdatedBy,
            bloques = input.Blocks.Select(b => new { key = b.Key, version = all[b.Key].Version, value = changes[b.Key], reserva = Public(all[b.Key], input.TabId, now).reserva }).ToArray() };
        Remember(ur, input.OperationId, input.TabId, fingerprint, response, now);
        await db.SaveChangesAsync(Ct);
        await audit.Write(connection, pg, "guardar_bloques", "formato_ur", ur, "confirmado", Ct, details: new { version = form.Version, bloques = input.Blocks.Select(b => b.Key), operacion = input.OperationId });
        var beforeCommit = await Now(connection, pg);
        if (deadlines.Any(deadline => deadline <= beforeCommit))
            throw Conflict("La reserva venció antes de confirmar el guardado. Conserva tu propuesta.");
        await tx.CommitAsync(Ct); notifications.Changed(ur); return response;
    }
    public async Task<object> Release(string ur, EditRequest input)
    {
        Validate(input); await Authorize(ur, true);
        await using var tx = await db.Database.BeginTransactionAsync(Ct);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection(); var pg = (NpgsqlTransaction)tx.GetDbTransaction();
        await store.LockUnit(ur, connection, pg, Ct);
        var blocks = await db.FormBlocks.Where(b => b.UnitId == ur).ToListAsync(Ct);
        foreach (var request in input.Blocks)
            if (blocks.FirstOrDefault(b => b.Key == request.Key) is { } block && Owner(block, input.TabId, request.LeaseId))
            { block.LeaseId = null; block.ExpiresAt = null; }
        await db.SaveChangesAsync(Ct); await tx.CommitAsync(Ct); notifications.Changed(ur);
        return new { liberado = true };
    }
    public async Task<object> Submit(string ur, SubmitRequest input)
    {
        if (input.TabId == Guid.Empty || input.OperationId == Guid.Empty || input.Version < 0) throw new DomainProblem(422, "Revisa la solicitud de envío.");
        var (context, scope) = await Authorize(ur, true);
        if (!new FormAccess(context.Directory).CanSubmit(context.Profile, scope.Unit)) throw new DomainProblem(403, "Sólo el responsable autorizado puede enviar este formato.");
        await using var tx = await db.Database.BeginTransactionAsync(Ct);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection(); var pg = (NpgsqlTransaction)tx.GetDbTransaction();
        await store.LockUnit(ur, connection, pg, Ct); var form = await Load(scope.Unit, context.Profile);
        var fingerprint = Fingerprint(input);
        if (await Receipt(ur, input.OperationId, input.TabId, fingerprint) is { } prior) return prior;
        Editable(form, scope.Unit);
        if (form.Version != input.Version) throw Conflict("Hay respuestas más recientes. Revísalas antes de confirmar el envío.");
        var now = await Now(connection, pg);
        var blocks = await db.FormBlocks.Where(b => b.UnitId == ur).ToListAsync(Ct);
        if (blocks.Any(b => b.LeaseId is not null && b.ExpiresAt > now && (b.SessionHash != state.SessionHash || b.TabId != input.TabId || b.ContextRevision != Revision)))
            throw Conflict("Otra sesión mantiene una reserva de edición. Espera a que termine antes de enviar.");
        var content = schema.Validate(JsonNode.Parse(form.Content)!.AsObject(), scope.Unit); FormSchema.RequireComplete(content);
        form.SubmissionSnapshot = JsonSerializer.Serialize(new { unidad = scope.Unit.Public(), contenido = JsonNode.Parse(form.Content), version = form.Version + 1 });
        form.SubmittedAt = now; form.SubmittedBy = Actor; form.SubmittedEffective = context.Profile.User.Email;
        form.SubmissionId = input.OperationId; form.Version++;
        foreach (var block in blocks) { block.LeaseId = null; block.ExpiresAt = null; }
        var response = new { version = form.Version, enviadoEn = now, enviadoPor = context.Profile.User.Email };
        Remember(ur, input.OperationId, input.TabId, fingerprint, response, now);
        await db.SaveChangesAsync(Ct);
        await audit.Write(connection, pg, "enviar", "formato_ur", ur, "confirmado", Ct, details: new { version = form.Version, ejercicio = form.Year, operacion = input.OperationId });
        await tx.CommitAsync(Ct); notifications.Changed(ur); return response;
    }
}
