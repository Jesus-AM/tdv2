using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Services;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    private static void RegisterToolCatalogCases(NativeDatabase db, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        JsonObject Seed(NativeApplication app)
        {
            var content = CoveredFixture(app); content["sistemas"] = new JsonArray();
            var p = content["identificacion"]![0]!.DeepClone(); p["id"] = "segundo"; p["codigo"] = "PO-02";
            content["identificacion"]!.AsArray().Add(p); return content;
        }
        JsonObject Tool(NativeApplication app, string id, string name = "microsoft_365", string code = "PO-01")
        {
            var row = app.Services.GetRequiredService<FormSchema>().Blank(new("A", "100", "Prueba", 2, null, 2026))["sistemas"]![0]!.DeepClone().AsObject();
            row["id"] = id; row["proceso"] = code; row["sistema"] = name; row["uso"] = "consultar"; row["estado"] = "bien";
            return row;
        }
        async Task<HttpResponseMessage> Store(HttpClient client, Guid tab, JsonObject row)
        {
            var live = await Live(client, tab); var key = "sistemas:" + row["id"];
            var version = live["bloques"]!.AsArray().FirstOrDefault(b => b!["key"]!.ToString() == key)?["version"]?.GetValue<int>() ?? 0;
            var lease = await Lease(client, tab, key, version);
            return await Patch(client, Edit(tab, lease with { Value = row }) with { Release = true });
        }
        Test("Edición/catálogo vigente: familias separadas, valores inválidos, cambio, recarga y envío", async (app, client) =>
        {
            await SeedStage(db, Seed(app)); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var row = Tool(app, "suite"); Check((await Store(client, tab, row)).IsSuccessStatusCode);
            var live = await Live(client, tab);
            Check(live["entrega"]!["revision"]!["pendientes"]!.AsArray().Single()!["campo"]!.ToString() == "aplicacionesMicrosoft365");
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), live["version"]!.GetValue<int>()))).StatusCode == HttpStatusCode.UnprocessableEntity);
            row["aplicacionesMicrosoft365"] = new JsonArray("excel", "forms", "outlook");
            row["aplicacionesAdobe"] = new JsonArray("photoshop", "acrobat_reader");
            Check((await Store(client, tab, row)).IsSuccessStatusCode);
            foreach (var invalid in new[] { new JsonArray("photoshop"), new JsonArray("excel", "excel"), new JsonArray("copilot"), new JsonArray(1) })
            {
                var bad = row.DeepClone().AsObject(); bad["aplicacionesMicrosoft365"] = invalid;
                Check((await Store(client, tab, bad)).StatusCode == HttpStatusCode.UnprocessableEntity);
            }
            var badAdobe = row.DeepClone().AsObject(); badAdobe["aplicacionesAdobe"] = new JsonArray("forms");
            Check((await Store(client, tab, badAdobe)).StatusCode == HttpStatusCode.UnprocessableEntity);
            row["sistema"] = "adobe_creative_cloud"; Check((await Store(client, tab, row)).IsSuccessStatusCode);
            row["sistema"] = "microsoft_365"; Check((await Store(client, tab, row)).IsSuccessStatusCode);
            live = await Live(client, tab);
            Check(JsonNode.DeepEquals(live["contenido"]!["sistemas"]![0], row));
            Check(live["porcentaje"]!.GetValue<int>() == 100);
            Check(!live["contenido"]!.AsObject().ContainsKey("medios") && !live["contenido"]!.AsObject().ContainsKey("medioOtro"));
            Check(!live["entrega"]!["resumen"]!.AsObject().ContainsKey("medios"));
            Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), live["version"]!.GetValue<int>()))).IsSuccessStatusCode);
        });
        Test("Edición/catálogo vigente: duplicados, propia fila, retiro, reasignación y sin asociación", async (app, client) =>
        {
            await SeedStage(db, Seed(app)); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var a = Tool(app, "a", "limesurvey"); var b = Tool(app, "b", "limesurvey");
            Check((await Store(client, tab, a)).IsSuccessStatusCode);
            var rejected = await Store(client, tab, b); Check(rejected.StatusCode == HttpStatusCode.UnprocessableEntity);
            Check((await Json(rejected))["message"]!.ToString().Contains("Tus respuestas se conservan"));
            a["fallas"] = "Comentario"; Check((await Store(client, tab, a)).IsSuccessStatusCode);
            b["proceso"] = "PO-02"; Check((await Store(client, tab, b)).IsSuccessStatusCode);
            var move = b.DeepClone().AsObject(); move["proceso"] = "PO-01";
            Check((await Store(client, tab, move)).StatusCode == HttpStatusCode.UnprocessableEntity);
            Check((await Live(client, tab))["contenido"]!["sistemas"]![1]!["proceso"]!.ToString() == "PO-02");
            foreach (var id in new[] { "u1", "u2" }) Check((await Store(client, tab, Tool(app, id, "limesurvey", ""))).IsSuccessStatusCode);
            Check((await Patch(client, await RemovalRequest(client, tab, new("sistemas", "a")))).IsSuccessStatusCode);
            Check((await Store(client, tab, move)).IsSuccessStatusCode);
        });
        Test("Edición/catálogo vigente: Otra normaliza igualdad exacta y conserva nombres originales", async (app, client) =>
        {
            await SeedStage(db, Seed(app)); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            var a = Tool(app, "a", "otra"); a["sistemaOtro"] = "  Mi   herramienta  ";
            Check((await Store(client, tab, a)).IsSuccessStatusCode);
            var b = Tool(app, "b", "otra"); b["sistemaOtro"] = "mi\tHERRAMIENTA";
            Check((await Store(client, tab, b)).StatusCode == HttpStatusCode.UnprocessableEntity);
            b["sistemaOtro"] = "Mi herramienta 2"; Check((await Store(client, tab, b)).IsSuccessStatusCode);
            Check((await Live(client, tab))["contenido"]!["sistemas"]![0]!["sistemaOtro"]!.ToString() == "  Mi   herramienta  ");
        });
        Test("Edición/catálogo vigente: históricos duplicados editables sin ampliación ni reclasificación", async (app, client) =>
        {
            var seed = Seed(app); var a = Tool(app, "a", "excel"); var b = Tool(app, "b", "excel");
            seed["sistemas"] = new JsonArray(a.DeepClone(), b.DeepClone());
            await SeedStage(db, seed); await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            a["fallas"] = "Historia"; Check((await Store(client, tab, a)).IsSuccessStatusCode);
            Check((await Store(client, tab, Tool(app, "c", "excel"))).StatusCode == HttpStatusCode.UnprocessableEntity);
            foreach (var retired in new[] { "excel", "correo", "formulario", "papel", "portal_cast" })
                Check((await Store(client, tab, Tool(app, "retired", retired))).StatusCode == HttpStatusCode.UnprocessableEntity);
            a["sistema"] = "power_bi"; Check((await Store(client, tab, a)).IsSuccessStatusCode);
            b["sistema"] = "power_bi"; Check((await Store(client, tab, b)).StatusCode == HttpStatusCode.UnprocessableEntity);
        });
        Test("Edición/catálogo vigente: dos conexiones concurrentes no introducen duplicados", async (app, client) =>
        {
            await SeedStage(db, Seed(app)); await Login(app, client); await Csrf(client);
            using var other = app.Client(); await Login(app, other); await Csrf(other);
            var a = Guid.NewGuid(); var b = Guid.NewGuid();
            var leaseA = await Lease(client, a, "sistemas:a"); var leaseB = await Lease(other, b, "sistemas:b");
            var requestA = Edit(a, leaseA with { Value = Tool(app, "a", "tableau") }) with { Release = true };
            var requestB = Edit(b, leaseB with { Value = Tool(app, "b", "tableau") }) with { Release = true };
            await using var blocker = new NpgsqlConnection(db.Admin); await blocker.OpenAsync(); await using var tx = await blocker.BeginTransactionAsync();
            await using (var command = new NpgsqlCommand("SELECT id_ur FROM unidades_responsables_poa WHERE id_ur='A' FOR UPDATE", blocker, tx)) await command.ExecuteScalarAsync();
            var writes = new[] { Patch(client, requestA), Patch(other, requestB) }; var deadline = DateTime.UtcNow.AddSeconds(8);
            while (Convert.ToInt32(await db.Scalar("SELECT count(*) FROM pg_stat_activity WHERE usename='tdv2_native_app' AND wait_event_type='Lock'")) < 2)
            { Check(DateTime.UtcNow < deadline, "No se observaron dos conexiones bloqueadas."); await Task.Delay(20); }
            await tx.CommitAsync(); var results = await Task.WhenAll(writes);
            Check(results.Count(r => r.IsSuccessStatusCode) == 1 && results.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity) == 1);
            Check((await Live(client, a))["contenido"]!["sistemas"]!.AsArray().Count == 1);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM formato_operaciones")) == 1);
        });
    }
}
