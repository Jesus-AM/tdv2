using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tdv2.Competencias;
namespace Tdv2.NativeVerification;
internal sealed partial class DigitalTests
{
    private void AddEditionActivityTests(List<(string Name, Func<NativeApplication, HttpClient, Task> Work)> cases)
    {
        void Test(string name, Func<NativeApplication,HttpClient,Task> work)=>cases.Add(("Edición/actividad: "+name,work));
        Test("histórico conserva perfil, herramientas y resultado congelados; no admite actualización ni downgrade con auditoría",async(app,client)=>
        {
            var legacy=new DigitalInstrument(await File.ReadAllTextAsync(Path.Combine(shared.Workspace,"tdv2/Contracts/competencias-digitales-v1.json")));
            var profile=new JsonObject();foreach(var f in legacy.Resource["profile"]!.AsArray())profile[f!["id"]!.ToString()]=f["multiple"]?.GetValue<bool>()==true?new JsonArray("Técnica"):f["options"]![0]!.DeepClone();
            profile["puesto"]="Personal docente";var answers=new Dictionary<string,DigitalAnswer>{{"m-bookings",new("level",3)},{"m-word",new("level",0)}};
            var frozen=legacy.Evaluate(profile,answers).Report with { Global=42.123 };
            await using(var db=Db()) {db.Surveys.Add(new(){Employee="0001",Email="persona@uacj.mx",Name="Persona histórica",UnitId="A4",InstrumentVersion=legacy.Version,InstrumentJson=legacy.Resource.ToJsonString(),ProfileJson=profile.ToJsonString(),AnswersJson=JsonSerializer.Serialize(answers,DigitalInstrument.Json),ReportJson=JsonSerializer.Serialize(frozen,DigitalInstrument.Json),CompletedAt=DateTimeOffset.UtcNow,CreatedAt=DateTimeOffset.UtcNow,UpdatedAt=DateTimeOffset.UtcNow});await db.SaveChangesAsync();}
            var before=(await Data("SELECT row_to_json(s)::text FROM cd_surveys s"))!.ToString();await Role("administrador");await Login(app,client);
            var view=await Json(await client.GetAsync(Base+"/api/informe"));Check(view["evaluation"]!["report"]!["global"]!.GetValue<double>()==42.123&&view["editionUpdate"] is null);
            Check(view["profile"]!["puesto"]!.ToString()=="Personal docente"&&view["answers"]!["m-bookings"]!["level"]!.GetValue<int>()==3);
            var aggregate=await Json(await client.GetAsync(Base+"/api/resultados?position="+Uri.EscapeDataString("Personal docente")));
            Check(aggregate["tools"]!.AsArray().Any(t=>t!["id"]!.ToString()=="m-bookings"));
            var survey=await Survey(client);await Status(client.PostAsJsonAsync(Base+"/api/encuesta/actualizar-instrumento",new DigitalUpgrade(survey.Version,"uacj-digital-2026-10-28.1",survey.Generation)),409);
            Check((await Data("SELECT row_to_json(s)::text FROM cd_surveys s"))!.ToString()==before);
            try {await Data("UPDATE cd_surveys SET \"InstrumentVersion\"='uacj-digital-2026-10-28.1'");throw new InvalidOperationException("Se modificó una definitiva");}catch(PostgresException e)when(e.SqlState=="P0001"){}
            try {await using var db=Db();await db.GetService<IMigrator>().MigrateAsync("20261027131404_AdministrativeDigitalReset");throw new InvalidOperationException("Se retiró auditoría");}catch(PostgresException e)when(e.SqlState=="P0001"){}
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM \"__CDMigrationsHistory\""))==3);
        });
        Test("función Nexo real confirma aplicación/actor y deduplica navegación, lectura y guardado idéntico",async(app,client)=>
        {
            await Login(app,client);await Status(client.GetAsync(Base),200);await Status(client.GetAsync(Base),200);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE accion='acceder'"))==1);
            var view=await CompleteAndSave(client);await Status(client.GetAsync(Base+"/mi-informe-ruta"),200);await Status(client.GetAsync(Base+"/mi-informe-ruta"),200);
            var count=Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad"));
            await Status(client.PutAsJsonAsync(Base+"/api/encuesta",new DigitalSave(view.Version,view.Profile,view.Answers,view.Generation)),200);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad"))==count);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/finalizar",new DigitalFinalize(view.Version,view.Generation)),200);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/finalizar",new DigitalFinalize(view.Version,view.Generation)),200);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE accion='finalizar' AND categoria='encuesta'"))==1);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE aplicacion_origen_id<>87 OR actor_email<>'persona@uacj.mx'"))==0);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_activity_events WHERE \"ReceivedAt\" IS NULL"))==0);
            Check(Convert.ToInt64(await shared.Scalar("SELECT count(*) FROM activity_logs WHERE entity='competencias_digitales' AND action<>'encuesta:guardar'"))==Convert.ToInt64(await Data("SELECT count(*) FROM cd_activity_events")));
            await Status(client.PostAsync("/logout",null),200);Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE accion='cierre_sesion'"))==1);
        });
        Test("fallo central conserva captura y cola; reintento recibe duplicado sin duplicar auditoría local",async(app,client)=>
        {
            await Login(app,client);await Nexo("UPDATE cuentas_postgresql SET habilitada=false");
            var view=await Accept(client);var full=Complete(view);await Status(client.PutAsJsonAsync(Base+"/api/encuesta",new DigitalSave(view.Version,full.Profile,view.Answers,view.Generation)),200);
            var saved=await Survey(client);Check(saved.Profile["puesto"] is not null);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",new DigitalManualSave(saved.Version,saved.Generation,Guid.NewGuid())),200);
            var localCount=Convert.ToInt64(await shared.Scalar("SELECT count(*) FROM activity_logs WHERE entity='competencias_digitales'"));
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_activity_events WHERE \"Failure\"='nexo_rechazado' AND \"ReceivedAt\" IS NULL"))>0);
            await Nexo("UPDATE cuentas_postgresql SET habilitada=true");await Data("UPDATE cd_activity_events SET \"NextAttemptAt\"=now()");
            using(var scope=app.Services.CreateScope())await scope.ServiceProvider.GetRequiredService<DigitalActivityDelivery>().Flush(100,default);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_activity_events WHERE \"ReceivedAt\" IS NULL"))==0);
            var count=Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad"));
            await Data("UPDATE cd_activity_events SET \"ReceivedAt\"=NULL,\"LocalRecordedAt\"=NULL,\"NextAttemptAt\"=now()");
            using(var a=app.Services.CreateScope())using(var b=app.Services.CreateScope())
                await Task.WhenAll(a.ServiceProvider.GetRequiredService<DigitalActivityDelivery>().Flush(100,default),b.ServiceProvider.GetRequiredService<DigitalActivityDelivery>().Flush(100,default));
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad"))==count);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_activity_events WHERE NOT \"DuplicateReceipt\" OR \"ReceivedAt\" IS NULL"))==0);
            Check(Convert.ToInt64(await shared.Scalar("SELECT count(*) FROM activity_logs WHERE entity='competencias_digitales'"))==localCount);
            Check(!(await Data("SELECT string_agg(\"Payload\"::text,' ') FROM cd_activity_events"))!.ToString()!.Contains("answers"));
        });
        Test("representación atribuye consulta al actor real, conserva afectado y no envía token",async(app,client)=>
        {
            await Role("administrador");await Login(app,client);await Status(client.GetAsync(Base),200);
            await Status(client.PostAsJsonAsync(Base+"/api/representacion",new{email="otra@uacj.mx",motivo="Verificación sintética de actividad"}),200);
            await Status(client.GetAsync(Base+"/mi-informe-ruta"),200);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE accion='consultar_representado' AND actor_email='persona@uacj.mx' AND usuario_afectado_email='otra@uacj.mx'"))==1);
            Check(!(await Data("SELECT string_agg(\"Payload\"::text,' ') FROM cd_activity_events"))!.ToString()!.Contains("token"));
            await Status(client.DeleteAsync(Base+"/api/representacion"),200);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE categoria='representacion'"))==2);
        });
        Test("borrador anterior se actualiza explícitamente, archiva respuestas y rechaza pestaña antigua",async(app,client)=>
        {
            var legacy=new DigitalInstrument(await File.ReadAllTextAsync(Path.Combine(shared.Workspace,"tdv2/Contracts/competencias-digitales-v1.json")));
            var eval=legacy.Evaluate(new(),new Dictionary<string,DigitalAnswer>());var initial=new DigitalSurveyView(0,null,new(),new(),new(),legacy.Resource,false,eval,legacy.Navigation(eval,true));
            var input=Complete(initial);input.Profile["puesto"]="Personal operativo";input.Profile["funciones"]=new JsonArray("Técnica","Administrativa");input.Answers["m-bookings"]=new("level",3);
            await using(var db=Db()) { db.Surveys.Add(new(){Employee="0001",Email="persona@uacj.mx",Name="Persona sintética",UnitId="A4",InstrumentVersion=legacy.Version,InstrumentJson=legacy.Resource.ToJsonString(),ProfileJson=input.Profile.ToJsonString(),AnswersJson=JsonSerializer.Serialize(input.Answers,DigitalInstrument.Json),CreatedAt=DateTimeOffset.UtcNow,UpdatedAt=DateTimeOffset.UtcNow});db.Consents.Add(new(){Employee="0001",Email="persona@uacj.mx",NoticeVersion=legacy.NoticeVersion,AcceptedAt=DateTimeOffset.UtcNow});await db.SaveChangesAsync(); }
            await Login(app,client);var before=await Survey(client);Check(before.EditionUpdate is not null&&before.Accepted);
            try {await Data("UPDATE cd_surveys SET \"InstrumentVersion\"='uacj-digital-2026-10-28.1'");throw new InvalidOperationException("Edición sin copia aceptada");}catch(PostgresException e)when(e.SqlState=="P0001"){}
            await Status(client.PutAsJsonAsync(Base+"/api/encuesta",new DigitalSave(before.Version,before.Profile,before.Answers,before.Generation)),409);
            var upgrade=new DigitalUpgrade(before.Version,before.EditionUpdate!.Current,before.Generation);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/actualizar-instrumento",upgrade),200);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/actualizar-instrumento",upgrade),200);
            var after=await Survey(client);Check(after.Version==before.Version+1&&after.EditionUpdate is null&&after.Navigation.Resume=="perfil");
            Check(!after.Profile.ContainsKey("puesto")&&!after.Answers.ContainsKey("m-bookings")&&after.Answers["m-word"]==before.Answers["m-word"]);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_consents"))==1);
            Check((await Data("SELECT \"InstrumentJson\"->'previousDraft'->'answers'->'m-bookings'->>'level' FROM cd_surveys WHERE \"Employee\"='0001'"))!.ToString()=="3");
            await Status(client.PutAsJsonAsync(Base+"/api/encuesta",new DigitalSave(before.Version,before.Profile,before.Answers,before.Generation)),409);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE accion='actualizar_instrumento'"))==1);
        });
    }
}
