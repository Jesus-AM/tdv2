using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tdv2.Competencias;

namespace Tdv2.NativeVerification;

internal sealed partial class DigitalTests
{
    private Task<long> QueuedSaves() => Count(Data("SELECT count(*) FROM cd_activity_events WHERE \"Payload\"->>'categoria'='encuesta' AND \"Payload\"->>'accion'='guardar'"));
    private Task<long> ReceivedSaves() => Count(Nexo("SELECT count(*) FROM registro_actividad WHERE categoria='encuesta' AND accion='guardar'"));
    private Task<long> LocalSaves() => Count(shared.Scalar("SELECT count(*) FROM activity_logs WHERE entity='competencias_digitales' AND action='encuesta:guardar'"));
    private static async Task<long> Count(Task<object?> query) => Convert.ToInt64(await query);
    private void AddManualSaveTests(List<(string Name, Func<NativeApplication, HttpClient, Task> Work)> cases)
    {
        void Test(string name, Func<NativeApplication,HttpClient,Task> work) => cases.Add(("Guardado manual: " + name, work));
        Test("autoguardados de perfil y cinco bloques, más reintentos, sólo auditan localmente", async (app, client) =>
        {
            await Login(app,client); var view=await CompleteAndSave(client);
            Check(await QueuedSaves()==0 && await ReceivedSaves()==0);
            var local=await LocalSaves(); Check(local>=6,"Se conserva la bitácora de cada revisión guardada");
            await Status(client.PutAsJsonAsync(Base+"/api/encuesta",new DigitalSave(view.Version-1,view.Profile,view.Answers,view.Generation)),200);
            Check(await LocalSaves()==local && await QueuedSaves()==0 && await ReceivedSaves()==0);
        });
        Test("confirmar después de autoguardar no modifica encuesta y deduplica doble envío concurrente", async (app,client) =>
        {
            await Login(app,client); var view=await CompleteAndSave(client);
            var before=(await Data("SELECT row_to_json(s)::text FROM cd_surveys s"))!.ToString();
            var input=new DigitalManualSave(view.Version,view.Generation,Guid.NewGuid());
            await Task.WhenAll(Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input),200),Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input),200));
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input),200);
            Check(await QueuedSaves()==1 && await ReceivedSaves()==1);
            Check((await Data("SELECT row_to_json(s)::text FROM cd_surveys s"))!.ToString()==before,"Confirmación sin revisión ni respuestas artificiales");
            // Otro clic explícito tiene su propio UUID, aunque las respuestas no cambien.
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input with { Operation=Guid.NewGuid() }),200);
            Check(await QueuedSaves()==2 && await ReceivedSaves()==2);
        });
        Test("reintento confirmado después de otra revisión no vuelve a auditar ni sobrescribe", async (app,client) =>
        {
            await Login(app,client); var view=await CompleteAndSave(client);
            var input=new DigitalManualSave(view.Version,view.Generation,Guid.NewGuid());
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input),200);
            view.Answers["m-word"]=new("level",0);
            await Status(client.PutAsJsonAsync(Base+"/api/encuesta",new DigitalSave(view.Version,view.Profile,view.Answers,view.Generation)),200);
            var latest=await Survey(client);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input),200);
            Check((await Survey(client)).Version==latest.Version && (await Survey(client)).Answers["m-word"].Level==0);
            Check(await QueuedSaves()==1 && await ReceivedSaves()==1);
        });
        Test("errores de consentimiento, generación, versión, CSRF y sección no crean éxito", async (app,client) =>
        {
            await Login(app,client); var view=await Survey(client);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",new DigitalManualSave(view.Version,view.Generation,Guid.NewGuid())),403);
            view=await Accept(client);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",new DigitalManualSave(view.Version,view.Generation,Guid.Empty)),422);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",new DigitalManualSave(view.Version+1,view.Generation,Guid.NewGuid())),409);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",new DigitalManualSave(view.Version,Guid.NewGuid(),Guid.NewGuid())),409);
            using(var noCsrf=app.Client()) { await Login(app,noCsrf); noCsrf.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); await Status(noCsrf.PostAsJsonAsync(Base+"/api/encuesta/guardar",new DigitalManualSave(view.Version,view.Generation,Guid.NewGuid())),419); }
            view.Answers["m-word"]=new("level",1);
            await Status(client.PutAsJsonAsync(Base+"/api/encuesta",new DigitalSave(view.Version,view.Profile,view.Answers,view.Generation)),422);
            Check(await QueuedSaves()==0 && await ReceivedSaves()==0 && await LocalSaves()==0);
        });
        Test("Nexo caído: captura confirmada, evento manual pendiente y recibo sin duplicados", async (app,client) =>
        {
            await Login(app,client); await Nexo("UPDATE cuentas_postgresql SET habilitada=false");
            var view=await CompleteAndSave(client);
            Check(await QueuedSaves()==0 && await ReceivedSaves()==0);
            var input=new DigitalManualSave(view.Version,view.Generation,Guid.NewGuid());
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input),200);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input),200);
            Check(await QueuedSaves()==1 && await ReceivedSaves()==0 && (await Survey(client)).Version==view.Version);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_activity_events WHERE \"ReceivedAt\" IS NULL AND \"Failure\"='nexo_rechazado'"))==1);
            var local=await LocalSaves();
            await Nexo("UPDATE cuentas_postgresql SET habilitada=true"); await Data("UPDATE cd_activity_events SET \"NextAttemptAt\"=now()");
            using(var scope=app.Services.CreateScope())await scope.ServiceProvider.GetRequiredService<DigitalActivityDelivery>().Flush(100,default);
            Check(await ReceivedSaves()==1);
            // Simula recibo central perdido, conservando UUID/payload original.
            await Data("UPDATE cd_activity_events SET \"ReceivedAt\"=NULL,\"LocalRecordedAt\"=NULL,\"NextAttemptAt\"=now()");
            using(var scope=app.Services.CreateScope())await scope.ServiceProvider.GetRequiredService<DigitalActivityDelivery>().Flush(100,default);
            Check(await ReceivedSaves()==1 && await LocalSaves()==local);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_activity_events WHERE \"DuplicateReceipt\" AND \"ReceivedAt\" IS NOT NULL"))==1);
        });
        Test("rollback de cola y reintento del mismo identificador", async (app,client) =>
        {
            await Login(app,client); var view=await CompleteAndSave(client); var input=new DigitalManualSave(view.Version,view.Generation,Guid.NewGuid());
            await Data("CREATE FUNCTION fixture_reject_event() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'synthetic failure'; END $$; CREATE TRIGGER fixture_reject_event BEFORE INSERT ON cd_activity_events FOR EACH ROW EXECUTE FUNCTION fixture_reject_event()");
            try { using var failed=await client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input); Check(!failed.IsSuccessStatusCode); Check(await QueuedSaves()==0 && await ReceivedSaves()==0); }
            finally {await Data("DROP TRIGGER fixture_reject_event ON cd_activity_events; DROP FUNCTION fixture_reject_event()");}
            Check((await Survey(client)).Version==view.Version);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input),200); Check(await QueuedSaves()==1 && await ReceivedSaves()==1);
        });
        Test("representación y finalizadas rechazan confirmación; finalización conserva su evento", async (app,client) =>
        {
            await Role("administrador"); await Login(app,client); var view=await CompleteAndSave(client);
            var input=new DigitalManualSave(view.Version,view.Generation,Guid.NewGuid());
            await Status(client.PostAsJsonAsync(Base+"/api/representacion",new {email="otra@uacj.mx",motivo="Consulta sintética"}),200);
            var page=await Json(await client.GetAsync(Base+"/encuesta")); client.DefaultRequestHeaders.Remove("X-TDV2-Context"); client.DefaultRequestHeaders.Add("X-TDV2-Context",page["props"]!["contextoEdicion"]!.ToString());
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input),403);
            await Status(client.DeleteAsync(Base+"/api/representacion"),200); client.DefaultRequestHeaders.Remove("X-TDV2-Context");
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/finalizar",new DigitalFinalize(view.Version,view.Generation)),200);
            await Status(client.PostAsJsonAsync(Base+"/api/encuesta/guardar",input),409);
            Check(await QueuedSaves()==0 && await ReceivedSaves()==0);
            Check(Convert.ToInt64(await Nexo("SELECT count(*) FROM registro_actividad WHERE categoria='encuesta' AND accion='finalizar' AND actor_email='persona@uacj.mx'"))==1);
        });
    }
}
