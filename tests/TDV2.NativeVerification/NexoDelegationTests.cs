using Npgsql;
using System.Text.Json.Nodes;
namespace Tdv2.NativeVerification;

internal static class NexoDelegationTests
{
    internal static async Task<int> Run(NativeDatabase database)
    {
        await database.ValidateCluster();
        var contract = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(database.Artifacts, "nexo-generated.json")))!;
        await database.Sql(await File.ReadAllTextAsync(Path.Combine(database.Workspace, "tests/TDV2.NativeVerification/nexo-scope-fixture.sql")));
        foreach (var (name, sql) in contract["views"]!.AsObject()) await database.Sql($"CREATE VIEW nexo_{name} WITH (security_barrier=true) AS {sql!.GetValue<string>()}");
        foreach (var group in new[] { "functions", "representation" })
            foreach (var (name, sql) in contract[group]!.AsObject()) await database.Sql($"CREATE FUNCTION nexo_a47_{name}{sql!.GetValue<string>()}");
        var password = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        await database.Sql($"CREATE ROLE nexo_scope_client LOGIN PASSWORD '{password}'; REVOKE ALL ON ALL FUNCTIONS IN SCHEMA public FROM PUBLIC; GRANT USAGE ON SCHEMA public TO nexo_scope_client; GRANT SELECT ON nexo_delegacion,nexo_delegacion_roles TO nexo_scope_client; GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA public TO nexo_scope_client;");
        var connection = new NpgsqlConnectionStringBuilder(database.Admin) { Username = "nexo_scope_client", Password = password }.ConnectionString;
        async Task<object?> Call(string sql, params object[] args)
        {
            await using var db = new NpgsqlConnection(connection); await db.OpenAsync();
            await using var command = new NpgsqlCommand(sql, db);
            foreach (var arg in args) command.Parameters.AddWithValue(arg);
            return await command.ExecuteScalarAsync();
        }
        Task<object?> Grant(string actor, string root, string person, long role, string origin) => Call("SELECT concesion_id FROM nexo_a47_conceder_acceso($1,$2,$3,$4,$5)", actor, root, person, role, origin);
        async Task Denied(Func<Task<object?>> action)
        {
            try { await action(); } catch (PostgresException e) when (e.SqlState is "P0001" or "42501") { return; }
            throw new InvalidOperationException("Se esperaba rechazo de la función real de Nexo.");
        }
        void Check(bool valid) { if (!valid) throw new InvalidOperationException("Contrato Nexo inesperado."); }
        var results = new List<object>(); var failed = 0;
        async Task Test(string name, Func<Task> run)
        {
            try { await run(); results.Add(new { name, passed = true }); Console.WriteLine("PASS " + name); }
            catch (Exception e) { failed++; results.Add(new { name, passed = false, error = e.GetType().Name }); Console.WriteLine("FAIL " + name + ": " + e.GetType().Name); }
        }
        await Test("Nexo real: nivel 3 busca misma UR y descendientes, excluye rama ajena", async () =>
        {
            Check(Convert.ToInt64(await Call("SELECT count(*) FROM nexo_a47_buscar_personas('jefe3@uacj.mx','A3','persona',1)")) == 2);
            Check(Convert.ToInt64(await Call("SELECT count(*) FROM nexo_a47_buscar_personas('jefe3@uacj.mx','A3','ajena',1)")) == 0);
        });
        await Test("Nexo real: alta local idempotente, misma UR y subordinada; auditoría y retiro", async () =>
        {
            var id = await Grant("jefe3@uacj.mx", "A3", "persona@uacj.mx", 30, "A4");
            Check(Equals(id, await Grant("jefe3@uacj.mx", "A3", "persona@uacj.mx", 30, "A4")));
            await Grant("jefe3@uacj.mx", "A3", "misma@uacj.mx", 30, "A3");
            Check(Convert.ToInt64(await database.Scalar("SELECT count(*) FROM registro_actividad WHERE accion='crear'")) == 2);
            Check((bool)(await Call("SELECT retirada FROM nexo_a47_retirar_acceso('jefe3@uacj.mx','A3',$1)", id!))!);
            Check(!(bool)(await Call("SELECT retirada FROM nexo_a47_retirar_acceso('jefe3@uacj.mx','A3',$1)", id!))!);
        });
        await Test("Nexo real: rechaza global, dependencias nivel 3, rol no delegable y rama ajena", async () =>
        {
            foreach (var role in new long[] { 31, 32, 33 }) await Denied(() => Grant("jefe3@uacj.mx", "A3", "persona@uacj.mx", role, "A4"));
            await Denied(() => Grant("jefe3@uacj.mx", "A3", "ajena@uacj.mx", 30, "B"));
            await Denied(() => Grant("jefe3@uacj.mx", "B", "ajena@uacj.mx", 30, "B"));
            await Denied(() => Call("SELECT * FROM usuarios"));
        });
        await Test("Nexo real: nivel 2 conserva dependencias; nivel 3 no retira esa concesión", async () =>
        {
            var id = await Grant("jefe2@uacj.mx", "A", "persona@uacj.mx", 31, "A4");
            await Denied(() => Call("SELECT * FROM nexo_a47_retirar_acceso('jefe3@uacj.mx','A3',$1)", id!));
            await Call("SELECT * FROM nexo_a47_retirar_acceso('jefe2@uacj.mx','A',$1)", id!);
        });
        await Test("Nexo real: nuevo rol solo conserva responsabilidad y tipo 0 no delega", async () =>
        {
            await database.Sql("UPDATE roles SET clave='responsable_ur_supervisor' WHERE id=10");
            Check(Convert.ToInt64(await Call("SELECT count(*) FROM nexo_delegacion WHERE email='jefe3@uacj.mx'")) == 1);
            await database.Sql("UPDATE unidades_responsables_poa SET tipo_ur='0' WHERE id_ur='A3'");
            await Denied(() => Grant("jefe3@uacj.mx", "A3", "persona@uacj.mx", 30, "A4"));
            await database.Sql("UPDATE unidades_responsables_poa SET tipo_ur='1' WHERE id_ur='A3'");
        });
        await Test("Nexo real: representación comprueba sesión y aplica el nivel efectivo", async () =>
        {
            var session = Guid.NewGuid();
            await database.Sql("INSERT INTO sesiones_representacion VALUES($1,47,1,2,'nexo_scope_client',true,null,(clock_timestamp() AT TIME ZONE 'UTC')+interval '5 minutes')", session);
            await Call("SELECT concesion_id FROM nexo_a47_representar_conceder('jefe3@uacj.mx','A3','persona@uacj.mx',30,'A4','jefe2@uacj.mx',$1)", session);
            await Denied(() => Call("SELECT * FROM nexo_a47_representar_conceder('jefe3@uacj.mx','A3','persona@uacj.mx',31,'A4','jefe2@uacj.mx',$1)", session));
            await database.Sql("UPDATE sesiones_representacion SET expira_en=(clock_timestamp() AT TIME ZONE 'UTC')-interval '1 second'");
            await Denied(() => Call("SELECT * FROM nexo_a47_representar_conceder('jefe3@uacj.mx','A3','persona@uacj.mx',30,'A4','jefe2@uacj.mx',$1)", session));
        });
        await Test("Nexo real: publicación distingue central/aplicacion y retira concesiones revocadas o suspendidas", async () =>
        {
            await database.Sql($"CREATE VIEW nexo_concesiones WITH (security_barrier=true) AS {contract["grants"]!.GetValue<string>()}; GRANT SELECT ON nexo_concesiones TO nexo_scope_client;");
            await database.Sql("INSERT INTO concesiones_acceso(usuario_aplicacion_id,rol_id,id_ur,origen,otorgado_en) SELECT id,30,'A4','central',now() FROM usuario_aplicacion WHERE usuario_id=(SELECT id FROM usuarios WHERE email='persona@uacj.mx')");
            Check(Convert.ToInt64(await Call("SELECT count(*) FROM nexo_concesiones WHERE email='persona@uacj.mx' AND rol_id=30 AND origen='central'")) == 1);
            Check(Convert.ToInt64(await Call("SELECT count(*) FROM nexo_concesiones WHERE email='persona@uacj.mx' AND rol_id=30 AND origen='aplicacion'")) == 1);
            await database.Sql("UPDATE usuario_aplicacion SET suspendido=true WHERE usuario_id=(SELECT id FROM usuarios WHERE email='persona@uacj.mx')");
            Check(Convert.ToInt64(await Call("SELECT count(*) FROM nexo_concesiones WHERE email='persona@uacj.mx'")) == 0);
            await database.Sql("UPDATE usuario_aplicacion SET suspendido=false; UPDATE concesiones_acceso SET revocado_en=now() WHERE origen='central'");
            Check(Convert.ToInt64(await Call("SELECT count(*) FROM nexo_concesiones WHERE origen='central'")) == 0);
            Check(Convert.ToInt64(await Call("SELECT count(*) FROM nexo_concesiones WHERE email='persona@uacj.mx' AND rol_id=30 AND origen='aplicacion'")) == 1);
        });
        await Test("Nexo real: otra aplicación conserva roles y UR antes permitidos", async () =>
        {
            await database.Sql("UPDATE aplicaciones SET clave='otra'; UPDATE unidades_responsables_poa SET tipo_ur='0' WHERE id_ur='A3'");
            await Grant("jefe3@uacj.mx", "A3", "persona@uacj.mx", 32, "A4");
            Check(Convert.ToInt64(await Call("SELECT count(*) FROM nexo_delegacion WHERE email='jefe3@uacj.mx'")) == 1);
        });
        await File.WriteAllTextAsync(Path.Combine(database.Artifacts, "nexo-results.json"), System.Text.Json.JsonSerializer.Serialize(results, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Nexo: {results.Count - failed} passed, {failed} failed.");
        return failed == 0 ? 0 : 1;
    }
}
