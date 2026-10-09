using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Tdv2.Domain.Entities;
using Tdv2.Infrastructure;
using Tdv2.Synchronization;

namespace Tdv2.NativeVerification;

internal static class MigrationTests
{
    internal static Tdv2DbContext Context(string cs) => new(new DbContextOptionsBuilder<Tdv2DbContext>()
        .UseNpgsql(cs, p => p.MigrationsHistoryTable("__EFMigrationsHistory", "public")).Options);

    public static async Task<int> Run(NativeDatabase database)
    {
        var checks = new List<string>();
        var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        async Task<object?> Sql(string cs, string sql)
        {
            await using var connection = new NpgsqlConnection(cs); await connection.OpenAsync();
            return await DatabaseInspection.Scalar(connection, sql);
        }
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks.Add(name); Console.WriteLine("EF PASS " + name);
        }
        async Task<bool> Reject(Func<Task> work)
        {
            try { await work(); return false; }
            catch (PostgresException) { return true; }
            catch (DbUpdateException) { return true; }
            catch (InvalidOperationException error) when (error.InnerException is PostgresException) { return true; }
        }
        async Task<bool> Inspect(string connection)
        {
            var settings = new NpgsqlConnectionStringBuilder(connection);
            var previous = Environment.GetEnvironmentVariable("ConnectionStrings__Tdv2");
            var output = Console.Out;
            using var capture = new StringWriter();
            try
            {
                Environment.SetEnvironmentVariable("ConnectionStrings__Tdv2", connection);
                Console.SetOut(capture);
                return await DatabaseInspection.Run(["--database-check", "--environment", "Production",
                    "--target-host=" + settings.Host, "--target-port=" + settings.Port,
                    "--target-database=" + settings.Database, "--target-user=" + settings.Username]) == 0;
            }
            finally { Console.SetOut(output); Environment.SetEnvironmentVariable("ConnectionStrings__Tdv2", previous); }
        }
        async Task<string> Fresh(string suffix)
        {
            var name = "tdv2_ef_" + suffix;
            await Sql(database.Admin, "CREATE DATABASE " + name + " OWNER tdv2_ef_owner TEMPLATE template0");
            return new NpgsqlConnectionStringBuilder(database.Admin) { Database = name, Username = "tdv2_ef_owner", Password = password, SearchPath = "public" }.ConnectionString;
        }
        const string historySql = "SELECT string_agg(\"MigrationId\"||':'||\"ProductVersion\",',' ORDER BY \"MigrationId\") FROM public.\"__EFMigrationsHistory\"";
        try
        {
            await Sql(database.Admin, $"CREATE ROLE tdv2_ef_owner LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD '{password}'");
            var cs = await Fresh("baseline");
            await using var db = Context(cs);
            Check(!db.Database.HasPendingModelChanges(), "snapshot EF coincide con el modelo completo");
            Check(db.Model.GetEntityTypes().Count() == 20 && db.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()).Count() == 8, "20 entidades y ocho relaciones locales; sin esquemas institucionales");
            Check(db.Database.GetMigrations().Count() == 9, "nueve migraciones estándar descubiertas por EF");
            Check(Convert.ToInt32(await Sql(cs, "SELECT count(*) FROM pg_tables WHERE schemaname='public'")) == 0, "construir DbContext y consultar modelo no aplica DDL");
            await db.GetService<IMigrator>().MigrateAsync(db.Database.GetMigrations().ElementAt(7));
            await using (var diagnostic = new NpgsqlConnection(cs))
            {
                await diagnostic.OpenAsync(); var sql = new SyncSql(diagnostic, null);
                string? state = null;
                try { await sql.Scalar("SELECT count(*) FROM sii_modulos", default); }
                catch (PostgresException error) { state = error.SqlState; }
                Check(state == "42P01", "reproduce 42P01 en SyncSql.Scalar con migración de módulos pendiente, sin SQL Server");
                Check((await SiiCatalogSchema.Inspect(sql, default))?.codigo == "migracion_pendiente", "diagnóstico identifica migración pendiente en la base conectada");
            }
            await db.Database.MigrateAsync();
            await using (var diagnostic = new NpgsqlConnection(cs))
            {
                await diagnostic.OpenAsync(); var sql = new SyncSql(diagnostic, null);
                await sql.Execute("SET search_path=pg_catalog", default);
                Check(await SiiCatalogSchema.Inspect(sql, default) is null && Convert.ToInt32(await sql.Scalar("SELECT count(*) FROM public.sii_modulos", default)) == 0,
                    "EF crea public.sii_modulos; consultas explícitas funcionan con search_path distinto");
                await using (var tx = await diagnostic.BeginTransactionAsync())
                {
                    var transactional = new SyncSql(diagnostic, tx);
                    await transactional.Execute("ALTER TABLE public.sii_modulos RENAME TO fixture_catalog_renamed", default);
                    Check((await SiiCatalogSchema.Inspect(transactional, default))?.codigo == "historial_incoherente", "historial aplicado y tabla ausente se distinguen de migración pendiente");
                    await tx.RollbackAsync();
                }
                await using (var tx = await diagnostic.BeginTransactionAsync())
                {
                    var transactional = new SyncSql(diagnostic, tx);
                    await transactional.Execute("CREATE SCHEMA fixture_catalog_schema; ALTER TABLE public.sii_modulos SET SCHEMA fixture_catalog_schema", default);
                    Check((await SiiCatalogSchema.Inspect(transactional, default))?.codigo == "esquema_distinto", "tabla EF en otro esquema se identifica sin crear tablas manualmente");
                    await tx.RollbackAsync();
                }
            }
            Check((await db.Database.GetAppliedMigrationsAsync()).Count() == 9 && !(await db.Database.GetPendingMigrationsAsync()).Any(), "aplicación con cuenta sin superusuario");
            Check(Convert.ToInt32(await Sql(cs, "SELECT count(*) FROM pg_tables WHERE schemaname='public'")) == 21, "20 tablas y único historial __EFMigrationsHistory");
            Check((bool)(await Sql(cs, "SELECT count(*)=1 AND bool_and(NOT activa AND NOT incluir_ilda AND proxima_en IS NULL AND propietario IS NULL AND reserva_hasta IS NULL) FROM sincronizacion_configuracion"))!, "configuración inicial pausada y sin reservas");
            Check(Convert.ToInt32(await Sql(cs, "SELECT (SELECT count(*) FROM users)+(SELECT count(*) FROM formatos_ur)+(SELECT count(*) FROM unidades_responsables_poa)+(SELECT count(*) FROM sincronizacion_ejecuciones)")) == 0, "migraciones sin usuarios, formatos, catálogos ni trabajos sintéticos");
            var columns = (string)(await Sql(cs, DatabaseInspection.ColumnsSql))!;
            var indexes = (string)(await Sql(cs, DatabaseInspection.IndexesSql))!;
            var constraints = (string)(await Sql(cs, DatabaseInspection.ConstraintsSql))!;
            var history = await Sql(cs, historySql);
            await db.Database.MigrateAsync();
            Check(Equals(history, await Sql(cs, historySql)) && columns == (string)(await Sql(cs, DatabaseInspection.ColumnsSql))!
                && indexes == (string)(await Sql(cs, DatabaseInspection.IndexesSql))!, "repetir Migrate no cambia esquema ni historial");

