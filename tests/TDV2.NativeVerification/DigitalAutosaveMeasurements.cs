using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Npgsql;
using Tdv2.Competencias;

namespace Tdv2.NativeVerification;

internal sealed partial class DigitalTests
{
    // pg_stat_statements cuenta también SQL dentro de funciones/triggers (track=all).
    // Exporta sólo contadores, tiempos y etiquetas; nunca SQL, parámetros o capturas.
    private sealed record SqlMeasure(string Database, string Operation, bool TopLevel, long Calls, long Rows, double ExecutionMs);
    private async Task<SqlMeasure[]> AutosaveSql()
    {
        await using var connection = new NpgsqlConnection(shared.Admin); await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT d.datname,s.query,s.toplevel,s.calls,s.rows,s.total_exec_time
            FROM pg_stat_statements s JOIN pg_database d ON d.oid=s.dbid
            JOIN pg_roles r ON r.oid=s.userid WHERE r.rolname<>'tdv2_test_admin' OR NOT s.toplevel
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(); var result = new List<SqlMeasure>();
        while (await reader.ReadAsync())
        {
            var query = reader.GetString(1).TrimStart();
            var verb = query.Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)[0].ToUpperInvariant();
            var table = new[] { "cd_surveys", "cd_control", "activity_logs", "cd_activity_events" }.FirstOrDefault(query.Contains);
            result.Add(new(reader.GetString(0), verb + (table is null ? "" : ":" + table), reader.GetBoolean(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetDouble(5)));
        }
        return result.GroupBy(s => (s.Database,s.Operation,s.TopLevel)).Select(g => new SqlMeasure(g.Key.Database,g.Key.Operation,g.Key.TopLevel,g.Sum(s=>s.Calls),g.Sum(s=>s.Rows),g.Sum(s=>s.ExecutionMs))).ToArray();
    }
    private void AddAutosaveMeasurements(List<(string Name, Func<NativeApplication, HttpClient, Task> Work)> cases)
    {
        if (Environment.GetEnvironmentVariable("TDV2_TEST_AUTOSAVE_MEASURE") != "true") return;
        cases.Add(("Autoguardado SQL: solicitudes, cambios reales, conflictos y bloqueos", async (app,client) =>
        {
            await shared.ValidateCluster(); await shared.Sql("CREATE EXTENSION IF NOT EXISTS pg_stat_statements");
            await Login(app,client); var view = await Accept(client); var full = Complete(view);
            var results = new List<object>();
            async Task<DigitalSurveyView?> Measure(string name, DigitalSave input, int status=200)
            {
                await shared.Sql("SELECT pg_stat_statements_reset()");
                var body = JsonSerializer.Serialize(input,DigitalInstrument.Json); var watch=Stopwatch.StartNew();
                using var response=await client.PutAsync(Base+"/api/encuesta",new StringContent(body,Encoding.UTF8,"application/json"));
                var bytes=await response.Content.ReadAsByteArrayAsync(); watch.Stop();
                Check((int)response.StatusCode==status, name+": HTTP esperado");
                var sql=await AutosaveSql();
                var updates=sql.Where(s=>s.TopLevel && s.Operation=="UPDATE:cd_surveys").Sum(s=>s.Rows);
                var updated=status==200 ? JsonSerializer.Deserialize<DigitalSurveyView>(bytes,DigitalInstrument.Json) : null;
                results.Add(new {name,status,requestBytes=Encoding.UTF8.GetByteCount(body),responseBytes=bytes.Length,httpMs=watch.Elapsed.TotalMilliseconds,
                    topLevelCalls=sql.Where(s=>s.TopLevel).Sum(s=>s.Calls),sqlExecutionMs=sql.Where(s=>s.TopLevel).Sum(s=>s.ExecutionMs),surveyUpdates=updates,sql});
                if(name.Contains("idéntico") || status==409) Check(updates==0,"Sin actualizaciones redundantes o por conflicto");
                else Check(updates==1,"Una única actualización de encuesta");
                return updated;
            }
            view=(await Measure("perfil completo",new(view.Version,full.Profile,view.Answers,view.Generation)))!;
            view=(await Measure("documento idéntico",new(view.Version,view.Profile,view.Answers,view.Generation)))!;
            var previous=view.Version;
            view.Answers["m-word"]=new("level",0);
            view=(await Measure("respuesta cero",new(view.Version,view.Profile,view.Answers,view.Generation)))!;
            view.Answers["m-word"]=new("level",3);view.Answers["m-excel"]=new("level",2);
            view=(await Measure("ráfaga agrupada",new(view.Version,view.Profile,view.Answers,view.Generation)))!;
            view=(await Measure("repetición idéntico versión antigua",new(previous,view.Profile,view.Answers,view.Generation)))!;
            var conflicting=JsonSerializer.Deserialize<Dictionary<string,DigitalAnswer>>(JsonSerializer.Serialize(view.Answers))!;
            conflicting["m-word"]=new("level",1);
            await Measure("otra pestaña versión antigua",new(previous,view.Profile,conflicting,view.Generation),409);
            Check((await Survey(client)).Answers["m-word"].Level==3);

            // Dos conexiones escriben la misma revisión. Una gana; la otra conserva el 409.
            await using(var blocker=new NpgsqlConnection(diagnosis))
            {
                await blocker.OpenAsync(); await using var tx=await blocker.BeginTransactionAsync();
                await using(var hold=new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('0001',27102026))",blocker,tx))await hold.ExecuteNonQueryAsync();
                var first=new DigitalSave(view.Version,view.Profile,conflicting,view.Generation);
                var second=new DigitalSave(view.Version,view.Profile,view.Answers,view.Generation);
                second.Answers["m-word"]=new("level",2);
                var timer=Stopwatch.StartNew();
                var requests=new[]{client.PutAsJsonAsync(Base+"/api/encuesta",first),client.PutAsJsonAsync(Base+"/api/encuesta",second)};
                var waiting=0L;
                for(var i=0;i<100;i++)
                {
                    waiting=Convert.ToInt64(await Data("SELECT count(*) FROM pg_stat_activity WHERE usename='cd_native_app' AND wait_event_type='Lock'"));
                    if(waiting==2)break; await Task.Delay(20);
                }
                await tx.CommitAsync(); var heldMs=timer.Elapsed.TotalMilliseconds;
                var replies=await Task.WhenAll(requests); var statuses=replies.Select(r=>(int)r.StatusCode).Order().ToArray();
                Check(waiting==2 && statuses.SequenceEqual(new[]{200,409}),"Serialización de dos guardados y conflicto sin pérdida");
                foreach(var response in replies)response.Dispose();
                results.Add(new{name="contención inducida de dos escritores",waitingLocks=waiting,heldMs,statuses});
            }
            view=await CompleteAndSave(client);
            view.Answers["m-word"]=new("level",0);
            view=(await Measure("documento de todos los bloques",new(view.Version,view.Profile,view.Answers,view.Generation)))!;
            Check((await Survey(client)).Answers["m-word"].Level==0 && await QueuedSaves()==0 && await ReceivedSaves()==0);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM pg_stat_activity WHERE usename='cd_native_app' AND wait_event_type='Lock'"))==0,"Sin bloqueos residuales");
            await File.WriteAllTextAsync(Path.Combine(shared.Artifacts,"autosave-sql.json"),JsonSerializer.Serialize(new {isolatedPostgreSql=true,statistics="pg_stat_statements track=all; tiempos top-level sin sumar otra vez triggers",results},new JsonSerializerOptions{WriteIndented=true}));
        }));
    }
}
