using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Services;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    private static JsonObject CoveredFixture(NativeApplication app)
    {
        var content = Complete(app);
        var tool = content["sistemas"]![0]!; tool["sistema"] = "excel"; tool["uso"] = "consultar"; tool["estado"] = "bien";
        return content;
    }
    private static void RegisterFormIntegrityCases(NativeDatabase db, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Edición/integridad: herramientas opcionales, filas incompletas, varias asociaciones y envío sin herramientas", async (app, client) =>
        {
            var content = CoveredFixture(app);
            var procedure = content["identificacion"]![0]!.DeepClone(); procedure["id"] = "segundo"; procedure["codigo"] = "PO-02";
            procedure["tramite"] = "Segundo procedimiento completo"; content["identificacion"]!.AsArray().Add(procedure);
            await SeedStage(db, content); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var live = await Live(client, tab);
            Check(live["porcentaje"]!.GetValue<int>() == 100 && live["entrega"]!["revision"]!["listo"]!.GetValue<bool>());
            Check(live["contenido"]!["sistemas"]!.AsArray().Count == 1);
            // Otro registro del mismo procedimiento, iniciado pero incompleto.
            var tool = content["sistemas"]![0]!.DeepClone(); tool["id"] = "segunda"; tool["sistema"] = "limesurvey"; tool["uso"] = "";
            var lease = await Lease(client, tab, "sistemas:segunda");
            Check((await Patch(client, Edit(tab, lease with { Value = tool }) with { Release = true })).IsSuccessStatusCode);
            live = await Live(client, tab);
            Check(live["porcentaje"]!.GetValue<int>() == 95);
            Check(live["entrega"]!["revision"]!["pendientes"]!.AsArray().Single()!["campo"]!.ToString() == "uso");
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), live["version"]!.GetValue<int>()))).StatusCode == HttpStatusCode.UnprocessableEntity);
            tool["uso"] = "consultar"; lease = await Lease(client, tab, "sistemas:segunda", 1);
            Check((await Patch(client, Edit(tab, lease with { Value = tool }) with { Release = true })).IsSuccessStatusCode);
            live = await Live(client, tab);
            Check(live["porcentaje"]!.GetValue<int>() == 100 && live["contenido"]!["sistemas"]!.AsArray().All(r => r!["proceso"]!.ToString() == "PO-01"));
            foreach (var id in new[] { "segunda", "inicial" })
                Check((await Patch(client, await RemovalRequest(client, tab, new("sistemas", id)))).IsSuccessStatusCode);
            live = await Live(client, tab);
            Check(live["contenido"]!["sistemas"]!.AsArray().Count == 0 && live["porcentaje"]!.GetValue<int>() == 100);
            Check(live["entrega"]!["revision"]!["pendientes"]!.AsArray().Count == 0);
            Check(Convert.ToInt32(await db.Scalar("SELECT porcentaje FROM formatos_ur WHERE id_ur='A'")) == 100);
            var index = (await Json(await client.GetAsync("/inicio")))["props"]!["formatos"]!.AsArray().Single(f => f!["id_ur"]!.ToString() == "A")!;
            Check(index["porcentaje"]!.GetValue<int>() == 100);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), live["version"]!.GetValue<int>()))).IsSuccessStatusCode);
            Check(!(await Live(client, tab))["editable"]!.GetValue<bool>());
            Check(JsonNode.Parse((string)(await db.Scalar("SELECT instantanea::text FROM formato_envios_etapas"))!)!["contenido"]!["sistemas"]!.AsArray().Count == 0);
        });
        Test("Edición/integridad: nuevo formato no crea herramientas al leer ni reservar identificación", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            Check((await Live(client, tab))["contenido"]!["sistemas"]!.AsArray().Count == 0);
            await Lease(client, tab, "identificacion:inicial");
            Check((await Live(client, tab))["contenido"]!["sistemas"]!.AsArray().Count == 0);
            Check(Convert.ToInt32(await db.Scalar("SELECT json_array_length(contenido->'sistemas') FROM formatos_ur WHERE id_ur='A'")) == 0);
        });
        Test("Edición/integridad: retiro relacionado atómico, conflicto, renombre y respuestas conservadas", async (app, client) =>
        {
            var content = CoveredFixture(app); var extra = content["sistemas"]![0]!.DeepClone(); extra["id"] = "otra";
            extra["fallas"] = "Comentarios que deben permanecer"; content["sistemas"]!.AsArray().Add(extra);
            await SeedStage(db, content); await Login(app, client); await Csrf(client);
            using var other = app.Client(); await Login(app, other); await Csrf(other);
            var a = Guid.NewGuid(); var b = Guid.NewGuid(); var target = new RowRemoval("identificacion", "inicial");
            var stalePlan = FormRemoval.Changes(content, target);
            var held = await Lease(other, b, "sistemas:otra");
            var denied = await client.PostAsJsonAsync("/formatos/A/reservas", Edit(a, stalePlan.Keys.Select(k => new BlockRequest(k, 0)).ToArray()) with { Removal = target });
            Check(denied.StatusCode == HttpStatusCode.Conflict);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM formato_bloques WHERE titular IS NOT NULL AND bloque='identificacion:inicial' AND reserva_id IS NOT NULL")) == 0);
            extra["fallas"] = "Respuesta de la otra sesión";
            Check((await Patch(other, Edit(b, held with { Value = extra }) with { Release = true })).IsSuccessStatusCode);
            var proc = content["identificacion"]![0]!.DeepClone(); proc["tramite"] = "Nombre actualizado";
            var procLease = await Lease(client, a, "identificacion:inicial");
            Check((await Patch(client, Edit(a, procLease with { Value = proc }) with { Release = true })).IsSuccessStatusCode);
            Check((await Live(client, a))["contenido"]!["sistemas"]!.AsArray().All(r => r!["proceso"]!.ToString() == "PO-01"));
            var removal = await RemovalRequest(client, a, target);
            var stale = removal with { Blocks = removal.Blocks.Select(r => r.Key == "sistemas:otra" ? r with { Value = stalePlan[r.Key] } : r).ToArray() };
            var before = await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'");
            Check((await Patch(client, stale)).StatusCode == HttpStatusCode.Conflict);
            Check(Equals(before, await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'")));
            Check((await Patch(client, removal)).IsSuccessStatusCode);
            var live = await Live(client, a); var tools = live["contenido"]!["sistemas"]!.AsArray();
            Check(tools.Count == 2 && tools.All(r => r!["proceso"]!.ToString() == ""));
            Check(tools.Single(r => r!["id"]!.ToString() == "otra")!["fallas"]!.ToString() == "Respuesta de la otra sesión");
            Check(live["entrega"]!["revision"]!["pendientes"]!.AsArray().Count(p => p!["campo"]!.ToString() == "proceso") == 2);
            Check((await Patch(client, removal)).IsSuccessStatusCode); // Recibo idempotente incluso después del retiro.
            Check((await other.PostAsJsonAsync("/formatos/A/reservas/actividad", Edit(b, held))).StatusCode == HttpStatusCode.Conflict);
        });
        Test("Edición/integridad: conexiones independientes bloquean y mezclan filas sin sobrescritura", async (app, client) =>
        {
            var content = CoveredFixture(app); var extra = content["sistemas"]![0]!.DeepClone(); extra["id"] = "otra";
            content["sistemas"]!.AsArray().Add(extra);
            await SeedStage(db, content); await Login(app, client); await Csrf(client);
            using var other = app.Client(); await Login(app, other); await Csrf(other);
            var a = Guid.NewGuid(); var b = Guid.NewGuid(); var first = await Lease(client, a, "sistemas:inicial"); var second = await Lease(other, b, "sistemas:otra");
            var row = content["sistemas"]![0]!.DeepClone(); row["fallas"] = "Guardado por sesión A"; extra["fallas"] = "Guardado por sesión B";
            var requestA = Edit(a, first with { Value = row }) with { Release = true };
            var requestB = Edit(b, second with { Value = extra }) with { Release = true };
            await using var blocker = new NpgsqlConnection(db.Admin); await blocker.OpenAsync(); await using var tx = await blocker.BeginTransactionAsync();
            await using (var command = new NpgsqlCommand("SELECT id_ur FROM unidades_responsables_poa WHERE id_ur='A' FOR UPDATE", blocker, tx)) await command.ExecuteScalarAsync();
            var writes = new[] { Patch(client, requestA), Patch(other, requestB) };
            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (Convert.ToInt32(await db.Scalar("SELECT count(*) FROM pg_stat_activity WHERE usename='tdv2_native_app' AND wait_event_type='Lock'")) < 2)
            { Check(DateTime.UtcNow < deadline, "No se observaron dos conexiones bloqueadas en PostgreSQL."); await Task.Delay(20); }
            Check(writes.All(t => !t.IsCompleted)); await tx.CommitAsync();
            Check((await Task.WhenAll(writes)).All(r => r.IsSuccessStatusCode));
            var live = await Live(client, a); var tools = live["contenido"]!["sistemas"]!.AsArray();
            Check(tools[0]!["fallas"]!.ToString() == "Guardado por sesión A" && tools[1]!["fallas"]!.ToString() == "Guardado por sesión B");
            Check(live["version"]!.GetValue<int>() == 2);
            Check((await Patch(client, requestA)).IsSuccessStatusCode && (await Patch(other, requestB)).IsSuccessStatusCode);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM formato_operaciones")) == 2);
            Check((await Patch(client, requestA with { OperationId = Guid.NewGuid() })).StatusCode == HttpStatusCode.Conflict);
            var staleLease = await Lease(client, a, "sistemas:inicial", 1);
            Check((await other.PostAsJsonAsync("/formatos/A/reservas", Edit(b, new BlockRequest("sistemas:inicial", 1)))).StatusCode == HttpStatusCode.Conflict);
            await db.Sql("UPDATE formato_bloques SET vence_en=clock_timestamp()-interval '1 second' WHERE bloque='sistemas:inicial'");
            var reassigned = await Lease(other, b, "sistemas:inicial", 1);
            row["fallas"] = "Atrasado";
            Check((await Patch(client, Edit(a, staleLease with { Value = row }))).StatusCode == HttpStatusCode.Conflict);
            row["fallas"] = "Vigente";
            Check((await Patch(other, Edit(b, reassigned with { Value = row }) with { Release = true })).IsSuccessStatusCode);
            Check((await Live(client, a))["contenido"]!["sistemas"]![0]!["fallas"]!.ToString() == "Vigente");
        });
        Test("Edición/integridad: JSON anterior se lee intacto, se versiona al guardar y snapshot no cambia", async (app, client) =>
        {
            var content = CoveredFixture(app); content.Remove(FormDocument.Field);
            await SeedStage(db, content); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var before = await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'");
            Check((await Live(client, tab))["contenido"]![FormDocument.Field] is null);
            await Form(client); Check(Equals(before, await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'")));
            var lease = await Lease(client, tab, "sistemas:inicial");
            Check(Equals(before, await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'")));
            var row = content["sistemas"]![0]!.DeepClone(); row["fallas"] = "Nuevo comentario";
            await db.Sql(FailureTrigger);
            try { Check((await Patch(client, Edit(tab, lease with { Value = row }) with { Release = true })).StatusCode == HttpStatusCode.ServiceUnavailable); }
            finally { await db.Sql("DROP TRIGGER fail_save ON formatos_ur; DROP FUNCTION fixture_fail_save()"); }
            Check(Equals(before, await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'")));
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM formato_operaciones")) == 0);
            Check((await Patch(client, Edit(tab, lease with { Value = row }) with { Release = true })).IsSuccessStatusCode);
            var live = await Live(client, tab); Check(live["contenido"]![FormDocument.Field]!.GetValue<int>() == 1);
            var expected = content.DeepClone(); expected[FormDocument.Field] = 1; expected["sistemas"]![0]!["fallas"] = "Nuevo comentario";
            Check(JsonNode.DeepEquals(expected, live["contenido"]));
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 1))).IsSuccessStatusCode);
            var frozen = await db.Scalar("SELECT instantanea::text FROM formato_envios_etapas");
            Check(JsonNode.Parse((string)frozen!)![FormDocument.Field]!.GetValue<int>() == 1);
            await Live(client, tab); await Form(client);
            Check((await Patch(client, Edit(tab, lease with { Version = 1, Value = row }))).StatusCode == HttpStatusCode.Conflict);
            Check(Equals(frozen, await db.Scalar("SELECT instantanea::text FROM formato_envios_etapas")));
        });
        Test("Edición/integridad: enviado anterior sin versión de estructura conserva contenido y snapshot", async (app, client) =>
        {
            var content = CoveredFixture(app); content.Remove(FormDocument.Field);
            await SeedStage(db, content);
            await db.Sql("UPDATE formatos_ur SET enviado_en=clock_timestamp(),enviado_por='persona@uacj.mx',instantanea_envio=jsonb_build_object('contenido',contenido,'version',version),porcentaje=37 WHERE id_ur='A'");
            var original = await db.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f WHERE id_ur='A'");
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var live = await Live(client, tab); await Form(client);
            Check(live["porcentaje"]!.GetValue<int>() == 37 && !live["editable"]!.GetValue<bool>());
            Check(JsonNode.DeepEquals(content, live["contenido"]));
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(tab, new BlockRequest("sistemas:inicial", 0)))).StatusCode == HttpStatusCode.Conflict);
            Check(Equals(original, await db.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f WHERE id_ur='A'")));
        });
    }
}
