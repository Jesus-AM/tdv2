using System.Text.Json.Nodes;
using Npgsql;
using Tdv2.Domain;

namespace Tdv2.Infrastructure;

/// <summary>SQL local de colaboraciones. El servicio conserva la transacción y su auditoría.</summary>
public sealed class PostgresCollaborationStore(DatabaseConnections connections)
{
    public async Task<IReadOnlyList<object>> List(string[] roots, UnitDirectory directory, CancellationToken ct)
    {
        var links = new List<object>();
        await using var connection = await connections.Open("Tdv2", ct);
        await using var command = new NpgsqlCommand("SELECT id,email,nombre,tipo,ur_otorgante,id_ur_alcance,revocada_en,retiro_central_pendiente FROM colaboraciones_ur WHERE ur_otorgante=ANY($1) AND (revocada_en IS NULL OR retiro_central_pendiente=true) ORDER BY nombre,id", connection);
        command.Parameters.AddWithValue(roots);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            links.Add(new
            {
                id = reader.GetInt64(0),
                email = reader.GetString(1),
                nombre = reader.GetString(2),
                tipo = reader.GetString(3),
                ur_otorgante = reader.GetString(4),
                alcance = directory.Get(reader.GetString(5))?.Description ?? reader.GetString(5),
                revocada = !reader.IsDBNull(6),
                pendiente = reader.GetBoolean(7)
            });
        return links;
    }

    public async Task<JsonObject?> Find(NpgsqlConnection connection, long id, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT to_jsonb(c)::text FROM colaboraciones_ur c WHERE id=$1", connection);
        command.Parameters.AddWithValue(id);
        return await command.ExecuteScalarAsync(ct) is string text ? JsonNode.Parse(text)!.AsObject() : null;
    }

    // El bloqueo abarca también la llamada central y sus dos commits locales. No convertirlo
    // en bloqueo de transacción: una revocación pendiente debe confirmarse antes de llamar Nexo.
    public async Task Lock(NpgsqlConnection connection, string email, string origin, long role, bool release, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT pg_advisory_" + (release ? "unlock" : "lock") + "(hashtextextended($1,1))", connection);
        command.Parameters.AddWithValue(email + "|" + origin + "|" + role);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<long> Save(NpgsqlConnection connection, NpgsqlTransaction transaction, Person user, string employee,
        string origin, string scope, string kind, long grant, long role, string actor, string unit, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("""
            INSERT INTO colaboraciones_ur(email,num_empleado,nombre,id_ur_origen,id_ur_alcance,tipo,nexo_concesion_id,nexo_rol_id,otorgado_por,ur_otorgante,created_at,updated_at)
            VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,timezone('UTC',now()),timezone('UTC',now()))
            ON CONFLICT(nexo_concesion_id,id_ur_alcance) DO UPDATE SET email=EXCLUDED.email,num_empleado=EXCLUDED.num_empleado,nombre=EXCLUDED.nombre,
              id_ur_origen=EXCLUDED.id_ur_origen,tipo=EXCLUDED.tipo,nexo_rol_id=EXCLUDED.nexo_rol_id,otorgado_por=EXCLUDED.otorgado_por,
              ur_otorgante=EXCLUDED.ur_otorgante,revocada_en=null,retiro_central_pendiente=false,updated_at=EXCLUDED.updated_at RETURNING id
            """, connection, transaction);
        foreach (var parameter in new object[] { user.Email, employee, user.Name, origin, scope, kind, grant, role, actor, unit })
            command.Parameters.AddWithValue(parameter);
        return Convert.ToInt64(await command.ExecuteScalarAsync(ct));
    }

    public async Task Revoke(NpgsqlConnection connection, NpgsqlTransaction transaction, long id, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("UPDATE colaboraciones_ur SET revocada_en=timezone('UTC',now()),updated_at=timezone('UTC',now()),retiro_central_pendiente=true WHERE id=$1", connection, transaction);
        command.Parameters.AddWithValue(id);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> HasOtherActiveLink(NpgsqlConnection connection, NpgsqlTransaction transaction, long grant, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT 1 FROM colaboraciones_ur WHERE nexo_concesion_id=$1 AND revocada_en IS NULL LIMIT 1", connection, transaction);
        command.Parameters.AddWithValue(grant);
        return await command.ExecuteScalarAsync(ct) is not null;
    }

    public async Task ConfirmRemoval(NpgsqlConnection connection, NpgsqlTransaction transaction, long id, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("UPDATE colaboraciones_ur SET retiro_central_pendiente=false,updated_at=timezone('UTC',now()) WHERE id=$1", connection, transaction);
        command.Parameters.AddWithValue(id);
        await command.ExecuteNonQueryAsync(ct);
    }
}
