using System.Net;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Services;
using Tdv2.Synchronization;
using Tdv2.Integrations.Catalogs;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    private static async Task<JsonObject> ImportLists(NativeApplication app, ApplicationLists? lists = null, bool preview = false)
    {
        using var scope = app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationListImport>().Run(lists ?? await ApplicationLists.Load(default), preview, default);
    }
    private static ApplicationLists ChangeList(ApplicationLists lists, string category, int index, Func<ApplicationListItem, ApplicationListItem> change) =>
        lists with { Catalogs = lists.Catalogs.Select(c => c.Category != category ? c : c with { Items = c.Items.Select((i, n) => n == index ? change(i) : i).ToArray() }).ToArray() };
    private static async Task SeedPreviousApplications(NativeDatabase db, ApplicationLists lists)
    {
        foreach (var c in lists.Catalogs) for (var i = 0; i < 2; i++)
            await db.Sql("INSERT INTO aplicaciones_catalogo(categoria,id,nombre,disponible) VALUES($1,$2,$3,false)", c.Category, (i+1).ToString(), c.Items[i].Name);
    }
    private static void RegisterApplicationCatalogCases(NativeDatabase db, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        async Task<string> Rows() => (string)(await db.Scalar("SELECT coalesce(json_agg(t ORDER BY categoria,id),'[]')::text FROM aplicaciones_catalogo t"))!;
        async Task<string> Metadata() => (string)(await db.Scalar("SELECT coalesce(json_agg(t ORDER BY categoria),'[]')::text FROM aplicaciones_importaciones t"))!;
        Test("Sync/listas: migración aditiva copia IDs sin activar ni alterar enviados", async (app, client) =>
        {
            await using var context = new Tdv2DbContext(new DbContextOptionsBuilder<Tdv2DbContext>().UseNpgsql(db.Admin).Options);
            Check(!context.Database.HasPendingModelChanges()); var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync("20261014180408_SiiApplicationsCatalog");
            await SeedStage(db, CoveredFixture(app));
            await db.Sql("UPDATE formatos_ur SET enviado_en=clock_timestamp(),enviado_por='persona@uacj.mx',instantanea_envio=jsonb_build_object('contenido',contenido) WHERE id_ur='A'");
            await db.Sql("INSERT INTO sii_aplicaciones_moviles(id_aplicacion_movil,nombre,id_ur,activa,presente,sincronizado_en) VALUES(-12,' Móvil histórica ',77,true,true,clock_timestamp()); INSERT INTO sii_aplicaciones_web(id_aplicacion_web,nombre,activa,presente,sincronizado_en) VALUES(-12,'Web histórica',true,true,clock_timestamp())");
            var before = await db.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f");
            await context.Database.MigrateAsync(); await context.Database.MigrateAsync();
            await db.Sql("GRANT SELECT,INSERT,UPDATE,DELETE ON aplicaciones_catalogo,aplicaciones_importaciones TO tdv2_native_app");
            Check(Equals(before, await db.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f")));
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM aplicaciones_catalogo WHERE id='-12' AND NOT disponible AND clave_lista IS NULL")) == 2);
            Check((await db.Scalar("SELECT nombre FROM aplicaciones_catalogo WHERE categoria='moviles'"))!.ToString() == " Móvil histórica ");
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM information_schema.columns WHERE table_name='aplicaciones_catalogo'")) == 5);
            await ImportLists(app);
            try { await migrator.MigrateAsync("20261014180408_SiiApplicationsCatalog"); Check(false); } catch (PostgresException) { }
            Check((await context.Database.GetAppliedMigrationsAsync()).Last() == "20261016190000_RemoveUsedMedia");
            Check(Equals(before, await db.Scalar("SELECT row_to_json(f)::text FROM formatos_ur f")));
        });
        Test("Sync/listas: 207 web y 26 móviles, primera importación, repetición y UUID estables", async (app, client) =>
        {
            var lists = await ApplicationLists.Load(default); var preview = await ImportLists(app, lists, true);
            Check(preview["web"]!["nuevas"]!.GetValue<int>() == 207 && preview["moviles"]!["nuevas"]!.GetValue<int>() == 26);
            Check(await Rows() == "[]" && await Metadata() == "[]");
            await ImportLists(app, lists); var first = await Rows();
            var repeated = await ImportLists(app, lists); Check(first == await Rows());
            Check(repeated["web"]!["nuevas"]!.GetValue<int>() == 0 && repeated["moviles"]!["conservadas"]!.GetValue<int>() == 26);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM aplicaciones_catalogo WHERE disponible AND id LIKE 'local:%'")) == 233);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM aplicaciones_importaciones")) == 2);
            Check(app.Sources.Queries.IsEmpty);
            await using (var context = new Tdv2DbContext(new DbContextOptionsBuilder<Tdv2DbContext>().UseNpgsql(
                new NpgsqlConnectionStringBuilder(db.Admin) { SearchPath = "pg_catalog" }.ConnectionString).Options))
            {
                var local = new LocalCatalog(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<LocalCatalog>.Instance);
                Check((await local.Applications(SystemApplications.Web, default)).aplicaciones.Count == 207);
            }
            await Login(app, client);
            foreach (var c in lists.Catalogs)
            {
                var local = await Json(await client.GetAsync($"/formatos/A/aplicaciones/{c.Category}"));
                Check(local["estado"]!.ToString() == "disponible" && local["aplicaciones"]!.AsArray().Count == c.Items.Count);
                Check(local["aplicaciones"]!.AsArray().All(v => v!.AsObject().Count == 3));
                foreach (var i in c.Items) Check(local["aplicaciones"]!.AsArray().Any(v => v!["id"]!.ToString() == i.Id && v["descripcion"]!.ToString() == i.Name));
            }
        });
        Test("Sync/listas: coincidencia exacta por categoría, ambigüedad, bajas y separación de nombres", async (app, client) =>
        {
            var lists = await ApplicationLists.Load(default); await SeedPreviousApplications(db, lists);
            var webName = lists.Catalogs.Single(c => c.Category == "web").Items[0].Name;
            await db.Sql("INSERT INTO aplicaciones_catalogo(categoria,id,nombre,disponible) VALUES('web','99',$1,true),('web','777','Anterior sin correspondencia',true)", " " + webName + " ");
            lists = ChangeList(lists, "moviles", 2, i => i with { Name = webName });
            var report = await ImportLists(app, lists);
            Check(report["web"]!["ambiguas"]!.AsArray().Count == 1 && report["web"]!["correspondencias"]!.GetValue<int>() == 1);
            Check(report["moviles"]!["correspondencias"]!.GetValue<int>() == 2);
            Check((bool)(await db.Scalar("SELECT NOT disponible AND clave_lista IS NULL FROM aplicaciones_catalogo WHERE categoria='web' AND id='1'"))!);
            Check((bool)(await db.Scalar("SELECT disponible AND clave_lista IS NOT NULL FROM aplicaciones_catalogo WHERE categoria='moviles' AND id='1'"))!);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM aplicaciones_catalogo WHERE nombre=$1 AND disponible", webName)) == 2);
            var before = await Rows(); await ImportLists(app, lists); Check(before == await Rows());
        });
        Test("Sync/listas: rollback conjunto por auditoría y rechazo de archivo incompleto o duplicado", async (app, client) =>
        {
            var lists = await ApplicationLists.Load(default); await ImportLists(app, lists); var before = await Rows(); var metadata = await Metadata();
            await db.Sql("CREATE FUNCTION fixture_app_fail() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.action='importar_listas_aplicaciones' THEN RAISE EXCEPTION 'SYNTHETIC_SECRET'; END IF; RETURN NEW; END $$; CREATE TRIGGER app_fail BEFORE INSERT ON activity_logs FOR EACH ROW EXECUTE FUNCTION fixture_app_fail()");
            try
            {
                var changed = ChangeList(lists, "web", 0, i => i with { Name = new string('ñ', 3999) });
                changed = ChangeList(changed, "moviles", 0, i => i with { Id = "local:" + Guid.NewGuid(), Name = "Nueva móvil" });
                try { await ImportLists(app, changed); Check(false); } catch (PostgresException) { }
                Check(before == await Rows() && metadata == await Metadata());
            }
            finally { await db.Sql("DROP TRIGGER app_fail ON activity_logs; DROP FUNCTION fixture_app_fail()"); }
            foreach (var invalid in new[] { lists with { Catalogs = [] }, ChangeList(lists, "web", 1, _ => lists.Catalogs.Single(c => c.Category == "web").Items[0]), ChangeList(lists, "web", 0, i => i with { Name = " " }) })
            {
                try { await ImportLists(app, invalid); Check(false); } catch (SyncProblem) { }
                Check(before == await Rows() && metadata == await Metadata());
            }
            var longText = ChangeList(lists, "web", 0, i => i with { Name = new string('á', 4000) });
            await ImportLists(app, longText);
            Check(Convert.ToInt32(await db.Scalar("SELECT max(char_length(nombre)) FROM aplicaciones_catalogo")) == 4000);
        });
        Test("Sync/listas: importaciones simultáneas no duplican y SQL Server sólo lee UR y módulos", async (app, client) =>
        {
            await Task.WhenAll(ImportLists(app), ImportLists(app));
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM aplicaciones_catalogo")) == 233);
            var before = await Rows(); Check(Completed(await RunSync(app, "ambas")));
            Check(before == await Rows()); Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM sincronizacion_catalogos")) == 3);
            Check(app.Sources.Queries.All(q => !q.Contains("APLICACIONES", StringComparison.OrdinalIgnoreCase)));
            using var scope = app.Services.CreateScope();
            foreach (var oldSource in new[] { "sii_moviles", "sii_web" })
                try { await scope.ServiceProvider.GetRequiredService<ICatalogSource>().Read(oldSource, default); Check(false); } catch (SyncProblem) { }
        });
        Test("Sync/listas comando: comprobar e importar desde paquete sin fuentes ni cola", async (app, client) =>
        {
            async Task Command(string flag)
            {
                var published = Path.Combine(db.Workspace, ".artifacts", "publish-application-lists");
                var directory = File.Exists(Path.Combine(published, "tdv2.dll")) ? published : AppContext.BaseDirectory;
                var start = new ProcessStartInfo(Environment.ProcessPath!) { WorkingDirectory = directory,
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                start.ArgumentList.Add(Path.Combine(directory, "tdv2.dll"));
                start.ArgumentList.Add("--environment"); start.ArgumentList.Add("Production"); start.ArgumentList.Add(flag);
                start.Environment.Clear();
                foreach (var name in new[] { "SystemRoot", "SystemDrive", "PATH", "DOTNET_ROOT" })
                    if (Environment.GetEnvironmentVariable(name) is { } value) start.Environment[name] = value;
                start.Environment["TEMP"] = db.Artifacts; start.Environment["TMP"] = db.Artifacts;
                start.Environment["ConnectionStrings__Tdv2"] = db.AppConnection;
                start.Environment["DataProtection__KeyDirectory"] = Path.Combine(db.Artifacts, "command-keys");
                using var child = Process.Start(start)!;
                var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
                try { await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30)); Check(child.ExitCode == 0, "El comando publicado falló."); }
                finally { if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); } }
                var report = JsonNode.Parse(await output)!; await errors;
                Check(report["web"]!["disponibles"]!.GetValue<int>() == 207 && report["moviles"]!["disponibles"]!.GetValue<int>() == 26);
            }
            await Command("--check-applications"); Check(await Rows() == "[]");
            await Command("--import-applications");
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM aplicaciones_catalogo WHERE disponible")) == 233);
            Check(Convert.ToInt32(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones")) == 0 && app.Sources.Queries.IsEmpty);
        });
        Test("Sync/listas: esquema pendiente y catálogo no importado se distinguen", async (app, client) =>
        {
            await Login(app, client);
            Check((await Json(await client.GetAsync("/formatos/A/aplicaciones/web")))["estado"]!.ToString() == "pendiente");
            await db.Sql("ALTER TABLE aplicaciones_catalogo RENAME TO fixture_missing_application");
            try
            {
                Check((await Json(await client.GetAsync("/formatos/A/aplicaciones/web")))["estado"]!.ToString() == "error");
                try { await ImportLists(app); Check(false); } catch (SyncProblem) { }
                Check(Completed(await RunSync(app, "sii")));
            }
            finally { await db.Sql("ALTER TABLE fixture_missing_application RENAME TO aplicaciones_catalogo"); }
        });
        foreach (var kind in SystemApplications.Kinds)
        {
            Test($"Edición/aplicaciones {kind.Key}: múltiples, histórico, permisos, cambio de sistema y avance", async (app, client) =>
            {
                await Login(app, client); await Csrf(client);
                Check((await Json(await client.GetAsync($"/formatos/A/aplicaciones/{kind.Key}")))["estado"]!.ToString() == "pendiente");
                Check((await client.GetAsync($"/formatos/B/aplicaciones/{kind.Key}")).StatusCode == HttpStatusCode.Forbidden);
                var lists = await ApplicationLists.Load(default); await SeedPreviousApplications(db, lists); await ImportLists(app, lists); var content = CoveredFixture(app);
                content["sistemas"]![0]!["sistema"] = kind.Tool; await SeedStage(db, content);
                var tab = Guid.NewGuid();
                async Task<HttpResponseMessage> Store(JsonNode row)
                {
                    var live = await Live(client, tab); var key = "sistemas:" + row["id"];
                    var version = live["bloques"]!.AsArray().FirstOrDefault(b => b!["key"]!.ToString() == key)?["version"]?.GetValue<int>() ?? 0;
                    var lease = await Lease(client, tab, key, version);
                    return await Patch(client, Edit(tab, lease with { Value = row }) with { Release = true });
                }
                JsonObject Item(string id, string? catalog = null) => new() { ["id"] = id, ["descripcion"] = "No confiar en navegador", ["catalogo"] = catalog ?? kind.Key };
                var row = content["sistemas"]![0]!.DeepClone(); row[kind.Field] = new JsonArray();
                Check((await Store(row)).IsSuccessStatusCode);
                Check((await Live(client, tab))["porcentaje"]!.GetValue<int>() < 100);
                row[kind.Field] = new JsonArray(Item("1")); Check((await Store(row)).IsSuccessStatusCode);
                var one = await Live(client, tab); Check(one["porcentaje"]!.GetValue<int>() == 100);
                row = one["contenido"]!["sistemas"]![0]!.DeepClone(); var originalName = row[kind.Field]![0]!["descripcion"]!.ToString();
                row[kind.Field]!.AsArray().Add(Item("2")); Check((await Store(row)).IsSuccessStatusCode);
                row = (await Live(client, tab))["contenido"]!["sistemas"]![0]!.DeepClone();
                Check(row[kind.Field]!.AsArray().Count == 2 && (await Live(client, tab))["porcentaje"]!.GetValue<int>() == 100);
                var stored = await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'");
                foreach (var invalid in new[] { new JsonArray(Item("1"), Item("1")), new JsonArray(Item("1", kind == SystemApplications.Mobile ? "web" : "moviles")), new JsonArray(Item("2147483648")), new JsonArray(Item("999")), new JsonArray(Item("01")) })
                {
                    var bad = row.DeepClone(); bad[kind.Field] = invalid; Check((await Store(bad)).StatusCode == HttpStatusCode.UnprocessableEntity);
                    Check(Equals(stored, await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'")));
                }
                lists = ChangeList(lists, kind.Key, 0, i => i with { Name = "Descripción posterior" }); await ImportLists(app, lists);
                lists = ChangeList(lists, kind.Key, 0, i => i with { Id = "local:" + Guid.NewGuid(), Name = "Aplicación nueva" }); await ImportLists(app, lists);
                Check(Equals(stored, await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'")));
                row["fallas"] = "Conservar historia"; Check((await Store(row)).IsSuccessStatusCode);
                Check((await Live(client, tab))["contenido"]!["sistemas"]![0]![kind.Field]![0]!["descripcion"]!.ToString() == originalName);
                var otherKind = kind == SystemApplications.Mobile ? SystemApplications.Web : SystemApplications.Mobile;
                row["sistema"] = otherKind.Tool; row[otherKind.Field] = new JsonArray(Item("2", otherKind.Key)); Check((await Store(row)).IsSuccessStatusCode);
                Check((await Live(client, tab))["porcentaje"]!.GetValue<int>() == 100);
                row["sistema"] = kind.Tool; Check((await Store(row)).StatusCode == HttpStatusCode.UnprocessableEntity); // No reactivar referencia retirada dormida.
                row[kind.Field] = new JsonArray(); Check((await Store(row)).IsSuccessStatusCode);
                var empty = await Live(client, tab); Check(empty["porcentaje"]!.GetValue<int>() < 100);
                Check(empty["entrega"]!["revision"]!["pendientes"]!.AsArray().Any(p => p!["campo"]!.ToString() == kind.Field));
                Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), empty["version"]!.GetValue<int>()))).StatusCode == HttpStatusCode.UnprocessableEntity);
                row[kind.Field] = new JsonArray(Item("2")); Check((await Store(row)).IsSuccessStatusCode);
                var done = await Live(client, tab);
                Check((await client.PostAsJsonAsync("/formatos/A/enviar", new SubmitRequest(tab, Guid.NewGuid(), done["version"]!.GetValue<int>()))).IsSuccessStatusCode);
                var frozen = await db.Scalar("SELECT instantanea::text FROM formato_envios_etapas");
                lists = ChangeList(lists, kind.Key, 1, i => i with { Name = "No alterar enviado" }); await ImportLists(app, lists);
                Check(Equals(frozen, await db.Scalar("SELECT instantanea::text FROM formato_envios_etapas")));
                Check(!(await Live(client, tab))["editable"]!.GetValue<bool>());
            });
            Test($"Edición/aplicaciones {kind.Key}: retirada concurrente revalida selección con conexiones independientes", async (app, client) =>
            {
                var lists = await ApplicationLists.Load(default); await SeedPreviousApplications(db, lists); await ImportLists(app, lists);
                var content = CoveredFixture(app); await SeedStage(db, content); await Login(app, client); await Csrf(client);
                var tab = Guid.NewGuid(); var lease = await Lease(client, tab, "sistemas:inicial");
                var row = content["sistemas"]![0]!.DeepClone(); row["sistema"] = kind.Tool;
                row[kind.Field] = new JsonArray(new JsonObject { ["id"] = "1", ["catalogo"] = kind.Key, ["descripcion"] = "No autorizado" });
                var before = await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'");
                await using var connection = new NpgsqlConnection(db.Admin); await connection.OpenAsync();
                await using var tx = await connection.BeginTransactionAsync();
                await new SyncSql(connection, tx).Execute("UPDATE aplicaciones_catalogo SET disponible=false WHERE categoria=$1 AND id='1'", default, kind.Key);
                var saving = Patch(client, Edit(tab, lease with { Value = row }) with { Release = true });
                var waiting = false;
                try
                {
                    for (var i = 0; i < 250 && !waiting; i++)
                    {
                        waiting = (bool)(await db.Scalar("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE usename='tdv2_native_app' AND wait_event_type='Lock' AND query LIKE $1)", "%aplicaciones_catalogo%"))!;
                        if (!waiting) await Task.Delay(20);
                    }
                }
                finally { await tx.CommitAsync(); }
                Check(waiting, "No se observó la espera de la selección en PostgreSQL.");
                Check((await saving).StatusCode == HttpStatusCode.UnprocessableEntity);
                Check(Equals(before, await db.Scalar("SELECT contenido::text FROM formatos_ur WHERE id_ur='A'")));
            });
        }
    }
}
