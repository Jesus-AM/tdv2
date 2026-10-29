using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Tdv2.NativeVerification;

// Observador de diagnóstico sólo en pruebas. No agrega sentencias ni parámetros.
internal sealed class DigitalCostTrace : IDisposable
{
    internal sealed record Command(string Database, string Category, string Purpose, string Fingerprint, double ClientMs);
    private readonly ConcurrentDictionary<string, ConcurrentQueue<Command>> requests = new();
    internal readonly ConcurrentDictionary<string, byte> TagNames = new();
    private readonly ActivityListener listener;
    internal DigitalCostTrace()
    {
        listener = new ActivityListener {
            ShouldListenTo = source => source.Name == "Npgsql",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => {
                if (!requests.TryGetValue(activity.TraceId.ToString(), out var target)) return;
                foreach (var tag in activity.TagObjects) TagNames.TryAdd(tag.Key, 0);
                var sql = (activity.GetTagItem("db.query.text") ?? activity.GetTagItem("db.statement"))?.ToString();
                if (sql is null) return;
                var database = (activity.GetTagItem("db.namespace") ?? activity.GetTagItem("db.name"))?.ToString() ?? "unknown";
                var (category,purpose) = Describe(sql,database);
                target.Enqueue(new(database,category,purpose,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)))[..12],activity.Duration.TotalMilliseconds));
            }
        };
        ActivitySource.AddActivityListener(listener);
    }
    internal void Start(string trace) => requests[trace] = new();
    internal Command[] Finish(string trace) => requests.TryRemove(trace,out var value) ? value.ToArray() : [];
    internal static (string Category,string Purpose) Describe(string sql,string database="")
    {
        var q=sql.ToLowerInvariant();
        if(q.Contains("registrar_actividad")) return ("actividad_nexo","entregar y confirmar recibo central");
        if(q.Contains("nexo_aplicacion"))return ("autorizacion","validar aplicación publicada");
        if(q.Contains("nexo_usuarios"))return ("autorizacion","identidad y empleado publicados");
        if(q.Contains("nexo_usuario_rol"))return ("autorizacion","roles vigentes");
        if(q.Contains("nexo_modulos"))return ("autorizacion","módulos permitidos y orden");
        if(q.Contains("tdv2_sessions"))return q.Contains("for share") ? ("concurrencia","sesión vigente bloqueada antes de escribir/commit") : ("identidad","ticket de sesión vigente");
        if(q.Contains("tdv2_access_contexts"))return ("representacion","contexto efectivo inicial y validaciones transaccionales");
        if(q.Contains("from users")||q.Contains("from \"users\"")||q.Contains("from public.users"))return ("identidad","vínculo usuario Microsoft y versión de sesión");
        if(q.Contains("unidades_responsables_poa"))return ("catalogo","jerarquía y fecha del catálogo local SII");
        if(q.Contains("2710202601"))return ("concurrencia","bloqueo compartido de generación");
        if(q.Contains("27102026"))return ("concurrencia","serialización por empleado");
        if(q.Contains("28102026"))return ("auditoria_local","bloqueo de deduplicación del evento");
        // pg_stat_statements normaliza también las constantes de los advisory locks.
        if(q.Contains("pg_advisory_xact_lock_shared"))return ("concurrencia","bloqueo compartido de generación");
        if(q.Contains("pg_advisory_xact_lock"))return database=="cd_native_test" ? ("concurrencia","serialización por empleado") : ("auditoria_local","bloqueo de deduplicación del evento");
        if(q.Contains("activity_logs"))return ("auditoria_local","insertar revisión/evento si no existe");
        if(q.Contains("cd_control"))return q.TrimStart().StartsWith("update") ? ("concurrencia","trigger incrementa revisión global") : ("concurrencia","leer generación vigente");
        if(q.Contains("cd_surveys"))return q.TrimStart().StartsWith("update") ? ("respuestas","actualizar captura y revisión") : ("encuesta","leer captura/versión antes de escribir y después del commit");
        if(q.Contains("cd_consents"))return ("privacidad","consentimiento vigente en escritura y respuesta");
        if(q.Contains("cd_activity_events"))return q.Contains("skip locked") ? ("actividad_nexo","buscar entrega pendiente con SKIP LOCKED") :
            q.TrimStart().StartsWith("insert") ? ("actividad_nexo","encolar evento manual idempotente") :
            q.TrimStart().StartsWith("update") ? ("actividad_nexo","persistir intento y recibo") : ("actividad_nexo","comprobar recibo manual existente");
        var verb=q.TrimStart().Split([' ','\r','\n','\t'],StringSplitOptions.RemoveEmptyEntries)[0];
        if(new[]{"begin","commit","rollback","savepoint","release"}.Contains(verb))return ("transaccion",verb.ToUpperInvariant());
        if(new[]{"discard","reset","set","unlisten","close","deallocate"}.Contains(verb)||q.Contains("pg_advisory_unlock_all"))return ("mantenimiento_conexion",verb.ToUpperInvariant());
        if(database=="cd_nexo_test")return ("actividad_nexo_interna","contrato central: "+verb.ToUpperInvariant());
        return ("otro",verb.ToUpperInvariant());
    }
    public void Dispose() => listener.Dispose();
}
