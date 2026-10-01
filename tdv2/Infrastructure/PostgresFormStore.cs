using Npgsql;
using NpgsqlTypes;
using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Security;
namespace Tdv2.Infrastructure;

// El arranque no crea esquemas, no carga datos y no abre conexiones.
public sealed class PostgresFormStore(DatabaseConnections connections, AccessState state, OperationAudit audit) : IFormStore
{
    private DateTime? catalogReadAt;
    private bool unitsRead;
    private Task<NpgsqlConnection> Open(CancellationToken cancellation) => connections.Open("Tdv2", cancellation);
    public async Task<IReadOnlyList<Unit>> Units(CancellationToken cancellation)
    {
        await using var connection = await Open(cancellation);
        await using var command = new NpgsqlCommand("SELECT id_ur,cve_ur,desc_ur,nivel_ur,id_ur_pertenece,ejercicio,num_empleado,presente,estatus_ur,(SELECT completada_en FROM sincronizacion_catalogos WHERE fuente='sii') FROM unidades_responsables_poa WHERE presente = TRUE", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var result = new List<Unit>();
        while (await reader.ReadAsync(cancellation))
        {
            catalogReadAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9); unitsRead = true;
            result.Add(new(reader.GetString(0), reader.GetString(1), NullableText(reader, 2),
                reader.IsDBNull(3) ? 0 : reader.GetInt32(3), NullableText(reader, 4), reader.GetInt32(5), NullableText(reader, 6), reader.GetBoolean(7), NullableText(reader, 8) ?? ""));
        }
        return result;
    }
    public async Task<IReadOnlyList<Collaboration>> Collaborations(string email, CancellationToken cancellation)
    {
        await using var connection = await Open(cancellation);
        await using var command = new NpgsqlCommand("SELECT email,num_empleado,id_ur_origen,id_ur_alcance,tipo,nexo_concesion_id,nexo_rol_id,ur_otorgante FROM colaboraciones_ur WHERE email = $1 AND revocada_en IS NULL", connection);
        command.Parameters.AddWithValue(email);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        var result = new List<Collaboration>();
        while (await reader.ReadAsync(cancellation)) result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetInt64(5), reader.GetInt64(6), reader.GetString(7)));
        return result;
    }
    public async Task<StoredForm?> Get(string unit, CancellationToken cancellation)
    {
        await using var connection = await Open(cancellation);
        return await Read(connection, null, unit, cancellation);
    }
    private static async Task<StoredForm?> Read(NpgsqlConnection connection, NpgsqlTransaction? transaction, string unit, CancellationToken cancellation)
    {
        await using var command = new NpgsqlCommand("SELECT contenido::text,version,porcentaje,updated_at,actualizado_por FROM formatos_ur WHERE id_ur = $1", connection, transaction);
        command.Parameters.AddWithValue(unit);
        await using var reader = await command.ExecuteReaderAsync(cancellation);
        if (!await reader.ReadAsync(cancellation)) return null;
        var date = DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc);
        return new(unit, JsonNode.Parse(reader.GetString(0))!.AsObject(), reader.GetInt32(1), Convert.ToInt32(reader.GetValue(2)), date, reader.GetString(4));
    }
    public async Task<StoredForm> Save(string unit, int expectedVersion, JsonObject content, string actor, CancellationToken cancellation)
    {
        await using var connection = await Open(cancellation);
        await using var transaction = await connection.BeginTransactionAsync(cancellation);
        await state.Guard(connection, transaction, cancellation);
        // Bloquear la UR serializa también el primer guardado, cuando aún no existe una fila de formato.
        await using (var unitLock = new NpgsqlCommand("SELECT id_ur FROM unidades_responsables_poa WHERE id_ur = $1 AND presente = TRUE AND lower(trim(estatus_ur)) = 'activo' FOR UPDATE", connection, transaction))
        {
            unitLock.Parameters.AddWithValue(unit);
            if (await unitLock.ExecuteScalarAsync(cancellation) is null) throw new DomainProblem(403, "La UR ya no está activa.");
        }
        // El alcance se resolvió con el catálogo leído en esta solicitud. Una publicación concurrente
        // puede cambiar responsable o rama; se exige revalidar para no guardar con permisos antiguos.
        await using (var changed = new NpgsqlCommand("SELECT completada_en FROM sincronizacion_catalogos WHERE fuente='sii'", connection, transaction))
        {
            var currentCatalog = await changed.ExecuteScalarAsync(cancellation) as DateTime?;
            if (!unitsRead || currentCatalog != catalogReadAt) throw new DomainProblem(409, "El catálogo de áreas cambió. Conserva tus cambios y recarga antes de guardar.");
        }
        var current = await Read(connection, transaction, unit, cancellation);
        if ((current?.Version ?? 0) != expectedVersion) throw new DomainProblem(409, "Otra persona actualizó el formato. Conserva tus cambios y recarga antes de continuar.", new { version = current?.Version ?? 0 });
        var now = DateTimeOffset.UtcNow;
        var saved = new StoredForm(unit, content, expectedVersion + 1, FormSchema.Progress(content), now, actor);
        const string sql = """
            INSERT INTO formatos_ur (id_ur,contenido,version,porcentaje,actualizado_por,created_at,updated_at)
            VALUES ($1,$2,$3,$4,$5,$6,$6)
            ON CONFLICT (id_ur) DO UPDATE SET contenido = EXCLUDED.contenido,version = EXCLUDED.version,
            porcentaje = EXCLUDED.porcentaje,actualizado_por = EXCLUDED.actualizado_por,updated_at = EXCLUDED.updated_at
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue(unit);
        command.Parameters.AddWithValue(NpgsqlDbType.Json, content.ToJsonString());
        command.Parameters.AddWithValue(saved.Version);
        command.Parameters.AddWithValue(saved.Progress);
        command.Parameters.AddWithValue(actor);
        command.Parameters.AddWithValue(NpgsqlDbType.Timestamp, DateTime.SpecifyKind(now.UtcDateTime, DateTimeKind.Unspecified));
        await command.ExecuteNonQueryAsync(cancellation);
        await audit.Write(connection, transaction, "guardar", "formato_ur", unit, "confirmado", cancellation, details: new { version = saved.Version });
        await transaction.CommitAsync(cancellation);
        return saved;
    }
    private static string? NullableText(NpgsqlDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
}
