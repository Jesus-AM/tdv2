using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
namespace Tdv2.Synchronization;

public sealed record SyncClaim(Guid Id, Guid Owner, string Sources, string Actor);
public sealed class SyncCoordinator(DatabaseConnections connections, IOptions<SyncOptions> options, AccessState access,
    ICatalogSource source, CatalogPublication publication, ILogger<SyncCoordinator> logger)
{
    private async Task<T> Transaction<T>(Func<SyncSql, Task<T>> work, CancellationToken ct)
    {
        await using var connection = await connections.Open("Tdv2", ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var result = await work(new(connection, transaction)); await transaction.CommitAsync(ct); return result;
    }
    public Task Configure(SyncSchedule schedule, string actor, bool fromHttp, CancellationToken ct) => Transaction(async db =>
    {
        var current = await db.Settings(ct);
        if (fromHttp) await access.Guard(db.Connection, db.Transaction!, ct);
        if (SyncSql.Number(current, "version") != schedule.Version) throw new DomainProblem(409, "La programación cambió. Actualiza la página antes de guardar.");
        var next = schedule.Next(await db.Now(ct));
        if (schedule.Active && schedule.IncludeIlda && !options.Value.IldaEnabled) throw InvalidIlda("incluir_ilda");
        await db.Execute("UPDATE sincronizacion_configuracion SET activa=$1,intervalo_minutos=$2,hora=$3,zona_horaria=$4,incluir_ilda=$5,version=version+1,proxima_en=$6,actualizado_por=$7,updated_at=timezone('UTC',clock_timestamp()) WHERE id=1", ct,
            schedule.Active, schedule.Minutes, schedule.Time, schedule.Zone, schedule.IncludeIlda, schedule.Active ? DateTime.SpecifyKind(next.UtcDateTime, DateTimeKind.Unspecified) : null, actor);
        await db.Audit(actor, "configurar", null, new() { ["resultado"] = "confirmado", ["antes"] = SyncSql.PublicSettings(current), ["despues"] = SyncSql.PublicSettings(await db.Settings(ct)) }, ct);
        return true;
    }, ct);
    private static DomainProblem InvalidIlda(string field) => new(422, "Configura y habilita la conexión ILDA en el servidor.", new { errors = new Dictionary<string, string> { [field] = "Configura y habilita la conexión ILDA en el servidor." } });
    public Task<Guid> Enqueue(string sources, string origin, string actor, bool fromHttp, CancellationToken ct)
    {
        if (sources is not ("sii" or "ilda" or "ambas")) throw new DomainProblem(422, "Selecciona una fuente válida.");
        if (sources != "sii" && !options.Value.IldaEnabled) throw InvalidIlda("fuentes");
        return Transaction(async db =>
        {
            var settings = await db.Settings(ct);
            if (fromHttp) await access.Guard(db.Connection, db.Transaction!, ct);
            if (settings["ejecucion_activa"] is not null) throw new DomainProblem(409, "Ya hay una sincronización pendiente o en ejecución. Se mostrará su estado.", new { id = settings["ejecucion_activa"]!.ToString() });
            return await Insert(db, sources, origin, actor, ct);
        }, ct);
    }
    private static async Task<Guid> Insert(SyncSql db, string sources, string origin, string actor, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await db.Execute("INSERT INTO sincronizacion_ejecuciones(id,fuentes,origen,solicitado_por,estado,resultado,solicitada_en) VALUES($1,$2,$3,$4,'pendiente','{}',timezone('UTC',clock_timestamp()))", ct, id, sources, origin, actor);
        await db.Execute("UPDATE sincronizacion_configuracion SET ejecucion_activa=$1,propietario=null,reserva_hasta=null WHERE id=1", ct, id);
        await db.Audit(actor, "solicitar", id, new() { ["fuentes"] = sources, ["origen"] = origin, ["resultado"] = "pendiente" }, ct); return id;
    }
    public async Task Heartbeat(CancellationToken ct)
    {
        await using var connection = await connections.Open("Tdv2", ct);
        // Un latido nunca renueva la reserva: un proceso bloqueado u obsoleto no recupera propiedad.
        await new SyncSql(connection, null).Execute("UPDATE sincronizacion_configuracion SET procesador_visto_en=timezone('UTC',clock_timestamp()) WHERE id=1", ct);
    }
    // La recuperación persistente no depende de que exista una solicitud HTTP esperando.
    public async Task<JsonObject?> Tick(CancellationToken ct, bool manualOnly = false)
    {
        await Transaction(async db =>
        {
            var settings = await db.Settings(ct); var now = await db.Now(ct);
            // El servicio web no anuncia que exista un procesador de horarios.
            if (!manualOnly) await db.Execute("UPDATE sincronizacion_configuracion SET procesador_visto_en=timezone('UTC',clock_timestamp()) WHERE id=1", ct);
            if (settings["ejecucion_activa"] is not null && settings["propietario"] is not null && SyncSql.Date(settings["reserva_hasta"]) <= now)
            {
                var id = Guid.Parse(settings["ejecucion_activa"]!.ToString());
                var run = await db.Object("SELECT to_jsonb(r)::text FROM sincronizacion_ejecuciones r WHERE id=$1", ct, id);
                var result = run["resultado"]!.AsObject(); result["interrupcion"] = new JsonObject { ["estado"] = "fallida", ["mensaje"] = "La ejecución se interrumpió o agotó su tiempo. Puedes volver a sincronizar." };
                var state = result.Any(p => p.Value?["estado"]?.ToString() == "completada") ? "parcial" : "fallida";
                await db.Execute("UPDATE sincronizacion_ejecuciones SET estado=$2,resultado=$3,terminada_en=timezone('UTC',clock_timestamp()),etapa=null WHERE id=$1", ct, id, state, result);
                await Release(db, ct); await db.Audit("sistema", "interrumpir", id, new() { ["estado"] = state, ["resultado"] = state }, ct); settings["ejecucion_activa"] = null;
            }
            if (!manualOnly && SyncSql.Flag(settings, "activa") && settings["ejecucion_activa"] is null && SyncSql.Date(settings["proxima_en"]) <= now)
            {
                await Insert(db, SyncSql.Flag(settings, "incluir_ilda") ? "ambas" : "sii", "programada", "sistema", ct);
                var next = SyncSql.Schedule(settings).Next(now);
                await db.Execute("UPDATE sincronizacion_configuracion SET proxima_en=$1 WHERE id=1", ct, DateTime.SpecifyKind(next.UtcDateTime, DateTimeKind.Unspecified));
            }
            return true;
        }, ct);
        return await Process(ct, manualOnly);
    }
    public Task<SyncClaim?> Claim(CancellationToken ct, bool manualOnly = false) => Transaction<SyncClaim?>(async db =>
    {
        var settings = await db.Settings(ct);
        if (settings["ejecucion_activa"] is null || settings["propietario"] is not null) return null;
        var id = Guid.Parse(settings["ejecucion_activa"]!.ToString());
        var run = await db.Object("SELECT to_jsonb(r)::text FROM sincronizacion_ejecuciones r WHERE id=$1", ct, id);
        if (SyncSql.Text(run, "estado") != "pendiente") return null;
        if (manualOnly && SyncSql.Text(run, "origen") != "manual") return null;
        var owner = Guid.NewGuid();
        await db.Execute("UPDATE sincronizacion_configuracion SET propietario=$1,reserva_hasta=timezone('UTC',clock_timestamp())+interval '30 minutes' WHERE id=1", ct, owner);
        await db.Execute("UPDATE sincronizacion_ejecuciones SET estado='ejecutando',iniciada_en=timezone('UTC',clock_timestamp()) WHERE id=$1", ct, id);
        return new(id, owner, SyncSql.Text(run, "fuentes")!, SyncSql.Text(run, "solicitado_por")!);
    }, ct);
    private static Task Release(SyncSql db, CancellationToken ct) => db.Execute("UPDATE sincronizacion_configuracion SET ejecucion_activa=null,propietario=null,reserva_hasta=null WHERE id=1", ct);
    private static async Task Guard(SyncSql db, SyncClaim claim, CancellationToken ct)
    {
        var matches = await db.Scalar("SELECT 1 FROM sincronizacion_configuracion WHERE id=1 AND ejecucion_activa=$1 AND propietario=$2 AND reserva_hasta>timezone('UTC',clock_timestamp()) FOR UPDATE", ct, claim.Id, claim.Owner);
        if (matches is null) throw new SyncLeaseLost();
    }
    private Task Owned(SyncClaim claim, Func<SyncSql, Task> work, CancellationToken ct, bool release = false) => Transaction(async db =>
    {
        await Guard(db, claim, ct); await work(db);
        // La transacción puede haber esperado un bloqueo de catálogo. Un ejecutor vencido debe
        // fallar también antes del commit; comprobar sólo al comenzar permitiría publicar fuera de plazo.
        await Guard(db, claim, ct);
        if (release) await Release(db, ct);
        else await db.Execute("UPDATE sincronizacion_configuracion SET reserva_hasta=timezone('UTC',clock_timestamp())+interval '30 minutes' WHERE id=1", ct);
        return true;
    }, ct);
    private static async Task Result(SyncSql db, SyncClaim claim, string name, JsonObject entry, CancellationToken ct)
    {
        var run = await db.Object("SELECT to_jsonb(r)::text FROM sincronizacion_ejecuciones r WHERE id=$1", ct, claim.Id);
        var result = run["resultado"]!.AsObject(); result[name] = entry;
        await db.Execute("UPDATE sincronizacion_ejecuciones SET resultado=$2 WHERE id=$1", ct, claim.Id, result);
    }
    public async Task<JsonObject?> Process(CancellationToken ct, bool manualOnly = false)
    {
        var claim = await Claim(ct, manualOnly); return claim is null ? null : await Execute(claim, ct);
    }
    public async Task<JsonObject> Execute(SyncClaim claim, CancellationToken ct)
    {
        var names = Catalogs(claim.Sources);
                  try
        {
            foreach (var name in names)
            {
                await Owned(claim, db => db.Execute("UPDATE sincronizacion_ejecuciones SET etapa=$2 WHERE id=$1", ct, claim.Id, name), ct);
                try
                {
                    if (name == "sii_modulos") await Owned(claim, db => SiiCatalogSchema.Require(db, ct), ct);
                    var snapshot = publication.Validate(name, await source.Read(name, ct));
                    await Owned(claim, async db =>
                    {
                        await publication.Apply(db, snapshot, ct);
                        await Result(db, claim, name, new() { ["estado"] = "completada", ["resumen"] = snapshot.Summary.DeepClone(), ["completada_en"] = (await db.Now(ct)).ToString("O") }, ct);
                        await db.Audit(claim.Actor, "sincronizar", claim.Id, new() { ["fuente"] = name, ["resumen"] = snapshot.Summary.DeepClone(), ["resultado"] = "completada" }, ct);
                    }, ct);
                }
                catch (SyncLeaseLost) { throw; }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception error)
                {
                    logger.LogWarning("Sincronización fallida. Ejecución {Run}, fuente {Source}, tipo {Type}", claim.Id, name, error.GetType().Name);
                    var publicName = name == "sii_modulos" ? "el catálogo de módulos de SIIv2" : name.ToUpperInvariant();
                    var message = error is SyncProblem ? error.Message : "No se pudo sincronizar " + publicName + ". Revisa conexión, permisos y validez del origen. Se conservó la copia anterior.";
                    await Owned(claim, async db =>
                    {
                        await Result(db, claim, name, new() { ["estado"] = "fallida", ["mensaje"] = message }, ct);
                        await db.Audit(claim.Actor, "fallar", claim.Id, new() { ["fuente"] = name, ["tipo"] = error.GetType().Name, ["resultado"] = "fallida" }, ct);
                    }, ct);
                }
            }
            await Owned(claim, async db =>
            {
                var run = await db.Object("SELECT to_jsonb(r)::text FROM sincronizacion_ejecuciones r WHERE id=$1", ct, claim.Id);
                var successes = names.Count(n => run["resultado"]?[n]?["estado"]?.ToString() == "completada");
                var state = successes == names.Length ? "completada" : successes > 0 ? "parcial" : "fallida";
                await db.Execute("UPDATE sincronizacion_ejecuciones SET estado=$2,etapa=null,terminada_en=timezone('UTC',clock_timestamp()) WHERE id=$1", ct, claim.Id, state);
            }, ct, true);
        }
        catch (SyncLeaseLost) { /* Nunca publicar, terminar ni liberar una reserva ajena o vencida. */ }
        await using var connection = await connections.Open("Tdv2", ct);
        return await new SyncSql(connection, null).Object("SELECT to_jsonb(r)::text FROM sincronizacion_ejecuciones r WHERE id=$1", ct, claim.Id);
    }
    public async Task<JsonObject> Check(string name, CancellationToken ct)
    {
        await using var connection = await connections.Open("Tdv2", ct);
        if (name == "sii_modulos") await SiiCatalogSchema.Require(new(connection, null), ct);
        var snapshot = publication.Validate(name, await source.Read(name, ct));
        await publication.CheckReduction(new(connection, null), snapshot, ct);
        var summary = snapshot.Summary.DeepClone().AsObject(); summary["comprobacion"] = true; return summary;
    }
    public static string[] Catalogs(string sources) => sources switch
    {
        "sii" => ["sii", "sii_modulos"], "ambas" => ["sii", "sii_modulos", "ilda"], "ilda" => ["ilda"],
        _ => throw new SyncProblem("Fuente no válida.")
    };
}
