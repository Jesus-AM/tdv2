using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Tdv2.Domain;
namespace Tdv2.NativeVerification;

internal static class CurrentRecipientsMigrationTests
{
    internal static async Task Run(string connection, string workspace, Func<string, string, Task<object?>> sql, Action<bool, string> check)
    {
        await using var db = MigrationTests.Context(connection);
        await db.GetService<IMigrator>().MigrateAsync("20261009032858_FormIldaExclusions");
        var schema = new FormSchema(await File.ReadAllTextAsync(Path.Combine(workspace, "tdv2/Contracts/procesos_operativos.json")));
        var original = NativeTests.Complete(schema);
        var template = original["identificacion"]![0]!.DeepClone(); var rows = new JsonArray();
        string[][] samples = [["Otro"], ["Externo"], ["Otro", "Externo"], ["Docentes", "Otro"], ["Estudiantes", "Externo"],
            ["Docentes", "Público en general"], ["Instituciones públicas externas", "Empresas y organizaciones privadas"]];
        for (var i = 0; i < samples.Length; i++)
        {
            var row = template.DeepClone(); row["id"] = "r" + i; row["codigo"] = "";
            row["usuario"] = new JsonArray(samples[i].Select(s => (JsonNode?)JsonValue.Create(s)).ToArray());
            if (i < 5) row["usuarioOtro"] = "Detalle anterior " + i;
            rows.Add(row);
        }
        original["identificacion"] = rows;
        var json = original.ToJsonString().Replace("'", "''");
        await sql(connection, $"""
            INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,nivel_ur) VALUES('DRAFT',2026,'06000',2),('SENT',2026,'07000',3),('HIST',2026,'08000',2);
            INSERT INTO formatos_ur(id_ur,ejercicio,contenido,version,porcentaje,actualizado_por)
                VALUES('DRAFT',2026,'{json}',7,100,'synthetic@example.test'),('HIST',2025,'{json}',3,71,'synthetic@example.test');
            INSERT INTO formatos_ur(id_ur,ejercicio,contenido,version,porcentaje,actualizado_por,enviado_en,instantanea_envio)
                VALUES('SENT',2026,'{json}',10,100,'synthetic@example.test',clock_timestamp(),'{json}');
            INSERT INTO formato_bloques(id_ur,bloque,version,revision_contexto,reserva_id,vence_en) VALUES
                ('DRAFT','identificacion:r0',4,0,gen_random_uuid(),clock_timestamp()+interval '1 hour'),
                ('DRAFT','medios',2,0,gen_random_uuid(),clock_timestamp()+interval '1 hour');
            """);
        var before = await sql(connection, "SELECT row_to_json(f)::text FROM formatos_ur f WHERE id_ur='DRAFT'");
        var immutable = await sql(connection, "SELECT jsonb_agg(to_jsonb(f) ORDER BY id_ur)::text FROM formatos_ur f WHERE id_ur<>'DRAFT'");
        await sql(connection, """
            CREATE FUNCTION fixture_fail_recipient_audit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'SYNTHETIC'; END $$;
            CREATE TRIGGER fail_recipient_audit BEFORE INSERT ON activity_logs FOR EACH ROW EXECUTE FUNCTION fixture_fail_recipient_audit();
            """);
        var rejected = false;
        try { await db.Database.MigrateAsync(); } catch (PostgresException) { rejected = true; }
        check(rejected && Equals(before, await sql(connection, "SELECT row_to_json(f)::text FROM formatos_ur f WHERE id_ur='DRAFT'"))
            && (await db.Database.GetAppliedMigrationsAsync()).Count() == 7, "destinatarios: fallo de auditoría revierte limpieza, versiones e historial");
        await sql(connection, "DROP TRIGGER fail_recipient_audit ON activity_logs; DROP FUNCTION fixture_fail_recipient_audit()");
        await db.Database.MigrateAsync();
        var expected = original.DeepClone().AsObject();
        foreach (var row in expected["identificacion"]!.AsArray())
        {
            row!.AsObject().Remove("usuarioOtro");
            row["usuario"] = new JsonArray(row["usuario"]!.AsArray().Where(v => v!.ToString() is not ("Otro" or "Externo")).Select(v => v!.DeepClone()).ToArray());
        }
        var stored = JsonNode.Parse((string)(await sql(connection, "SELECT contenido::text FROM formatos_ur WHERE id_ur='DRAFT'"))!)!;
        check(JsonNode.DeepEquals(expected, stored), "destinatarios: cinco combinaciones, conserva opciones válidas, campos, filas y prioridades; elimina detalle sin compatibilidad");
        check(Equals(immutable, await sql(connection, "SELECT jsonb_agg(to_jsonb(f)-'etapa_activa' ORDER BY id_ur)::text FROM formatos_ur f WHERE id_ur<>'DRAFT'")),
            "destinatarios: enviados, instantáneas y ejercicio histórico permanecen idénticos");
        check(Convert.ToInt32(await sql(connection, "SELECT porcentaje FROM formatos_ur WHERE id_ur='DRAFT'")) == 81
            && !FormSchema.HasRecipients(stored["identificacion"]![0]) && FormCapture.Progress(expected) < 100,
            "destinatarios: migración histórica conserva cálculo de 81; la revisión vigente mantiene el vacío pendiente");
        check((bool)(await sql(connection, """
            SELECT (SELECT version=8 FROM formatos_ur WHERE id_ur='DRAFT')
              AND (SELECT version=5 AND reserva_id IS NULL AND vence_en IS NULL FROM formato_bloques WHERE id_ur='DRAFT' AND bloque='identificacion:r0')
              AND (SELECT version=1 FROM formato_bloques WHERE id_ur='DRAFT' AND bloque='identificacion:r4')
              AND (SELECT version=2 AND reserva_id IS NOT NULL FROM formato_bloques WHERE id_ur='DRAFT' AND bloque='medios')
              AND NOT EXISTS(SELECT 1 FROM formato_bloques WHERE id_ur='DRAFT' AND bloque='identificacion:r5')
            """))!, "destinatarios: invalida sólo versiones y reservas de filas limpiadas");
        await sql(connection, """UPDATE formatos_ur SET contenido=jsonb_set(contenido::jsonb,'{identificacion,0,usuario}','["Público en general","Docentes"]')::json WHERE id_ur='DRAFT'""");
        var after = await sql(connection, "SELECT row_to_json(f)::text FROM formatos_ur f WHERE id_ur='DRAFT'");
        await db.Database.MigrateAsync();
        await sql(connection, db.GetService<IMigrator>().GenerateScript("20261009032858_FormIldaExclusions", options: MigrationsSqlGenerationOptions.Idempotent));
        check(Equals(after, await sql(connection, "SELECT row_to_json(f)::text FROM formatos_ur f WHERE id_ur='DRAFT'"))
            && Convert.ToInt32(await sql(connection, "SELECT count(*) FROM activity_logs WHERE action='actualizar_destinatarios'")) == 1,
            "destinatarios: comando y SQL idempotentes no repiten limpieza ni borran nuevas selecciones");
    }
}
