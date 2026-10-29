using System.Globalization;
using System.Net.Mail;
using System.Text.Json.Nodes;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Infrastructure;
namespace Tdv2.Integrations.Nexo;

// Las vistas compartidas están filtradas por la cuenta PostgreSQL publicada por Nexo.
// Ningún ID de aplicación, rol, módulo o UR enviado por el navegador concede permisos.
public sealed class PostgresNexoProfiles : INexoProfiles
{
    private readonly DatabaseConnections connections;
    private readonly ILogger<PostgresNexoProfiles> logger;
    private readonly long? expectedApplication;
    public string ConnectionName { get; }
    public PostgresNexoProfiles(DatabaseConnections connections, ILogger<PostgresNexoProfiles> logger)
        : this(connections, logger, "Nexo", null) { }
    public PostgresNexoProfiles(DatabaseConnections connections, ILogger<PostgresNexoProfiles> logger, string connectionName, long? applicationId)
    { this.connections = connections; this.logger = logger; ConnectionName = connectionName; expectedApplication = applicationId; }
    private readonly Dictionary<string, Profile> profiles = new(StringComparer.Ordinal);
    private long? application;
    private static DomainProblem Denied() => new(403, Tdv2.Web.ApplicationEntry.NoApplications);
    private static DomainProblem Unavailable() => new(503, Tdv2.Web.ApplicationEntry.Unavailable);
    private static string? Text(JsonObject row, string key) => row[key] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
    private static long Positive(JsonNode? node) => long.TryParse(node?.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0 ? id : throw Unavailable();
    public static string Email(string email)
    {
        email = email.Trim().ToLowerInvariant();
        if (email.Length > 254 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || !email.EndsWith("@uacj.mx", StringComparison.Ordinal)) throw Denied();
        return email;
    }
    public async Task<long> ApplicationId(CancellationToken cancellation)
    {
        if (application is { } id) return id;
        if (ConnectionName != "Nexo" && expectedApplication is not > 0) throw Unavailable();
        await using var connection = await connections.Open(ConnectionName, cancellation);
        await using var command = new NpgsqlCommand("SELECT to_jsonb(a)::text FROM public.nexo_aplicacion a LIMIT 2", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (!await reader.ReadAsync(cancellation)) throw Denied();
        var row = JsonNode.Parse(reader.GetString(0))!.AsObject();
        if (await reader.ReadAsync(cancellation)) throw Unavailable();
        if (ConnectionName == "Nexo"
            ? Text(row, "clave") != "tdv2" || row["nivel_control"]?.ToString() != "2"
            : Positive(row["id"]) != expectedApplication || Text(row, "clave") == "tdv2") throw Unavailable();
        return (application = Positive(row["id"])).Value;
    }
    public Task<Profile> ForEmail(string email, CancellationToken cancellation) => LoadProfile(Email(email), null, cancellation);
    public Task<Profile> ForUser(long userId, CancellationToken cancellation) => userId > 0 ? LoadProfile(null, userId, cancellation) : throw Denied();
    private async Task<Profile> LoadProfile(string? email, long? userId, CancellationToken cancellation)
    {
        var key = userId is null ? "email:" + email : "id:" + userId;
        if (profiles.TryGetValue(key, out var existing)) return existing;
        try
        {
            var app = await ApplicationId(cancellation);
            await using var connection = await connections.Open(ConnectionName, cancellation);
            JsonObject row;
            await using (var command = new NpgsqlCommand("SELECT to_jsonb(u)::text FROM public.nexo_usuarios u WHERE " + (userId is null ? "email" : "usuario_id") + " = $1 LIMIT 2", connection))
            {
                command.Parameters.AddWithValue(userId is null ? (object)email! : userId.Value);
                await using var reader = await command.ExecuteReaderAsync(cancellation);
                if (!await reader.ReadAsync(cancellation)) throw Denied();
                row = JsonNode.Parse(reader.GetString(0))!.AsObject();
                if (await reader.ReadAsync(cancellation)) throw Unavailable();
            }
            var canonicalId = Positive(row["usuario_id"]);
            var canonicalEmail = Email(Text(row, "email") ?? "");
            if (userId is not null && canonicalId != userId || email is not null && canonicalEmail != email || Text(row, "tipo_cuenta") != "individual") throw Denied();
            email = canonicalEmail;
            foreach (var field in new[] { "num_empleado", "ID_UR", "origen_ur" }) if (!row.ContainsKey(field)) throw Unavailable();
            var membership = Positive(row["usuario_aplicacion_id"]);
            var roles = new List<Role>();
            await using (var command = new NpgsqlCommand("SELECT rol_id::bigint,rol_clave,rol_nombre FROM public.nexo_usuario_rol WHERE usuario_aplicacion_id = $1 AND email = $2", connection))
            {
                command.Parameters.AddWithValue(membership); command.Parameters.AddWithValue(email);
                await using var reader = await command.ExecuteReaderAsync(cancellation);
                while (await reader.ReadAsync(cancellation)) roles.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
            }
            if (roles.Count == 0) throw Denied();
            var modules = new List<Module>();
            await using (var command = new NpgsqlCommand("""
                SELECT DISTINCT m.id::bigint,m.clave,m.nombre,m.ruta,m.modulo_padre_id::bigint,m.icono,m.orden
                FROM public.nexo_modulos m JOIN public.nexo_modulo_rol mr ON mr.modulo_id = m.id
                WHERE mr.rol_id = ANY($1) AND m.aplicacion_id = $2 ORDER BY m.orden,m.id::bigint
                """, connection))
            {
                command.Parameters.AddWithValue(roles.Select(r => r.Id).ToArray()); command.Parameters.AddWithValue(app);
                await using var reader = await command.ExecuteReaderAsync(cancellation);
                while (await reader.ReadAsync(cancellation)) modules.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? "" : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetInt64(4), reader.IsDBNull(5) ? "mdi-view-grid-outline" : reader.GetString(5)));
            }
            var profile = new Profile(new(email, Text(row, "nombre") ?? email, Text(row, "num_empleado"), Text(row, "ID_UR"), Text(row, "origen_ur") ?? "sin_ur"), roles, modules, canonicalId);
            profiles["email:" + email] = profile; profiles["id:" + canonicalId] = profile;
            return profile;
        }
        catch (DomainProblem) { throw; }
        catch (Exception error) when (error is not OperationCanceledException)
        { logger.LogWarning("No se pudo consultar Nexo. Tipo: {Type}", error.GetType().Name); throw Unavailable(); }
    }
    // Resolución de identidad antes de consultar permisos. Todas las coincidencias;
    // nunca elegir una dirección según los roles que tenga asignados.
    public async Task<IReadOnlyList<NexoIdentity>> IdentitiesForAddresses(string[] addresses, CancellationToken cancellation)
    {
        try
        {
            await ApplicationId(cancellation);
            await using var connection = await connections.Open(ConnectionName, cancellation);
            await using var command = new NpgsqlCommand("SELECT DISTINCT usuario_id::bigint,email,nombre,tipo_cuenta FROM public.nexo_usuarios WHERE email = ANY($1)", connection);
            command.Parameters.AddWithValue(addresses.Select(Email).Distinct().ToArray());
            await using var reader = await command.ExecuteReaderAsync(cancellation); var identities = new List<NexoIdentity>();
            while (await reader.ReadAsync(cancellation))
            {
                var id = reader.GetInt64(0); if (id < 1) throw Unavailable();
                var email = Email(reader.GetString(1));
                identities.Add(new(id, email, reader.IsDBNull(2) ? email : reader.GetString(2), reader.IsDBNull(3) ? "" : reader.GetString(3)));
            }
            return identities;
        }
        catch (DomainProblem) { throw; }
        catch (Exception error) when (error is not OperationCanceledException)
        { logger.LogWarning("No se pudo comprobar la correspondencia institucional. Tipo: {Type}", error.GetType().Name); throw Unavailable(); }
    }
    public async Task<IReadOnlyList<Grant>> Grants(string email, CancellationToken cancellation)
    {
        try
        {
            await ApplicationId(cancellation);
            await using var connection = await connections.Open(ConnectionName, cancellation);
            // La publicación distingue central/aplicacion. No inferir origen a partir de usuario_rol.
            // Las concesiones históricas de entrada sin rol no habilitan un alcance de colaboración.
            await using var command = new NpgsqlCommand("SELECT concesion_id::bigint,rol_id::bigint,id_ur_acceso,origen FROM public.nexo_concesiones WHERE email = $1 AND rol_id IS NOT NULL", connection);
            command.Parameters.AddWithValue(Email(email));
            await using var reader = await command.ExecuteReaderAsync(cancellation);
            var result = new List<Grant>();
            while (await reader.ReadAsync(cancellation)) result.Add(new(reader.GetInt64(0), reader.GetInt64(1),
                reader.IsDBNull(2) ? "" : reader.GetString(2), reader.IsDBNull(3) ? "" : reader.GetString(3)));
            return result;
        }
        catch (DomainProblem) { throw; }
        catch (Exception error) when (error is not OperationCanceledException)
        { logger.LogWarning("No se pudieron consultar concesiones Nexo. Tipo: {Type}", error.GetType().Name); throw Unavailable(); }
    }
}
