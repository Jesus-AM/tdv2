using Npgsql;
namespace Tdv2.NativeVerification;

internal sealed class NativeDatabase
{
    internal string Admin { get; }
    internal string AppConnection { get; private set; } = "";
    internal string NexoConnection { get; private set; } = "";
    internal string Workspace { get; } = Directory.GetCurrentDirectory();
    internal string Artifacts { get; } = Environment.GetEnvironmentVariable("TDV2_TEST_ARTIFACTS") ?? throw new InvalidOperationException("Use scripts/Test-NativePostgres.ps1.");
    internal NativeDatabase()
    {
        Admin = Environment.GetEnvironmentVariable("TDV2_TEST_CONNECTION") ?? throw new InvalidOperationException("Use scripts/Test-NativePostgres.ps1.");
        var settings = new NpgsqlConnectionStringBuilder(Admin);
        if (settings.Host != "127.0.0.1" || settings.Port == 5432 || settings.Database != "tdv2_native_test" || settings.Username != "tdv2_test_admin") throw new InvalidOperationException("Refusing non-isolated PostgreSQL.");
    }
    internal async Task Initialize()
    {
        await ValidateCluster();
        await CreateSchema();
    }
    private async Task ValidateCluster()
    {
        var expected = Path.GetFullPath(Environment.GetEnvironmentVariable("TDV2_TEST_DATA_DIRECTORY")!);
        if (!expected.StartsWith(Path.GetFullPath(Path.Combine(Workspace, ".artifacts")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe cluster directory.");
        var actual = (await Scalar("SHOW data_directory"))!.ToString()!;
        if (!Path.GetFullPath(actual).Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Cluster identity mismatch; no writes executed.");
    }
    internal async Task Attach()
    {
        await ValidateCluster();
        var app=Environment.GetEnvironmentVariable("TDV2_SYNC_APP_CONNECTION")!;
        var nexo=Environment.GetEnvironmentVariable("TDV2_SYNC_NEXO_CONNECTION")!;
        var expected=new NpgsqlConnectionStringBuilder(Admin);
        foreach(var value in new[] {app,nexo})
        {
            var settings=new NpgsqlConnectionStringBuilder(value);
            if(settings.Host!=expected.Host || settings.Port!=expected.Port || settings.Database!=expected.Database || settings.Username is not ("tdv2_native_app" or "tdv2_native_nexo")) throw new InvalidOperationException("Unsafe child process connection.");
        }
        AppConnection=app; NexoConnection=nexo;
    }
    private async Task CreateSchema()
    {
        await Sql(await File.ReadAllTextAsync(Path.Combine(Workspace, "database/001_core.sql")));
        await Sql(await File.ReadAllTextAsync(Path.Combine(Workspace, "database/002_aspnet_sessions.sql")));
        await Sql(await File.ReadAllTextAsync(Path.Combine(Workspace, "database/003_access_contexts.sql")));
        await Sql(await File.ReadAllTextAsync(Path.Combine(Workspace, "database/004_synchronizations.sql")));
        await Sql(await File.ReadAllTextAsync(Path.Combine(Workspace, "tests/TDV2.NativeVerification/nexo-fixture.sql")));
        await Sql(await File.ReadAllTextAsync(Path.Combine(Workspace, "tests/TDV2.NativeVerification/access-fixture.sql")));
        // Random credentials are never printed, committed or sent to the browser.
        var password = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await Sql($"CREATE ROLE tdv2_native_app LOGIN PASSWORD '{password}'; CREATE ROLE tdv2_native_nexo LOGIN PASSWORD '{password}';");
        await Sql("""
            GRANT USAGE ON SCHEMA public TO tdv2_native_app,tdv2_native_nexo;
            GRANT SELECT,INSERT,UPDATE,DELETE ON users,ms_graph_tokens,activity_logs,tdv2_sessions,tdv2_oauth_attempts,formatos_ur TO tdv2_native_app;
            GRANT SELECT ON unidades_responsables_poa,colaboraciones_ur TO tdv2_native_app;
            GRANT UPDATE ON unidades_responsables_poa TO tdv2_native_app;
            GRANT INSERT ON unidades_responsables_poa TO tdv2_native_app;
            GRANT SELECT,INSERT,UPDATE,DELETE ON sincronizaciones_institucionales,sincronizacion_catalogos,ilda_informacion_area,sincronizacion_ejecuciones,sincronizacion_configuracion TO tdv2_native_app;
            GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO tdv2_native_app;
            GRANT SELECT,INSERT,UPDATE,DELETE ON tdv2_access_contexts,colaboraciones_ur TO tdv2_native_app;
            GRANT SELECT ON nexo_delegacion,nexo_delegacion_roles TO tdv2_native_nexo;
            GRANT EXECUTE ON FUNCTION nexo_a47_buscar_personas(text,text,text,integer),nexo_a47_conceder_acceso(text,text,text,bigint,text),nexo_a47_retirar_acceso(text,text,bigint),nexo_a47_representacion(jsonb) TO tdv2_native_nexo;
            GRANT SELECT ON nexo_aplicacion,nexo_usuarios,nexo_usuario_rol,nexo_modulos,nexo_modulo_rol,nexo_concesiones TO tdv2_native_nexo;
            """);
        AppConnection = new NpgsqlConnectionStringBuilder(Admin) { Username = "tdv2_native_app", Password = password }.ConnectionString;
        NexoConnection = new NpgsqlConnectionStringBuilder(Admin) { Username = "tdv2_native_nexo", Password = password }.ConnectionString;
    }
    internal async Task Reset()
    {
        await Sql("""
            TRUNCATE sincronizacion_configuracion,sincronizacion_ejecuciones,sincronizacion_catalogos,sincronizaciones_institucionales,ilda_informacion_area RESTART IDENTITY;
            INSERT INTO sincronizacion_configuracion(id) VALUES(1);
            TRUNCATE users,ms_graph_tokens,activity_logs,tdv2_sessions,tdv2_oauth_attempts,formatos_ur,colaboraciones_ur,unidades_responsables_poa,
              fixture_app,fixture_users,fixture_roles,fixture_modules,fixture_module_roles,fixture_grants RESTART IDENTITY CASCADE;
            TRUNCATE fixture_units,fixture_delegation,fixture_delegation_roles,fixture_people,fixture_capability,fixture_representation,fixture_calls,fixture_faults RESTART IDENTITY;
            ALTER SEQUENCE fixture_grant_ids RESTART WITH 900;
            GRANT SELECT ON nexo_aplicacion,nexo_usuarios,nexo_usuario_rol,nexo_modulos,nexo_modulo_rol,nexo_concesiones TO tdv2_native_nexo;
            GRANT SELECT ON nexo_delegacion,nexo_delegacion_roles TO tdv2_native_nexo;
            INSERT INTO fixture_app VALUES (47,'tdv2','TDV2 sintético',2);
            INSERT INTO fixture_users VALUES(10,20,'persona@uacj.mx','Persona sintética','individual','0001','A4','adscripcion');
            INSERT INTO fixture_roles VALUES(10,'persona@uacj.mx',30,'responsable_ur','Responsable');
            INSERT INTO fixture_modules VALUES (9,47,'procesos_operativos','Procesos operativos','/inicio',null,1,null),
              (50,47,'configuracion','Configuración','/configuracion',null,10,null),
              (51,47,'sincronizaciones','Sincronizaciones','/configuracion/sincronizaciones',null,11,50);
            INSERT INTO fixture_module_roles VALUES(30,9);
            INSERT INTO fixture_delegation VALUES('persona@uacj.mx','A','0001',2);
            INSERT INTO fixture_delegation_roles VALUES('colaborador_local',31),('colaborador_dependencias',32);
            INSERT INTO fixture_units VALUES('A',null,'0001',2),('A3','A','0002',3),('A4','A3',null,4),('B',null,'0003',2),('B4','B',null,4);
            INSERT INTO fixture_people VALUES(50,'colaboradora@uacj.mx','Colaboradora sintética','A4','0050','Área sintética A4'),
              (60,'ajena@uacj.mx','Persona ajena sintética','B4','0060','Área ajena B4');
            INSERT INTO fixture_module_roles VALUES(31,9),(32,9);
            INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,desc_ur,num_empleado,id_ur_pertenece,nivel_ur,estatus_ur,presente)
              VALUES('A',2026,'100','Área sintética A','0001',null,2,'Activo',true),
              ('A3',2026,'110','Área sintética A3','0002','A',3,'Activo',true),
              ('A4',2026,'111','Área sintética A4',null,'A3',4,'Activo',true),
              ('B',2026,'200','Área sintética B','0003',null,2,'Activo',true);
            """);
    }
    internal async Task Sql(string sql, params object[] parameters)
    {
        await using var connection = new NpgsqlConnection(Admin); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter);
        await command.ExecuteNonQueryAsync();
    }
    internal async Task<object?> Scalar(string sql)
    {
        await using var connection = new NpgsqlConnection(Admin); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection); return await command.ExecuteScalarAsync();
    }
}
