using System.Security.Claims;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Tdv2.Domain;
using Tdv2.Infrastructure;
namespace Tdv2.Security;

public sealed class AccessState(DatabaseConnections connections, ProtectedValues crypto, IHttpContextAccessor accessor)
{
    public const string Claim = "tdv2.session_hash";
    public string? SessionHash => accessor.HttpContext?.User.FindFirstValue(Claim);
    public AccessSnapshot? Loaded { get; private set; }
    public async Task<AccessSnapshot> Load(CancellationToken ct)
    {
        if (Loaded is not null) return Loaded;
        if (SessionHash is null) return Loaded = new(0, null);
        await using var connection = await connections.Open("Tdv2", ct);
        return Loaded = await Read(connection, null, ct);
    }
    private async Task<AccessSnapshot> Read(NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT revision,selection FROM tdv2_access_contexts WHERE session_hash=$1", connection, transaction);
        command.Parameters.AddWithValue(SessionHash!);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return new(0, null);
        return new(reader.GetInt64(0), reader.IsDBNull(1) ? null : JsonSerializer.Deserialize<AccessSelection>(crypto.Unprotect("access-context", reader.GetString(1)))
            ?? throw new DomainProblem(503, "No se pudo leer el contexto de acceso."));
    }
    /// <summary>Bloquea la sesión y compara su revisión antes de escribir o cambiar de identidad efectiva.</summary>
    /// <remarks>Debe ejecutarse dentro de la misma transacción que el cambio y la auditoría.</remarks>
    public async Task Guard(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct, bool change = false)
    {
        if (SessionHash is null) throw new DomainProblem(401, "Inicia sesión nuevamente para continuar.");
        await using var command = new NpgsqlCommand("SELECT id_hash FROM tdv2_sessions WHERE id_hash=$1 AND expires_at>clock_timestamp() FOR " + (change ? "UPDATE" : "SHARE"), connection, transaction);
        command.Parameters.AddWithValue(SessionHash);
        if (await command.ExecuteScalarAsync(ct) is null) throw new DomainProblem(401, "La sesión ya no está vigente.");
        var current = await Read(connection, transaction, ct);
        if (Loaded is null || current.Revision != Loaded.Revision) throw new DomainProblem(409, "El contexto cambió en otra pestaña. Recarga antes de continuar.");
        if (!change && current.Selection is { } selection && (selection.Kind == "preview" || !selection.Write || selection.ExpiresAt <= DateTimeOffset.UtcNow))
            throw new DomainProblem(409, "El contexto ya no permite cambios. Vuelve a tu usuario.");
    }
    public async Task Set(NpgsqlConnection connection, NpgsqlTransaction transaction, AccessSelection? selection, CancellationToken ct)
    {
        // El llamador mantiene el bloqueo UPDATE de sesión. Conservar la revisión incluso después de salir.
        await using var command = new NpgsqlCommand("""
            INSERT INTO tdv2_access_contexts(session_hash,revision,selection) VALUES($1,$2,$3)
            ON CONFLICT(session_hash) DO UPDATE SET revision=EXCLUDED.revision,selection=EXCLUDED.selection
            """, connection, transaction);
        command.Parameters.AddWithValue(SessionHash!); command.Parameters.AddWithValue(Loaded!.Revision + 1);
        command.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlDbType.Text,
            Value = selection is null ? DBNull.Value : crypto.Protect("access-context", JsonSerializer.Serialize(selection))
        });
        await command.ExecuteNonQueryAsync(ct);
    }
}
