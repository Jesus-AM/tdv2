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
public sealed record EditRequest(Guid TabId, Guid OperationId, BlockRequest[] Blocks, string Section = "contexto", bool Release = false, RowRemoval? Removal = null);
public sealed record SubmitRequest(Guid TabId, Guid OperationId, [property: JsonRequired] int Version, int Stage = FormStages.First);
public sealed record FormPositionRequest(string Section);
public sealed record LeaseView(Guid? id, bool propia, string? titular, DateTimeOffset? venceEn, bool otraPestana, int color, string? foto);
public sealed record BlockView(string key, int version, LeaseView? reserva);

/// <summary>PostgreSQL arbitra reservas, escrituras parciales y envío; la red sólo transporta propuestas.</summary>
public sealed class FormEditingService(Tdv2DbContext db, PostgresFormStore store, RequestAccess access,
    AccessState state, FormSchema schema, ILocalCatalog catalogs, OperationAudit audit, IHttpContextAccessor accessor,
    FormNotifications notifications, ParticipantPhotos photos)
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
            || !FormBlocks.Locations.Contains(input.Section)) throw new DomainProblem(422, "Revisa los bloques y la sesión de edición.");
    }
    private bool Owner(FormBlock block, Guid tab, Guid? lease) => block.LeaseId is not null && block.LeaseId == lease
        && block.SessionHash == state.SessionHash && block.TabId == tab && block.ContextRevision == Revision;
    private static void Editable(UnitForm form, Unit unit)
    {
        if (FormStages.Status(form).Blocked) throw Conflict("La etapa está bloqueada para edición o todavía no está habilitada.");
        if (form.Year != 0 && form.Year != unit.Year) throw Conflict("El ejercicio del catálogo cambió. El formato histórico se conserva y no puede sobrescribirse.");
    }
    private async Task<UnitForm> Load(Unit unit, Profile effective)
    {
        var form = await db.UnitForms.Include(f => f.StageSubmissions).SingleOrDefaultAsync(f => f.UnitId == unit.Id, Ct);
        if (form is not null)
        {
            if (!FormStages.Status(form).Blocked && form.ActiveStage == FormStages.First && FormCapture.CurrentYear(form.Year, unit))
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
        var form = await db.UnitForms.AsNoTracking().Include(f => f.StageSubmissions).SingleOrDefaultAsync(f => f.UnitId == ur, Ct);
        var content = form is null ? schema.Blank(scope.Unit) : JsonNode.Parse(form.Content)!.AsObject();
        var availableProcedures = form is null ? [] : ProcedureEligibility.Options(content);
        var stage = FormStages.Status(form, scope.Unit.Year);
        var current = !stage.Blocked && FormCapture.CurrentYear(form?.Year, scope.Unit);
        if (current && stage.id == FormStages.First) LocalCatalog.Merge(content, await catalogs.Inventory(scope.Unit, Ct));
        var delivery = FormStages.Describe(stage, content);
        var blocks = await db.FormBlocks.AsNoTracking().Where(b => b.UnitId == ur).ToListAsync(Ct);
        var section = await db.FormPositions.AsNoTracking().Where(p => p.UnitId == ur && p.Actor == Actor && p.Effective == context.Profile.User.Email).Select(p => p.Section).SingleOrDefaultAsync(Ct);
        var result = new { contenido = content, version = form?.Version ?? 0,
            entrega = delivery,
            porcentaje = !current ? form!.Progress : FormStages.Progress(stage.id, content),
            porcentajeEtapa = FormStages.Progress(stage.id, content), revisionEtapa = delivery.revision,
            procedimientosDisponibles = availableProcedures,
            seccionesPosteriores = FormCapture.LaterSections(context.Profile),
            revisionEnvio = current ? delivery.revision : null,
            actualizadoEn = form?.UpdatedAt is { } updated ? new DateTimeOffset(DateTime.SpecifyKind(updated, DateTimeKind.Utc)) : (DateTimeOffset?)null,
            actualizadoPor = form?.UpdatedBy, enviadoEn = stage.enviadoEn, enviadoPor = stage.enviadoPor,
            editable = scope.Edit && !context.ReadOnly && current,
            puedeEnviar = scope.Edit && !context.ReadOnly && current && new FormAccess(context.Directory).CanSubmit(context.Profile, scope.Unit),
            seccion = FormCapture.Section(context.Profile, section), servidorEn = now,
            bloques = blocks.Select(b => Public(b, tab, now)).ToArray() };
        await transaction.CommitAsync(Ct);
        return result;
    }
    public async Task<object> Position(string ur, FormPositionRequest input)
    {
        var (context, _) = await Authorize(ur, false);
        if (!FormBlocks.Locations.Contains(input.Section) || FormCapture.Section(context.Profile, input.Section) != input.Section)
            throw new DomainProblem(422, "La ubicación no está disponible.");
        await using var tx = await db.Database.BeginTransactionAsync(Ct);
        await store.LockUnit(ur, (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)tx.GetDbTransaction(), Ct);
        // La navegación de un formato nuevo no crea respuestas ni altera el avance.
        if (await db.UnitForms.AnyAsync(f => f.UnitId == ur, Ct))
        {
            var position = await db.FormPositions.SingleOrDefaultAsync(p => p.UnitId == ur && p.Actor == Actor && p.Effective == context.Profile.User.Email, Ct);
            if (position is null) { position = new() { UnitId = ur, Actor = Actor, Effective = context.Profile.User.Email }; db.FormPositions.Add(position); }
            position.Section = input.Section; await db.SaveChangesAsync(Ct);
        }
        await tx.CommitAsync(Ct); return new { seccion = input.Section };
    }
    private BlockView Public(FormBlock block, Guid tab, DateTimeOffset now) => new(block.Key, block.Version,
        block.ExpiresAt > now && block.LeaseId is not null ? new(Owner(block, tab, block.LeaseId) ? block.LeaseId : null,
            Owner(block, tab, block.LeaseId), block.Holder, block.ExpiresAt,
            block.SessionHash == state.SessionHash && block.ContextRevision == Revision && block.TabId != tab, block.Color,
            block.ParticipantUserId is not null && block.Participant is not null
                ? $"/formatos/{Uri.EscapeDataString(block.UnitId)}/participantes/{block.Participant}/foto" : null) : null);
    public async Task<object> Reserve(string ur, EditRequest input, bool renew = false)
    {
        Validate(input); var (context, scope) = await Authorize(ur, true);
        await using var tx = await db.Database.BeginTransactionAsync(Ct);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection(); var pg = (NpgsqlTransaction)tx.GetDbTransaction();
        // Orden común: sesión → UR → formato. Adquirir, guardar y enviar se serializan en la UR,
        // nunca se mantiene una transacción abierta mientras la persona llena un campo.
        await store.LockUnit(ur, connection, pg, Ct);
        var form = await Load(scope.Unit, context.Profile); Editable(form, scope.Unit);
        FormStages.RequireWritable(form, input.Blocks.Select(b => b.Key));
        if (input.Removal is { } removal)
        {
            FormRemoval.Validate(removal);
            FormCapture.Require(context.Profile, [removal.Section + ":" + removal.Id]);
            var current = JsonNode.Parse(form.Content)!.AsObject();
            if (!FormRemoval.Exists(current, removal)) return await MissingRemoval(ur, removal);
            var related = FormRemoval.Changes(current, removal);
            // Puede reservar un subconjunto porque otros bloques ya tienen reserva propia.
            if (input.Blocks.Any(b => !related.ContainsKey(b.Key))) throw new DomainProblem(403, "La reserva solicitada no corresponde a esta eliminación.");
        }
        else FormCapture.Require(context.Profile, input.Blocks.Select(b => b.Key));
        var all = await db.FormBlocks.Where(b => b.UnitId == ur).ToDictionaryAsync(b => b.Key, Ct);
        var valuesAtReservation = FormBlocks.Split(JsonNode.Parse(form.Content)!.AsObject());
        foreach (var requested in input.Blocks)
        {
            var split = requested.Key.IndexOf(':');
            if (split > 0 && requested.Key[..split] is "identificacion" or "sistemas" or "datos" or "acuerdos"
                && all.TryGetValue(requested.Key, out var previous) && previous.Version > 0 && !valuesAtReservation.ContainsKey(requested.Key))
                return await MissingRemoval(ur, new(requested.Key[..split], requested.Key[(split + 1)..]));
        }
        var now = await Now(connection, pg);
        var result = new List<FormBlock>();
        // La UR ya está bloqueada: todos los observadores reciben la misma asignación de color.
        // La identidad efectiva comparte color entre pestañas, pero nunca comparte el testigo de escritura.
        var participant = ParticipantIdentity.Key(context.Profile.User.Email);
        var participantUser = await photos.VerifiedUser(context.Profile.User.Email, Ct);
        var ownColors = all.Values.Where(b => b.Participant == participant).OrderByDescending(b => b.ExpiresAt > now).ThenBy(b => b.Key).ToArray();
        var occupiedColors = all.Values.Where(b => b.ExpiresAt > now && b.LeaseId != null && b.Participant != participant).Select(b => b.Color).ToHashSet();
        var preferred = ownColors.FirstOrDefault()?.Color ?? Convert.ToInt32(participant[..4], 16) % 8;
        var color = ownColors.Any(b => b.ExpiresAt > now && b.LeaseId != null) || !occupiedColors.Contains(preferred)
            ? preferred : Enumerable.Range(0, 8).FirstOrDefault(c => !occupiedColors.Contains(c), preferred);
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
                throw Conflict($"{block.Holder ?? "Otra persona"} está editando este registro en otra sesión. Espera a que termine.");
            if (!active) block.LeaseId = Guid.NewGuid(); // Testigo nuevo: el titular vencido nunca recupera autoridad con el anterior.
            block.SessionHash = state.SessionHash; block.TabId = input.TabId; block.ContextRevision = Revision;
            block.Holder = context.Profile.User.Name; block.ExpiresAt = now.AddSeconds(45); result.Add(block);
            block.Participant = participant; block.Color = color; block.ParticipantUserId = participantUser;
        }
        await db.SaveChangesAsync(Ct); await tx.CommitAsync(Ct);
        // Una lectura simultánea puede empezar durante el commit y aún ver la reserva anterior.
        // Fechar la confirmación después del commit evita que ese mensaje tardío invalide la adquisición.
        // El vencimiento sigue siendo el original: confirmar no prolonga la reserva.
        // EF puede cerrar la conexión al confirmar la transacción; abrir/cerrar balancea su contador.
        DateTimeOffset confirmedAt;
        await db.Database.OpenConnectionAsync(Ct);
        try { confirmedAt = await Now(connection, null); }
        finally { await db.Database.CloseConnectionAsync(); }
        notifications.Changed(ur);
        // La reserva y sus respuestas/versiones se confirman bajo el mismo bloqueo de UR.
        // El navegador habilita el bloque sólo después de recibir este contenido vigente.
        var values = FormBlocks.Split(JsonNode.Parse(form.Content)!.AsObject());
        return new { bloques = result.Select(b => new { key = b.Key, version = b.Version,
            reserva = Public(b, input.TabId, confirmedAt).reserva, value = values.GetValueOrDefault(b.Key) }), servidorEn = confirmedAt };
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

    private async Task<EditRejection> MissingRemoval(string ur, RowRemoval removal)
    {
        var key = removal.Section + ":" + removal.Id;
        // Sólo un recibo confirmado con valor null acredita un retiro. Un ID desconocido no es éxito.
        // Se consulta después de autorizar y bajo el bloqueo de UR, nunca a partir de un 409 genérico.
        var confirmed = await db.Database.SqlQuery<bool>($"""
            SELECT EXISTS(SELECT 1 FROM formato_operaciones o,
                LATERAL jsonb_array_elements(coalesce(o.respuesta->'bloques','[]'::jsonb)) b
                WHERE o.id_ur={ur} AND b->>'key'={key} AND b->'value'='null'::jsonb) AS "Value"
            """).SingleAsync(Ct);
        return confirmed ? new(409, "registro_eliminado", "El registro ya fue eliminado. Se actualizará el formato; puedes continuar con los demás registros.")
            : new(404, "registro_desconocido", "No se encontró el registro solicitado. Revisa el contenido vigente.");
    }

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
        var requested = input.Blocks.ToDictionary(b => b.Key, b => b.Value, StringComparer.Ordinal);
        if (input.Removal is { } removal)
        {
            FormRemoval.Validate(removal);
            FormCapture.Require(context.Profile, [removal.Section + ":" + removal.Id]);
            if (!FormRemoval.Exists(old, removal)) return await MissingRemoval(ur, removal);
            var planned = FormRemoval.Changes(old, removal);
            // La cascada se determina en el servidor. Ni el cliente ni una reserva auxiliar pueden
            // aprovechar el retiro para cambiar otras respuestas, incluidas secciones no habilitadas.
            if (!planned.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(requested.Keys)
                || planned.Any(p => !JsonNode.DeepEquals(p.Value, requested[p.Key])))
                throw Conflict("El registro o sus relaciones cambiaron. Revisa la versión vigente antes de eliminar.");
            requested = planned;
            if (removal.Section == "identificacion" && removal.Id.StartsWith("ilda:", StringComparison.Ordinal))
                db.FormExclusions.Add(new() { UnitId = ur, Year = scope.Unit.Year, RowId = removal.Id,
                    CreatedAt = now, Actor = Actor, Effective = context.Profile.User.Email });
        }
        else
        {
            FormCapture.Require(context.Profile, requested.Keys);
            if (requested.Any(p => p.Value is null && p.Key.StartsWith("identificacion:", StringComparison.Ordinal)))
                throw new DomainProblem(422, "Confirma la eliminación del procedimiento y sus relaciones.");
        }
        foreach (var request in input.Blocks.Where(b => b.Value is not null && b.Key.StartsWith("identificacion:", StringComparison.Ordinal)))
        {
            var persisted = old["identificacion"]!.AsArray().FirstOrDefault(r => "identificacion:" + r?["id"] == request.Key);
            if (persisted is null && request.Key.StartsWith("identificacion:ilda:", StringComparison.Ordinal))
                throw FormSchema.FieldError(request.Key, "id", "Este registro de inventario no está disponible en el formato.");
            if ((request.Value!["codigo"]?.ToString() ?? "") != (persisted?["codigo"]?.ToString() ?? ""))
                throw FormSchema.FieldError(request.Key, "codigo", "El código del procedimiento se asigna al confirmar la validación en el servidor.");
            if ((request.Value["fuente"]?.ToString() ?? "") != (persisted is null ? "Nuevo" : persisted["fuente"]?.ToString() ?? ""))
                throw FormSchema.FieldError(request.Key, "fuente", "El origen del procedimiento se conserva en el servidor.");
        }
        foreach (var request in input.Blocks.Where(b => b.Value is not null && b.Key.StartsWith("sistemas:", StringComparison.Ordinal)))
        {
            var persisted = old["sistemas"]!.AsArray().FirstOrDefault(r => "sistemas:" + r?["id"] == request.Key);
            var code = request.Value!["proceso"]?.ToString();
            if (!string.IsNullOrEmpty(code) && persisted?["proceso"]?.ToString() != code
                && !ProcedureEligibility.Contains(old, code))
                throw FormSchema.FieldError(request.Key, "proceso", "Selecciona un procedimiento completo y válido, confirmado por el servidor como Vigente o Ajustar.");
        }
        var candidate = FormBlocks.Apply(old, requested);
        var keys = input.Blocks.Select(b => b.Key).ToHashSet(StringComparer.Ordinal);
        var generated = schema.CompleteProcesses(candidate, keys, all.Keys);
        // No tomar por implicación una reserva que otro editor obtuvo para una evaluación nueva.
        if (generated.Any(k => all.TryGetValue(k, out var existing) && existing.LeaseId is not null && existing.ExpiresAt > now))
            throw Conflict("Otra sesión está editando la evaluación de este proceso.");
        keys.UnionWith(generated);
        FormStages.RequireWritable(form, keys);
        var removedCodes = old["identificacion"]!.AsArray().Select(r => r!["codigo"]?.ToString() ?? "")
            .Except(candidate["identificacion"]!.AsArray().Select(r => r!["codigo"]?.ToString() ?? "")).ToHashSet(StringComparer.Ordinal);
        var validated = schema.Validate(candidate, scope.Unit, keys, removedCodes, old);
        foreach (var row in validated["sistemas"]!.AsArray().OfType<JsonObject>().Where(r => keys.Contains("sistemas:" + r["id"])))
        {
            var previous = old["sistemas"]!.AsArray().FirstOrDefault(r => r?["id"]?.ToString() == row["id"]?.ToString());
            var moduleId = row["moduloSiiId"]?.ToString() ?? "";
            // Las descripciones son instantáneas del servidor, jamás texto autorizado por el navegador.
            row["moduloSiiDescripcion"] = previous?["moduloSiiDescripcion"]?.DeepClone() ?? JsonValue.Create("");
            if (row["sistema"]?.ToString() == "sii_v2" && moduleId.Length > 0
                && (previous?["sistema"]?.ToString() != "sii_v2" || previous?["moduloSiiId"]?.ToString() != moduleId))
            {
                // El bloqueo comparte la transacción del formato: una baja no puede intercalarse antes del commit.
                if (await SiiCatalogSchema.Inspect(new(connection, pg), Ct) is not null)
                    throw FormSchema.FieldError("sistemas:" + row["id"], "moduloSiiId", "El catálogo local de módulos requiere revisión del administrador en Configuración → Sincronizaciones.");
                var module = (await db.SiiModules.FromSqlInterpolated($"SELECT * FROM public.sii_modulos WHERE id_modulo={moduleId} AND presente FOR SHARE").AsNoTracking().ToListAsync(Ct)).SingleOrDefault();
                if (module is null) throw FormSchema.FieldError("sistemas:" + row["id"], "moduloSiiId", "Selecciona un módulo disponible del catálogo local.");
                row["moduloSiiDescripcion"] = module.Description;
            }
            else if (moduleId.Length == 0) row["moduloSiiDescripcion"] = "";
        }
        var canonical = FormBlocks.Split(validated);
        // Normaliza sólo los bloques de esta operación. Jamás sustituye otros bloques con una copia del cliente.
        var changes = keys.ToDictionary(k => k, k => canonical.GetValueOrDefault(k), StringComparer.Ordinal);
        var content = FormBlocks.Apply(old, changes);
        form.Content = content.ToJsonString(); form.Year = scope.Unit.Year; form.Version++;
        form.Progress = checked((short)FormStages.Progress(form.ActiveStage, content)); form.UpdatedBy = context.Profile.User.Email;
        form.UpdatedAt = DateTime.SpecifyKind(now.UtcDateTime, DateTimeKind.Unspecified);
        foreach (var request in input.Blocks)
        {
            var block = all[request.Key]; block.Version++;
            if (input.Release || requested[request.Key] is null) { block.LeaseId = null; block.ExpiresAt = null; }
        }
        foreach (var key in generated)
        {
            if (!all.TryGetValue(key, out var block)) { block = new FormBlock { UnitId = ur, Key = key }; all[key] = block; db.FormBlocks.Add(block); }
            block.Version++;
        }
        var position = await db.FormPositions.SingleOrDefaultAsync(p => p.UnitId == ur && p.Actor == Actor && p.Effective == context.Profile.User.Email, Ct);
        if (position is null) { position = new() { UnitId = ur, Actor = Actor, Effective = context.Profile.User.Email }; db.FormPositions.Add(position); }
        position.Section = FormCapture.Section(context.Profile, input.Section);
        var response = new { version = form.Version, porcentaje = form.Progress, actualizadoEn = now, actualizadoPor = form.UpdatedBy,
            entrega = FormStages.Describe(FormStages.Status(form, scope.Unit.Year), content),
            porcentajeEtapa = FormStages.Progress(form.ActiveStage, content), revisionEtapa = FormStages.Definition(form.ActiveStage).Review(content),
            procedimientosDisponibles = ProcedureEligibility.Options(content),
            revisionEnvio = FormStages.Definition(form.ActiveStage).Review(content),
            bloques = keys.Select(key => new { key, version = all[key].Version, value = changes[key], reserva = Public(all[key], input.TabId, now).reserva }).ToArray() };
        Remember(ur, input.OperationId, input.TabId, fingerprint, response, now);
        await db.SaveChangesAsync(Ct);
        await audit.Write(connection, pg, "guardar_bloques", "formato_ur", ur, "confirmado", Ct, details: new { version = form.Version, bloques = keys, operacion = input.OperationId, retiro = input.Removal });
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
        FormStages.RequireEnabled(input.Stage);
        if (form.ActiveStage != input.Stage) throw Conflict("La etapa activa cambió. Revisa el estado antes de enviar.");
        Editable(form, scope.Unit);
        if (form.Version != input.Version) throw Conflict("Hay respuestas más recientes. Revísalas antes de confirmar el envío.");
        var now = await Now(connection, pg);
        var blocks = await db.FormBlocks.Where(b => b.UnitId == ur).ToListAsync(Ct);
        if (blocks.Any(b => b.LeaseId is not null && b.ExpiresAt > now && (b.SessionHash != state.SessionHash || b.TabId != input.TabId || b.ContextRevision != Revision)))
            throw Conflict("Otra sesión mantiene una reserva de edición. Espera a que termine antes de enviar.");
        var original = JsonNode.Parse(form.Content)!.AsObject();
        var definition = FormStages.Definition(input.Stage);
        var content = schema.Validate(original, scope.Unit,
            FormBlocks.Split(original).Keys.Where(definition.IncludesBlock).ToHashSet(StringComparer.Ordinal), previous: original);
        var review = definition.Review(content);
        if (!review.listo) throw new DomainProblem(422, "Completa los pendientes de Revisar y enviar antes de enviar.", new { revisionEnvio = review });
        form.Progress = checked((short)definition.Progress(content));
        var submission = new FormStageSubmission { UnitId = ur, Stage = input.Stage, Year = form.Year, Version = form.Version + 1,
            OperationId = input.OperationId, SubmittedAt = now, Actor = Actor, Effective = context.Profile.User.Email, Name = context.Profile.User.Name,
            Snapshot = JsonSerializer.Serialize(new { etapa = input.Stage, unidad = scope.Unit.Public(),
                contenido = FormStages.Capture(original, input.Stage), version = form.Version + 1 }) };
        form.StageSubmissions.Add(submission); form.Version++;
        foreach (var block in blocks) { block.LeaseId = null; block.ExpiresAt = null; }
        var response = new { version = form.Version, enviadoEn = now, enviadoPor = context.Profile.User.Email,
            entrega = FormStages.Describe(FormStages.Status(form, scope.Unit.Year), original) };
        Remember(ur, input.OperationId, input.TabId, fingerprint, response, now);
        await db.SaveChangesAsync(Ct);
        await audit.Write(connection, pg, "enviar", "formato_ur", ur, "confirmado", Ct, details: new { etapa = input.Stage, version = form.Version, ejercicio = form.Year, operacion = input.OperationId });
        await tx.CommitAsync(Ct); notifications.Changed(ur); return response;
    }
}
