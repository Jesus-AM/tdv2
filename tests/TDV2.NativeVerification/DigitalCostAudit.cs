using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Tdv2.Competencias;
using Tdv2.Integrations.Microsoft;

namespace Tdv2.NativeVerification;

internal sealed partial class DigitalTests
{
    private sealed record CostSql(string Database,string Category,string Purpose,bool TopLevel,bool Excluded,long Calls,long Rows,double ServerMs);
    private sealed record CostSample(int Actor,int Status,double BeforeSendMs,double HttpMs,int RequestBytes,int ResponseBytes,DigitalCostTrace.Command[] Commands);
    private sealed class CostActor(int id,HttpClient client,DigitalSurveyView view)
    { internal int Id=id; internal HttpClient Client=client; internal DigitalSurveyView View=view; }
    private async Task<CostSql[]> CostSqlSnapshot()
    {
        await using var connection=new NpgsqlConnection(shared.Admin);await connection.OpenAsync();
        await using var command=new NpgsqlCommand("""
            SELECT d.datname,s.query,s.toplevel,r.rolname='tdv2_test_admin' AND s.toplevel,s.calls,s.rows,s.total_exec_time
            FROM pg_stat_statements s JOIN pg_database d ON d.oid=s.dbid JOIN pg_roles r ON r.oid=s.userid
            """,connection);
        await using var reader=await command.ExecuteReaderAsync();var rows=new List<CostSql>();
        while(await reader.ReadAsync())
        {
            var (category,purpose)=DigitalCostTrace.Describe(reader.GetString(1),reader.GetString(0));
            rows.Add(new(reader.GetString(0),category,purpose,reader.GetBoolean(2),reader.GetBoolean(3),reader.GetInt64(4),reader.GetInt64(5),reader.GetDouble(6)));
        }
        return rows.GroupBy(s=>(s.Database,s.Category,s.Purpose,s.TopLevel,s.Excluded))
            .Select(g=>new CostSql(g.Key.Database,g.Key.Category,g.Key.Purpose,g.Key.TopLevel,g.Key.Excluded,g.Sum(s=>s.Calls),g.Sum(s=>s.Rows),g.Sum(s=>s.ServerMs))).ToArray();
    }
    private Task ClearCostStats()=>shared.Sql("SELECT pg_stat_statements_reset()");
    private void AddCostAudit(List<(string Name,Func<NativeApplication,HttpClient,Task> Work)> cases)
    {
        if(Environment.GetEnvironmentVariable("TDV2_TEST_COST_AUDIT")!="true")return;
        cases.Add(("Costo autoguardado: aislamiento, 1/10/25 actores y desglose",async(_,_)=>await CostAudit()));
    }
    private async Task CostAudit()
    {
        await shared.ValidateCluster();await shared.Sql("CREATE EXTENSION IF NOT EXISTS pg_stat_statements");
        var probe=Environment.GetEnvironmentVariable("TDV2_TEST_COST_PROBE")=="true";
        var pooled=Environment.GetEnvironmentVariable("TDV2_TEST_COST_POOLING")=="true";
        var rounds=probe?1:10; var actors=new List<CostActor>();var phases=new List<object>();var batches=new List<object>();
        using var trace=new DigitalCostTrace();
        var reportPath=Path.Combine(shared.Artifacts,"autosave-cost-audit.json");
        async Task Report(bool complete)=>await File.WriteAllTextAsync(reportPath,JsonSerializer.Serialize(new {complete,probe,rounds,
            real="ASP.NET Kestrel, cookies/CSRF, EF/Npgsql, PostgreSQL 18 y SQL de actividad publicado",
            synthetic="Microsoft OAuth/Graph, vistas de permisos Nexo, empleados/catálogos, identidades, esquema HTTPS sobre transporte loopback HTTP de prueba",
            pooling=pooled,statistics="pg_stat_statements track=all; SQL cliente por Activity traceparent; p95 rango más cercano",
            phases,batches,traceTagNames=trace.TagNames.Keys.Order().ToArray()},new JsonSerializerOptions{WriteIndented=true}));
        try
        {
            await ClearCostStats();var setupClock=Stopwatch.StartNew();
            await Nexo("""
                INSERT INTO fixture_users SELECT 2000+i,1000+i,'costo'||i||'@uacj.mx','Persona de costo '||i,'individual','910'||lpad(i::text,3,'0'),'A4','adscripcion' FROM generate_series(1,25) i;
                INSERT INTO fixture_roles SELECT 2000+i,'costo'||i||'@uacj.mx',30,'participante','Participante' FROM generate_series(1,25) i;
                INSERT INTO usuarios SELECT 1000+i,'costo'||i||'@uacj.mx','Persona de costo '||i,'individual',true FROM generate_series(1,25) i;
                INSERT INTO usuario_aplicacion SELECT 2000+i,1000+i,87,true,false FROM generate_series(1,25) i;
                INSERT INTO usuario_rol SELECT 2000+i,30 FROM generate_series(1,25) i;
                """);
            for (var i = 1; i <= 25; i++) await shared.VerifiedIdentity($"bbbbbbbb-bbbb-4bbb-8bbb-{i:000000000000}", 1000 + i);
            phases.Add(new {name="preparar catálogo sintético",ms=setupClock.Elapsed.TotalMilliseconds,sql=await CostSqlSnapshot()});

            // Kestrel/HTTP y cookies reales. El marcador de transporte sólo existe en Testing.
            var config=Configuration;
            if(pooled) {
                config["ConnectionStrings:Tdv2"]=new NpgsqlConnectionStringBuilder(shared.AppConnection){Pooling=true}.ConnectionString;
                config["ConnectionStrings:Nexo"]=new NpgsqlConnectionStringBuilder(shared.NexoConnection){Pooling=true}.ConnectionString;
                foreach(var name in new[]{"CompetenciasDigitales","CompetenciasDigitalesNexo"})config["ConnectionStrings:"+name]=new NpgsqlConnectionStringBuilder(config["ConnectionStrings:"+name]){Pooling=true}.ConnectionString;
            }
            await using var app=new NativeApplication(shared){DigitalConfiguration=config,Browser=true};
            app.UseKestrel(o=>o.Listen(IPAddress.Loopback,0));using var bootstrap=app.CreateClient(new(){AllowAutoRedirect=false});
            var upstream=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single().TrimEnd('/');
            var origin=upstream.Replace("http://","https://",StringComparison.Ordinal);
            app.Services.GetRequiredService<IOptions<MicrosoftSettings>>().Value.PublicOrigin=origin;
            await ClearCostStats();setupClock.Restart();
            for(var i=1;i<=(probe?1:25);i++)
            {
                // Cada login usa dos solicitudes. No desactivar el limitador real de 20/min/IP.
                if(i>1&&(i-1)%9==0)await Task.Delay(TimeSpan.FromSeconds(61));
                app.Microsoft.Email=app.Microsoft.Mail=$"costo{i}@uacj.mx";app.Microsoft.Id=$"bbbbbbbb-bbbb-4bbb-8bbb-{i:000000000000}";
                var client=new HttpClient(new CostCookieTransport(new Uri(upstream),app.ControlToken)){BaseAddress=new Uri(origin),Timeout=TimeSpan.FromSeconds(35)};
                await Login(app,client);actors.Add(new(i,client,null!));
            }
            phases.Add(new{name="autenticación inicial y CSRF (fuera del guardado)",ms=setupClock.Elapsed.TotalMilliseconds,sql=await CostSqlSnapshot()});
            await ClearCostStats();setupClock.Restart();
            foreach(var actor in actors)
            {
                var initial=await Accept(actor.Client);var input=Complete(initial);
                var response=await actor.Client.PutAsJsonAsync(Base+"/api/encuesta",new DigitalSave(initial.Version,input.Profile,initial.Answers,initial.Generation));
                Check(response.IsSuccessStatusCode,"Preparación de perfil");actor.View=(await response.Content.ReadFromJsonAsync<DigitalSurveyView>(DigitalInstrument.Json))!;response.Dispose();
            }
            phases.Add(new{name="consentimiento, perfil y calentamiento (fuera del guardado)",ms=setupClock.Elapsed.TotalMilliseconds,sql=await CostSqlSnapshot()});
            Check(actors.Select(a=>a.View.Institution["employee"]!.ToString()).Distinct().Count()==actors.Count,"Identidades de empleados distintas");

            foreach(var pass in probe?new[]{"probe"}:pooled?new[]{"pooling_aislado"}:new[]{"antes","despues"})
            foreach(var users in probe?new[]{1}:new[]{1,10,25})
            foreach(var mode in new[]{"cambios","identico_forzado_api","manual_sin_cambios","selecciones_rapidas"})
            {
                var subset=actors.Take(users).ToArray();var samples=new List<CostSample>();
                var queuedBefore=await QueuedSaves();var receivedBefore=await ReceivedSaves();
                await ClearCostStats();var batchClock=Stopwatch.StartNew();
                using var stopObserver=new CancellationTokenSource();var observed=ObserveCostLocks(stopObserver.Token);
                object waits;
                try { for(var round=0;round<rounds;round++)
                {
                    var wave=await Task.WhenAll(subset.Select(async actor=>
                    {
                        var prior=actor.View;var answers=prior.Answers.ToDictionary();
                        var beforeSend=Stopwatch.StartNew();
                        if(mode is "cambios" or "selecciones_rapidas")
                        {
                            var last=answers.GetValueOrDefault("m-word")?.Level??0;
                            if(mode=="selecciones_rapidas")for(var edit=0;edit<5;edit++) { answers["m-word"]=new("level",(last+edit+1)%4);if(!probe)await Task.Delay(80); }
                            else answers["m-word"]=new("level",(last+1)%4);
                        }
                        // Modelo de carga cerrado: máximo un envío por persona, espera explícita
                        // conocida. El debounce React real se verifica en el navegador por separado.
                        if(!probe&&mode!="manual_sin_cambios")await Task.Delay(1500);
                        object payload=mode=="manual_sin_cambios"?new DigitalManualSave(prior.Version,prior.Generation,Guid.NewGuid()):new DigitalSave(prior.Version,prior.Profile,answers,prior.Generation);
                        var raw=JsonSerializer.Serialize(payload,payload.GetType(),DigitalInstrument.Json);
                        using var request=new HttpRequestMessage(mode=="manual_sin_cambios"?HttpMethod.Post:HttpMethod.Put,Base+"/api/encuesta"+(mode=="manual_sin_cambios"?"/guardar":"")){
                            Content=new StringContent(raw,Encoding.UTF8,"application/json")};
                        var id=ActivityTraceId.CreateRandom().ToString();request.Headers.Add("traceparent","00-"+id+"-"+ActivitySpanId.CreateRandom()+"-01");trace.Start(id);
                        var waitMs=beforeSend.Elapsed.TotalMilliseconds;
                        var watch=Stopwatch.StartNew();using var result=await actor.Client.SendAsync(request);var bytes=await result.Content.ReadAsByteArrayAsync();watch.Stop();
                        var commands=trace.Finish(id);
                        if(result.IsSuccessStatusCode)actor.View=JsonSerializer.Deserialize<DigitalSurveyView>(bytes,DigitalInstrument.Json)!;
                        return new CostSample(actor.Id,(int)result.StatusCode,waitMs,watch.Elapsed.TotalMilliseconds,Encoding.UTF8.GetByteCount(raw),bytes.Length,commands);
                    }));
                    samples.AddRange(wave);
                } } finally {stopObserver.Cancel();waits=await observed;}
                batchClock.Stop();var sql=await CostSqlSnapshot();
                var queuedAfter=await QueuedSaves();var receivedAfter=await ReceivedSaves();
                var batch=new {pass,users,mode,sampleCount=samples.Count,batchMs=batchClock.Elapsed.TotalMilliseconds,
                    http=CostDistribution(samples.Select(s=>s.HttpMs)),beforeSend=CostDistribution(samples.Select(s=>s.BeforeSendMs)),
                    sqlClient=CostDistribution(samples.Select(s=>s.Commands.Sum(c=>c.ClientMs))),sqlServerMs=sql.Where(s=>!s.Excluded&&s.TopLevel).Sum(s=>s.ServerMs),
                    statements=sql.Where(s=>!s.Excluded&&s.TopLevel).Sum(s=>s.Calls),queued=queuedAfter-queuedBefore,received=receivedAfter-receivedBefore,
                    errors=samples.Count(s=>s.Status!=200),sql,waits,samples};
                batches.Add(batch);await Report(false);
                Check(samples.All(s=>s.Status==200),$"Costo {pass}/{users}/{mode}: errores HTTP");
                Check(samples.All(s=>s.Commands.Length>0),"La correlación SQL por solicitud debe estar activa");
                var untraced=sql.Where(s=>!s.Excluded&&s.TopLevel&&(s.Category=="mantenimiento_conexion"||s.Category=="transaccion"&&new[]{"BEGIN","COMMIT","ROLLBACK"}.Contains(s.Purpose))).Sum(s=>s.Calls);
                Check(samples.Sum(s=>s.Commands.Length)+untraced==batch.statements,"Trazas de la solicitud y PostgreSQL concilian; sin tráfico de preparación");
                Check(queuedAfter-queuedBefore==(mode=="manual_sin_cambios"?samples.Count:0),"Actividad manual exacta; automática cero");
                var updates=sql.Where(s=>!s.Excluded&&s.Category=="respuestas").Sum(s=>s.Rows);
                Check(updates==(mode is "cambios" or "selecciones_rapidas"?samples.Count:0),"Actualizaciones según cambios reales");
                await ClearCostStats();var verifyClock=Stopwatch.StartNew();
                foreach(var actor in subset)
                {
                    var fresh=await Survey(actor.Client);
                    Check(fresh.Institution["employee"]!.ToString()==$"910{actor.Id:000}","Identidad validada de cada actor");
                    Check(fresh.Version==actor.View.Version&&fresh.Answers.GetValueOrDefault("m-word")==actor.View.Answers.GetValueOrDefault("m-word"),"Última respuesta y versión persistidas por actor");
                }
                phases.Add(new{name=$"verificar {pass}/{users}/{mode} (fuera del guardado)",ms=verifyClock.Elapsed.TotalMilliseconds,sql=await CostSqlSnapshot()});
                Console.WriteLine($"COST PASS {pass} users={users} mode={mode} n={samples.Count} sql={batch.statements} errors={batch.errors}");
            }
            await Report(true);
        }
        catch(Exception e) { await Report(false);Console.WriteLine("COST FAILURE "+e.GetType().Name+" inner="+e.InnerException?.GetType().Name+" at="+e.StackTrace?.Split('\n').FirstOrDefault()?.Trim());throw; }
        finally {foreach(var actor in actors)actor.Client.Dispose();}
    }
    // Schannel no admite el certificado efímero en este host. No omitir autenticación:
    // las cookies cifradas/CSRF son del servidor real; sólo se simula el esquema TLS.
    private sealed class CostCookieTransport(Uri upstream,string marker):DelegatingHandler(new HttpClientHandler{AllowAutoRedirect=false,UseCookies=false,UseProxy=false})
    {
        private readonly CookieContainer cookies=new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var secure=request.RequestUri!;
            if(secure.Host!="127.0.0.1"||secure.Port!=upstream.Port)throw new InvalidOperationException("Only isolated loopback");
            request.Headers.TryAddWithoutValidation("Cookie",cookies.GetCookieHeader(secure));
            request.Headers.Add("X-Synthetic-Tls",marker);
            request.RequestUri=new Uri(upstream,secure.PathAndQuery);
            var response=await base.SendAsync(request,ct);
            if(response.Headers.TryGetValues("Set-Cookie",out var values))foreach(var value in values)cookies.SetCookies(secure,value);
            return response;
        }
    }
    private static object CostDistribution(IEnumerable<double> values)
    {
        var sorted=values.Order().ToArray();return new {n=sorted.Length,p50=sorted[(int)Math.Ceiling(sorted.Length*.5)-1],p95=sorted[(int)Math.Ceiling(sorted.Length*.95)-1],min=sorted[0],max=sorted[^1],mean=sorted.Average()};
    }
    private async Task<object> ObserveCostLocks(CancellationToken stop)
    {
        var samples=0;var peak=0L;var waiting=0L;double queryMs=0;var events=new Dictionary<string,long>();
        await using var connection=new NpgsqlConnection(new NpgsqlConnectionStringBuilder(shared.Admin){ApplicationName="isolated_cost_observer"}.ConnectionString);await connection.OpenAsync();
        while(!stop.IsCancellationRequested)
        {
            var timer=Stopwatch.StartNew();
            await using var command=new NpgsqlCommand("SELECT wait_event_type,wait_event,count(*) FROM pg_stat_activity WHERE usename IN ('cd_native_app','cd_native_nexo','tdv2_native_app') AND state='active' AND wait_event_type='Lock' GROUP BY wait_event_type,wait_event",connection);
            await using(var reader=await command.ExecuteReaderAsync())
            {var count=0L;while(await reader.ReadAsync()){var key=reader.GetString(0)+":"+reader.GetString(1);var n=reader.GetInt64(2);events[key]=events.GetValueOrDefault(key)+n;count+=n;}peak=Math.Max(peak,count);waiting+=count;}
            queryMs+=timer.Elapsed.TotalMilliseconds;samples++;
            try{await Task.Delay(25,stop);}catch(OperationCanceledException){break;}
        }
        return new{samples,peakWaiting=peak,waitingBackendSamples=waiting,events,instrumentationSqlClientMs=queryMs,pollMs=25};
    }
}
