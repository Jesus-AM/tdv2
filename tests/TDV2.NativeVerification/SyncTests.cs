using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Tdv2.Synchronization;
namespace Tdv2.NativeVerification;

internal static partial class NativeTests
{
    private const string SyncPath = "/configuracion/sincronizaciones";
    internal static async Task SetupSync(NativeDatabase db)
    { await SetupAccess(db); await db.Sql("INSERT INTO fixture_module_roles VALUES(34,51)"); }
    private static object Schedule(bool active = true,int version = 1,bool ilda = false,int minutes = 60,string time = "08:00",string zone = "America/Ciudad_Juarez") =>
        new { version,activa=active,incluir_ilda=ilda,intervalo_minutos=minutes,hora=time,zona_horaria=zone };
    private static async Task<JsonObject> RunSync(NativeApplication app,string name)
    {
        using var scope = app.Services.CreateScope(); var sync = scope.ServiceProvider.GetRequiredService<SyncCoordinator>();
        await sync.Enqueue(name,"comando","consola",false,CancellationToken.None);
        return (await sync.Tick(CancellationToken.None))!;
    }
    private static async Task<string> CatalogRows(NativeDatabase db,string table) => (string)(await db.Scalar("SELECT coalesce(json_agg(t ORDER BY t."+(table=="ilda_informacion_area"?"id_origen":"id_ur")+"),'[]')::text FROM "+table+" t"))!;
    private static bool Completed(JsonObject run) => run["estado"]!.ToString()=="completada";
    private static void RegisterSyncCases(NativeDatabase db,Action<string,Func<NativeApplication,HttpClient,Task>> Test)
    {
        Test("Sync/reglas: horario Juárez, cambio estacional y zona inválida",async (app,client) =>
        {
            var s = new SyncSchedule(1,true,1440,"08:00","America/Ciudad_Juarez",false);
            var next = s.Next(DateTimeOffset.Parse("2026-09-30T15:00:00Z")); Check(next==DateTimeOffset.Parse("2026-10-01T14:00:00Z"));
            Check((s with { Time="02:30" }).Next(DateTimeOffset.Parse("2026-03-08T07:00:00Z"))==DateTimeOffset.Parse("2026-03-08T09:30:00Z"));
            Check((s with { Minutes=15 }).Next(next)==next.AddMinutes(15));
            try { SyncSchedule.ZoneInfo("incorrecta"); Check(false); } catch (SyncProblem) { }
            await Task.CompletedTask;
        });
        Test("Sync/conector simulado: SELECT fijos, READ COMMITTED y valores SII íntegros",async (app,client) =>
        {
            app.Sources.Sii.Rows[0]["NUM_EMPLEADO"]="00123"; app.Sources.Sii.Rows[0]["ENCARGADO"]=" Persona Á ";
            app.Sources.Sii.Rows[1]["TIPO_UR"]=true; app.Sources.Sii.Rows[1]["ENCARGADO"]=false;
            using var scope=app.Services.CreateScope(); var rows=await scope.ServiceProvider.GetRequiredService<ICatalogSource>().Read("sii",default);
            Check(rows[0]["num_empleado"]!.ToString()=="00123" && rows[0]["encargado"]!.ToString()==" Persona Á ");
            Check(rows[1]["tipo_ur"]!.ToString()=="1" && rows[1]["encargado"]!.ToString()=="0");
            Check(app.Sources.Queries.SequenceEqual(new[] { "SET TRANSACTION ISOLATION LEVEL READ COMMITTED",CatalogSource.SiiSelect+CatalogSource.SiiLatest }));
            Check(rows.Count==4 && rows[0].Count==10);
        });
        Test("Sync/conector simulado: ILDA completa conserva columnas, nulos, espacios y binarios reversibles",async (app,client) =>
        {
            app.Sources.Ilda.Columns.Add("binario",typeof(object)); app.Sources.Ilda.Rows[0]["binario"]=new byte[] {0,255,1};
            Check(Completed(await RunSync(app,"ilda")));
            Check((await db.Scalar("SELECT datos->>'extra' FROM ilda_informacion_area WHERE id_origen='1'"))!.ToString()=="Valor íntegro áéíóú");
            Check((await db.Scalar("SELECT datos->>'ur2' FROM ilda_informacion_area WHERE id_origen='2'"))!.ToString()==" 110 ");
            Check((await db.Scalar("SELECT ur2 FROM ilda_informacion_area WHERE id_origen='2'"))!.ToString()=="110");
            Check((await db.Scalar("SELECT datos->'binario'->>'$binary_base64' FROM ilda_informacion_area WHERE id_origen='1'"))!.ToString()=="AP8B");
            Check((bool)(await db.Scalar("SELECT datos->'extra'='null'::jsonb FROM (SELECT datos::jsonb datos FROM ilda_informacion_area WHERE id_origen='2') r"))!);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM ilda_informacion_area"))==5);
        });
        Test("Sync/conector simulado: lectura interrumpida o incompleta jamás publica una fracción",async (app,client) =>
        {
            Check(Completed(await RunSync(app,"ilda"))); var before=await CatalogRows(db,"ilda_informacion_area");
            app.Sources.FailAfter=2; var failed=await RunSync(app,"ilda"); Check(failed["estado"]!.ToString()=="fallida");
            Check(!failed.ToJsonString().Contains("SECRET") && await CatalogRows(db,"ilda_informacion_area")==before);
            app.Sources.FailAfter=-1; app.Sources.Ilda.Columns.Remove("ur2"); Check((await RunSync(app,"ilda"))["estado"]!.ToString()=="fallida");
            Check(await CatalogRows(db,"ilda_informacion_area")==before);
        });
        Test("Sync/conector simulado: límites de filas y bytes rechazan sin truncar catálogos",async (app,client) =>
        {
            var settings=app.Services.GetRequiredService<IOptions<SyncOptions>>().Value; settings.MaxRows=4;
            Check((await RunSync(app,"ilda"))["estado"]!.ToString()=="fallida"); settings.MaxRows=100000; settings.IldaMaxBytes=2;
            Check((await RunSync(app,"ilda"))["estado"]!.ToString()=="fallida");
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM ilda_informacion_area"))==0);
        });
        Test("Sync/SII: cambios y bajas lógicas preservan formato, relación y respuestas",async (app,client) =>
        {
            await Login(app,client); await Csrf(client); var content=(await Form(client))["contenido"]!;
            content["encabezado"]!["responsable"]="Conservar respuesta"; Check((await Save(client,0,content)).IsSuccessStatusCode);
            app.Sources.Sii.Rows.Add("extra",2026,"300","Área extra",null,null,null,"A",2,"Activo");
            Check(Completed(await RunSync(app,"sii"))); app.Sources.Sii.Rows[0]["DESC_UR"]="Nuevo nombre";
            app.Sources.Sii.Rows.RemoveAt(0); Check(Completed(await RunSync(app,"sii")));
            Check((bool)(await db.Scalar("SELECT NOT presente FROM unidades_responsables_poa WHERE id_ur='A'"))!);
            Check((await db.Scalar("SELECT contenido->'encabezado'->>'responsable' FROM formatos_ur WHERE id_ur='A'"))!.ToString()=="Conservar respuesta");
            Check(Convert.ToInt32(await db.Scalar("SELECT version FROM formatos_ur WHERE id_ur='A'"))==1);
            Check((await client.GetAsync("/formatos/A")).StatusCode==HttpStatusCode.Forbidden);
        });
        Test("Sync/SII: publicación concurrente exige revalidar alcance antes de guardar",async (app,client) =>
        {
            await Login(app,client); await Csrf(client); var content=(await Form(client))["contenido"]!;
            using var scope=app.Services.CreateScope(); var publisher=scope.ServiceProvider.GetRequiredService<CatalogPublication>();
            var source=scope.ServiceProvider.GetRequiredService<ICatalogSource>(); app.Sources.Sii.Rows[0]["NUM_EMPLEADO"]="00999";
            var snapshot=publisher.Validate("sii",await source.Read("sii",default));
            await using var connection=new NpgsqlConnection(db.Admin); await connection.OpenAsync();
            await using var transaction=await connection.BeginTransactionAsync();
            await publisher.Apply(new SyncSql(connection,transaction),snapshot,default);
            var saving=Save(client,0,content);
            try
            {
                var waiting=false;
                for(var i=0;i<250 && !waiting;i++)
                {
                    waiting=(bool)(await db.Scalar("SELECT EXISTS(SELECT 1 FROM pg_stat_activity WHERE usename='tdv2_native_app' AND wait_event_type='Lock' AND query LIKE '%unidades_responsables_poa%FOR UPDATE%')"))!;
                    if(!waiting) await Task.Delay(20);
                }
                Check(waiting);
            }
            finally { await transaction.CommitAsync(); }
            var response=await saving; Check(response.StatusCode==HttpStatusCode.Conflict);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM formatos_ur"))==0);
        });
        Test("Sync/SII: responsable por empleado Nexo se revalida tras publicar",async (app,client) =>
        {
            await Login(app,client); await Csrf(client); var content=(await Form(client))["contenido"]!;
            app.Sources.Sii.Rows[0]["NUM_EMPLEADO"]="1"; Check(Completed(await RunSync(app,"sii")));
            Check((await client.GetAsync("/formatos/A")).StatusCode==HttpStatusCode.Forbidden);
            Check((await Save(client,0,content)).StatusCode==HttpStatusCode.Forbidden);
            app.Sources.Sii.Rows[0]["NUM_EMPLEADO"]="0001"; Check(Completed(await RunSync(app,"sii")));
            Check((await client.GetAsync("/formatos/A")).IsSuccessStatusCode);
        });
        Test("Sync/SII: vacío, duplicado, ciclo, ejercicio mixto y reducción conservan copia",async (app,client) =>
        {
            Check(Completed(await RunSync(app,"sii"))); var before=await CatalogRows(db,"unidades_responsables_poa");
            foreach (var invalid in new Action<DataTable>[] { t=>t.Rows.Clear(), t=>t.ImportRow(t.Rows[0]),t=>t.Rows[0]["ID_UR_PERTENECE"]="A4",t=>t.Rows[1]["EJERCICIO"]=2025,t=>t.Rows.RemoveAt(0) })
            {
                app.Sources.Sii=SyntheticSources.Units(); invalid(app.Sources.Sii);
                Check((await RunSync(app,"sii"))["estado"]!.ToString()=="fallida"); Check(await CatalogRows(db,"unidades_responsables_poa")==before);
            }
        });
        Test("Sync/ILDA: duplicados, ID inválido y reducción conservan réplica previa",async (app,client) =>
        {
            Check(Completed(await RunSync(app,"ilda"))); var before=await CatalogRows(db,"ilda_informacion_area");
            foreach (var invalid in new Action<DataTable>[] { t=>t.Rows.Clear(),t=>t.ImportRow(t.Rows[0]),t=>t.Rows[0]["id"]="1e10",t=>t.Rows[0]["ur2"]=new string('x',256) })
            {
                app.Sources.Ilda=SyntheticSources.Inventory(); invalid(app.Sources.Ilda);
                Check((await RunSync(app,"ilda"))["estado"]!.ToString()=="fallida"); Check(await CatalogRows(db,"ilda_informacion_area")==before);
            }
        });
        Test("Sync/ILDA: réplica supera 200 filas y formulario usa igualdad exacta de clave",async (app,client) =>
        {
            for(var i=6;i<=211;i++) app.Sources.Ilda.Rows.Add(i,"100","Registro "+i,null,"");
            app.Sources.Ilda.Rows.Add(212,"1000","No incluir prefijo",null,"");
            Check(Completed(await RunSync(app,"ilda"))); await Login(app,client);
            var form=await Form(client); Check(form["contenido"]!["identificacion"]!.AsArray().Count==200 && form["ilda"]!["aviso"]!.ToString().Contains("200"));
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM ilda_informacion_area"))==212);
            Check(!form["contenido"]!.ToJsonString().Contains("No incluir prefijo"));
        });
        Test("Sync/ILDA: abrir formato sólo consulta copia local sin tocar fuentes",async (app,client) =>
        {
            await Login(app,client); Check((await Form(client))["ilda"]!["estado"]!.ToString()=="pendiente" && app.Sources.Queries.IsEmpty);
            Check(Completed(await RunSync(app,"ilda"))); var calls=app.Sources.Queries.Count;
            app.Sources.Failure="ilda"; app.Services.GetRequiredService<IOptions<SyncOptions>>().Value.IldaEnabled=false;
            var form=await Json(await client.GetAsync("/formatos/A?ur2=200")); Check(app.Sources.Queries.Count==calls);
            Check(form["props"]!["contenido"]!["identificacion"]![0]!["fuente"]!.ToString()=="ILDA");
            Check(form["props"]!["contenido"]!["identificacion"]![0]!["tramite"]!.ToString()=="Constancias sintéticas");
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM formatos_ur"))==0);
        });
        Test("Sync/ILDA: cambios y desaparición de origen no sustituyen respuestas guardadas",async (app,client) =>
        {
            Check(Completed(await RunSync(app,"ilda"))); await Login(app,client); await Csrf(client);
            var content=(await Form(client))["contenido"]!; content["identificacion"]![0]!["tramite"]="Respuesta histórica"; content["identificacion"]![0]!["codigo"]="PO-01";
            Check((await Save(client,0,content)).IsSuccessStatusCode);
            app.Sources.Ilda.Rows[0]["informacion_generada"]="Cambio externo"; app.Sources.Ilda.Rows.Add(6,"100","Registro nuevo",null,"");
            Check(Completed(await RunSync(app,"ilda"))); app.Sources.Ilda.Rows.RemoveAt(0); Check(Completed(await RunSync(app,"ilda")));
            var form=await Form(client); Check(form["contenido"]!["identificacion"]![0]!["tramite"]!.ToString()=="Respuesta histórica");
            Check(form["contenido"]!["identificacion"]!.AsArray().Count==3 && form["ilda"]!["nuevos"]!.GetValue<int>()==1);
            Check(Convert.ToInt32(await db.Scalar("SELECT version FROM formatos_ur"))==1);
            Check((bool)(await db.Scalar("SELECT NOT presente FROM ilda_informacion_area WHERE id_origen='1'"))!);
        });
        Test("Sync/comprobación: lee y valida sin publicaciones, cola ni auditoría",async (app,client) =>
        {
            using var scope=app.Services.CreateScope(); var sync=scope.ServiceProvider.GetRequiredService<SyncCoordinator>();
            Check((await sync.Check("sii",default))["comprobacion"]!.GetValue<bool>());
            Check((await sync.Check("ilda",default))["registros"]!.GetValue<int>()==5);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones"))==0);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_catalogos"))==0);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM activity_logs"))==0);
        });
        Test("Sync/HTTP: administrador real, módulos padre/ruta y CSRF obligatorios",async (app,client) =>
        {
            await Login(app,client); await Csrf(client); Check((await client.PostAsJsonAsync(SyncPath+"/ejecutar",new { fuentes="sii",role="administrador" })).StatusCode==HttpStatusCode.Forbidden);
            await SetupSync(db); client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); Check((int)(await client.PostAsJsonAsync(SyncPath+"/ejecutar",new { fuentes="sii" })).StatusCode==419); await Csrf(client);
            await db.Sql("DELETE FROM fixture_module_roles WHERE modulo_id=51"); Check((await client.GetAsync(SyncPath)).StatusCode==HttpStatusCode.Forbidden);
            await db.Sql("INSERT INTO fixture_module_roles VALUES(34,51); UPDATE fixture_modules SET ruta='/incorrecta' WHERE id=51"); Check((await client.PutAsJsonAsync(SyncPath+"/programacion",Schedule())).StatusCode==HttpStatusCode.Forbidden);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones"))==0);
        });
        Test("Sync/HTTP: representación y vista bloquean GET y endpoints directos",async (app,client) =>
        {
            await SetupSync(db); await Login(app,client); await Csrf(client);
            foreach(var representation in new[] {false,true})
            {
                Check((representation ? await StartRepresentation(client) : await StartPreview(client)).IsSuccessStatusCode); await Context(client);
                Check((await client.GetAsync(SyncPath)).StatusCode==HttpStatusCode.Conflict);
                Check((await client.PutAsJsonAsync(SyncPath+"/programacion",Schedule())).StatusCode==HttpStatusCode.Conflict);
                Check((await client.PostAsJsonAsync(SyncPath+"/ejecutar",new { fuentes="sii" })).StatusCode==HttpStatusCode.Conflict);
                await client.DeleteAsync(representation ? "/actuar-como-usuario" : "/vista-prueba"); await Context(client);
            }
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones"))==0);
        });
        Test("Sync/programación: versión concurrente, validaciones y auditoría",async (app,client) =>
        {
            await SetupSync(db); await Login(app,client); await Csrf(client);
            Check((int)(await client.PutAsJsonAsync(SyncPath+"/programacion",Schedule(minutes:1))).StatusCode==422);
            Check((int)(await client.PutAsJsonAsync(SyncPath+"/programacion",Schedule(zone:"incorrecta"))).StatusCode==422);
            var saved=await Task.WhenAll(client.PutAsJsonAsync(SyncPath+"/programacion",Schedule()),client.PutAsJsonAsync(SyncPath+"/programacion",Schedule()));
            Check(saved.Count(r=>r.IsSuccessStatusCode)==1 && saved.Count(r=>r.StatusCode==HttpStatusCode.Conflict)==1);
            Check(Convert.ToInt32(await db.Scalar("SELECT version FROM sincronizacion_configuracion"))==2);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM activity_logs WHERE action='configurar'"))==1);
        });
        Test("Sync/cola: 202 persistente, sin descarga HTTP y exclusión de solicitudes",async (app,client) =>
        {
            await SetupSync(db); await Login(app,client); await Csrf(client);
            var queued=await Task.WhenAll(client.PostAsJsonAsync(SyncPath+"/ejecutar",new {fuentes="ambas"}),client.PostAsJsonAsync(SyncPath+"/ejecutar",new {fuentes="ilda"}));
            Check(queued.Count(r=>r.StatusCode==HttpStatusCode.Accepted)==1 && queued.Count(r=>r.StatusCode==HttpStatusCode.Conflict)==1);
            Check(app.Sources.Queries.IsEmpty);
            var props=(await Json(await client.GetAsync(SyncPath)))["props"]!;
            Check(props["historial"]!.AsArray().Count==1 && props["estado"]!["ejecucion_activa"] is not null);
            Check(!props["configuracion"]!.AsObject().ContainsKey("propietario"));
            using var scope=app.Services.CreateScope(); Check(Completed((await scope.ServiceProvider.GetRequiredService<SyncCoordinator>().Tick(default))!));
        });
        Test("Sync/automática: incluye SII, ILDA opcional, intervalos vencidos no se acumulan",async (app,client) =>
        {
            using var scope=app.Services.CreateScope(); var sync=scope.ServiceProvider.GetRequiredService<SyncCoordinator>();
            await db.Sql("UPDATE sincronizacion_configuracion SET activa=true,proxima_en=timezone('UTC',clock_timestamp())-interval '3 days'");
            Check((await sync.Tick(default))!["fuentes"]!.ToString()=="sii"); Check(await sync.Tick(default) is null);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM ilda_informacion_area"))==0);
            await db.Sql("UPDATE sincronizacion_configuracion SET incluir_ilda=true,proxima_en=timezone('UTC',clock_timestamp())-interval '1 minute'");
            Check((await sync.Tick(default))!["fuentes"]!.ToString()=="ambas"); Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones"))==2);
        });
        Test("Sync/ILDA deshabilitada: valida manual/activa y conserva SII y copia local",async (app,client) =>
        {
            await SetupSync(db); await Login(app,client); await Csrf(client); app.Services.GetRequiredService<IOptions<SyncOptions>>().Value.IldaEnabled=false;
            Check((int)(await client.PostAsJsonAsync(SyncPath+"/ejecutar",new {fuentes="ilda"})).StatusCode==422);
            Check((int)(await client.PutAsJsonAsync(SyncPath+"/programacion",Schedule(ilda:true))).StatusCode==422);
            Check((await client.PutAsJsonAsync(SyncPath+"/programacion",Schedule(active:false,ilda:true))).IsSuccessStatusCode);
            Check(Completed(await RunSync(app,"sii")));
        });
        Test("Sync/parcial: conserva éxito por fuente y nunca publica secretos del error",async (app,client) =>
        {
            app.Sources.Failure="ilda"; var run=await RunSync(app,"ambas"); Check(run["estado"]!.ToString()=="parcial");
            Check(run["resultado"]!["sii"]!["estado"]!.ToString()=="completada");
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_catalogos"))==1 && !run.ToJsonString().Contains("SECRET"));
            app.Sources.Failure="sii"; run=await RunSync(app,"ambas"); Check(run["estado"]!.ToString()=="parcial" && run["resultado"]!["ilda"]!["estado"]!.ToString()=="completada");
            Check(!(await db.Scalar("SELECT coalesce(string_agg(meta::text,''),'') FROM activity_logs"))!.ToString()!.Contains("SECRET"));
        });
        Test("Sync/rollback: error SQL revierte toda publicación y admite reintento",async (app,client) =>
        {
            Check(Completed(await RunSync(app,"ilda"))); var before=await CatalogRows(db,"ilda_informacion_area"); app.Sources.Ilda.Rows[0]["extra"]="Cambio";
            await db.Sql("CREATE FUNCTION fixture_fail_catalog() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'SYNTHETIC_CATALOG_SECRET'; END $$; CREATE TRIGGER fail_catalog BEFORE INSERT ON ilda_informacion_area FOR EACH ROW EXECUTE FUNCTION fixture_fail_catalog()");
            try { Check((await RunSync(app,"ilda"))["estado"]!.ToString()=="fallida"); Check(await CatalogRows(db,"ilda_informacion_area")==before); }
            finally { await db.Sql("DROP TRIGGER fail_catalog ON ilda_informacion_area; DROP FUNCTION fixture_fail_catalog()"); }
            Check(Completed(await RunSync(app,"ilda")));
        });
        Test("Sync/auditoría: fallo al publicar revierte catálogo, metadatos y resultado",async (app,client) =>
        {
            await db.Sql("CREATE FUNCTION fixture_fail_sync_audit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.action='sincronizar' THEN RAISE EXCEPTION 'SYNTHETIC_AUDIT_SECRET'; END IF; RETURN NEW; END $$; CREATE TRIGGER fail_sync_audit BEFORE INSERT ON activity_logs FOR EACH ROW EXECUTE FUNCTION fixture_fail_sync_audit()");
            try { Check((await RunSync(app,"ambas"))["estado"]!.ToString()=="fallida"); Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_catalogos"))==0); Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizaciones_institucionales"))==0); }
            finally { await db.Sql("DROP TRIGGER fail_sync_audit ON activity_logs; DROP FUNCTION fixture_fail_sync_audit()"); }
        });
        Test("Sync/auditoría: fallo al configurar o encolar revierte versión y cola",async (app,client) =>
        {
            await SetupSync(db); await Login(app,client); await Csrf(client);
            await db.Sql("CREATE FUNCTION fixture_fail_enqueue_audit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'SYNTHETIC_AUDIT_SECRET'; END $$; CREATE TRIGGER fail_enqueue_audit BEFORE INSERT ON activity_logs FOR EACH ROW EXECUTE FUNCTION fixture_fail_enqueue_audit()");
            try
            {
                Check((int)(await client.PutAsJsonAsync(SyncPath+"/programacion",Schedule())).StatusCode==503);
                Check((int)(await client.PostAsJsonAsync(SyncPath+"/ejecutar",new {fuentes="sii"})).StatusCode==503);
                Check(Convert.ToInt32(await db.Scalar("SELECT version FROM sincronizacion_configuracion"))==1);
                Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones"))==0);
            }
            finally { await db.Sql("DROP TRIGGER fail_enqueue_audit ON activity_logs; DROP FUNCTION fixture_fail_enqueue_audit()"); }
        });
        Test("Sync/comandos: comprobar no escribe y procesar devuelve fallo parcial",async (app,client) =>
        {
            Check(await SyncCommands.Run(app.Services,["--sync-check=ambas"])==0);
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_ejecuciones"))==0);
            Check(await SyncCommands.Run(app.Services,["--sync-check=invalida"])==2);
            using var scope=app.Services.CreateScope(); var sync=scope.ServiceProvider.GetRequiredService<SyncCoordinator>();
            await sync.Enqueue("ambas","comando","consola",false,default); app.Sources.Failure="ilda";
            Check(await SyncCommands.Run(app.Services,["--sync-once"])==1);
            Check((await db.Scalar("SELECT estado FROM sincronizacion_ejecuciones"))!.ToString()=="parcial");
            Check(await SyncCommands.Run(app.Services,["--sync-once"])==0);
        });
        Test("Sync/exclusión: dos trabajadores y reserva persistente permiten una descarga",async (app,client) =>
        {
            using var a=app.Services.CreateScope(); using var b=app.Services.CreateScope(); var first=a.ServiceProvider.GetRequiredService<SyncCoordinator>(); var second=b.ServiceProvider.GetRequiredService<SyncCoordinator>();
            await first.Enqueue("ilda","manual","persona@uacj.mx",false,default); app.Sources.Block="ilda";
            var running=first.Process(default); await app.Sources.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            try { Check(await second.Process(default) is null); try { await second.Enqueue("sii","manual","otra@uacj.mx",false,default); Check(false); } catch (Tdv2.Domain.DomainProblem p) { Check(p.Status==409); } }
            finally { app.Sources.Continue.TrySetResult(); }
            Check(Completed((await running)!)); Check(app.Sources.Queries.Count==1);
        });
        Test("Sync/procesador: latido reciente no renueva ni recupera una reserva vencida",async (app,client) =>
        {
            using var scope=app.Services.CreateScope(); var sync=scope.ServiceProvider.GetRequiredService<SyncCoordinator>();
            await sync.Enqueue("sii","comando","consola",false,default); var claim=await sync.Claim(default);
            await db.Sql("UPDATE sincronizacion_configuracion SET procesador_visto_en=timezone('UTC',clock_timestamp())-interval '10 minutes',reserva_hasta=timezone('UTC',clock_timestamp())-interval '1 minute'");
            await sync.Heartbeat(default);
            Check((bool)(await db.Scalar("SELECT procesador_visto_en>timezone('UTC',clock_timestamp())-interval '10 seconds' AND reserva_hasta<timezone('UTC',clock_timestamp()) AND propietario IS NOT NULL FROM sincronizacion_configuracion"))!);
            await sync.Tick(default); Check((await db.Scalar("SELECT estado FROM sincronizacion_ejecuciones"))!.ToString()=="fallida");
        });
        Test("Sync/reserva: trabajador obsoleto no publica ni libera una ejecución nueva",async (app,client) =>
        {
            using var a=app.Services.CreateScope(); using var b=app.Services.CreateScope(); var first=a.ServiceProvider.GetRequiredService<SyncCoordinator>(); var second=b.ServiceProvider.GetRequiredService<SyncCoordinator>();
            await first.Enqueue("ilda","manual","persona@uacj.mx",false,default); app.Sources.Block="ilda";
            var running=first.Process(default); await app.Sources.Entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
            await db.Sql("UPDATE sincronizacion_configuracion SET reserva_hasta=timezone('UTC',clock_timestamp())-interval '1 minute'");
            Check(await second.Tick(default) is null); var next=await second.Enqueue("sii","manual","otra@uacj.mx",false,default); var claim=await second.Claim(default);
            app.Sources.Continue.TrySetResult(); Check((await running)!["estado"]!.ToString()=="fallida");
            Check((await db.Scalar("SELECT ejecucion_activa::text FROM sincronizacion_configuracion"))!.ToString()==next.ToString());
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM ilda_informacion_area"))==0); Check(Completed(await second.Execute(claim!,default)));
        });
        Test("Sync/reserva: caducidad durante transacción revierte publicación antes del commit",async (app,client) =>
        {
            app.Sources.OnRead=()=>db.Sql("UPDATE sincronizacion_configuracion SET reserva_hasta=timezone('UTC',clock_timestamp())+interval '1 second'");
            await db.Sql("CREATE FUNCTION fixture_slow_catalog() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN IF NEW.id_origen='1' THEN PERFORM pg_sleep(1.3); END IF; RETURN NEW; END $$; CREATE TRIGGER slow_catalog BEFORE INSERT ON ilda_informacion_area FOR EACH ROW EXECUTE FUNCTION fixture_slow_catalog()");
            try { Check((await RunSync(app,"ilda"))["estado"]!.ToString()=="ejecutando"); Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM ilda_informacion_area"))==0); Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizacion_catalogos"))==0); }
            finally { await db.Sql("DROP TRIGGER slow_catalog ON ilda_informacion_area; DROP FUNCTION fixture_slow_catalog()"); }
        });
        Test("Sync/interrupción: proceso terminado conserva parcial y otro proceso recupera reserva",async (app,client) =>
        {
            using var scope=app.Services.CreateScope(); var sync=scope.ServiceProvider.GetRequiredService<SyncCoordinator>(); await sync.Enqueue("ambas","manual","persona@uacj.mx",false,default);
            var start=new ProcessStartInfo("dotnet") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=db.Workspace };
            start.ArgumentList.Add(typeof(NativeTests).Assembly.Location); start.ArgumentList.Add("--sync-child");
            start.Environment["TDV2_SYNC_APP_CONNECTION"]=db.AppConnection; start.Environment["TDV2_SYNC_NEXO_CONNECTION"]=db.NexoConnection;
            using var child=Process.Start(start)!; var errors=child.StandardError.ReadToEndAsync();
            try { Check(await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(40))=="SYNC_RESERVED"); }
            finally { if (!child.HasExited) child.Kill(true); await child.WaitForExitAsync(); }
            Check((await db.Scalar("SELECT estado FROM sincronizacion_ejecuciones"))!.ToString()=="ejecutando");
            await db.Sql("UPDATE sincronizacion_configuracion SET reserva_hasta=timezone('UTC',clock_timestamp())-interval '1 minute'");
            await using var restarted=new NativeApplication(db); using var other=restarted.Services.CreateScope(); var processor=other.ServiceProvider.GetRequiredService<SyncCoordinator>();
            Check(await processor.Tick(default) is null); Check((await db.Scalar("SELECT estado FROM sincronizacion_ejecuciones"))!.ToString()=="parcial");
            Check(Convert.ToInt64(await db.Scalar("SELECT count(*) FROM sincronizaciones_institucionales"))==1);
            await processor.Enqueue("ilda","manual","persona@uacj.mx",false,default); Check(Completed((await processor.Tick(default))!));
        });
    }
}
