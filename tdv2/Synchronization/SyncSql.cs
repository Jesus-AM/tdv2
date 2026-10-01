using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;
namespace Tdv2.Synchronization;

public sealed class SyncSql(NpgsqlConnection connection, NpgsqlTransaction? transaction)
{
    public NpgsqlConnection Connection => connection;
    public NpgsqlTransaction? Transaction => transaction;
    private NpgsqlCommand Command(string sql, object?[] values)
    {
        var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var value in values)
            if (value is JsonNode json) command.Parameters.AddWithValue(NpgsqlDbType.Json, json.ToJsonString());
            else command.Parameters.AddWithValue(value ?? DBNull.Value);
        return command;
    }
    public async Task<object?> Scalar(string sql, CancellationToken ct, params object?[] values)
    { await using var command = Command(sql, values); return await command.ExecuteScalarAsync(ct); }
    public async Task Execute(string sql, CancellationToken ct, params object?[] values)
    { await using var command = Command(sql, values); await command.ExecuteNonQueryAsync(ct); }
    public async Task<JsonObject> Object(string sql, CancellationToken ct, params object?[] values) =>
        JsonNode.Parse((string)(await Scalar(sql, ct, values) ?? throw new SyncProblem("No se encontró el estado local de sincronización.")))!.AsObject();
    public async Task<DateTimeOffset> Now(CancellationToken ct) => new((DateTime)(await Scalar("SELECT clock_timestamp()", ct))!);
    public Task<JsonObject> Settings(CancellationToken ct, bool locked = true) => Object("SELECT to_jsonb(c)::text FROM sincronizacion_configuracion c WHERE id=1" + (locked ? " FOR UPDATE" : ""), ct);
    public Task Audit(string actor, string action, Guid? run, JsonObject details, CancellationToken ct)
    {
        var meta = details.DeepClone().AsObject(); meta["ejecucion"] = run?.ToString(); meta["contexto"] = "own";
        return Execute("INSERT INTO activity_logs(user_email,entity,action,meta,created_at,updated_at) VALUES($1,'sincronizacion',$2,$3,timezone('UTC',clock_timestamp()),timezone('UTC',clock_timestamp()))", ct, actor, action, meta);
    }
    public static string? Text(JsonObject o, string name) => o[name]?.ToString();
    public static bool Flag(JsonObject o, string name) => o[name]?.GetValue<bool>() == true;
    public static int Number(JsonObject o, string name) => o[name]!.GetValue<int>();
    public static DateTimeOffset? Date(JsonNode? node) => node is null ? null : new(DateTime.SpecifyKind(DateTime.Parse(node.ToString(), System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc));
    public static JsonNode? Iso(JsonNode? node) => Date(node)?.ToString("O");
    public static SyncSchedule Schedule(JsonObject s) => new(Number(s, "version"), Flag(s, "activa"), Number(s, "intervalo_minutos"), Text(s, "hora")!, Text(s, "zona_horaria")!, Flag(s, "incluir_ilda"));
    public static JsonObject PublicSettings(JsonObject s) => new() { ["version"] = Number(s, "version"), ["activa"] = Flag(s, "activa"), ["intervalo_minutos"] = Number(s, "intervalo_minutos"), ["hora"] = Text(s, "hora"), ["zona_horaria"] = Text(s, "zona_horaria"), ["incluir_ilda"] = Flag(s, "incluir_ilda") };
}
