using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tdv2.Infrastructure;

namespace Tdv2.NativeVerification;

/// <summary>Comprobación administrativa de sólo lectura: no aplica migraciones ni consulta identidades.</summary>
internal static class DatabaseInspection
{
    internal const string ColumnsSql = """
        SELECT coalesce(string_agg(c.relname||'.'||a.attname||':'||format_type(a.atttypid,a.atttypmod)||':'||a.attnotnull::text||':'||coalesce(pg_get_expr(d.adbin,d.adrelid),''),E'\n' ORDER BY c.relname,a.attname),'')
        FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace JOIN pg_attribute a ON a.attrelid=c.oid
        LEFT JOIN pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum
        WHERE n.nspname='public' AND c.relkind='r' AND a.attnum>0 AND NOT a.attisdropped AND c.relname <> '__EFMigrationsHistory'
        """;
    internal const string ConstraintsSql = """
        SELECT coalesce(string_agg(c.relname||':'||x.conname||':'||pg_get_constraintdef(x.oid)||':'||x.convalidated::text,E'\n' ORDER BY c.relname,x.conname),'')
        FROM pg_constraint x JOIN pg_class c ON c.oid=x.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace
        WHERE n.nspname='public' AND c.relname <> '__EFMigrationsHistory'
        """;
    internal const string IndexesSql = """
        SELECT coalesce(string_agg(pg_get_indexdef(i.indexrelid)||':'||i.indisvalid::text||':'||i.indisready::text,E'\n' ORDER BY c.relname,ic.relname),'')
        FROM pg_index i JOIN pg_class c ON c.oid=i.indrelid JOIN pg_class ic ON ic.oid=i.indexrelid JOIN pg_namespace n ON n.oid=c.relnamespace
        WHERE n.nspname='public' AND c.relname <> '__EFMigrationsHistory'
        """;
    internal const string SequencesSql = """
        SELECT coalesce(string_agg(sequencename||':'||data_type||':'||start_value||':'||min_value||':'||max_value||':'||increment_by||':'||cycle||':'||cache_size,E'\n' ORDER BY sequencename),'')
        FROM pg_sequences WHERE schemaname='public'
        """;

    internal static async Task<object?> Scalar(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }
    internal static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    internal static async Task<string> Fingerprint(NpgsqlConnection connection) => Hash(
        string.Join("\n", new[] { await Scalar(connection, ColumnsSql), await Scalar(connection, ConstraintsSql),
            await Scalar(connection, IndexesSql), await Scalar(connection, SequencesSql) }));