            var legacy = await Fresh("legacy");
            foreach (var file in Directory.GetFiles(Path.Combine(database.Workspace, "tests/TDV2.NativeVerification/LegacySchema"), "*.sql").Order())
                await Sql(legacy, await File.ReadAllTextAsync(file));
            var legacyColumns = (string)(await Sql(legacy, DatabaseInspection.ColumnsSql))!;
            Check(legacyColumns.Split('\n').All(column => columns.Split('\n').Contains(column)), "columnas, tipos, nulabilidad y defaults anteriores conservados; ampliación aditiva");
            Check((string)(await Sql(cs, DatabaseInspection.SequencesSql))! == (string)(await Sql(legacy, DatabaseInspection.SequencesSql))!, "secuencias serial/bigserial conservadas");
            var oldIndexes = ((string)(await Sql(legacy, DatabaseInspection.IndexesSql))!).Split('\n');
            Check(oldIndexes.All(i => indexes.Split('\n').Contains(i)), "todos los índices y unicidades anteriores conservados");
            var oldConstraints = ((string)(await Sql(legacy, DatabaseInspection.ConstraintsSql))!).Split('\n')
                .Where(c => !c.Contains(":users_microsoft_identity_unique:") && !c.Contains(":ms_graph_tokens_user_id_key:"));
            Check(oldConstraints.All(c => constraints.Split('\n').Contains(c)), "PK, FK, RESTRICT, CASCADE, check singleton y claves alternativas conservadas");
            await using (var legacyDb = Context(legacy))
                Check(await Reject(() => legacyDb.Database.MigrateAsync()) && legacyColumns == (string)(await Sql(legacy, DatabaseInspection.ColumnsSql))!,
                    "base previa sin historial EF se rechaza y conserva; no adopción implícita");

