using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;

namespace Tdv2.NativeVerification;

/// <summary>Ensaya respaldo, restauración y conversión sólo dentro del clúster sintético validado.</summary>
internal static class TransitionTests
{
    public static async Task<int> Run(NativeDatabase database)
    {
        var checks = new List<string>();
        var original = new NpgsqlConnectionStringBuilder(database.Admin);
        var source = new NpgsqlConnectionStringBuilder(database.Admin) { Database = "tdv2_transition_source" };
        var target = new NpgsqlConnectionStringBuilder(database.Admin) { Database = "tdv2_transition_test" };
        async Task<object?> Sql(NpgsqlConnectionStringBuilder configuration, string sql)
        {
            await using var connection = new NpgsqlConnection(configuration.ConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            return await command.ExecuteScalarAsync();
        }
        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks.Add(name); Console.WriteLine("TRANSITION PASS " + name);
        }
        var root = database.Workspace;
        string Read(string name) => File.ReadAllText(Path.Combine(root, name));
        string Body(string sql) => System.Text.RegularExpressions.Regex.Replace(sql, @"(?m)^\s*(BEGIN|COMMIT);\s*$", "");
        var preflight = Read("database/transition/preflight.sql");
        var conversion = Read("tests/TDV2.NativeVerification/TransitionSchema/010_laravel_copy.sql");
        var sessions = Body(Read("tests/TDV2.NativeVerification/LegacySchema/002_aspnet_sessions.sql")) + Body(Read("tests/TDV2.NativeVerification/LegacySchema/003_access_contexts.sql"));
        var start = "BEGIN; SET LOCAL tdv2.transition_target='tdv2_transition_test';";
        var preserve = Read("tests/TDV2.NativeVerification/TransitionSchema/preserve.sql");
        var verify = Read("tests/TDV2.NativeVerification/TransitionSchema/verify-preservation.sql");
        async Task<bool> Reject(string sql)
        {
            try { await Sql(target, sql); return false; }
            catch (PostgresException) { return true; }
        }
        try
        {
            // NativeDatabase.Initialize ya verificó loopback, puerto, usuario y data_directory.
            await Sql(original, "CREATE DATABASE tdv2_transition_source TEMPLATE template0");
            await Sql(original, "CREATE DATABASE tdv2_transition_test TEMPLATE template0");
            await Sql(source, Read("tests/TDV2.NativeVerification/laravel-transition-fixture.sql"));
            var schema = new Tdv2.Domain.FormSchema(Read("tdv2/Contracts/procesos_operativos.json"));
            var unit = new Tdv2.Domain.Unit("A", "100", "Área sintética", 2, null, 2026, "0001");
            var form = schema.Blank(unit);
            form["encabezado"]!["responsable"] = "Respuesta sintética que debe conservarse";
            schema.Validate(form, unit);
            await using (var connection = new NpgsqlConnection(source.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand("""
                    INSERT INTO users(id,name,email,password) VALUES(11,'Persona sintética','persona@uacj.mx','hash-sintetico');
                    INSERT INTO ms_graph_tokens(user_id,email,access_token,refresh_token,expires) VALUES(11,'persona@uacj.mx','cifrado-laravel-sintetico','cifrado-sintetico','1');
                    INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,desc_ur,num_empleado,nivel_ur,estatus_ur) VALUES('A',2026,'100','Área sintética','0001',2,'Activo');
                    INSERT INTO colaboraciones_ur(email,num_empleado,nombre,id_ur_origen,id_ur_alcance,tipo,nexo_concesion_id,nexo_rol_id,otorgado_por,ur_otorgante)
                      VALUES('persona@uacj.mx','0001','Persona sintética','A','A','local',9,3,'actor@uacj.mx','A');
                    INSERT INTO ilda_informacion_area SELECT n::text,'100',CASE WHEN n%2=0 THEN '' ELSE NULL END,json_build_object('id',n,'ur2','100','extra',null,'vacio',''),true,now() FROM generate_series(1,205) n;
                    INSERT INTO activity_logs(action,meta) VALUES('guardar','{"historico":true}');
                    CREATE TABLE catalogo_historico(id integer PRIMARY KEY,valor text);
                    INSERT INTO catalogo_historico VALUES(1,'Conservar');
                    UPDATE sincronizacion_configuracion SET activa=true,incluir_ilda=true,proxima_en=now();
                    """, connection);
                await command.ExecuteNonQueryAsync();
                await using var insertForm = new NpgsqlCommand("INSERT INTO formatos_ur(id_ur,contenido,version,porcentaje,actualizado_por,created_at,updated_at) VALUES('A',$1,7,35,'persona@uacj.mx',now(),now())", connection);
                insertForm.Parameters.AddWithValue(NpgsqlTypes.NpgsqlDbType.Json, form.ToJsonString());
                await insertForm.ExecuteNonQueryAsync();
            }
            var dump = Path.Combine(database.Artifacts, "synthetic-laravel.dump");
            await PostgresTool("pg_dump", source, ["--format=custom", "--no-owner", "--no-privileges", "--file", dump]);
            await PostgresTool("pg_restore", target, ["--no-owner", "--no-privileges", "--exit-on-error", "--single-transaction", dump]);
            Check(Convert.ToInt64(await Sql(target, "SELECT count(*) FROM formatos_ur")) == 1, "respaldo y restauración de base sintética no vacía");
            Check(await Reject("BEGIN; SET LOCAL tdv2.transition_target='tdv2_db';" + preflight), "destino incorrecto rechazado antes del DDL");
            Check(await Reject(start + "ALTER TABLE formatos_ur RENAME COLUMN version TO version_incompatible;" + preflight), "esquema incompatible revierte sin alterar la copia");
            Check(await Reject(start + "ALTER TABLE formatos_ur DROP CONSTRAINT formatos_ur_id_ur_key;" + preflight), "clave única ausente impide una conversión incompleta");
            Check(await Reject(start + "UPDATE sincronizacion_configuracion SET propietario=gen_random_uuid(),reserva_hasta=now()+interval '1 hour';" + preflight), "reserva vigente impide migrar o liberar trabajo ajeno");
            Check(await Reject(start + preflight + preserve + conversion + sessions + "UPDATE formatos_ur SET version=99;" + verify), "huella detecta cambio y revierte incluso el DDL");
            Check(await Sql(target, "SELECT to_regclass('public.tdv2_sessions')::text") is DBNull, "rollback conserva la copia previa a ASP.NET");
            await Sql(target, start + preflight + preserve + conversion + sessions + verify + "COMMIT;");
            Check(Convert.ToInt32(await Sql(target, "SELECT version FROM formatos_ur")) == 7 && Convert.ToInt32(await Sql(target, "SELECT porcentaje FROM formatos_ur")) == 35, "respuestas, versión y avance conservados sin recálculo");
            var preservedForm = JsonNode.Parse((string)(await Sql(target, "SELECT contenido::text FROM formatos_ur"))!)!.AsObject();
            Check(JsonNode.DeepEquals(form, schema.Validate(preservedForm, unit)), "contenido convertido aceptado por el validador real ASP.NET");
            Check((string)(await Sql(target, "SELECT num_empleado FROM unidades_responsables_poa"))! == "0001", "empleado textual conserva ceros iniciales");
            Check(Convert.ToInt64(await Sql(target, "SELECT count(*) FROM ilda_informacion_area WHERE datos::jsonb->>'vacio'='' AND datos::jsonb->'extra'='null'::jsonb")) == 205, "réplica íntegra conserva más de 200 filas, vacíos y nulos");
            Check(Convert.ToInt64(await Sql(target, "SELECT count(*) FROM catalogo_historico")) == 1 && Convert.ToInt64(await Sql(target, "SELECT count(*) FROM colaboraciones_ur")) == 1, "catálogos adicionales y vínculos locales conservados");
            Check(Convert.ToInt64(await Sql(target, "SELECT count(*) FROM laravel_archive.ms_graph_tokens")) == 1 && Convert.ToInt64(await Sql(target, "SELECT count(*) FROM ms_graph_tokens")) == 0, "tokens heredados aislados; ASP.NET requiere nuevo login");
            Check(await Sql(target, "SELECT NOT activa AND NOT incluir_ilda AND proxima_en IS NULL FROM sincronizacion_configuracion") is true, "copia convertida queda con programación apagada");
            Check(await Sql(source, "SELECT activa AND incluir_ilda FROM sincronizacion_configuracion") is true && await Sql(source, "SELECT to_regclass('public.tdv2_sessions')::text") is DBNull, "origen sintético conserva esquema y configuración");
            Check(await Reject(start + preflight), "reaplicación rechazada sin sobrescribir datos");
            var report = new { utc = DateTimeOffset.UtcNow, synthetic = true, realLaravelDatabaseRead = false, passed = checks.Count, checks };
            await File.WriteAllTextAsync(Path.Combine(database.Artifacts, "transition-verification.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception error)
        {
            Console.WriteLine("TRANSITION FAIL [" + error.GetType().Name + "]; datos y detalles del proveedor omitidos.");
            Console.WriteLine(error.StackTrace?.Split('\n').FirstOrDefault(line => line.Contains("TransitionTests.cs"))?.Trim());
            return 1;
        }
    }

    private static async Task PostgresTool(string tool, NpgsqlConnectionStringBuilder connection, string[] args)
    {
        var bin = Environment.GetEnvironmentVariable("TDV2_TEST_POSTGRES_BIN")!;
        var start = new ProcessStartInfo(Path.Combine(bin, tool + (OperatingSystem.IsWindows() ? ".exe" : "")))
        { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in new[] { "-h", connection.Host!, "-p", connection.Port.ToString(), "-U", connection.Username!, "-d", connection.Database! }.Concat(args)) start.ArgumentList.Add(arg);
        start.Environment["PGPASSWORD"] = connection.Password;
        start.Environment["PGOPTIONS"] = "-c default_transaction_read_only=" + (tool == "pg_dump" ? "on" : "off");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(); await output; await errors;
        if (process.ExitCode != 0) throw new InvalidOperationException("Fallo herramienta PostgreSQL sintética.");
    }
}
