using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Tdv2.Competencias;

namespace Tdv2.NativeVerification;

internal sealed partial class DigitalTests
{
    private async Task PopulationPerson(int id, string employee, string unit, bool survey = true)
    {
        var email = $"population-{id}@uacj.mx";
        await Nexo("INSERT INTO fixture_users VALUES($1,$2,$3,'Persona sintética','individual',$4,$5,'adscripcion')", 1000L + id, 2000L + id, email, employee, unit);
        await Nexo("INSERT INTO fixture_roles VALUES($1,$2,31,'participante','Participante')", 1000L + id, email);
        if (!survey)
        {
            await Nexo("UPDATE fixture_roles SET rol_id=999 WHERE email=$1", email);
            await Nexo("INSERT INTO fixture_module_roles VALUES(999,103)"); // Informe personal, sin módulo de encuesta.
        }
    }
    private Task HistoricalTarget(int value) => Data("INSERT INTO cd_targets(\"UnitId\",\"Value\",\"Version\",\"UpdatedAt\",\"UpdatedBy\") VALUES('A',$1,1,now(),'synthetic@uacj.mx')", value);
    private async Task BrowserPopulation()
    {
        for (var i = 3; i <= 10; i++) { await PopulationPerson(i, i.ToString("D4"), "A4"); await Seed(i.ToString("D4"), "A4", 1, i <= 8); }
        await shared.Sql("INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,desc_ur,nivel_ur,estatus_ur,presente,tipo_ur) VALUES('ROOT',2026,'000','Institución sintética',1,'Activo',true,'N'); UPDATE unidades_responsables_poa SET id_ur_pertenece='ROOT' WHERE id_ur IN ('A','B'); UPDATE unidades_responsables_poa SET tipo_ur='N' WHERE id_ur='A3'");
    }
    private async Task PopulationResponsibility()
    {
        await Role("responsable_ur");
        await shared.Sql("UPDATE unidades_responsables_poa SET num_empleado=NULL WHERE id_ur='A'; UPDATE unidades_responsables_poa SET num_empleado='0001' WHERE id_ur='A3'");
    }
    private void AddPopulationTests(List<(string Name, Func<NativeApplication, HttpClient, Task> Work)> cases)
    {
        void Test(string name, Func<NativeApplication, HttpClient, Task> work) => cases.Add(("Población: " + name, work));
        Test("10 personas, 6 finalizadas, 2 en curso, 2 sin iniciar; roles duplicados y sin usuarios locales", async (app, client) =>
        {
            for (var i = 3; i <= 10; i++) await PopulationPerson(i, i.ToString("D4"), "A4");
            for (var i = 1; i <= 8; i++) await Seed(i.ToString("D4"), "A4", 0, i <= 6);
            await Seed("0009", "A4", 0, false);
            await Data("UPDATE cd_surveys SET \"ProfileJson\"='{}',\"AnswersJson\"='{}' WHERE \"Employee\"='0009'");
            // La persona 10 tiene acceso a la aplicación, pero ningún permiso explícito a Encuesta.
            await Nexo("DELETE FROM fixture_roles WHERE usuario_aplicacion_id=1010");
            await Nexo("INSERT INTO fixture_roles VALUES(1010,'population-10@uacj.mx',999,'participante','Participante'); INSERT INTO fixture_module_roles VALUES(999,103)");
            await PopulationPerson(11, "0011", "A4", false); await Seed("0011", "A4", 3, true);
            await Nexo("DELETE FROM fixture_roles WHERE usuario_aplicacion_id=1011"); // Sin acceso vigente a la aplicación.
            await Nexo("INSERT INTO fixture_roles SELECT * FROM fixture_roles WHERE usuario_aplicacion_id=1003");
            await Nexo("UPDATE fixture_users SET email=upper(email) WHERE usuario_aplicacion_id=1004; UPDATE fixture_roles SET email=upper(email) WHERE usuario_aplicacion_id=1004");
            await HistoricalTarget(999); await Role("administrador"); await Login(app, client);
            var progress = await Json(await client.GetAsync(Base + "/api/avance?unit=A&level=2")); var s = progress["summary"]!;
            Check(s["populationAvailable"]!.GetValue<bool>() && s["target"]!.GetValue<int>() == 10);
            Check(s["completed"]!.GetValue<int>() == 6 && s["inProgress"]!.GetValue<int>() == 2 && s["notStarted"]!.GetValue<int>() == 2);
            Check(s["participation"]!.GetValue<double>() == 60 && s["maturity"]!.GetValue<double>() == 0);
            Check(progress["units"]!.AsArray().Count == 1 && JsonNode.DeepEquals(progress["units"]![0]!["summary"], s));
            Check(Convert.ToInt64(await shared.Scalar("SELECT count(*) FROM users WHERE email LIKE 'population-%'")) == 0);
            Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_surveys WHERE \"Employee\"='0010'")) == 0);
            Check(Convert.ToInt64(await Data("SELECT \"Value\" FROM cd_targets WHERE \"UnitId\"='A'")) == 999);
            await Status(client.PutAsJsonAsync(Base + "/api/metas/A", new DigitalTargetInput(1, 1, (await Survey(client)).Generation)), 410);
            await Status(client.GetAsync(Base + "/api/avance?position=Dirección"), 422);
        });
        Test("adscripción vigente, revocación y resultados congelados separados", async (app, client) =>
        {
            await Seed("0002", "A3", 3, true); await Role("administrador"); await Login(app, client);
            await Nexo("UPDATE fixture_users SET \"ID_UR\"='B4',email='alias-nuevo@uacj.mx' WHERE usuario_id=21; UPDATE fixture_roles SET email='alias-nuevo@uacj.mx' WHERE usuario_aplicacion_id=11");
            var a = await Json(await client.GetAsync(Base + "/api/avance?unit=A"));
            var b = await Json(await client.GetAsync(Base + "/api/avance?unit=B"));
            Check(a["summary"]!["target"]!.GetValue<int>() == 1 && a["summary"]!["completed"]!.GetValue<int>() == 0);
            Check(b["summary"]!["target"]!.GetValue<int>() == 1 && b["summary"]!["completed"]!.GetValue<int>() == 1);
            var historical = await Json(await client.GetAsync(Base + "/api/resultados?unit=A")); Check(historical["summary"]!["completed"]!.GetValue<int>() == 1);
            var frozen = await Data("SELECT \"ReportJson\"::text FROM cd_surveys WHERE \"Employee\"='0002'");
            await Nexo("DELETE FROM fixture_roles WHERE usuario_aplicacion_id=11");
            b = await Json(await client.GetAsync(Base + "/api/avance?unit=B")); Check(b["summary"]!["target"]!.GetValue<int>() == 0 && b["summary"]!["participation"] is null && b["summary"]!["maturity"] is null);
            historical = await Json(await client.GetAsync(Base + "/api/resultados?unit=A")); Check(historical["summary"]!["completed"]!.GetValue<int>() == 1);
            Check(Equals(frozen, await Data("SELECT \"ReportJson\"::text FROM cd_surveys WHERE \"Employee\"='0002'")));
        });
        Test("opciones sin nivel 1 ni tipo N conservan descendientes y alcances 2/3", async (app, client) =>
        {
            await shared.Sql("INSERT INTO unidades_responsables_poa(id_ur,ejercicio,cve_ur,desc_ur,nivel_ur,estatus_ur,presente,tipo_ur) VALUES('ROOT',2026,'000','Institución sintética',1,'Activo',true,'N'); UPDATE unidades_responsables_poa SET id_ur_pertenece='ROOT' WHERE id_ur IN ('A','B'); UPDATE unidades_responsables_poa SET tipo_ur='N' WHERE id_ur='A3'");
            await Role("administrador"); await Login(app, client);
            foreach (var endpoint in new[] { "avance", "resultados" })
            {
                var data = await Json(await client.GetAsync(Base + "/api/" + endpoint));
                Check(data["selectedLevel"]!.GetValue<int>() == 2 && data["levels"]!.ToJsonString() == "[2,3,4,5]");
                Check(!data["levels"]!.AsArray().Any(n => n!.GetValue<int>() == 1));
                Check(!data["options"]!.AsArray().Any(n => n!["id"]!.ToString() is "ROOT" or "A3"));
                Check(data["options"]!.AsArray().Single(n => n!["id"]!.ToString() == "A4")!["ancestors"]!.AsArray().Any(n => n!.ToString() == "A"));
                Check(data["options"]!.AsArray().Single(n => n!["id"]!.ToString() == "A4")!["parentName"]!.ToString().Contains("Área sintética A3"));
                await Status(client.GetAsync(Base + "/api/" + endpoint + "?level=1"), 403);
                await Status(client.GetAsync(Base + "/api/" + endpoint + "?unit=A3"), 422);
            }
            var progress = await Json(await client.GetAsync(Base + "/api/avance?unit=A&level=2")); Check(progress["summary"]!["target"]!.GetValue<int>() == 2);
            await shared.Sql("UPDATE unidades_responsables_poa SET tipo_ur='N' WHERE nivel_ur=3");
            foreach (var endpoint in new[] { "avance", "resultados" })
            {
                var grouped = await Json(await client.GetAsync(Base + "/api/" + endpoint + "?unit=A&level=3"));
                Check(grouped["levels"]!.AsArray().Any(n => n!.GetValue<int>() == 3));
                Check(grouped["units"]!.AsArray().Any(n => n!["id"]!.ToString() == "A3"));
                Check(!grouped["options"]!.AsArray().Any(n => n!["level"]!.GetValue<int>() == 3));
                if (endpoint == "avance") Check(grouped["summary"]!["target"]!.GetValue<int>() == 2);
            }
            await Role("responsable_ur_supervisor"); Check((await Json(await client.GetAsync(Base + "/api/avance")))["institutional"]!.GetValue<bool>());
            await Role("responsable_ur"); progress = await Json(await client.GetAsync(Base + "/api/avance"));
            Check(progress["levels"]!.AsArray().Count == 0 && !progress["options"]!.AsArray().Any(n => n!["id"]!.ToString() == "B"));
            await Status(client.GetAsync(Base + "/api/avance?unit=B"), 403);
            await shared.Sql("UPDATE unidades_responsables_poa SET num_empleado=NULL WHERE id_ur='A'; UPDATE unidades_responsables_poa SET num_empleado='0001' WHERE id_ur='A3'");
            progress = await Json(await client.GetAsync(Base + "/api/avance")); Check(progress["summary"]!["target"]!.GetValue<int>() == 2);
            Check(progress["units"]!.AsArray().Any(n => n!["id"]!.ToString() == "A3") && progress["options"]!.AsArray().All(n => n!["id"]!.ToString() == "A4"));
            await Status(client.GetAsync(Base + "/api/avance?unit=A31"), 403);
            await Status(client.GetAsync(Base + "/api/resultados?unit=A"), 403);
        });
        Test("población ambigua o fuente caída no se presenta como cero; captura independiente", async (app, client) =>
        {
            await Role("administrador"); await Login(app, client);
            await Nexo("UPDATE fixture_users SET origen_ur='multiple',\"ID_UR\"=NULL WHERE usuario_id=21");
            var progress = await Json(await client.GetAsync(Base + "/api/avance"));
            Check(progress["summary"]!["populationAvailable"]!.GetValue<bool>() == false && progress["summary"]!["target"] is null && progress["summary"]!["completed"] is null);
            await Nexo("UPDATE fixture_users SET origen_ur='adscripcion',\"ID_UR\"='A3',num_empleado='0001' WHERE usuario_id=21");
            progress = await Json(await client.GetAsync(Base + "/api/avance")); Check(progress["summary"]!["target"]!.GetValue<int>()==2);
            await Nexo("UPDATE fixture_users SET num_empleado='0002' WHERE usuario_id=21");
            // El doble simula fallo exclusivamente en lectura colectiva; las consultas personales siguen disponibles.
            await Nexo("CREATE FUNCTION fixture_population_unit(text) RETURNS text LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'synthetic population unavailable'; END $$; CREATE OR REPLACE VIEW nexo_usuarios AS SELECT usuario_aplicacion_id,usuario_id,email,nombre,tipo_cuenta,num_empleado,CASE WHEN usuario_id=21 THEN fixture_population_unit(\"ID_UR\") ELSE \"ID_UR\" END AS \"ID_UR\",origen_ur FROM fixture_users");
            try
            {
                progress = await Json(await client.GetAsync(Base + "/api/avance")); Check(progress["summary"]!["target"] is null && !progress["summary"]!["populationAvailable"]!.GetValue<bool>());
                var view = await Accept(client); await Status(client.PutAsJsonAsync(Base + "/api/encuesta", new DigitalSave(view.Version, Complete(view).Profile, view.Answers, view.Generation)), 200);
                Check(Convert.ToInt64(await Data("SELECT count(*) FROM cd_activity_events WHERE \"Payload\"->>'accion'='guardar'")) == 0);
            }
            finally { await Nexo("CREATE OR REPLACE VIEW nexo_usuarios AS SELECT * FROM fixture_users; DROP FUNCTION fixture_population_unit(text)"); }
        });
    }
}