    internal static async Task<int> Run(string[] args)
    {
        try
        {
            string Arg(string name) => args.Single(a => a.StartsWith(name + "=", StringComparison.Ordinal))[(name.Length + 1)..];
            await using var db = new Tdv2DbContextFactory().CreateDbContext(args
                .Where(a => a != "--database-check" && !a.StartsWith("--target-", StringComparison.Ordinal)).ToArray());
            var settings = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
            var target = new { host = Arg("--target-host"), port = int.Parse(Arg("--target-port")), database = Arg("--target-database"), user = Arg("--target-user") };
            Require(settings.Host == target.host && settings.Port == target.port && settings.Database == target.database && settings.Username == target.user, "Destino distinto de ConnectionStrings:Tdv2; no se conectó.");
            Require(settings.SearchPath == "public", "Se requiere Search Path=public.");
            var loopback = IPAddress.TryParse(target.host, out var ip) && IPAddress.IsLoopback(ip);
            Require(loopback || settings.SslMode is SslMode.Require or SslMode.VerifyCA or SslMode.VerifyFull, "El destino remoto requiere TLS sin degradación.");
            settings.Pooling = false; settings.IncludeErrorDetail = false; settings.LogParameters = false;
            settings.Timeout = 10; settings.CommandTimeout = 30;
            await using var connection = new NpgsqlConnection(settings.ConnectionString);
            await connection.OpenAsync();
            await using var tx = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
            await Scalar(connection, "SET TRANSACTION READ ONLY");
            Require((string)(await Scalar(connection, "SELECT current_database()"))! == target.database
                && (string)(await Scalar(connection, "SELECT current_user"))! == target.user
                && Convert.ToInt32(await Scalar(connection, "SELECT inet_server_port()")) == target.port, "El servidor no confirmó destino/usuario/puerto.");
            var address = (string)(await Scalar(connection, "SELECT host(inet_server_addr())"))!;
            var addresses = ip is null ? await Dns.GetHostAddressesAsync(target.host) : [ip];
            Require(addresses.Any(x => x.Equals(IPAddress.Parse(address))), "La dirección del servidor no coincide.");
            var tls = (bool)(await Scalar(connection, "SELECT coalesce((SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid()),false)"))!;
            Require(loopback || tls, "El servidor no confirmó TLS.");
            Require((bool)(await Scalar(connection, """
                SELECT NOT pg_is_in_recovery() AND current_schema()='public' AND current_schemas(false)=ARRAY['public']::name[]
                    AND current_setting('default_transaction_read_only')='off'
                    AND has_database_privilege(current_user,current_database(),'CONNECT')
                    AND has_schema_privilege(current_user,'public','USAGE') AND has_schema_privilege(current_user,'public','CREATE')
                """))!, "Destino o permisos de migración no válidos.");
            var names = (string)(await Scalar(connection, """
                SELECT coalesce(string_agg(c.relname,',' ORDER BY c.relname),'') FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='public' AND c.relkind IN ('r','p') AND c.relname <> '__EFMigrationsHistory'
                """))!;
            var tables = names.Length == 0 ? [] : names.Split(',');
            var expected = db.Model.GetEntityTypes().Select(x => x.GetTableName()!).Order(StringComparer.Ordinal).ToArray();
            var historyExists = (bool)(await Scalar(connection, "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL"))!;
            var applied = historyExists ? (string)(await Scalar(connection, "SELECT coalesce(string_agg(\"MigrationId\",',' ORDER BY \"MigrationId\"),'') FROM public.\"__EFMigrationsHistory\""))! : "";
            var migrations = applied.Length == 0 ? [] : applied.Split(',');
            var known = db.Database.GetMigrations().ToArray();
            // También reconoce el punto de partida de esta actualización, sin adoptar tablas desconocidas.
            var priorTables = expected.Except(new[] { "formato_bloques", "formato_operaciones", "formato_posiciones", "configuracion_procesos" }).ToArray();
            var previousVersion = migrations.SequenceEqual(known.Take(2)) && tables.SequenceEqual(priorTables);
            var previousCapture = migrations.SequenceEqual(known.Take(3));
            var previousPresence = migrations.SequenceEqual(known.Take(4));
            var recognized = tables.SequenceEqual(expected) || previousVersion
                || previousCapture && tables.SequenceEqual(expected.Except(new[] { "configuracion_procesos" }));
            var counts = new Dictionary<string, long>();
            if (recognized)
            {
                foreach (var table in tables)
                {
                    // Identificadores del modelo compilado, nunca de parámetros HTTP o de datos externos.
                    counts[table] = Convert.ToInt64(await Scalar(connection, $"SELECT count(*) FROM public.\"{table}\""));
                }
            }
            var owned = (bool)(await Scalar(connection, """
                SELECT coalesce(bool_and(pg_has_role(current_user,c.relowner,'USAGE')),true)
                FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='public' AND c.relkind IN ('r','p','S')
                """))!;
            Require(owned, "Hay objetos con propietario distinto; se conservaron.");
            var otherObjects = Convert.ToInt32(await Scalar(connection, """
                SELECT count(*) FROM (
                    SELECT c.oid FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND n.nspname NOT LIKE 'pg_toast%' AND n.nspname NOT LIKE 'pg_temp_%'
                      AND c.relkind IN ('r','p','S','v','m','f') AND (n.nspname <> 'public' OR c.relkind IN ('v','m','f'))
                      AND NOT EXISTS(SELECT 1 FROM pg_depend d WHERE d.classid='pg_class'::regclass AND d.objid=c.oid AND d.deptype='e')
                    UNION ALL SELECT p.oid FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
                    WHERE n.nspname NOT IN ('pg_catalog','information_schema')
                      AND NOT (n.nspname='public' AND p.proname='proteger_formato_enviado' AND p.pronargs=0 AND p.prorettype='trigger'::regtype)
                      AND NOT EXISTS(SELECT 1 FROM pg_depend d WHERE d.classid='pg_proc'::regclass AND d.objid=p.oid AND d.deptype='e')
                    UNION ALL SELECT t.oid FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace
                    WHERE n.nspname NOT IN ('pg_catalog','information_schema') AND t.typtype IN ('e','d')
                      AND NOT EXISTS(SELECT 1 FROM pg_depend d WHERE d.classid='pg_type'::regclass AND d.objid=t.oid AND d.deptype='e')
                ) objects
                """));
            var sequenceCount = Convert.ToInt32(await Scalar(connection, "SELECT count(*) FROM pg_sequences WHERE schemaname='public'"));
            var empty = tables.Length == 0 && sequenceCount == 0 && otherObjects == 0 && migrations.Length == 0;
            var paused = recognized && (bool)(await Scalar(connection, "SELECT count(*)=1 AND coalesce(bool_and(id=1 AND NOT activa AND NOT incluir_ilda AND proxima_en IS NULL AND ejecucion_activa IS NULL AND propietario IS NULL AND reserva_hasta IS NULL),false) FROM sincronizacion_configuracion"))!;
            var protection = (bool)(await Scalar(connection, """
                SELECT EXISTS(SELECT 1 FROM pg_trigger t JOIN pg_proc p ON p.oid=t.tgfoid
                    JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname='public' AND c.relname='formatos_ur' AND p.proname='proteger_formato_enviado'
                      AND NOT t.tgisinternal AND t.tgenabled IN ('O','A'))
                """))!;
            var valid = empty || recognized && otherObjects == 0 && paused
                && (previousVersion || (previousCapture || previousPresence || migrations.SequenceEqual(known)) && protection);
            var report = new { utc = DateTimeOffset.UtcNow, readOnly = true, target, address, tls, permissionsVerified = true,
                serverVersion = await Scalar(connection, "SHOW server_version"), empty, recognized, tables, counts, otherObjects,
                applied = migrations, pending = known.Except(migrations).ToArray(), submissionProtection = protection,
                automaticSynchronizationDisabled = recognized ? (bool?)paused : null,
                schemaSha256 = await Fingerprint(connection), valid };
            Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return valid ? 0 : 1;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine("Comprobación no completada. Tipo=" + error.GetType().Name + ". Sin SQL ni credenciales.");
            return 1;
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
