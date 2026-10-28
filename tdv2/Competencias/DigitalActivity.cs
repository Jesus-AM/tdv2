using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Integrations.Nexo;
using Tdv2.Security;

namespace Tdv2.Competencias;

// Cola y recibos locales: nunca son por sí mismos evidencia de recepción central.
public sealed class DigitalActivityEvent
{
    public Guid Id { get; set; }
    public long ApplicationId { get; set; }
    public string Key { get; set; } = "";
    public string Session { get; set; } = "";
    public string Payload { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LocalRecordedAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public bool DuplicateReceipt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public string? Failure { get; set; }
}
public sealed record DigitalActivityReceipt(Guid Event, bool Duplicate);
public interface IDigitalActivityTransport { Task<DigitalActivityReceipt> Send(long application, string payload, CancellationToken ct); }
public sealed class DigitalActivityTransport(DatabaseConnections connections, IConfiguration config, ILogger<PostgresNexoProfiles> logger) : IDigitalActivityTransport
{
    public async Task<DigitalActivityReceipt> Send(long application, string payload, CancellationToken ct)
    {
        if (!long.TryParse(config["CompetenciasDigitales:Nexo:ApplicationId"], out var configured) || application != configured || application < 1)
            throw new InvalidOperationException("Aplicación de actividad no coincide con la configuración.");
        var profiles = new PostgresNexoProfiles(connections, logger, "CompetenciasDigitalesNexo", configured);
        if (await profiles.ApplicationId(ct) != application) throw new InvalidOperationException("Publicación de aplicación no coincide.");
        await using var connection = await connections.Open("CompetenciasDigitalesNexo", ct);
        // Contrato real: PostgresPublication::object + Postgres/registrar_actividad.sql (Nexo).
        await using var command = new NpgsqlCommand("SELECT evento_uuid,duplicado FROM public.nexo_a" + application.ToString(CultureInfo.InvariantCulture) + "_registrar_actividad($1)", connection);
        command.Parameters.AddWithValue(NpgsqlDbType.Jsonb, payload);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) throw new InvalidOperationException("Nexo no confirmó el evento.");
        var receipt = new DigitalActivityReceipt(reader.GetGuid(0), reader.GetBoolean(1));
        if (receipt.Event != Guid.Parse(JsonNode.Parse(payload)!["evento_uuid"]!.ToString()) || await reader.ReadAsync(ct)) throw new InvalidOperationException("Recibo de actividad inválido.");
        return receipt;
    }
}
public sealed class DigitalActivity(DigitalDbContext db, IConfiguration config, AccessState state, IHttpContextAccessor accessor, ILogger<DigitalActivity> logger)
{
    public static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public string Session => Digest("cd-session:" + (state.SessionHash ?? throw new InvalidOperationException("Sesión requerida.")));
    public static JsonObject Payload(Guid id, DateTimeOffset time, string actor, string name, string category, string action, string entity, string resource, string? affected = null) => new()
    {
        ["evento_uuid"] = id.ToString(), ["ocurrido_en"] = time.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
        ["actor_email"] = actor, ["actor_nombre"] = name, ["categoria"] = category, ["accion"] = action, ["resultado"] = "exitoso",
        ["entidad_tipo"] = entity, ["entidad_id"] = resource, ["usuario_afectado_email"] = affected
    };
    // Requiere transacción del llamador; la escritura y el evento se confirman juntos.
    public async Task Queue(DigitalScope scope, string key, string category, string action, string entity, string resource, CancellationToken ct)
    {
        var actor = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? throw new InvalidOperationException("Actor validado requerido.");
        var name = accessor.HttpContext?.User.Identity?.Name ?? actor;
        await QueuePayload(key, Session, Payload(Guid.NewGuid(), DateTimeOffset.UtcNow, actor, name, category, action, entity, resource,
            state.Loaded?.Selection is { Kind: "representation" } rep ? rep.Email : actor != scope.Profile.User.Email ? scope.Profile.User.Email : null), ct);
    }
    private async Task QueuePayload(string key, string session, JsonObject payload, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("La actividad requiere transacción.");
        if (!long.TryParse(config["CompetenciasDigitales:Nexo:ApplicationId"], out var app) || app < 1) throw new InvalidOperationException("Falta la aplicación de Competencias.");
        var id = Guid.Parse(payload["evento_uuid"]!.ToString()); var now = DateTimeOffset.UtcNow; var data = payload.ToJsonString(); var hash = Digest(app + ":" + key);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO public.cd_activity_events ("Id","ApplicationId","Key","Session","Payload","CreatedAt","NextAttemptAt","Attempts","DuplicateReceipt")
            VALUES ({id},{app},{hash},{session},{data}::jsonb,{now},{now},0,false) ON CONFLICT ("Key") DO NOTHING
            """, ct);
    }
    public async Task Visit(DigitalScope scope, bool report, int version, Guid generation, CancellationToken ct)
    {
        // Sólo navegación explícita; ni lecturas/autoguardados de fondo ni cada render.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var context = state.Loaded?.Selection?.Id ?? "own";
        await Queue(scope, "access:" + Session, "competencias", "acceder", "aplicacion", "competencias-digitales", ct);
        if (report) await Queue(scope, $"report:{Session}:{context}:{generation}:{version}", "informe", context == "own" ? "consultar" : "consultar_representado", "informe", version.ToString(CultureInfo.InvariantCulture), ct);
        await tx.CommitAsync(ct);
    }
    public async Task Representation(AccessSelection selection, bool end, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var actor = selection.ActorEmail;
        var payload = Payload(Guid.NewGuid(), DateTimeOffset.UtcNow, actor, accessor.HttpContext?.User.Identity?.Name ?? actor,
            "representacion", end ? "finalizar" : "iniciar", "representacion", selection.Id, selection.Email);
        await QueuePayload($"representation:{selection.Id}:{(end ? "end" : "start")}", Session, payload, ct);
        await tx.CommitAsync(ct);
    }
    public async Task Logout(CancellationToken ct)
    {
        var session = Session;
        // Sólo sesiones que efectivamente ingresaron al subsitio; no se concede acceso por cerrar sesión.
        var first = await db.ActivityEvents.AsNoTracking().Where(e => e.Session == session).OrderBy(e => e.CreatedAt).FirstOrDefaultAsync(ct);
        if (first is null) return;
        var source = JsonNode.Parse(first.Payload)!;
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (state.Loaded?.Selection is { Kind: "representation", Application: "competencias-digitales" } selection)
            await QueuePayload($"representation:{selection.Id}:end", session, Payload(Guid.NewGuid(), DateTimeOffset.UtcNow,
                selection.ActorEmail, source["actor_nombre"]!.ToString(), "representacion", "finalizar", "representacion", selection.Id, selection.Email), ct);
        await QueuePayload("logout:" + session, session, Payload(Guid.NewGuid(), DateTimeOffset.UtcNow, source["actor_email"]!.ToString(), source["actor_nombre"]!.ToString(), "autenticacion", "cierre_sesion", "aplicacion", "competencias-digitales"), ct);
        await tx.CommitAsync(ct);
    }
    public async Task BestEffort(Func<Task> work)
    {
        try { await work(); }
        catch (Exception e) { logger.LogWarning("Actividad de Competencias pendiente de registro local. Tipo: {Type}", e.GetType().Name); }
    }
}

public sealed class DigitalActivityDelivery(DigitalDbContext db, DatabaseConnections connections, IDigitalActivityTransport transport, ILogger<DigitalActivityDelivery> logger)
{
    public Task<int> Pending(CancellationToken ct) => db.ActivityEvents.CountAsync(e => e.ReceivedAt == null, ct);
    public async Task<int> Flush(int limit, CancellationToken ct)
    {
        var sent = 0;
        for (var i = 0; i < Math.Clamp(limit, 1, 100); i++)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var row = await db.ActivityEvents.FromSqlRaw("SELECT * FROM public.cd_activity_events WHERE \"ReceivedAt\" IS NULL AND \"NextAttemptAt\" <= now() ORDER BY \"CreatedAt\" LIMIT 1 FOR UPDATE SKIP LOCKED").SingleOrDefaultAsync(ct);
            if (row is null) break;
            row.Attempts++;
            try
            {
                if (row.LocalRecordedAt is null) { await Mirror(row, ct); row.LocalRecordedAt = DateTimeOffset.UtcNow; }
                using var sendDeadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
                sendDeadline.CancelAfter(TimeSpan.FromSeconds(2));
                var receipt = await transport.Send(row.ApplicationId, row.Payload, sendDeadline.Token);
                if (receipt.Event != row.Id) throw new InvalidOperationException("Recibo no corresponde al evento.");
                row.ReceivedAt = DateTimeOffset.UtcNow; row.DuplicateReceipt = receipt.Duplicate; row.Failure = null; sent++;
            }
            catch (Exception e) when (!ct.IsCancellationRequested)
            {
                row.Failure = e is PostgresException { SqlState: "P0001" } ? "nexo_rechazado" : "entrega_no_confirmada";
                row.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Min(3600, 10 * Math.Pow(2, Math.Min(row.Attempts, 8))));
                logger.LogWarning("Actividad de Competencias sin recepción confirmada. Evento {Event}, estado {State}", row.Id, row.Failure);
            }
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); db.Entry(row).State = EntityState.Detached;
        }
        return sent;
    }
    public async Task TryFlush()
    {
        // La captura ya está confirmada. El fallo o cancelación de entrega nunca la revierte ni muestra fallo de guardado.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        try { await Flush(2, deadline.Token); }
        catch (Exception e) { logger.LogWarning("Entrega de actividad aplazada; la cola conserva los eventos. Tipo: {Type}", e.GetType().Name); }
    }
    private async Task Mirror(DigitalActivityEvent row, CancellationToken ct)
    {
        var data = JsonNode.Parse(row.Payload)!;
        var local = data.DeepClone().AsObject(); local["aplicacion_id"] = row.ApplicationId;
        await using var connection = await connections.Open("Tdv2", ct); await using var tx = await connection.BeginTransactionAsync(ct);
        await using (var guard = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended($1,28102026))", connection, tx))
        { guard.Parameters.AddWithValue(row.Id.ToString()); await guard.ExecuteNonQueryAsync(ct); }
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO public.activity_logs(user_email,user_name,entity,action,meta,created_at,updated_at)
            SELECT $1,$2,'competencias_digitales',$3,$4::json,timezone('UTC',now()),timezone('UTC',now())
            WHERE NOT EXISTS(SELECT 1 FROM public.activity_logs WHERE entity='competencias_digitales' AND meta->>'evento_uuid'=$5)
            """, connection, tx);
        cmd.Parameters.AddWithValue(data["actor_email"]!.ToString()); cmd.Parameters.AddWithValue(data["actor_nombre"]!.ToString());
        cmd.Parameters.AddWithValue(data["categoria"] + ":" + data["accion"]); cmd.Parameters.AddWithValue(local.ToJsonString()); cmd.Parameters.AddWithValue(row.Id.ToString());
        await cmd.ExecuteNonQueryAsync(ct); await tx.CommitAsync(ct);
    }
}

public static class DigitalActivityHooks
{
    public static async Task Logout(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        if (string.IsNullOrWhiteSpace(config.GetConnectionString("CompetenciasDigitales"))) return;
        try
        {
            var activity = services.GetRequiredService<DigitalActivity>();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            await activity.Logout(deadline.Token);
            await services.GetRequiredService<DigitalActivityDelivery>().TryFlush();
        }
        catch (Exception e)
        {
            services.GetRequiredService<ILogger<DigitalActivity>>().LogWarning("Cierre local completado; registro de actividad de Competencias no confirmado. Tipo: {Type}", e.GetType().Name);
        }
    }
}
