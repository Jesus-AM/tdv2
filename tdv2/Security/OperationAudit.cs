using System.Security.Claims;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using Tdv2.Domain;
using Tdv2.Infrastructure;
namespace Tdv2.Security;

public sealed class OperationAudit(DatabaseConnections connections, AccessState state, IHttpContextAccessor accessor, ILogger<OperationAudit> logger)
{
    public Profile? Effective { get; set; }
    public AccessSelection? Resolved { get; set; }
    public async Task Write(NpgsqlConnection connection, NpgsqlTransaction transaction, string action, string entity,
        string resource, string result, CancellationToken ct, AccessSelection? selection = null, object? details = null)
    {
        var http = accessor.HttpContext!;
        selection ??= Resolved ?? state.Loaded?.Selection;
        // Lista permitida explícita: nunca serializar contexto/token, cuerpo, cabeceras, respuesta externa ni excepción.
        var meta = new
        {
            entidad_id = resource,
            resultado = result,
            representacion_id = selection?.Kind == "representation" ? selection.Id : null,
            representado_email = selection?.Kind == "representation" ? selection.Email : null,
            representado_nombre = selection?.Kind == "representation" ? selection.Name : null,
            roles_efectivos = Effective?.Roles.Select(r => new { id = r.Id, key = r.Key, name = r.Name }),
            ur_efectiva = selection?.UnitId ?? Effective?.User.UnitId,
            escritura = selection?.Write,
            contexto = selection?.Kind ?? "own",
            escenario_rol = selection?.Role,
            detalles = details
        };
        await using var command = new NpgsqlCommand("""
            INSERT INTO activity_logs(user_email,user_name,entity,action,meta,created_at,updated_at)
            VALUES($1,$2,$3,$4,$5,timezone('UTC',now()),timezone('UTC',now()))
            """, connection, transaction);
        command.Parameters.AddWithValue(http.User.FindFirstValue(ClaimTypes.Email) ?? "");
        command.Parameters.AddWithValue(http.User.Identity?.Name ?? ""); command.Parameters.AddWithValue(entity);
        command.Parameters.AddWithValue(action); command.Parameters.AddWithValue(NpgsqlDbType.Json, JsonSerializer.Serialize(meta));
        await command.ExecuteNonQueryAsync(ct);
    }
    public async Task Record(string action, string entity, string resource, string result, CancellationToken ct, object? details = null)
    {
        await using var connection = await connections.Open("Tdv2", ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        await Write(connection, transaction, action, entity, resource, result, ct, details: details);
        await transaction.CommitAsync(ct);
    }
    public async Task Failure(HttpContext http, int status)
    {
        if (state.SessionHash is null || !(http.Request.Path.StartsWithSegments("/formatos") || http.Request.Path.StartsWithSegments("/colaboradores")
            || http.Request.Path.StartsWithSegments("/actuar-como-usuario") || http.Request.Path == "/vista-prueba" || http.Request.Path.StartsWithSegments("/configuracion/sincronizaciones"))) return;
        var path = http.Request.Path.Value ?? "/";
        try
        {
            // CSRF puede fallar antes de que la autorización cargue la identidad efectiva.
            await state.Load(CancellationToken.None);
            await Record(http.Request.Method.ToLowerInvariant(), "acceso", path[..Math.Min(150, path.Length)],
                status >= 500 ? "no_disponible" : "rechazado", CancellationToken.None, new { status });
        }
        catch (Exception e) { logger.LogWarning("No se pudo auditar el rechazo. Tipo: {Type}", e.GetType().Name); }
    }
}
