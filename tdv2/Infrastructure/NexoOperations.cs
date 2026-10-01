using System.Data;
using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;
using Tdv2.Domain;
namespace Tdv2.Infrastructure;

public sealed class NexoOperations(DatabaseConnections connections, PostgresNexoProfiles profiles, ILogger<NexoOperations> logger)
{
    public static string? Text(JsonObject row, string key) => row[key] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;
    public static long Number(JsonObject row, string key) => long.TryParse(row[key]?.ToString(), out var n) ? n : 0;
    public static bool Flag(JsonObject row, string key) => row[key] is JsonValue v && v.TryGetValue<bool>(out var b) && b;
    private static DomainProblem Unavailable() => new(503, "No se pudo comprobar la operación con Nexo. Inténtalo más tarde.");
    public async Task<JsonArray> Rows(string resource, string? email, CancellationToken ct)
    {
        if (resource is not ("delegacion" or "delegacion_roles" or "usuarios")) throw new ArgumentException("Unknown Nexo view.");
        return await Guard(async () =>
        {
            await profiles.ApplicationId(ct);
            await using var connection = await connections.Open("Nexo", ct);
            await using var command = new NpgsqlCommand("SELECT to_jsonb(r)::text FROM public.nexo_" + resource + " r" + (email is null ? "" : " WHERE email=$1"), connection);
            if (email is not null) command.Parameters.AddWithValue(email);
            return await ReadRows(command, ct);
        });
    }
    public async Task<JsonArray> Delegate(string operation, object[] parameters, CancellationToken ct)
    {
        if (operation is not ("buscar_personas" or "conceder_acceso" or "retirar_acceso")) throw new ArgumentException("Unknown delegation operation.");
        return await Guard(async () =>
        {
            var id = await profiles.ApplicationId(ct);
            await using var connection = await connections.Open("Nexo", ct);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            var function = "public.nexo_a" + id.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_" + operation;
            // Conservar una fila también cuando la función devuelve una única columna de confirmación.
            await using var command = new NpgsqlCommand("SELECT to_jsonb(r)::text FROM (SELECT * FROM " + function + "(" + string.Join(',', Enumerable.Range(1, parameters.Length).Select(n => "$" + n)) + ")) r", connection, transaction);
            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter);
            var result = await ReadRows(command, ct); await transaction.CommitAsync(ct); return result;
        });
    }
    public async Task<JsonNode> Represent(string action, string actor, JsonObject? data, CancellationToken ct)
    {
        if (action is not ("capacidad" or "buscar" or "iniciar" or "validar" or "finalizar" or "buscar_personas" or "conceder_acceso" or "retirar_acceso")) throw new ArgumentException("Unknown representation action.");
        return await Guard(async () =>
        {
            var id = await profiles.ApplicationId(ct);
            var input = data?.DeepClone().AsObject() ?? new(); input["accion"] = action; input["actor_email"] = actor;
            await using var connection = await connections.Open("Nexo", ct);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            await using var command = new NpgsqlCommand("SELECT public.nexo_a" + id.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_representacion($1)::text", connection, transaction);
            command.Parameters.AddWithValue(NpgsqlDbType.Jsonb, input.ToJsonString());
            var raw = await command.ExecuteScalarAsync(ct) as string;
            var result = raw is null ? null : JsonNode.Parse(raw);
            if (result is not (JsonObject or JsonArray)) throw Unavailable();
            await transaction.CommitAsync(ct); return result;
        }, true);
    }
    public async Task<JsonObject> Capability(string actor, CancellationToken ct)
    {
        try
        {
            var result = (await Represent("capacidad", actor, null, ct)).AsObject();
            return new()
            {
                ["permitido"] = Flag(result, "permitido"),
                ["escritura"] = Flag(result, "escritura"),
                ["alcance"] = Text(result, "alcance"),
                ["duracion_minutos"] = Number(result, "duracion_minutos")
            };
        }
        catch (DomainProblem problem) when (problem.Status is 422 or 403 or 503)
        { return new() { ["permitido"] = false, ["escritura"] = false, ["no_disponible"] = problem.Status == 503 }; }
    }
    private static async Task<JsonArray> ReadRows(NpgsqlCommand command, CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct); var rows = new JsonArray();
        while (await reader.ReadAsync(ct)) rows.Add(JsonNode.Parse(reader.GetString(0))!.AsObject());
        return rows;
    }
    private async Task<T> Guard<T>(Func<Task<T>> work, bool representation = false)
    {
        try { return await work(); }
        catch (PostgresException e) when (e.SqlState == "P0001")
        {
            var key = representation ? "representacion" : "colaborador";
            var message = representation ? "Nexo rechazó la representación. Revisa acceso, alcance y vigencia; vuelve a tu usuario."
                : "Nexo rechazó la operación. Revisa el responsable, la adscripción y los roles permitidos para delegar.";
            throw new DomainProblem(422, message, new { errors = new Dictionary<string, string> { [key] = message } });
        }
        catch (DomainProblem) { throw; }
        catch (Exception e) when (e is not OperationCanceledException)
        { logger.LogWarning("Operación Nexo no disponible. Tipo: {Type}", e.GetType().Name); throw Unavailable(); }
    }
}
