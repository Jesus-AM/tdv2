using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Tdv2.Domain;
using Tdv2.Synchronization;
namespace Tdv2.NativeVerification;
internal sealed class ReadCounter : DbCommandInterceptor
{
    public int Count;
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { Interlocked.Increment(ref Count); return ValueTask.FromResult(result); }
}
internal static partial class NativeTests
{
    private static void RegisterPerformanceCases(NativeDatabase database, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        Test("Rendimiento/Inicio consultas por conjuntos", async (app, client) =>
        {
            await database.Sql("""
                INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,desc_ur,id_ur_pertenece,nivel_ur,tipo_ur,estatus_ur,presente)
                SELECT 'perf-'||n,2026,'perf-'||n,'Área sintética '||n,'A',3,'0','Activo',true FROM generate_series(1,100) n;
                INSERT INTO sincronizacion_catalogos(fuente,registros,completada_en) VALUES('ilda',0,timezone('UTC',now()));
                """);
            await Login(app, client); app.Reads.Count = 0;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.GetAsync("/inicio"); Check(response.IsSuccessStatusCode);
            var count = (await Json(response))["props"]!["formatos"]!.AsArray().Count;
            var report = new { synthetic = true, units = count, efCommands = app.Reads.Count, elapsedMs = clock.Elapsed.TotalMilliseconds };
            await File.WriteAllTextAsync(Path.Combine(Environment.GetEnvironmentVariable("TDV2_TEST_ARTIFACTS")!, "index-performance.json"), JsonSerializer.Serialize(report));
            Console.WriteLine(JsonSerializer.Serialize(report)); Check(count >= 100);
            Check(report.efCommands <= 10, "Inicio no debe volver a consultar formato e ILDA por cada UR.");
        });
        Test("Rendimiento/ILDA por conjuntos conserva igualdad exacta, orden y límite independiente de 200", async (app, client) =>
        {
            await database.Sql("""
                INSERT INTO sincronizacion_catalogos(fuente,registros,completada_en) VALUES('ilda',203,timezone('UTC',now()));
                INSERT INTO ilda_informacion_area(id_origen,ur2,informacion_generada,datos,presente,sincronizado_en)
                SELECT n::text,'06000','Trámite sintético '||n,'{}',true,now() FROM generate_series(1,202) n;
                INSERT INTO ilda_informacion_area(id_origen,ur2,informacion_generada,datos,presente,sincronizado_en)
                VALUES('203','6000','Otra asociación','{}',true,now());
                """);
            using var scope = app.Services.CreateScope(); var catalogs = scope.ServiceProvider.GetRequiredService<ILocalCatalog>();
            Unit[] units = [new("A", "06000", "Área", 2, null, 2026), new("B", "6000", "Otra", 2, null, 2026)];
            app.Reads.Count = 0; var many = await catalogs.Inventories(units, default); Check(app.Reads.Count == 2);
            Check(many["A"].Rows.Count == 200 && many["A"].Notice is not null && many["B"].Rows.Count == 1);
            foreach (var unit in units)
            {
                var one = await catalogs.Inventory(unit, default);
                Check(one.State == many[unit.Id].State && one.Notice == many[unit.Id].Notice
                    && one.Rows.Select(r => r.ToJsonString()).SequenceEqual(many[unit.Id].Rows.Select(r => r.ToJsonString())));
            }
        });
    }
}
