using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Tdv2.Domain;
using Tdv2.Services;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    internal static JsonObject RetiredValidationFixture(FormSchema schema)
    {
        var content = StageOnly(schema);
        content["identificacion"]![0]!["validacion"] = "D";
        var second = content["identificacion"]![0]!.DeepClone();
        second["id"] = "segundo"; second["codigo"] = "PO-02"; second["validacion"] = "N";
        content["identificacion"]!.AsArray().Add(second);
        content["evaluaciones"]!["PO-02"] = content["evaluaciones"]!["PO-01"]!.DeepClone();
        return content;
    }
    private static void RegisterProcedureValidationCases(NativeDatabase db, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Edición/validación: nuevas asignaciones D/N rechazadas sin cambios parciales", async (app, client) =>
        {
            await SeedStage(db, StageOnly(app.Services.GetRequiredService<FormSchema>())); await Login(app, client); await Csrf(client);
            var tab = Guid.NewGuid(); var before = (await Live(client, tab))["contenido"]!;
            var lease = await Lease(client, tab, "identificacion:inicial");
            var added = await Lease(client, tab, "identificacion:nuevo");
            var header = await Lease(client, tab);
            foreach (var value in new[] { "D", "N" })
                foreach (var target in new[] { lease, added })
                {
                    var row = before["identificacion"]![0]!.DeepClone(); row["validacion"] = value;
                    if (target == added) { row["id"] = "nuevo"; row["codigo"] = ""; row["fuente"] = "Nuevo"; }
                    var response = await Patch(client, Edit(tab, target with { Value = row }, header with { Value = Header("No debe guardarse") }));
                    Check(response.StatusCode == HttpStatusCode.UnprocessableEntity);
                    Check((await Json(response))["validation"]![0]!["field"]!.ToString() == "validacion");
                    var after = await Live(client, tab);
                    Check(after["version"]!.GetValue<int>() == 0 && JsonNode.DeepEquals(before, after["contenido"]));
                }
        });
        Test("Edición/validación: D/N históricos permiten otros campos, quedan pendientes y se corrigen a V/A", async (app, client) =>
        {
            var content = RetiredValidationFixture(app.Services.GetRequiredService<FormSchema>());
            await SeedStage(db, content); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            for (var i = 0; i < 2; i++)
            {
                var row = content["identificacion"]![i]!.DeepClone(); row["resultado"] = "Respuesta modificada sin tocar validación";
                var lease = await Lease(client, tab, "identificacion:" + row["id"]);
                Check((await Patch(client, Edit(tab, lease with { Value = row }) with { Release = true })).IsSuccessStatusCode);
                content["identificacion"]![i] = row;
            }
            var header = await Lease(client, tab);
            var updatedHeader = Header("Edición independiente");
            Check((await Patch(client, Edit(tab, header with { Value = updatedHeader }) with { Release = true })).IsSuccessStatusCode);
            var pending = await Live(client, tab);
            Check(JsonNode.DeepEquals(content["identificacion"], pending["contenido"]!["identificacion"]));
            Check(pending["porcentajeEtapa"]!.GetValue<int>() < 100 && !pending["revisionEnvio"]!["listo"]!.GetValue<bool>());
            Check(pending["revisionEnvio"]!["pendientes"]!.AsArray().Count(p => p!["mensaje"]!.ToString().Contains("validación pendiente de actualizar")) == 2);
            Check(pending["procedimientosDisponibles"]!.AsArray().Count == 0);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), pending["version"]!.GetValue<int>()))).StatusCode == HttpStatusCode.UnprocessableEntity);
            for (var i = 0; i < 2; i++)
            {
                var row = content["identificacion"]![i]!.DeepClone(); var key = "identificacion:" + row["id"];
                var lease = await Lease(client, tab, key, 1);
                row["validacion"] = i == 0 ? "N" : "D";
                Check((await Patch(client, Edit(tab, lease with { Value = row }))).StatusCode == HttpStatusCode.UnprocessableEntity);
                row["validacion"] = i == 0 ? "V" : "A";
                Check((await Patch(client, Edit(tab, lease with { Value = row }) with { Release = true })).IsSuccessStatusCode);
                content["identificacion"]![i] = row;
            }
            var ready = await Live(client, tab);
            Check(ready["revisionEnvio"]!["listo"]!.GetValue<bool>() && ready["porcentajeEtapa"]!.GetValue<int>() == 100);
            Check(ready["procedimientosDisponibles"]!.AsArray().Count == 2);
            Check(JsonNode.DeepEquals(content["identificacion"], ready["contenido"]!["identificacion"]));
            foreach (var section in new[] { "sistemas", "datos", "evaluaciones", "preguntas", "acuerdos" })
                Check(JsonNode.DeepEquals(content[section], ready["contenido"]![section]));
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), ready["version"]!.GetValue<int>()))).IsSuccessStatusCode);
        });
        Test("Edición/validación: D/N heredados no autorizan duplicar filas y se eliminan directamente", async (app, client) =>
        {
            var content = RetiredValidationFixture(app.Services.GetRequiredService<FormSchema>());
            await SeedStage(db, content); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var lease = await Lease(client, tab, "identificacion:nuevo");
            foreach (var original in content["identificacion"]!.AsArray())
            {
                var copy = original!.DeepClone(); copy["id"] = "nuevo"; copy["codigo"] = ""; copy["fuente"] = "Nuevo";
                var response = await Patch(client, Edit(tab, lease with { Value = copy }));
                Check(response.StatusCode == HttpStatusCode.UnprocessableEntity);
                Check((await Json(response))["validation"]![0]!["field"]!.ToString() == "validacion");
            }
            var removal = await RemovalRequest(client, tab, new("identificacion", "segundo"));
            Check((await Patch(client, removal)).IsSuccessStatusCode);
            var after = (await Live(client, tab))["contenido"]!;
            Check(after["identificacion"]!.AsArray().Count == 1 && after["identificacion"]![0]!["validacion"]!.ToString() == "D");
        });
        foreach (var sent in new[] { false, true })
            Test("Edición/validación: D/N intactos y escritura bloqueada en " + (sent ? "enviados" : "ejercicio histórico"), async (app, client) =>
            {
                var content = RetiredValidationFixture(app.Services.GetRequiredService<FormSchema>());
                await SeedStage(db, content);
                if (sent) await db.Sql("""
                    UPDATE formatos_ur SET porcentaje=100,enviado_en=clock_timestamp(),enviado_por='persona@uacj.mx',
                      enviado_como='persona@uacj.mx',envio_id=gen_random_uuid(),
                      instantanea_envio=jsonb_build_object('unidad',jsonb_build_object('id_ur','A','cve_ur','100','desc_ur','Área sintética A'),
                        'contenido',contenido::jsonb,'version',version)
                    """);
                else await db.Sql("UPDATE formatos_ur SET ejercicio=2025,porcentaje=37");
                var original = await db.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f");
                await Login(app, client); await Csrf(client); var tab = Guid.NewGuid(); var live = await Live(client, tab);
                Check(!live["editable"]!.GetValue<bool>() && JsonNode.DeepEquals(content, live["contenido"]));
                var input = Edit(tab, new BlockRequest("identificacion:inicial", 0, Guid.NewGuid(), content["identificacion"]![0]));
                Check((await client.PostAsJsonAsync("/formatos/A/reservas", input)).StatusCode == HttpStatusCode.Conflict);
                Check((await Patch(client, input)).StatusCode == HttpStatusCode.Conflict);
                Check((await client.PostAsJsonAsync("/formatos/A/reservas", input with { Removal = new("identificacion", "inicial") })).StatusCode == HttpStatusCode.Conflict);
                Check(Equals(original, await db.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f")));
            });
    }
}
