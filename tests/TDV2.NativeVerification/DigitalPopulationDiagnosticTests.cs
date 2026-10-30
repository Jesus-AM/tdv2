using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Tdv2.Competencias;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Integrations.Nexo;

namespace Tdv2.NativeVerification;

// Fixture mínimo exclusivo del clúster validado; no llama a Migrate ni ejecuta
// reinicios/reaperturas. El historial EF de este fixture es sintético.
internal static class DigitalPopulationDiagnosticTests
{
    public static async Task<int> Run(NativeDatabase fixture)
    {
        await fixture.ValidateCluster();
        await fixture.Sql(await File.ReadAllTextAsync(Path.Combine(fixture.Workspace, "tests/TDV2.NativeVerification/nexo-fixture.sql")));
        await fixture.Sql("""
            CREATE TABLE unidades_responsables_poa(id_ur text,cve_ur text,nivel_ur integer,id_ur_pertenece text,tipo_ur text,estatus_ur text,presente boolean);
            INSERT INTO unidades_responsables_poa VALUES('ROOT','ROOT',1,null,'N','Activo',true),('A','A',2,'ROOT','D','Activo',true),
                ('A3','A3',3,'A','N','Activo',true),('A4','A4',4,'A3','D','Activo',true),('B','B',2,'ROOT','D','Activo',true);
            CREATE TABLE cd_control("Id" int,"Generation" uuid,"Revision" bigint);
            INSERT INTO cd_control VALUES(1,'11111111-1111-1111-1111-111111111111',0);
            CREATE TABLE cd_surveys("Employee" text,"UnitId" text,"CompletedAt" timestamptz,"ReportJson" jsonb,"ProfileJson" jsonb,"AnswersJson" jsonb,"Id" uuid DEFAULT gen_random_uuid(),"Revision" integer DEFAULT 1,"Email" text DEFAULT 'legacy@uacj.mx',"CreatedAt" timestamptz DEFAULT now());
            CREATE TABLE cd_submissions("Generation" uuid,"SurveyId" uuid,"UnitId" text,"Revision" integer,"SnapshotJson" jsonb);
            CREATE TABLE cd_reopen_operations("Id" uuid);
            CREATE TABLE users(id bigint,microsoft_tenant_id uuid,microsoft_id uuid,email text,created_at timestamp DEFAULT '2000-01-01',updated_at timestamp DEFAULT '2000-01-01');
            CREATE TABLE tdv2_identity_links(nexo_user_id bigint,microsoft_tenant_id uuid,microsoft_id uuid,verified_at timestamptz DEFAULT now());
            CREATE TABLE "__CDMigrationsHistory"("MigrationId" text,"ProductVersion" text);
            CREATE TABLE "__EFMigrationsHistory"("MigrationId" text);
            INSERT INTO fixture_app VALUES(87,'competencias-digitales','Aplicación sintética',2);
            INSERT INTO fixture_users SELECT i,i,'persona-'||i||'@uacj.mx','Persona sintética','individual',lpad(i::text,4,'0'),'A4','adscripcion' FROM generate_series(1,10) i;
            INSERT INTO fixture_roles SELECT i,'persona-'||i||'@uacj.mx',1,'participante','Participante' FROM generate_series(1,10) i;
            INSERT INTO fixture_roles SELECT * FROM fixture_roles WHERE usuario_aplicacion_id=2;
            """);
        var password = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
        await fixture.Sql($"CREATE ROLE population_reader LOGIN PASSWORD '{password}'; GRANT USAGE ON SCHEMA public TO population_reader; GRANT SELECT ON ALL TABLES IN SCHEMA public TO population_reader");
        var connection = new NpgsqlConnectionStringBuilder(fixture.Admin) { Username = "population_reader", Password = password }.ConnectionString;
        var configValues = new Dictionary<string,string?> { ["ConnectionStrings:Tdv2"] = connection, ["ConnectionStrings:CompetenciasDigitales"] = connection,
            ["ConnectionStrings:CompetenciasDigitalesNexo"] = connection, ["CompetenciasDigitales:Nexo:ApplicationId"] = "87" };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
        var instrument = new DigitalInstrument(await File.ReadAllTextAsync(Path.Combine(fixture.Workspace, "tdv2/Contracts/competencias-digitales-v2.json")));
        var answers = new Dictionary<string,DigitalAnswer> { [instrument.Blocks[0].Tools[0].Id] = new("level",0) };
        var profile = new JsonObject { ["puesto"] = "Dirección" };
        var report = JsonSerializer.Serialize(instrument.Evaluate(profile, answers).Report, DigitalInstrument.Json);
        for (var i = 1; i <= 9; i++)
            await fixture.Sql("INSERT INTO cd_surveys(\"Employee\",\"UnitId\",\"CompletedAt\",\"ReportJson\",\"ProfileJson\",\"AnswersJson\") VALUES($1,'A4',CASE WHEN $2 THEN now() ELSE null END,CASE WHEN $2 THEN $3::jsonb ELSE null END,$4::jsonb,$5::jsonb)",
                i.ToString("D4"), i <= 6, report, i == 9 ? "{}" : profile.ToJsonString(), i == 9 ? "{}" : JsonSerializer.Serialize(answers, DigitalInstrument.Json));
        await using var db = new DigitalDbContext(new DbContextOptionsBuilder<DigitalDbContext>().UseNpgsql(connection).Options);
        foreach (var migration in db.Database.GetMigrations()) await fixture.Sql("INSERT INTO \"__CDMigrationsHistory\" VALUES($1,'synthetic')", migration);
        var units = new[] { new Unit("ROOT","ROOT",null,1,null,2026,Kind:"N"), new Unit("A","A",null,2,"ROOT",2026, "0001"),
            new Unit("A3","A3",null,3,"A",2026, "0003",Kind:"N"), new Unit("A4","A4",null,4,"A3",2026), new Unit("B","B",null,2,"ROOT",2026) };
        DigitalScope Scope(string role = "administrador", string employee = "0001") => new(new(new("test@uacj.mx","Prueba",employee,"A4"),[new(1,role,role)],[]), new(units));
        var nexo = new DigitalNexo(new(config), config, NullLogger<PostgresNexoProfiles>.Instance, NullLogger<NexoOperations>.Instance);
        var aggregate = new DigitalAggregates(db, instrument, nexo, new(config));
        var results = new List<object>(); var failures = 0;
        void Check(bool value) { if (!value) throw new InvalidOperationException("Condición incumplida"); }
        async Task Test(string name, Func<Task> work)
        {
            try { await work(); results.Add(new { name, passed = true }); Console.WriteLine("PASS " + name); }
            catch (Exception e) { failures++; results.Add(new { name, passed = false, error = DigitalPopulationReadOnly.SafeError(e) }); Console.WriteLine("FAIL " + name + ": " + DigitalPopulationReadOnly.SafeError(e)); }
        }
        async Task<(int Code, string Text, JsonNode[] Rows)> Diagnose(IConfiguration? configuration = null)
        {
            using var text = new StringWriter(); var code = await DigitalPopulationReadOnly.Run(configuration ?? config, text);
            var content = text.ToString();
            Check(!content.Contains(password) && !content.Contains("persona-1@") && !content.Contains("Persona sintética") && !content.Contains("0001"));
            return (code,content,content.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => JsonNode.Parse(l)!).ToArray());
        }
        JsonNode Data(JsonNode[] rows, string phase) => rows.Single(r => r["phase"]!.ToString() == phase && r["status"]!.ToString() == "COUNTS")["data"]!;
        await Test("Diagnóstico completo: sólo lecturas, catálogo activo, aplicación y conteos 10=6+2+2", async () => {
            var before = await fixture.Scalar("SELECT count(*) FROM cd_surveys"); var result = await Diagnose(); Check(result.Code == 0);
            var counts = Data(result.Rows, "survey.counts");
            Check(counts["stored"]!.GetValue<int>() == 9 && counts["level2Population"]!.GetValue<int>() == 10 && counts["level2Finalized"]!.GetValue<int>() == 6
                && counts["level2InProgress"]!.GetValue<int>() == 2 && counts["level2NotStarted"]!.GetValue<int>() == 2 && counts["populationVerified"]!.GetValue<bool>());
            Check(Data(result.Rows,"catalog.hierarchy")["validChains"]!.GetValue<int>() == 5);
            Check(Equals(before, await fixture.Scalar("SELECT count(*) FROM cd_surveys")));
            await File.WriteAllTextAsync(Path.Combine(fixture.Artifacts,"diagnostic-valid.jsonl"),result.Text);
        });
        await Test("Reporte real conserva nivel 2, dependientes N, cero válido y acceso sin módulo Encuesta", async () => {
            var result = await aggregate.Read(Scope(),null,null,default); var s = result.Summary;
            Check(s.Target == 10 && s.Completed == 6 && s.InProgress == 2 && s.NotStarted == 2 && s.Participation == 60 && s.Maturity == 0);
            Check(result.SelectedLevel == 2 && result.Levels.SequenceEqual(new[]{2,3,4,5}) && !result.Options.Any(o => o.Id=="A3"));
            Check(result.Units.Single(u => u.Id=="A").Summary.Target==10);
            foreach (var role in new[]{"responsable_ur_supervisor","responsable_ur"}) Check((await aggregate.Read(Scope(role),null,null,default)).Summary.Target==10);
            Check((await aggregate.Read(Scope("responsable_ur","0003"),null,null,default)).Summary.Target==10);
            try { await aggregate.Read(Scope("responsable_ur"),"B",null,default); Check(false); } catch (DomainProblem p) { Check(p.Status==403); }
        });
        await Test("Una adscripción incompleta se identifica y no oculta las encuestas almacenadas", async () => {
            await fixture.Sql("UPDATE fixture_users SET \"ID_UR\"=NULL,origen_ur='multiple' WHERE usuario_id=10");
            try {
                var result = await Diagnose(); Check(result.Code == 1);
                var issues = Data(result.Rows,"population.validation")["Issues"]!;
                Check(issues["unit_missing"]!.GetValue<int>()==1 && issues["origin_multiple"]!.GetValue<int>()==1);
                Check(Data(result.Rows,"survey.counts")["finalizedStored"]!.GetValue<int>()==6);
                var s = (await aggregate.Read(Scope(),null,null,default)).Summary;
                Check(s.Target is null && s.Completed is null && s.PopulationProblem=="integrity");
                Check(s.Registered is { Stored:9,Completed:6,InProgress:2,WithoutCapture:1 } && s.Participation is null);
                await File.WriteAllTextAsync(Path.Combine(fixture.Artifacts,"diagnostic-incidence.jsonl"),result.Text);
            } finally { await fixture.Sql("UPDATE fixture_users SET \"ID_UR\"='A4',origen_ur='adscripcion' WHERE usuario_id=10"); }
        });
        await Test("Permiso y contrato caídos conservan SQLSTATE original y fase sin datos privados", async () => {
            await fixture.Sql("REVOKE SELECT ON nexo_usuarios FROM population_reader");
            try { var result = await Diagnose(); Check(result.Code==1 && result.Text.Contains("42501") && result.Text.Contains("population.validation"));
                Check(Data(result.Rows,"survey.counts")["stored"]!.GetValue<int>()==9);
                Check((await aggregate.Read(Scope(),null,null,default)).Summary.PopulationProblem=="source");
            } finally { await fixture.Sql("GRANT SELECT ON nexo_usuarios TO population_reader"); }
            await fixture.Sql("ALTER VIEW nexo_usuarios RENAME COLUMN num_empleado TO missing_employee");
            try { var result = await Diagnose(); Check(result.Code==1 && result.Text.Contains("42703")); }
            finally { await fixture.Sql("ALTER VIEW nexo_usuarios RENAME COLUMN missing_employee TO num_empleado"); }
        });
        await Test("Esquema pendiente se informa sin aplicar migraciones ni impedir conteos básicos", async () => {
            await fixture.Sql("DELETE FROM \"__CDMigrationsHistory\" WHERE \"MigrationId\"='20261030042854_IndividualDigitalReopening'");
            try { var result = await Diagnose(); Check(result.Code==1 && result.Text.Contains("SCHEMA_PENDING")); Check(Data(result.Rows,"survey.counts")["stored"]!.GetValue<int>()==9); }
            finally { await fixture.Sql("INSERT INTO \"__CDMigrationsHistory\" VALUES('20261030042854_IndividualDigitalReopening','synthetic')"); }
        });
        await Test("Identificador incorrecto y correos/roles duplicados no amplían la población", async () => {
            var wrong = new ConfigurationBuilder().AddInMemoryCollection(configValues).AddInMemoryCollection(new Dictionary<string,string?>{["CompetenciasDigitales:Nexo:ApplicationId"]="88"}).Build();
            var invalid = await Diagnose(wrong); Check(invalid.Code==1 && Data(invalid.Rows,"nexo.application")["matchesConfiguredId"]!.GetValue<int>()==0);
            await fixture.Sql("UPDATE fixture_users SET email=upper(email) WHERE usuario_id=1; UPDATE fixture_roles SET email=upper(email) WHERE usuario_aplicacion_id=1");
            Check((await aggregate.Read(Scope(),null,null,default)).Summary.Target==10);
            await fixture.Sql("UPDATE fixture_users SET num_empleado='0001' WHERE usuario_id=10");
            try { var result = await Diagnose(); Check(result.Code==1 && Data(result.Rows,"population.validation")["Issues"]!["employee_conflict"]!.GetValue<int>()==1); }
            finally { await fixture.Sql("UPDATE fixture_users SET num_empleado='0010' WHERE usuario_id=10"); }
        });
        await Test("Conexión impone sólo lectura incluso a propietario; saneado de errores no filtra mensajes", async () => {
            await using var c = new NpgsqlConnection(DigitalPopulationReadOnly.ReadOnlyConnection(fixture.Admin)); await c.OpenAsync();
            try { await using var q = new NpgsqlCommand("UPDATE fixture_app SET nombre=nombre",c); await q.ExecuteNonQueryAsync(); Check(false); }
            catch(PostgresException e) { Check(e.SqlState=="25006"); }
            var sanitized = DigitalPopulationReadOnly.SafeError(new PostgresException("SECRET persona@uacj.mx", "ERROR", "ERROR", "42501"));
            Check(sanitized.Contains("42501") && !sanitized.Contains("SECRET") && !sanitized.Contains('@'));
        });
        await Test("Población vacía comprobada se distingue de empleados o cadenas incompletos", async () => {
            await fixture.Sql("UPDATE fixture_users SET num_empleado=NULL WHERE usuario_id=10");
            try { var result = await Diagnose(); Check(result.Code==1 && Data(result.Rows,"population.validation")["Issues"]!["employee"]!.GetValue<int>()==1); }
            finally { await fixture.Sql("UPDATE fixture_users SET num_empleado='0010' WHERE usuario_id=10"); }
            await fixture.Sql("UPDATE unidades_responsables_poa SET estatus_ur='Inactivo' WHERE id_ur='A3'");
            try { var result = await Diagnose(); Check(result.Code==1 && Data(result.Rows,"catalog.hierarchy")["chainReasons"]!["missing_or_inactive_node"]!.GetValue<int>()==1); }
            finally { await fixture.Sql("UPDATE unidades_responsables_poa SET estatus_ur='Activo' WHERE id_ur='A3'"); }
            await fixture.Sql("DELETE FROM fixture_roles");
            try {
                var result = await Diagnose(); Check(result.Code==0 && Data(result.Rows,"survey.counts")["finalizedStored"]!.GetValue<int>()==6);
                var s = (await aggregate.Read(Scope(),null,null,default)).Summary;
                Check(s.PopulationAvailable && s.Target==0 && s.Completed==0 && s.Participation is null && s.Maturity is null);
            } finally { await fixture.Sql("INSERT INTO fixture_roles SELECT usuario_aplicacion_id,email,1,'participante','Participante' FROM fixture_users"); }
        });
        await Test("Tres conflictos son tres grupos de empleados; roles/filas/asignaciones repetidas no inventan canónicos", async () => {
            await fixture.Sql("UPDATE fixture_users SET num_empleado=CASE usuario_id WHEN 8 THEN '0001' WHEN 9 THEN '0002' WHEN 10 THEN '0003' ELSE num_empleado END; INSERT INTO fixture_roles SELECT * FROM fixture_roles; INSERT INTO fixture_users SELECT * FROM fixture_users WHERE usuario_id=1; INSERT INTO fixture_users SELECT 101,usuario_id,email,nombre,tipo_cuenta,num_empleado,\"ID_UR\",origen_ur FROM fixture_users WHERE usuario_id=2; INSERT INTO fixture_roles VALUES(101,'persona-2@uacj.mx',1,'participante','Participante')");
            // La cuenta 1 se había normalizado en una prueba previa; el resto permanece literal.
            try {
                var diagnostic = await Diagnose();
                Check(Data(diagnostic.Rows,"population.validation")["Issues"]!["employee_conflict"]!.GetValue<int>()==3);
                Check(Data(diagnostic.Rows,"population.validation")["Candidates"]!.GetValue<int>()==10);
                var s = (await aggregate.Read(Scope(),null,null,default)).Summary;
                Check(s.Target==10 && s.Participation is null && s.Registered is { Stored:9,Completed:6,InProgress:2 });
                await fixture.Sql("INSERT INTO users(id,microsoft_tenant_id,microsoft_id,email) VALUES(1,'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','verified@uacj.mx'); INSERT INTO tdv2_identity_links(nexo_user_id,microsoft_tenant_id,microsoft_id) VALUES(1,'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222')");
                var ro = new ConfigurationBuilder().AddInMemoryCollection(configValues.Select(k=>new KeyValuePair<string,string?>(k.Key,k.Key.StartsWith("ConnectionStrings:") ? DigitalPopulationReadOnly.ReadOnlyConnection(k.Value!) : k.Value))).Build();
                var export = await DigitalPopulationPrivateConflicts.Export(new DatabaseConnections(ro),87,Path.Combine(fixture.Artifacts,"private-synthetic"));
                Check(export.Groups==3 && export.CanonicalIds==6 && export.SourcesComplete);
                var content = await File.ReadAllTextAsync(Path.Combine(fixture.Artifacts,"private-synthetic",export.FileName));
                Check(!content.Contains('@') && !content.Contains("Persona sintética") && !content.Contains(password) && !content.Contains("22222222-2222-2222-2222-222222222222"));
                var parsed = JsonNode.Parse(content)!;
                Check(parsed["verifiedIdentity"]!["available"]!.GetValue<bool>() && parsed["verifiedIdentity"]!["rows"]![0]!["matchingLocalUsers"]!.GetValue<int>()==1);
                Check(parsed["surveys"]!["available"]!.GetValue<bool>() && parsed["groups"]!.AsArray().Count==3);
                Check(parsed["groups"]![0]!["accounts"]!.AsArray().Any(a=>a!["publishedRows"]!.GetValue<int>()==2));
                await fixture.Sql("REVOKE SELECT ON tdv2_identity_links FROM population_reader");
                try {
                    var incomplete = await DigitalPopulationPrivateConflicts.Export(new DatabaseConnections(ro),87,Path.Combine(fixture.Artifacts,"private-synthetic"));
                    Check(!incomplete.SourcesComplete && incomplete.Groups==3 && incomplete.FileName!=export.FileName);
                } finally { await fixture.Sql("GRANT SELECT ON tdv2_identity_links TO population_reader"); }
            } finally {
                await fixture.Sql("DELETE FROM fixture_users WHERE usuario_aplicacion_id=101; DELETE FROM fixture_users WHERE ctid NOT IN (SELECT DISTINCT ON (usuario_aplicacion_id) ctid FROM fixture_users ORDER BY usuario_aplicacion_id,ctid); UPDATE fixture_users SET num_empleado=lpad(usuario_id::text,4,'0'); DELETE FROM fixture_roles WHERE usuario_aplicacion_id=101");
            }
        });
        await Test("Conteos registrados conservan alcance por UR aunque falle la población; sin denominador sustituto", async () => {
            await fixture.Sql("UPDATE fixture_users SET num_empleado='0001' WHERE usuario_id=10");
            var before = await fixture.Scalar("SELECT md5(string_agg(row_to_json(s)::text,',' ORDER BY \"Employee\")) FROM cd_surveys s");
            try {
                var a = await aggregate.Read(Scope(),"A",null,default);
                var b = await aggregate.Read(Scope(),"B",null,default);
                Check(a.Summary.Registered is { Completed:6,InProgress:2,WithoutCapture:1 } && b.Summary.Completed==0 && b.Summary.Target==0);
                Check(a.Summary.Target==10 && b.Summary.Target==0 && a.Summary.NotStarted is null && a.Summary.Maturity is null);
                Check(a.Units.Single().Summary.Registered!.Completed==6);
                await fixture.Sql("UPDATE cd_surveys SET \"UnitId\"='B' WHERE \"Employee\"='0001'");
                a=await aggregate.Read(Scope("responsable_ur"),null,null,default);
                Check(a.Summary.Registered is { Completed:5,Stored:8 });
                try { await aggregate.Read(Scope("responsable_ur"),"B",null,default); Check(false); } catch(DomainProblem p) { Check(p.Status==403); }
            } finally {
                await fixture.Sql("UPDATE cd_surveys SET \"UnitId\"='A4' WHERE \"Employee\"='0001'; UPDATE fixture_users SET num_empleado='0010' WHERE usuario_id=10");
            }
            Check(Equals(before,await fixture.Scalar("SELECT md5(string_agg(row_to_json(s)::text,',' ORDER BY \"Employee\")) FROM cd_surveys s")));
        });
        await Test("Revisión reabierta sintética cambia conteos registrados y conserva el definitivo histórico", async () => {
            await fixture.Sql("UPDATE fixture_users SET num_empleado='0001' WHERE usuario_id=10; INSERT INTO cd_submissions SELECT (SELECT \"Generation\" FROM cd_control),s.\"Id\",s.\"UnitId\",1,to_jsonb(s) FROM cd_surveys s WHERE \"Employee\"='0001'; UPDATE cd_surveys SET \"CompletedAt\"=NULL,\"ReportJson\"=NULL,\"Revision\"=2 WHERE \"Employee\"='0001'");
            try {
                var before = await fixture.Scalar("SELECT md5(\"SnapshotJson\"::text) FROM cd_submissions");
                var progress = await aggregate.Read(Scope(),null,null,default);
                Check(progress.Summary.Registered is { Stored:9,Completed:5,InProgress:3,WithoutCapture:1 } && progress.Summary.Participation==50);
                var historical = await aggregate.Read(Scope(),null,null,default,finalizedOnly:true);
                Check(historical.Summary.Completed==6 && historical.Summary.Maturity==0);
                Check(Equals(before,await fixture.Scalar("SELECT md5(\"SnapshotJson\"::text) FROM cd_submissions")));
            } finally {
                await fixture.Sql("UPDATE cd_surveys s SET \"CompletedAt\"=(h.\"SnapshotJson\"->>'CompletedAt')::timestamptz,\"ReportJson\"=h.\"SnapshotJson\"->'ReportJson',\"Revision\"=1 FROM cd_submissions h WHERE h.\"SurveyId\"=s.\"Id\"; DELETE FROM cd_submissions; UPDATE fixture_users SET num_empleado='0010' WHERE usuario_id=10");
            }
        });
        await Test("80 canónicos, tres empleados compartidos sin encuestas afectadas: 8 + 0 + 72 y 10 %", async () => {
            await fixture.Sql("DELETE FROM fixture_roles; DELETE FROM fixture_users; INSERT INTO fixture_users SELECT i,i,'canonical-'||i||'@uacj.mx','Sintético','individual',lpad((CASE i WHEN 78 THEN 11 WHEN 79 THEN 12 WHEN 80 THEN 13 ELSE i END)::text,4,'0'),'A4','adscripcion' FROM generate_series(1,80) i; INSERT INTO fixture_roles SELECT i,'canonical-'||i||'@uacj.mx',1,'participante','Participante' FROM generate_series(1,80) i; INSERT INTO fixture_roles SELECT * FROM fixture_roles WHERE usuario_aplicacion_id=1");
            await fixture.Sql("UPDATE cd_surveys SET \"CompletedAt\"=now(),\"ReportJson\"=$1::jsonb WHERE \"Employee\" IN ('0007','0008')",report);
            await fixture.Sql("UPDATE cd_surveys SET \"ProfileJson\"=$1::jsonb,\"AnswersJson\"=$2::jsonb WHERE \"Employee\"='0009'", "{\"puesto\":\"\",\"funciones\":[]}","{\"m-word\":{\"state\":\"pending\",\"level\":0}}");
            try {
                var result=await aggregate.Read(Scope(),null,null,default); var s=result.Summary;
                Check(s.PopulationAvailable && s.Target==80 && s.Completed==8 && s.InProgress==0 && s.NotStarted==72 && s.Participation==10 && s.Registered is null);
                Check(result.Units.Single(u=>u.Id=="A").Summary.Target==80);
                var diagnostic=await Diagnose(); Check(diagnostic.Code==0);
                Check(Data(diagnostic.Rows,"population.validation")["Issues"]!["employee_conflict"]!.GetValue<int>()==3);
                Check(Data(diagnostic.Rows,"survey.counts")["level2Population"]!.GetValue<int>()==80);
                Check(Data(diagnostic.Rows,"survey.canonicalStates")["finalizedPercentage"]!.GetValue<double>()==10);
            } finally {
                await fixture.Sql("DELETE FROM fixture_roles; DELETE FROM fixture_users; INSERT INTO fixture_users SELECT i,i,'persona-'||i||'@uacj.mx','Sintético','individual',lpad(i::text,4,'0'),'A4','adscripcion' FROM generate_series(1,10) i; INSERT INTO fixture_roles SELECT i,'persona-'||i||'@uacj.mx',1,'participante','Participante' FROM generate_series(1,10) i; UPDATE cd_surveys SET \"CompletedAt\"=NULL,\"ReportJson\"=NULL WHERE \"Employee\" IN ('0007','0008')");
            }
        });
        await Test("Encuesta ambigua: total visible; vínculo Microsoft previo resuelve sólo su dueño, sin duplicarla", async () => {
            await fixture.Sql("UPDATE fixture_users SET num_empleado='0001' WHERE usuario_id=10; UPDATE cd_surveys SET \"Email\"='verified@uacj.mx' WHERE \"Employee\"='0001'; UPDATE tdv2_identity_links SET verified_at='2000-01-01'");
            try {
                var s=(await aggregate.Read(Scope(),null,null,default)).Summary;
                Check(s.Target==10 && s.Completed==6 && s.InProgress==2 && s.NotStarted==2 && s.Participation==60);
                await fixture.Sql("UPDATE fixture_users SET num_empleado='0002' WHERE usuario_id=1");
                s=(await aggregate.Read(Scope(),null,null,default)).Summary; Check(s.Target==10 && s.Completed is null && s.NotStarted is null);
                await fixture.Sql("UPDATE fixture_users SET num_empleado='0001' WHERE usuario_id=1");
                await fixture.Sql("UPDATE users SET updated_at='2100-01-01'");
                s=(await aggregate.Read(Scope(),null,null,default)).Summary; Check(s.Target==10 && s.Completed is null);
                await fixture.Sql("UPDATE users SET updated_at='2000-01-01'; UPDATE fixture_users SET \"ID_UR\"='B' WHERE usuario_id=1");
                s=(await aggregate.Read(Scope(),"A",null,default)).Summary;
                Check(s.Target==9 && s.Completed==5 && s.InProgress==2 && s.NotStarted==2);
                s=(await aggregate.Read(Scope(),"B",null,default)).Summary; Check(s.Target==1 && s.Completed==1);
                await fixture.Sql("UPDATE fixture_users SET \"ID_UR\"='A4' WHERE usuario_id=1");
                await fixture.Sql("UPDATE tdv2_identity_links SET verified_at='2100-01-01'");
                s=(await aggregate.Read(Scope(),null,null,default)).Summary;
                Check(s.Target==10 && s.Completed is null && s.InProgress==2 && s.NotStarted is null && s.Participation is null && s.Registered!.Completed==6);
                await fixture.Sql("UPDATE tdv2_identity_links SET verified_at='2000-01-01'; REVOKE SELECT ON tdv2_identity_links FROM population_reader");
                try { s=(await aggregate.Read(Scope(),null,null,default)).Summary; Check(s.Target==10 && s.Completed is null); }
                finally { await fixture.Sql("GRANT SELECT ON tdv2_identity_links TO population_reader"); }
            } finally { await fixture.Sql("UPDATE fixture_users SET num_empleado='0010' WHERE usuario_id=10; UPDATE cd_surveys SET \"Email\"='legacy@uacj.mx' WHERE \"Employee\"='0001'"); }
        });
        await File.WriteAllTextAsync(Path.Combine(fixture.Artifacts,"population-diagnosis-tests.json"),JsonSerializer.Serialize(new { syntheticSources=true, isolatedPostgreSql=true, migrationsExecuted=false, failures, results },new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine($"Diagnóstico: {results.Count-failures} correctas; {failures} fallidas. Sin migraciones.");
        return failures==0 ? 0 : 1;
    }
}
