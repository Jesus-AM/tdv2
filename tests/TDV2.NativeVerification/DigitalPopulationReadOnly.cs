using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Tdv2.Competencias;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Integrations.Nexo;

namespace Tdv2.NativeVerification;

// Comando explícito anterior al arranque de fixtures/servidor. No invoca servicios
// de guardado, auditoría, sincronización o migración. No imprime filas personales.
internal static class DigitalPopulationReadOnly
{
    internal static string ReadOnlyConnection(string raw) => new NpgsqlConnectionStringBuilder(raw) {
        IncludeErrorDetail = false, LogParameters = false, Timeout = 8, CommandTimeout = 20,
        Options = "-c default_transaction_read_only=on -c statement_timeout=20000"
    }.ConnectionString;

    internal static string SafeError(Exception error) => error switch {
        PostgresException p => "SQLSTATE=" + p.SqlState + "; detalle=" + (p.SqlState switch {
            "42501" => "permiso de lectura insuficiente",
            "42P01" => "relación requerida no publicada o esquema pendiente",
            "42703" => "columna requerida ausente en contrato o esquema",
            "42883" => "función u operador incompatible con tipos publicados",
            "42804" => "tipo de dato incompatible",
            "25006" => "escritura impedida por conexión de sólo lectura",
            "57014" => "consulta cancelada o tiempo límite",
            "28P01" => "autenticación de conexión rechazada",
            "3D000" => "base configurada no disponible",
            _ => "error del proveedor; mensaje y detalle originales omitidos por privacidad" }),
        DomainProblem p => "HTTP=" + p.Status + "; detalle=validación del contrato no satisfecha",
        UnauthorizedAccessException => "CONFIG_ACCESS_DENIED; detalle=el proceso no puede leer la configuración segura",
        NpgsqlException => "CONNECTION_FAILURE; detalle=fallo de conexión o transporte",
        IOException => "CONFIG_IO; detalle=no fue posible leer el archivo de configuración",
        _ => "tipo=" + error.GetType().Name + "; detalle=error sin datos sensibles"
    };

