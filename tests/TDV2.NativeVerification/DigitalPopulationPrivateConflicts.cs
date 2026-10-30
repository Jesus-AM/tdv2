using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Tdv2.Competencias;
using Tdv2.Infrastructure;
using Tdv2.Integrations.Nexo;

namespace Tdv2.NativeVerification;

// Exclusivo del diagnóstico CLI. Nunca se registra en DI ni se sirve por HTTP.
internal static class DigitalPopulationPrivateConflicts
{
    internal static async Task<(string FileName, int Groups, int CanonicalIds, bool SourcesComplete)> Export(DatabaseConnections dbs, long applicationId, string? testDirectory = null)
    {
        var profiles = new PostgresNexoProfiles(dbs, NullLogger<PostgresNexoProfiles>.Instance, "CompetenciasDigitalesNexo", applicationId);
        await profiles.ApplicationId(default);
        async Task<JsonArray> Read(string name, string sql, params object[] parameters)
        {
            await using var c = await dbs.Open(name);
            await using var q = new NpgsqlCommand(sql,c);
            foreach (var p in parameters) q.Parameters.AddWithValue(p);
            return JsonNode.Parse((string)(await q.ExecuteScalarAsync())!)!.AsArray();
        }
        var groups = await Read("CompetenciasDigitalesNexo", PostgresNexoProfiles.PopulationIdentityCte + "\n" + """
            , conflicts AS (SELECT employee,count(DISTINCT id) canonical_count FROM identities GROUP BY employee HAVING count(DISTINCT id)>1),
            accounts AS (
                SELECT u.num_empleado employee,u.usuario_id::bigint canonical_id,u.usuario_aplicacion_id assignment_id,
                    u."ID_UR" unit_id,u.origen_ur origin,count(*) published_rows,
                    (SELECT count(*) FROM public.nexo_usuario_rol r WHERE r.usuario_aplicacion_id=u.usuario_aplicacion_id
                        AND r.email=u.email AND r.rol_clave=ANY($1)) accepted_role_rows
                FROM public.nexo_usuarios u
                WHERE u.tipo_cuenta='individual' AND EXISTS(SELECT 1 FROM identities i WHERE i.id=u.usuario_id AND i.employee=u.num_empleado)
                GROUP BY u.num_empleado,u.usuario_id,u.usuario_aplicacion_id,u."ID_UR",u.origen_ur,u.email)
            SELECT coalesce(jsonb_agg(jsonb_build_object('employee',c.employee,'canonicalCount',c.canonical_count,
                'accounts',(SELECT jsonb_agg(jsonb_build_object('canonicalId',a.canonical_id,'applicationAssignmentId',a.assignment_id,
                    'unitId',a.unit_id,'unitOrigin',a.origin,'publishedRows',a.published_rows,'acceptedRoleRows',a.accepted_role_rows)
                    ORDER BY a.canonical_id,a.assignment_id) FROM accounts a WHERE a.employee=c.employee)) ORDER BY c.employee),'[]'::jsonb)::text
            FROM conflicts c
            """, (object)DigitalAccess.EmployeeRoles);
        var ids = groups.SelectMany(g => g!["accounts"]!.AsArray()).Select(a => a!["canonicalId"]!.GetValue<long>()).Distinct().ToArray();
        var employees = groups.Select(g => g!["employee"]!.GetValue<string>()).ToArray();
        var privateData = new JsonObject { ["format"] = "tdv2-employee-conflicts-v1", ["generatedAt"] = DateTimeOffset.UtcNow.ToString("O"),
            ["warning"] = "Datos privados para revisión institucional. No compartir el archivo en chat, Git ni documentación pública. No constituye una vinculación de cuentas ni autorización.",
            ["groups"] = groups };
        // Fallos de una fuente quedan dentro del archivo privado sin ocultar los grupos.
        async Task Optional(string key, Func<Task<JsonArray>> work)
        {
            try { privateData[key] = new JsonObject { ["available"] = true, ["rows"] = await work() }; }
            catch(Exception e) { privateData[key] = new JsonObject { ["available"] = false, ["error"] = DigitalPopulationReadOnly.SafeError(e) }; }
        }
        await Optional("verifiedIdentity", () => Read("Tdv2", """
            SELECT coalesce(jsonb_agg(to_jsonb(x)),'[]'::jsonb)::text FROM (
                SELECT l.nexo_user_id AS "canonicalId",count(*) AS "verifiedLinks",count(u.id) AS "matchingLocalUsers",
                    min(l.verified_at) AS "verifiedAt",max(u.created_at) AS "localCreatedAt",max(u.updated_at) AS "localUpdatedAt"
                FROM public.tdv2_identity_links l LEFT JOIN public.users u
                  ON u.microsoft_tenant_id=l.microsoft_tenant_id AND u.microsoft_id=l.microsoft_id
                WHERE l.nexo_user_id=ANY($1) GROUP BY l.nexo_user_id) x
            """, (object)ids));
        await Optional("surveys", () => Read("CompetenciasDigitales", """
            SELECT coalesce(jsonb_agg(to_jsonb(x)),'[]'::jsonb)::text FROM (
                SELECT s."Employee" AS employee,s."Id" AS "surveyId",s."UnitId" AS "storedUnitId",s."Revision" AS revision,s."CreatedAt" AS "createdAt",
                    s."CompletedAt" AS "lastSubmittedAt",s."CompletedAt" IS NOT NULL AS finalized,
                    (SELECT count(*) FROM public.cd_submissions h WHERE h."SurveyId"=s."Id") AS "archivedSubmissions"
                FROM public.cd_surveys s WHERE s."Employee"=ANY($1)) x
            """, (object)employees));
        await Optional("units", () => Read("Tdv2", """
            SELECT coalesce(jsonb_agg(to_jsonb(x)),'[]'::jsonb)::text FROM (
                SELECT id_ur AS "unitId",nivel_ur AS level,id_ur_pertenece AS "parentId",tipo_ur AS kind,
                    presente AS present,estatus_ur AS status FROM public.unidades_responsables_poa WHERE id_ur=ANY($1)) x
            """, (object)groups.SelectMany(g=>g!["accounts"]!.AsArray()).Select(a=>a!["unitId"]?.GetValue<string>()).OfType<string>().Distinct().ToArray()));
        var root = testDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TDV2", "diagnosticos");
        if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("Private directory unavailable");
        root = Path.GetFullPath(root);
        var repository = Path.GetFullPath(Directory.GetCurrentDirectory());
        if (testDirectory is null && (root.Equals(repository,StringComparison.OrdinalIgnoreCase) || root.StartsWith(repository + Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Private report must stay outside repository");
        Directory.CreateDirectory(root);
        var filename = "employee-conflicts-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".json";
        var options = new FileStreamOptions { Mode=FileMode.CreateNew, Access=FileAccess.Write, Share=FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode=UnixFileMode.UserRead|UnixFileMode.UserWrite;
        await using var stream = new FileStream(Path.Combine(root,filename),options);
        await System.Text.Json.JsonSerializer.SerializeAsync(stream,privateData,new System.Text.Json.JsonSerializerOptions{WriteIndented=true});
        return (filename,groups.Count,ids.Length,new[]{"verifiedIdentity","surveys","units"}.All(k=>privateData[k]!["available"]!.GetValue<bool>()));
    }
}
