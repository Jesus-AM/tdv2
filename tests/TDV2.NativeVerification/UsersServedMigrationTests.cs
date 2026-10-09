using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Tdv2.Domain;

namespace Tdv2.NativeVerification;

internal static class UsersServedMigrationTests
{
    internal static async Task Run(string connection, string workspace, Func<string, string, Task<object?>> sql, Action<bool, string> check)
    {
        await using var db = MigrationTests.Context(connection);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261007161549_ParticipantPhotographs");
        var schema = new FormSchema(await File.ReadAllTextAsync(Path.Combine(workspace, "tdv2/Contracts/procesos_operativos.json")));
        var original = NativeTests.Complete(schema);
        original["identificacion"]![0]!["usuario"] = "Valor libre anterior";
        original["identificacion"]![0]!["prioridad"] = "4";
        var second = original["identificacion"]![0]!.DeepClone(); second["id"] = "segunda"; second["codigo"] = ""; second["usuario"] = null; second["prioridad"] = "17";
        var current = second.DeepClone(); current["id"] = "nueva"; current["usuario"] = new JsonArray("Docentes", "Estudiantes");
        original["identificacion"]!.AsArray().Add(second); original["identificacion"]!.AsArray().Add(current);
        var json = original.ToJsonString().Replace("'", "''");
        await sql(connection, $"""
            INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,nivel_ur) VALUES('DRAFT',2026,'06000',2),('SENT',2026,'07000',3),('HIST',2026,'08000',2);
            INSERT INTO formatos_ur(id_ur,ejercicio,contenido,version,porcentaje,actualizado_por)
                VALUES('DRAFT',2026,'{json}',7,100,'synthetic@example.test');
            INSERT INTO formatos_ur(id_ur,ejercicio,contenido,version,porcentaje,actualizado_por,enviado_en,instantanea_envio)
                VALUES('SENT',2026,'{json}',10,100,'synthetic@example.test',clock_timestamp(),'{json}');
            INSERT INTO formatos_ur(id_ur,ejercicio,contenido,version,porcentaje,actualizado_por)
                VALUES('HIST',2025,'{json}',5,100,'synthetic@example.test');
            INSERT INTO formato_bloques(id_ur,bloque,version,revision_contexto,reserva_id,vence_en) VALUES
                ('DRAFT','identificacion:inicial',4,0,gen_random_uuid(),clock_timestamp()+interval '1 hour'),
                ('DRAFT','medios',2,0,gen_random_uuid(),clock_timestamp()+interval '1 hour'),
                ('SENT','identificacion:inicial',3,0,gen_random_uuid(),clock_timestamp()+interval '1 hour');
            """);
        var submitted = await sql(connection, "SELECT (to_jsonb(f)-'etapa_activa')::text FROM formatos_ur f WHERE id_ur='SENT'");
        var historical = await sql(connection, "SELECT (to_jsonb(f)-'etapa_activa')::text FROM formatos_ur f WHERE id_ur='HIST'");
        var sentBlock = await sql(connection, "SELECT row_to_json(b)::text FROM formato_bloques b WHERE id_ur='SENT'");
        var draftBefore = await sql(connection, "SELECT (to_jsonb(f)-'etapa_activa')::text FROM formatos_ur f WHERE id_ur='DRAFT'");
        await sql(connection, """
            CREATE FUNCTION fixture_fail_users_audit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'SYNTHETIC'; END $$;
            CREATE TRIGGER fail_users_audit BEFORE INSERT ON activity_logs FOR EACH ROW EXECUTE FUNCTION fixture_fail_users_audit();
            """);
        bool rejected = false;
        try { await db.Database.MigrateAsync(); } catch (PostgresException) { rejected = true; }
        check(rejected && Equals(draftBefore, await sql(connection, "SELECT (to_jsonb(f)-'etapa_activa')::text FROM formatos_ur f WHERE id_ur='DRAFT'"))
            && (await db.Database.GetAppliedMigrationsAsync()).Count() == 5, "usuarios: fallo de auditoría revierte contenido, versiones e historial EF");
        await sql(connection, "DROP TRIGGER fail_users_audit ON activity_logs; DROP FUNCTION fixture_fail_users_audit()");
        await db.Database.MigrateAsync();
        var expected = original.DeepClone();
        expected["identificacion"]![0]!["usuario"] = new JsonArray(); expected["identificacion"]![1]!["usuario"] = new JsonArray();
        var stored = JsonNode.Parse((string)(await sql(connection, "SELECT contenido::text FROM formatos_ur WHERE id_ur='DRAFT'"))!)!;
        check(JsonNode.DeepEquals(expected, stored), "usuarios: limpia sólo campo anterior y conserva todas las filas, prioridades 4/17, listas nuevas y demás respuestas");
        check(Equals(submitted, await sql(connection, "SELECT (to_jsonb(f)-'etapa_activa')::text FROM formatos_ur f WHERE id_ur='SENT'"))
            && Equals(sentBlock, await sql(connection, "SELECT row_to_json(b)::text FROM formato_bloques b WHERE id_ur='SENT'")),
            "usuarios: enviado, instantánea, porcentaje y reservas inmutables");
        check(Equals(historical, await sql(connection, "SELECT (to_jsonb(f)-'etapa_activa')::text FROM formatos_ur f WHERE id_ur='HIST'")),
            "usuarios: conserva borrador histórico no editable por cambio de ejercicio");
        check(Convert.ToInt32(await sql(connection, "SELECT porcentaje FROM formatos_ur WHERE id_ur='DRAFT'")) == 96
            && Convert.ToInt32(await sql(connection, "SELECT version FROM formatos_ur WHERE id_ur='DRAFT'")) == 8, "usuarios: conserva avance histórico de 96 y aumenta versión del formato");
        check((bool)(await sql(connection, """
            SELECT (SELECT version=5 AND reserva_id IS NULL AND vence_en IS NULL FROM formato_bloques WHERE id_ur='DRAFT' AND bloque='identificacion:inicial')
              AND (SELECT version=1 FROM formato_bloques WHERE id_ur='DRAFT' AND bloque='identificacion:segunda')
              AND (SELECT version=2 AND reserva_id IS NOT NULL FROM formato_bloques WHERE id_ur='DRAFT' AND bloque='medios')
              AND NOT EXISTS(SELECT 1 FROM formato_bloques WHERE id_ur='DRAFT' AND bloque='identificacion:nueva')
            """))!, "usuarios: invalida reserva/versiones antiguas únicamente de los bloques modificados");
        await sql(connection, """UPDATE formatos_ur SET contenido=jsonb_set(contenido::jsonb,'{identificacion,0,usuario}','["Otro","Comunidad universitaria"]')::json WHERE id_ur='DRAFT'""");
        var after = await sql(connection, "SELECT (to_jsonb(f)-'etapa_activa')::text FROM formatos_ur f WHERE id_ur='DRAFT'");
        await db.Database.MigrateAsync();
        var script = migrator.GenerateScript("20261007161549_ParticipantPhotographs", options: MigrationsSqlGenerationOptions.Idempotent);
        await sql(connection, script);
        check(Equals(after, await sql(connection, "SELECT (to_jsonb(f)-'etapa_activa')::text FROM formatos_ur f WHERE id_ur='DRAFT'"))
            && Convert.ToInt32(await sql(connection, "SELECT count(*) FROM activity_logs WHERE action='migrar_usuarios_atendidos'")) == 1,
            "usuarios: repetir comando y script idempotente no limpia selecciones nuevas ni duplica auditoría");
    }
}
