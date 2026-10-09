using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Tdv2.Domain;
using Tdv2.Services;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    internal static JsonObject StageOnly(FormSchema schema)
    {
        var content = Complete(schema);
        var blank = schema.Blank(new("A", "100", "Área sintética A", 2, null, 2026));
        foreach (var section in new[] { "datos", "preguntas", "acuerdos" }) content[section] = blank[section]!.DeepClone();
        foreach (var row in content["evaluaciones"]!["PO-01"]!.AsArray()) { row!["valor"] = ""; row["obs"] = ""; }
        content["preguntas"]![0]!["respuesta"] = "Respuesta posterior que debe conservarse";
        return content;
    }
    private static void RegisterDeliveryCases(NativeDatabase db, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Edición/entrega: responsable y administrador envían sólo primera etapa sin alterar respuestas posteriores", async (app, client) =>
        {
            var content = StageOnly(app.Services.GetRequiredService<FormSchema>());
            await SeedStage(db, content); await Login(app, client); await Csrf(client);
            var tab = Guid.NewGuid(); var live = await Live(client, tab);
            Check(live["puedeEnviar"]!.GetValue<bool>() && live["revisionEnvio"]!["listo"]!.GetValue<bool>());
            Check(live["porcentaje"]!.GetValue<int>() == 100 && live["porcentajeEtapa"]!.GetValue<int>() == 100);
            await FinalStage(db); // Mostrar las secciones posteriores no cambia ningún requisito.
            var admin = await Live(client, tab); Check(JsonNode.DeepEquals(live["revisionEnvio"], admin["revisionEnvio"]));
            await db.Sql("DELETE FROM fixture_roles WHERE rol_clave='administrador'");
            var send = await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), 0));
            Check(send.IsSuccessStatusCode, "Envío primera etapa HTTP " + (int)send.StatusCode);
            var stored = JsonNode.Parse((string)(await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'"))!);
            Check(JsonNode.DeepEquals(content, stored));
            Check((await client.PostAsJsonAsync("/formatos/A/reservas", Edit(tab, new BlockRequest("sistemas:inicial", 0)))).StatusCode == HttpStatusCode.Conflict);
            Check((await Patch(client, Edit(tab, new BlockRequest("sistemas:inicial", 0, Guid.NewGuid(), content["sistemas"]![0])))).StatusCode == HttpStatusCode.Conflict);
        });
        Test("Edición/entrega: elegibilidad única, cambios concurrentes y vínculos históricos pendientes", async (app, client) =>
        {
            var content = StageOnly(app.Services.GetRequiredService<FormSchema>());
            await SeedStage(db, content); await Login(app, client); await Csrf(client);
            using var other = app.Client(); await Login(app, other); await Csrf(other);
            var tab = Guid.NewGuid(); var otherTab = Guid.NewGuid();
            Check((await Live(client, tab))["procedimientosDisponibles"]!.AsArray().Count == 1);
            var procedure = content["identificacion"]![0]!.DeepClone(); procedure["resultado"] = "";
            var reserve = await Lease(other, otherTab, "identificacion:inicial");
            Check((await Patch(other, Edit(otherTab, reserve with { Value = procedure }) with { Release = true })).IsSuccessStatusCode);
            var after = await Live(client, tab); Check(after["procedimientosDisponibles"]!.AsArray().Count == 0);
            Check(after["revisionEnvio"]!["pendientes"]!.AsArray().Any(p => p!["bloque"]!.ToString() == "sistemas:inicial" && p["campo"]!.ToString() == "proceso"));
            var old = content["sistemas"]![0]!.DeepClone(); old["fallas"] = "Sólo comentario";
            var rowLease = await Lease(client, tab, "sistemas:inicial");
            Check((await Patch(client, Edit(tab, rowLease with { Value = old }) with { Release = true })).IsSuccessStatusCode);
            var newRow = old.DeepClone(); newRow["id"] = "nueva"; newRow["sistema"] = "excel"; newRow["uso"] = "consultar"; newRow["estado"] = "bien";
            var newLease = await Lease(client, tab, "sistemas:nueva");
            Check((await Patch(client, Edit(tab, newLease with { Value = newRow }))).StatusCode == HttpStatusCode.UnprocessableEntity);
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), after["version"]!.GetValue<int>() + 1))).StatusCode == HttpStatusCode.UnprocessableEntity);
            procedure["resultado"] = "Entrega confirmada"; reserve = await Lease(other, otherTab, "identificacion:inicial", 1);
            Check((await Patch(other, Edit(otherTab, reserve with { Value = procedure }) with { Release = true })).IsSuccessStatusCode);
            Check((await Live(client, tab))["procedimientosDisponibles"]!.AsArray().Count == 1);
            Check((await Patch(client, Edit(tab, newLease with { Value = newRow }) with { Release = true })).IsSuccessStatusCode);
            Check((await Live(client, tab))["contenido"]!["sistemas"]!.AsArray().Count == 2);
        });
    }
}
