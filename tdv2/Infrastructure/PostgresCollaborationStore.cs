using System.Text.Json.Nodes;
using Npgsql;
using Tdv2.Domain;
using Microsoft.EntityFrameworkCore;

namespace Tdv2.Infrastructure;

/// <summary>SQL local de colaboraciones. El servicio conserva la transacción y su auditoría.</summary>
public sealed class PostgresCollaborationStore(Tdv2DbContext db)
{
    public async Task<IReadOnlyList<object>> List(string[] roots, UnitDirectory directory, CancellationToken ct)
    {
        var links = new List<object>();
        var rows = await db.UnitCollaborations.AsNoTracking().Where(c => roots.Contains(c.GrantorUnitId)
            && (c.RevokedAt == null || c.CentralRemovalPending)).OrderBy(c => c.Name).ThenBy(c => c.Id).ToListAsync(ct);
        foreach (var row in rows)
            links.Add(new
            {
                id = row.Id,
                email = row.Email,
                nombre = row.Name,
                tipo = row.Kind,
                ur_otorgante = row.GrantorUnitId,
                alcance = directory.Get(row.ScopeUnitId)?.Description ?? row.ScopeUnitId,
                revocada = row.RevokedAt != null,
                pendiente = row.CentralRemovalPending
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