    public static async Task<int> Run(IConfiguration? supplied = null, TextWriter? output = null, bool privateEmployeeConflicts = false)
    {
        output ??= Console.Out;
        var failed = false;
        void Write(string phase, string status, object data) => output.WriteLine(JsonSerializer.Serialize(new { phase, status, data }));
        async Task<bool> Step(string phase, Func<Task> work)
        {
            try { await work(); Write(phase, "OK", new { readOnly = true }); return true; }
            catch (Exception error) { failed = true; Write(phase, "ERROR", SafeError(error)); return false; }
        }
        IConfiguration? config = supplied;
        if (!await Step("configuration", () => {
            if (config is null)
            {
                var root = Path.GetFullPath("tdv2");
                var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
                var builder = new ConfigurationBuilder().SetBasePath(root).AddJsonFile("appsettings.json", optional: true);
                if (environment is "Development" or "Production" or "Staging") builder.AddJsonFile("appsettings." + environment + ".json", optional: true);
                config = builder.AddUserSecrets(typeof(DigitalDbContext).Assembly, optional: true).AddEnvironmentVariables().Build();
            }
            return Task.CompletedTask;
        })) return 2;
        var values = new Dictionary<string, string?>();
        foreach (var name in new[] { "Tdv2", "CompetenciasDigitales", "CompetenciasDigitalesNexo" })
        {
            var raw = config!.GetConnectionString(name);
            if (string.IsNullOrWhiteSpace(raw)) { Write("configuration", "ERROR", new { code = "CONFIG_MISSING", key = "ConnectionStrings:" + name }); return 2; }
            if (!await Step("configuration." + name, () => { values["ConnectionStrings:" + name] = ReadOnlyConnection(raw); return Task.CompletedTask; })) return 2;
        }
        if (!long.TryParse(config!["CompetenciasDigitales:Nexo:ApplicationId"], out var id) || id <= 0)
        { Write("configuration", "ERROR", new { code = "APPLICATION_ID_INVALID" }); return 2; }

        var dbs = new DatabaseConnections(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        async Task<string> Json(string name, string sql, params object[] parameters)
        {
            await using var connection = await dbs.Open(name);
            await using var command = new NpgsqlCommand(sql, connection);
            foreach (var parameter in parameters) command.Parameters.AddWithValue(parameter);
            return (string)(await command.ExecuteScalarAsync())!;
        }
        async Task Counts(string phase, string name, string sql, params object[] parameters)
            => Write(phase, "COUNTS", JsonSerializer.Deserialize<JsonElement>(await Json(name, sql, parameters)));

        foreach (var name in new[] { "Tdv2", "CompetenciasDigitales", "CompetenciasDigitalesNexo" })
            await Step("connection." + name, async () => {
                var state = await Json(name, "SELECT current_setting('default_transaction_read_only')");
                if (state != "on") throw new InvalidOperationException("Read-only required");
            });
        // Cada objeto por separado: una vista caída no oculta los demás resultados.
        foreach (var (name, objects) in new[] {
            ("Tdv2", new[] { "unidades_responsables_poa", "__EFMigrationsHistory" }),
            ("CompetenciasDigitalesNexo", new[] { "nexo_aplicacion", "nexo_usuarios", "nexo_usuario_rol", "nexo_modulos", "nexo_modulo_rol" }),
            ("CompetenciasDigitales", new[] { "cd_surveys", "cd_control", "cd_submissions", "cd_reopen_operations", "__CDMigrationsHistory" }) })
            foreach (var relation in objects)
                await Step("object." + name + "." + relation, async () => {
                    var metadata = JsonSerializer.Deserialize<JsonElement>(await Json(name, """
                    SELECT jsonb_build_object('exists',o IS NOT NULL,
                        'select',CASE WHEN o IS NOT NULL THEN has_table_privilege(o,'SELECT') ELSE false END,
                        'columns',COALESCE((SELECT jsonb_object_agg(a.attname,pg_catalog.format_type(a.atttypid,a.atttypmod))
                            FROM pg_catalog.pg_attribute a WHERE a.attrelid=o AND a.attnum>0 AND NOT a.attisdropped),'{}'::jsonb))::text
                    FROM (SELECT to_regclass($1) o) x
                    """, "public.\"" + relation + "\""));
                    Write("object." + relation, "COUNTS", metadata);
                    if (!metadata.GetProperty("exists").GetBoolean() || !metadata.GetProperty("select").GetBoolean())
                    { failed = true; Write("object." + relation, "INCIDENCE", "OBJECT_OR_SELECT_MISSING: comprobar publicación/esquema y permisos, sin modificarlos desde este diagnóstico."); }
                });

        var profiles = new PostgresNexoProfiles(dbs, NullLogger<PostgresNexoProfiles>.Instance, "CompetenciasDigitalesNexo", id);
        var applicationValid = await Step("nexo.application", async () => {
            await Counts("nexo.application", "CompetenciasDigitalesNexo", """
                SELECT jsonb_build_object('publishedRows',count(*),'matchesConfiguredId',count(*) FILTER(WHERE id=$1),
                    'isProceduresPublication',count(*) FILTER(WHERE clave='tdv2'))::text FROM public.nexo_aplicacion
                """, id);
            await profiles.ApplicationId(default);
        });
        await Step("nexo.membership", () => Counts("nexo.membership", "CompetenciasDigitalesNexo", """
            SELECT jsonb_build_object('publishedUsers',count(DISTINCT u.usuario_id),
                'individuals',count(DISTINCT u.usuario_id) FILTER(WHERE tipo_cuenta='individual'),
                'eligibleApplicationUsers',count(DISTINCT u.usuario_id) FILTER(WHERE tipo_cuenta='individual' AND EXISTS(
                    SELECT 1 FROM public.nexo_usuario_rol r WHERE r.usuario_aplicacion_id=u.usuario_aplicacion_id AND r.email=u.email AND r.rol_clave=ANY($1))),
                'withoutAcceptedRole',count(DISTINCT u.usuario_id) FILTER(WHERE tipo_cuenta='individual' AND NOT EXISTS(
                    SELECT 1 FROM public.nexo_usuario_rol r WHERE r.usuario_aplicacion_id=u.usuario_aplicacion_id AND r.email=u.email AND r.rol_clave=ANY($1))))::text
            FROM public.nexo_usuarios u
            """, (object)DigitalAccess.EmployeeRoles));

        DigitalScope? scope = null; string[] valid = [], filtered = [];
        var catalogValid = await Step("catalog.hierarchy", async () => {
            var units = new List<Unit>();
            await using var connection = await dbs.Open("Tdv2");
            await using var q = new NpgsqlCommand("SELECT id_ur,cve_ur,nivel_ur,id_ur_pertenece,tipo_ur,estatus_ur FROM public.unidades_responsables_poa WHERE presente", connection);
            await using var r = await q.ExecuteReaderAsync();
            while (await r.ReadAsync()) units.Add(new(r.GetString(0), r.GetString(1), null, r.IsDBNull(2) ? 0 : r.GetInt32(2), r.IsDBNull(3) ? null : r.GetString(3),
                0, null, true, r.IsDBNull(5) ? "" : r.GetString(5), r.IsDBNull(4) ? null : r.GetString(4), !r.IsDBNull(2)));
            scope = new DigitalScope(new(new("diagnostic@example.invalid", "diagnóstico", null, null), [new(1,"administrador", "diagnóstico")], []), new(units));
            valid = scope.Directory.Units.Values.Where(u => scope.Chain(u.Id) is not null).Select(u => u.Id).ToArray();
            var roots = scope.Allowed.Where(u => u.Level == 2).ToArray();
            filtered = scope.Filter(null).Where(u => roots.Any(root => scope.Directory.Within(u, root.Id))).ToArray();
            string ChainProblem(Unit unit)
            {
                var seen = new HashSet<string>(); Unit? child = null; string? next = unit.Id;
                while (next is not null)
                {
                    var node = scope.Directory.Get(next);
                    if (node is null) return "missing_or_inactive_node";
                    if (!seen.Add(node.Id)) return "cycle";
                    if (!node.LevelKnown) return "unknown_level";
                    if (child is not null && node.Level >= child.Level) return "nonascending_level";
                    child = node; next = string.IsNullOrWhiteSpace(node.Parent) ? null : node.Parent;
                }
                return "valid";
            }
            Write("catalog.hierarchy", "COUNTS", new { present = units.Count, active = scope.Allowed.Length, validChains = valid.Length,
                invalidChains = scope.Allowed.Length - valid.Length, level2Groups = roots.Length, level2AndDependents = filtered.Length,
                cumulativeNodes = scope.Allowed.Count(u => !UnitDirectory.IsEligible(u)), outsideLevel2 = scope.Allowed.Length - filtered.Length,
                chainReasons = scope.Allowed.GroupBy(ChainProblem).ToDictionary(g => g.Key, g => g.Count()) });
        });

        NexoPopulationInspection? inspection = null; var populationVerified = false;
        if (applicationValid && catalogValid)
            await Step("population.validation", async () => {
                inspection = await profiles.InspectPopulation(DigitalAccess.EmployeeRoles, scope!.Allowed.Select(u => u.Id).ToArray(), valid, default);
                Write("population.validation", "COUNTS", new { inspection.Candidates, inspection.Issues, verified = !inspection.Invalid });
                populationVerified = !inspection.Invalid;
                if (!populationVerified) { failed = true; Write("population.validation", "INCIDENCE", "POPULATION_INTEGRITY: una incidencia basta para impedir certificar el total. No se excluyen personas ni se publica un subconjunto."); }
            });
        else Write("population.validation", "SKIPPED", "Requiere aplicación y catálogo comprobados.");

        if (privateEmployeeConflicts && applicationValid)
            await Step("population.privateConflicts", async () => {
                var result = await DigitalPopulationPrivateConflicts.Export(dbs,id);
                Write("population.privateConflicts", "PRIVATE_FILE", new { result.FileName,result.Groups,result.CanonicalIds,result.SourcesComplete,
                    directory="LocalApplicationData/TDV2/diagnosticos", warning="Datos privados: revisar localmente; no compartir el archivo en Git/chat/documentación." });
                if (!result.SourcesComplete) { failed=true; Write("population.privateConflicts", "INCIDENCE", "PRIVATE_SOURCES_INCOMPLETE: revisar los estados available y errores saneados dentro del archivo privado."); }
            });

        await Step("schema.migrations", async () => {
            await using var db = new DigitalDbContext(new DbContextOptionsBuilder<DigitalDbContext>().UseNpgsql(values["ConnectionStrings:CompetenciasDigitales"], p => p.MigrationsHistoryTable("__CDMigrationsHistory")).Options);
            var known = db.Database.GetMigrations().ToArray(); // Sólo metadatos del ensamblado.
            await using var connection = await dbs.Open("CompetenciasDigitales");
            await using var q = new NpgsqlCommand("SELECT \"MigrationId\" FROM public.\"__CDMigrationsHistory\"", connection);
            var applied = new HashSet<string>(); await using var r = await q.ExecuteReaderAsync();
            while (await r.ReadAsync()) applied.Add(r.GetString(0));
            var pending = known.Except(applied).ToArray();
            Write("schema.migrations", "COUNTS", new { appliedKnown = known.Count(applied.Contains), unknownCount = applied.Except(known).Count(), pending });
            if (pending.Length > 0) { failed = true; Write("schema.migrations", "INCIDENCE", "SCHEMA_PENDING: no se aplica ninguna migración. El aviso de población y un error de esquema son causas distintas."); }
        });

        string? generation = null;
        await Step("survey.generation", async () => {
            await Counts("survey.generation", "CompetenciasDigitales", """
                SELECT jsonb_build_object('controlRows',count(*),'validControlRows',count(*) FILTER(WHERE "Id"=1 AND "Generation"<>'00000000-0000-0000-0000-000000000000'::uuid))::text FROM public.cd_control
                """);
            generation = await Json("CompetenciasDigitales", "SELECT \"Generation\"::text FROM public.cd_control WHERE \"Id\"=1");
            if (!Guid.TryParse(generation, out var parsed) || parsed == Guid.Empty)
            { failed = true; Write("survey.generation", "INCIDENCE", "GENERATION_INVALID: no se puede comprobar el ciclo actual."); }
        });

        // Sólo booleanos y agregados: no descarga perfiles, respuestas, correos o nombres.
        var matching = inspection?.People.Select(p => p.Employee).OfType<string>().Distinct().ToArray() ?? [];
        var selected = inspection?.People.Where(p => filtered.Contains(p.UnitId)).Select(p => p.Employee).OfType<string>().Distinct().ToArray() ?? [];
        int? canonicalFinal = null,canonicalProgress = null,canonicalNotStarted = null;
        var canonicalPopulation = inspection?.People.Where(p=>filtered.Contains(p.UnitId)).DistinctBy(p=>p.UserId).ToArray() ?? [];
        if(populationVerified) await Step("survey.canonicalStates", async () => {
            var references = new List<DigitalSurveyReference>();
            await using var c = await dbs.Open("CompetenciasDigitales");
            await using var q = new NpgsqlCommand("""
                SELECT "Employee","Email","CreatedAt","CompletedAt" IS NOT NULL,
                  "CompletedAt" IS NOT NULL OR EXISTS(SELECT 1 FROM jsonb_each("ProfileJson") p WHERE
                    CASE WHEN jsonb_typeof(p.value)='array' THEN jsonb_array_length(p.value)>0 ELSE coalesce(btrim(p.value#>>'{}'),'')<>'' END)
                    OR EXISTS(SELECT 1 FROM jsonb_each("AnswersJson") a WHERE a.value->>'state' IN ('level','na','unused'))
                FROM public.cd_surveys WHERE "Employee"=ANY($1)
                """,c);
            q.Parameters.AddWithValue(selected);
            await using(var r=await q.ExecuteReaderAsync()) while(await r.ReadAsync()) references.Add(new(r.GetString(0),r.GetString(1),r.GetFieldValue<DateTimeOffset>(2),r.GetBoolean(4),r.GetBoolean(3)));
            var resolved=await DigitalSurveyCorrespondence.Resolve(dbs,canonicalPopulation,references.ToArray(),default);
            var pending=resolved.Pending.Values.ToArray();
            var assigned=references.Where(s=>resolved.Owners.ContainsKey(s.Employee)).ToArray();
            canonicalFinal=pending.Any(p=>p.Finalized) ? null : assigned.Count(s=>s.Finalized);
            canonicalProgress=pending.Any(p=>p.InProgress) ? null : assigned.Count(s=>s.Started && !s.Finalized);
            canonicalNotStarted=pending.Any(p=>p.NotStarted) ? null : canonicalPopulation.Length-assigned.Count(s=>s.Started);
            Write("survey.canonicalStates","COUNTS",new { population=canonicalPopulation.Length,finalized=canonicalFinal,inProgress=canonicalProgress,notStarted=canonicalNotStarted,
                finalizedPercentage=canonicalPopulation.Length>0 && canonicalFinal is not null ? canonicalFinal*100d/canonicalPopulation.Length : null,
                pendingUsers=resolved.Pending.Count,proofSourceAvailable=resolved.ProofAvailable });
            if(pending.Length>0) { failed=true; Write("survey.canonicalStates","INCIDENCE","SURVEY_CORRESPONDENCE_PENDING: población canónica comprobada; sólo los estados afectados siguen pendientes."); }
        });
        await Step("survey.counts", () => Counts("survey.counts", "CompetenciasDigitales", """
            WITH surveys AS (SELECT "Employee" employee,"CompletedAt" IS NOT NULL final,
                "ReportJson" IS NOT NULL report,
                "CompletedAt" IS NOT NULL OR EXISTS(SELECT 1 FROM jsonb_each("ProfileJson") p WHERE
                    CASE WHEN jsonb_typeof(p.value)='array' THEN jsonb_array_length(p.value)>0 ELSE coalesce(btrim(p.value#>>'{}'),'')<>'' END)
                    OR EXISTS(SELECT 1 FROM jsonb_each("AnswersJson") a WHERE a.value->>'state' IN ('level','na','unused')) started
                FROM public.cd_surveys)
            SELECT jsonb_build_object('stored',count(*),'finalizedStored',count(*) FILTER(WHERE final),
                'inProgressStored',count(*) FILTER(WHERE NOT final AND started),'unstartedRows',count(*) FILTER(WHERE NOT started),
                'duplicateEmployeeRows',count(*)-count(DISTINCT employee),
                'inconsistentFinalReport',count(*) FILTER(WHERE final<>report),
                'matchedRows',count(*) FILTER(WHERE employee=ANY($1)),
                'matchedFinalized',count(*) FILTER(WHERE employee=ANY($1) AND final),
                'matchedInProgress',count(*) FILTER(WHERE employee=ANY($1) AND NOT final AND started),
                'unmatchedRows',count(*) FILTER(WHERE NOT(employee=ANY($1))),
                'level2Finalized',$5::integer,'level2InProgress',$6::integer,'level2NotStarted',$7::integer,
                'level2Population',$8::integer,'populationVerified',$3,'matchingAvailable',$4)::text FROM surveys
            """, matching, selected, populationVerified, inspection is not null,(object?)canonicalFinal ?? DBNull.Value,(object?)canonicalProgress ?? DBNull.Value,(object?)canonicalNotStarted ?? DBNull.Value,populationVerified ? canonicalPopulation.Length : DBNull.Value));
        if (!populationVerified) Write("survey.counts", "UNVERIFIED", "Conteos almacenados independientes. Cruces no certificados: no son población ni participación verificadas; unmatched no demuestra revocación.");
        await Step("survey.historical", () => Counts("survey.historical", "CompetenciasDigitales", """
            SELECT jsonb_build_object('archivedSubmissions',count(*),'currentGeneration',count(*) FILTER(WHERE "Generation"=(SELECT "Generation" FROM public.cd_control WHERE "Id"=1)))::text FROM public.cd_submissions
            """));
        if (generation is not null) await Step("survey.generationStable", async () => {
            if (generation != await Json("CompetenciasDigitales", "SELECT \"Generation\"::text FROM public.cd_control WHERE \"Id\"=1"))
                throw new InvalidOperationException("Generation changed during diagnostic");
        });
        Write("result", failed ? "INCOMPLETE" : "OK", "Sólo lecturas SQL; no sesión HTTP/OAuth. No se iniciaron servidores, trabajadores, migraciones ni auditoría. No se imprimen identidades ni respuestas.");
        return failed ? 1 : 0;
    }
}