            var occupied = await Fresh("occupied");
            await Sql(occupied, "CREATE TABLE previa(id integer PRIMARY KEY); INSERT INTO previa VALUES(17)");
            await using (var occupiedDb = Context(occupied))
                Check(await Reject(() => occupiedDb.Database.MigrateAsync()) && Convert.ToInt32(await Sql(occupied, "SELECT id FROM previa")) == 17
                    && await Sql(occupied, "SELECT to_regclass('public.users')::text") is DBNull, "contenido inesperado conservado tras rechazo");

            var partial = await Fresh("partial");
            await using (var partialDb = Context(partial))
            {
                await partialDb.GetService<IMigrator>().MigrateAsync(partialDb.Database.GetMigrations().First());
                await Sql(partial, """
                    CREATE FUNCTION fixture_fail_seed() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'SYNTHETIC'; END $$;
                    CREATE TRIGGER fail_seed BEFORE INSERT ON sincronizacion_configuracion FOR EACH ROW EXECUTE FUNCTION fixture_fail_seed();
                    """);
                Check(await Reject(() => partialDb.Database.MigrateAsync())
                    && Convert.ToInt32(await Sql(partial, "SELECT count(*) FROM sincronizacion_configuracion")) == 0
                    && (await partialDb.Database.GetAppliedMigrationsAsync()).Count() == 1, "fallo en segunda migración revierte datos e historial");
                await Sql(partial, "DROP TRIGGER fail_seed ON sincronizacion_configuracion; DROP FUNCTION fixture_fail_seed()");
                await partialDb.Database.MigrateAsync();
                Check((await partialDb.Database.GetAppliedMigrationsAsync()).Count() == 9, "reanudación aplica sólo la pendiente");
                await partialDb.GetService<IMigrator>().MigrateAsync("0");
                Check(Convert.ToInt32(await Sql(partial, "SELECT count(*) FROM pg_tables WHERE schemaname='public' AND tablename <> '__EFMigrationsHistory'")) == 0,
                    "Down revierte únicamente las tablas EF en la base desechable");
                await partialDb.Database.MigrateAsync();
                Check((await partialDb.Database.GetAppliedMigrationsAsync()).Count() == 9, "Up después de Down recrea esquema y configuración pausada");
            }
            var upgrade = await Fresh("upgrade");
            await using (var previous = Context(upgrade))
            {
                await previous.GetService<IMigrator>().MigrateAsync(previous.Database.GetMigrations().ElementAt(1));
                await Sql(upgrade, """
                    INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,num_empleado,tipo_ur) VALUES('HIST',2025,'06000','00001','0');
                    INSERT INTO formatos_ur(id_ur,contenido,version,porcentaje,actualizado_por)
                    VALUES('HIST','{"encabezado":{"responsable":"Histórico"},"identificacion":[{"id":"original","prioridad":"17"}],"nulo":null,"vacio":""}',7,25,'synthetic@example.test');
                    """);
                var original = await Sql(upgrade, "SELECT contenido::text FROM formatos_ur");
                await using (var baselineConnection = new NpgsqlConnection(upgrade))
                {
                    await baselineConnection.OpenAsync();
                    await File.WriteAllTextAsync(Path.Combine(database.Artifacts, "ef-prior-schema.json"),
                        JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, synthetic = true, initialMigrations = 2,
                            schemaSha256 = await DatabaseInspection.Fingerprint(baselineConnection) }, new JsonSerializerOptions { WriteIndented = true }));
                }
                Check(await Inspect(upgrade), "verificador de sólo lectura reconoce esquema previo y migración pendiente");
                await previous.Database.MigrateAsync();
                Check(await Inspect(upgrade), "verificador reconoce tablas actuales y trigger de protección sin objetos ajenos");
                Check(Equals(original, await Sql(upgrade, "SELECT contenido::text FROM formatos_ur"))
                    && (bool)(await Sql(upgrade, "SELECT ejercicio=2025 AND version=7 AND porcentaje=25 AND enviado_en IS NULL FROM formatos_ur"))!,
                    "actualización conserva JSON, prioridad antigua, versión e historia tipo 0; completa ejercicio sin enviar");
                await Sql(upgrade, "UPDATE formatos_ur SET enviado_en=clock_timestamp(),instantanea_envio='{}'");
                Check(await Reject(() => previous.GetService<IMigrator>().MigrateAsync(previous.Database.GetMigrations().ElementAt(1)))
                    && (await previous.Database.GetAppliedMigrationsAsync()).Count() == 9
                    && Equals(original, await Sql(upgrade, "SELECT contenido::text FROM formatos_ur")),
                    "Down rechaza retirar protección de formatos enviados y conserva contenido e historial");
            }
            var concurrent = await Fresh("concurrent");
            var exclusionsUpgrade = await Fresh("exclusions");
            await using (var existing = Context(exclusionsUpgrade))
            {
                await existing.GetService<IMigrator>().MigrateAsync("20261007195019_StructuredUsersServed");
                await Sql(exclusionsUpgrade, """
                    INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur) VALUES('EX',2026,'06000');
                    INSERT INTO formatos_ur(id_ur,ejercicio,contenido,version,porcentaje,actualizado_por)
                    VALUES('EX',2026,'{"identificacion":[{"id":"ilda:17","usuario":["Externo","Otro"],"usuarioOtro":"Detalle existente","prioridad":"17"}],"oculto":"conservar"}',9,32,'synthetic@example.test');
                    """);
                var original = await Sql(exclusionsUpgrade, "SELECT row_to_json(f)::text FROM formatos_ur f");
                await existing.GetService<IMigrator>().MigrateAsync("20261009032858_FormIldaExclusions");
                Check(Equals(original, await Sql(exclusionsUpgrade, "SELECT row_to_json(f)::text FROM formatos_ur f")),
                    "exclusiones: actualización desde seis migraciones conserva toda respuesta, Otro, Externo, prioridad y versión sin repetir limpieza");
                await Sql(exclusionsUpgrade, "INSERT INTO formato_exclusiones_ilda VALUES('EX',2026,'ilda:17',now(),'actor@example.test','effective@example.test')");
                var exclusion = await Sql(exclusionsUpgrade, "SELECT row_to_json(e)::text FROM formato_exclusiones_ilda e");
                await existing.GetService<IMigrator>().MigrateAsync("20261009032858_FormIldaExclusions");
                Check(Equals(exclusion, await Sql(exclusionsUpgrade, "SELECT row_to_json(e)::text FROM formato_exclusiones_ilda e"))
                    && Equals(original, await Sql(exclusionsUpgrade, "SELECT row_to_json(f)::text FROM formatos_ur f")),
                    "exclusiones: repetir aplicación no altera retiro, actores ni respuestas");
                Check(await Reject(() => existing.GetService<IMigrator>().MigrateAsync("20261007195019_StructuredUsersServed"))
                    && Equals(exclusion, await Sql(exclusionsUpgrade, "SELECT row_to_json(e)::text FROM formato_exclusiones_ilda e")),
                    "exclusiones: reversión rechazada para no hacer reaparecer inventario retirado");
            }
            await CurrentRecipientsMigrationTests.Run(await Fresh("current_recipients"), database.Workspace, Sql, Check);
            var moduleConnection = await Fresh("sii_modules");
            await using (var modules = Context(moduleConnection))
            {
                var migrator = modules.GetService<IMigrator>();
                await migrator.MigrateAsync("20261009052502_CurrentRecipients");
                var previousTables = await Sql(moduleConnection, "SELECT count(*) FROM pg_tables WHERE schemaname='public'");
                await modules.Database.MigrateAsync();
                Check(Convert.ToInt32(await Sql(moduleConnection, "SELECT count(*) FROM pg_tables WHERE schemaname='public'")) == Convert.ToInt32(previousTables) + 1,
                    "SiiModulesCatalog añade sólo su réplica sobre las ocho migraciones anteriores");
                await Sql(moduleConnection, "INSERT INTO sii_modulos VALUES('1','Igual',true,now()),('2','Igual',true,now())");
                await modules.Database.MigrateAsync();
                Check(Convert.ToInt32(await Sql(moduleConnection, "SELECT count(*) FROM sii_modulos")) == 2,
                    "repetir migración conserva IDs distintos con descripción igual");
            }
            await UsersServedMigrationTests.Run(await Fresh("users_served"), database.Workspace, Sql, Check);
            var photographUpgrade = await Fresh("photos");
            await using (var existing = Context(photographUpgrade))
            {
                await existing.GetService<IMigrator>().MigrateAsync(existing.Database.GetMigrations().ElementAt(3));
                await Sql(photographUpgrade, """
                    INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur) VALUES('PHOTO',2026,'06000');
                    INSERT INTO formatos_ur(id_ur,contenido,version,porcentaje,actualizado_por,enviado_en,instantanea_envio)
                    VALUES('PHOTO','{"historico":"sintético"}',7,50,'synthetic@example.test',clock_timestamp(),'{"estable":true}');
                    INSERT INTO formato_bloques(id_ur,bloque,version,revision_contexto,participante,color) VALUES('PHOTO','medios',3,0,'sintetico',5);
                    """);
                Check(await Inspect(photographUpgrade), "verificador reconoce participación/presencia con fotografía pendiente");
                var original = await Sql(photographUpgrade, "SELECT row_to_json(f)::text FROM formatos_ur f");
                await existing.Database.MigrateAsync();
                Check(Equals(original, await Sql(photographUpgrade, "SELECT row_to_json(f)::text FROM formatos_ur f"))
                    && (bool)(await Sql(photographUpgrade, "SELECT version=3 AND color=5 AND usuario_participante_id IS NULL FROM formato_bloques"))!,
                    "fotografías conserva enviado, instantánea, versión, color y reservas históricas sin inventar identidades");
                Check(await Reject(() => Sql(photographUpgrade, "UPDATE formato_bloques SET usuario_participante_id=999")), "FK fotografía rechaza identidad local inexistente");
            }
            await using (var a = Context(concurrent))
            await using (var b = Context(concurrent))
            {
                // El bloqueo estándar de Npgsql se toma sobre __EFMigrationsHistory.
                // El primer bootstrap se ejecuta por un operador; comprobamos dos aplicadores
                // de una migración pendiente con ese historial ya establecido.
                await a.GetService<IMigrator>().MigrateAsync(a.Database.GetMigrations().ElementAt(1));
                // EF/Npgsql puede leer pendientes antes de esperar el bloqueo entre migraciones.
                // Se documenta esa colisión, sin ocultar otro error ni introducir un migrador propio.
                async Task<bool> Apply(Tdv2DbContext context)
                {
                    try { await context.Database.MigrateAsync(); return true; }
                    catch (PostgresException error) when (error.SqlState is "42701" or "42P07") { return false; }
                }
                var attempts = await Task.WhenAll(Apply(a), Apply(b));
                Check(attempts.Any(ok => ok), "al menos un aplicador concurrente termina; duplicado DDL puede rechazar al otro (usar un solo operador)");
                await using var retry = Context(concurrent);
                await retry.Database.MigrateAsync();
                Check((await retry.Database.GetAppliedMigrationsAsync()).SequenceEqual(retry.Database.GetMigrations()), "reintento secuencial confirma historial completo y único tras competencia de aplicadores");
                Check((string)(await Sql(concurrent, DatabaseInspection.ColumnsSql))! == columns
                    && (string)(await Sql(concurrent, DatabaseInspection.IndexesSql))! == indexes,
                    "competencia de migradores no pierde columnas ni índices; esquema idéntico al aplicado secuencialmente");
            }
            var scriptTarget = await Fresh("script");
            var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            await Sql(scriptTarget, script);
            var scriptHistory = await Sql(scriptTarget, historySql);
            await Sql(scriptTarget, script);
            Check(Equals(scriptHistory, await Sql(scriptTarget, historySql))
                && columns == (string)(await Sql(scriptTarget, DatabaseInspection.ColumnsSql))!, "script idempotente generado por EF se aplica y repite sin cambios");
            await using (var scriptConnection = new NpgsqlConnection(scriptTarget))
            {
                await scriptConnection.OpenAsync();
                await File.WriteAllTextAsync(Path.Combine(database.Artifacts, "ef-schema.json"),
                    JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, synthetic = true,
                        schemaSha256 = await DatabaseInspection.Fingerprint(scriptConnection) }, new JsonSerializerOptions { WriteIndented = true }));
            }
            // Comprueba el token de concurrencia del proveedor real, además de los conflictos HTTP de la suite completa.
            await Sql(cs, "INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,num_empleado) VALUES('EF',2026,'001','00001')");
            db.UnitForms.Add(new UnitForm { UnitId = "EF", Content = "{\"nulo\":null,\"vacio\":\"\"}", UpdatedBy = "synthetic@example.test", Version = 1 });
            await db.SaveChangesAsync();
            await using (var a = Context(cs))
            await using (var b = Context(cs))
            {
                var first = await a.UnitForms.SingleAsync(); var second = await b.UnitForms.SingleAsync();
                first.Version++; first.Content = "{\"guardado\":true}"; await a.SaveChangesAsync();
                second.Version++; second.Content = "{}";
                var rejected = false;
                try { await b.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { rejected = true; }
                Check(rejected && (await db.UnitForms.AsNoTracking().SingleAsync()).Content.Contains("guardado"), "EF rechaza versión obsoleta sin sobrescribir");
            }
            Check(await Reject(() => Sql(cs, "DELETE FROM unidades_responsables_poa WHERE id_ur='EF'")), "FK impide borrar UR con formato");
            Check(await Reject(() => Sql(cs, "INSERT INTO sincronizacion_configuracion(id) VALUES(2)")), "CHECK limita configuración a id=1");
            Check((await db.ResponsibleUnits.AsNoTracking().SingleAsync()).EmployeeNumber == "00001", "EF conserva ceros iniciales del empleado");
            var report = new { utc = DateTimeOffset.UtcNow, realPostgreSql = true, passed = checks.Count, checks,
                schemaSha256 = DatabaseInspection.Hash(columns + "\n" + constraints + "\n" + indexes),
                legacyDifference = "Unicidades nullable pasan de restricciones UNIQUE a índices UNIQUE; se añade el índice de FK en configuración." };
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(database.Artifacts, "ef-migrations.json"), json);
            return 0;
        }
        catch (Exception error)
        {
            // No incluir mensajes del proveedor, SQL ni credenciales en la evidencia.
            Console.Error.WriteLine("EF FAIL " + error.GetType().Name + " después de " + checks.Count + " comprobaciones.");
            if (error is PostgresException pg) Console.Error.WriteLine("SQLSTATE=" + pg.SqlState);
            if (error is InvalidOperationException) Console.Error.WriteLine(error.Message);
            return 1;
        }
    }
}
