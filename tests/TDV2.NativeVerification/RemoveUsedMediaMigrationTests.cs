using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
namespace Tdv2.NativeVerification;

internal static class RemoveUsedMediaMigrationTests
{
    internal static async Task Run(string connection, Func<string, string, Task<object?>> sql, Action<bool, string> check)
    {
        await using var db = MigrationTests.Context(connection);
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261015165555_LocalApplicationLists");
        await sql(connection, """
            INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur) VALUES('D',2026,'1'),('H',2025,'2'),('S',2026,'3'),('E',2026,'4');
            INSERT INTO formatos_ur(id_ur,ejercicio,contenido,version,porcentaje,actualizado_por,created_at,updated_at)
              SELECT id_ur,ejercicio,'{"medios":{"Correo":true,"Otro":false},"medioOtro":"Retirar sólo esto","identificacion":[{"id":"p","codigo":"PO-01"}],"sistemas":[{"id":"s","sistema":"otra","sistemaOtro":"Conservar","fallas":"medios y medioOtro son palabras libres"}],"datos":[{"medios":"propiedad ajena anidada"}],"nulo":null,"vacio":"","evaluaciones":{}}',7,37,'synthetic@example.test',now(),now()
              FROM unidades_responsables_poa;
            UPDATE formatos_ur SET instantanea_envio=jsonb_build_object('contenido',contenido,'porcentaje',37,'autor','Conservar'),enviado_en=clock_timestamp() WHERE id_ur='S';
            UPDATE formatos_ur SET instantanea_envio=contenido::jsonb,enviado_en=clock_timestamp() WHERE id_ur='H';
            INSERT INTO formato_envios_etapas(id_ur,etapa,ejercicio,version,operacion,enviado_en,actor,efectivo,nombre,instantanea)
              SELECT id_ur,1,ejercicio,version,gen_random_uuid(),clock_timestamp(),'actor@example.test','effective@example.test','Autor',jsonb_build_object('contenido',contenido,'porcentaje',37) FROM formatos_ur WHERE id_ur='E';
            INSERT INTO formato_bloques(id_ur,bloque,version,revision_contexto,reserva_id,vence_en)
              VALUES('D','medios',2,0,gen_random_uuid(),clock_timestamp()+interval '1 hour'),('D','sistemas:s',4,0,gen_random_uuid(),clock_timestamp()+interval '1 hour');
            INSERT INTO formato_operaciones(id_ur,operacion,sesion,revision_contexto,pestana,huella,respuesta,creado_en)
              VALUES('D',gen_random_uuid(),'synthetic',0,gen_random_uuid(),'synthetic','{"version":7,"porcentaje":37,"entrega":{"resumen":{"medios":["Correo"],"herramientas":1}},"bloques":[{"key":"medios","version":2,"value":{"medios":{"Otro":true},"medioOtro":"Retirar"}},{"key":"sistemas:s","version":4,"value":{"sistemaOtro":"Conservar"}}]}',clock_timestamp());
            """);
        async Task<JsonNode> Snapshot() => JsonNode.Parse((string)(await sql(connection, """
            SELECT jsonb_build_object('forms',(SELECT jsonb_agg(to_jsonb(f) ORDER BY id_ur) FROM formatos_ur f),
                'stages',(SELECT jsonb_agg(to_jsonb(s)) FROM formato_envios_etapas s),
                'blocks',(SELECT jsonb_agg(to_jsonb(b) ORDER BY bloque) FROM formato_bloques b),
                'receipts',(SELECT jsonb_agg(to_jsonb(o)) FROM formato_operaciones o))::text
            """))!)!;
        var before = await Snapshot();
        await sql(connection, """
            CREATE FUNCTION fixture_fail_media() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'SYNTHETIC'; END $$;
            CREATE TRIGGER fail_media BEFORE UPDATE ON formato_operaciones FOR EACH ROW EXECUTE FUNCTION fixture_fail_media();
            """);
        var rejected = false;
        try { await db.Database.MigrateAsync(); } catch (PostgresException) { rejected = true; }
        check(rejected && JsonNode.DeepEquals(before, await Snapshot()) && (await db.Database.GetPendingMigrationsAsync()).Count() == 1,
            "medios: fallo tardío revierte documentos, instantáneas, reservas, recibos e historial");
        check(Convert.ToInt32(await sql(connection, "SELECT count(*) FROM pg_trigger WHERE tgname IN ('formato_enviado_inmutable','respuestas_etapas_inmutables','envio_etapa_inmutable') AND tgenabled='O'")) == 3,
            "medios: rollback restaura los tres triggers de inmutabilidad");
        await sql(connection, "DROP TRIGGER fail_media ON formato_operaciones; DROP FUNCTION fixture_fail_media()");
        await db.Database.MigrateAsync();
        var expected = before.DeepClone();
        void Clean(JsonNode? node) { if (node is JsonObject obj) { obj.Remove("medios"); obj.Remove("medioOtro"); } }
        foreach (var f in expected["forms"]!.AsArray()) { Clean(f!["contenido"]); Clean(f["instantanea_envio"]); Clean(f["instantanea_envio"]?["contenido"]); }
        foreach (var s in expected["stages"]!.AsArray()) Clean(s!["instantanea"]!["contenido"]);
        expected["blocks"]!.AsArray().RemoveAt(0); // Orden: medios, sistemas:s.
        var receipt = expected["receipts"]![0]!["respuesta"]!;
        receipt["entrega"]!["resumen"]!.AsObject().Remove("medios"); receipt["bloques"]!.AsArray().RemoveAt(0);
        check(JsonNode.DeepEquals(expected, await Snapshot()), "medios: sólo rutas explícitas retiradas; resto de respuestas, versiones, porcentajes, autores, fechas y reservas idéntico");
        await db.Database.MigrateAsync(); await sql(connection, migrator.GenerateScript("20261015165555_LocalApplicationLists", options: MigrationsSqlGenerationOptions.Idempotent));
        check(JsonNode.DeepEquals(expected, await Snapshot()), "medios: repetición EF y script idempotente conservan el resultado");
        foreach (var command in new[] { "UPDATE formatos_ur SET porcentaje=2 WHERE id_ur='S'", "UPDATE formatos_ur SET contenido='{}' WHERE id_ur='E'", "DELETE FROM formato_envios_etapas" })
        {
            rejected = false; try { await sql(connection, command); } catch (PostgresException) { rejected = true; }
            check(rejected, "medios: inmutabilidad de enviados permanece activa tras limpiar");
        }
        rejected = false; try { await migrator.MigrateAsync("20261015165555_LocalApplicationLists"); } catch (PostgresException) { rejected = true; }
        check(rejected && JsonNode.DeepEquals(expected, await Snapshot()), "medios: Down exige respaldo y no simula recuperar respuestas");
    }
}
