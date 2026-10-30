using System.Text.Json;
using Npgsql;
using Tdv2.Domain;

namespace Tdv2.Integrations.Nexo;

public sealed record NexoPopulationPerson(long UserId, string Employee, string UnitId, string Email, bool EmployeeUnique = true);
// Conteos de validación, no identidades; los detalles no se publican en el reporte.
public sealed record NexoPopulationInspection(Dictionary<string, int> Issues, NexoPopulationPerson[] People, int Candidates)
{
    // El acceso se cuenta por canónico; empleado ausente/compartido afecta el cruce,
    // no la comprobación de usuarios con acceso y adscripción resoluble.
    public bool Invalid => Issues.Any(i => i.Value > 0 && i.Key is not ("employee" or "employee_conflict"));
}
public sealed record NexoPopulationProblem(string Kind);

public sealed partial class PostgresNexoProfiles
{
    // Compartida con la inspección privada del operador: mismo universo y misma
    // agrupación exacta, sin normalizar num_empleado ni resolver por correo.
    public const string PopulationIdentityCte = """
        WITH eligible AS MATERIALIZED (
            SELECT DISTINCT u.usuario_id::bigint AS id,u.num_empleado AS employee,u."ID_UR" AS unit,u.origen_ur AS origin,lower(btrim(u.email)) AS email
            FROM public.nexo_usuarios u
            WHERE u.tipo_cuenta='individual'
              AND EXISTS (SELECT 1 FROM public.nexo_usuario_rol r
                  WHERE r.usuario_aplicacion_id=u.usuario_aplicacion_id AND r.email=u.email AND r.rol_clave=ANY($1))
        ), identities AS (SELECT DISTINCT id,employee,unit,email FROM eligible)
        """;
    // Las mismas vistas efectivas que LoadProfile: membresía y roles vigentes.
    // La publicación resuelve concesiones centrales/delegadas y adscripción; no se reproduce aquí.
    // Una consulta agrupada, sin usuarios locales, Graph ni búsquedas por alias/persona.
    public async Task<NexoPopulationPerson[]> Population(string[] roles, string[] units, string[] validUnits, CancellationToken ct)
    {
        var phase = "application";
        try
        {
            await ApplicationId(ct); // Verifica la publicación filtrada para esta aplicación.
            phase = "population";
            var result = await InspectPopulation(roles, units, validUnits, ct);
            if (result.Invalid)
            {
                foreach (var issue in result.Issues.Where(i => i.Value > 0))
                    logger.LogWarning("Población no comprobada. Validación: {Code}; cantidad: {Count}", issue.Key, issue.Value);
                throw new DomainProblem(503, Tdv2.Web.ApplicationEntry.Unavailable, new NexoPopulationProblem("integrity"));
            }
            return result.People;
        }
        catch (DomainProblem error)
        { logger.LogWarning("Población no comprobada. Fase: {Phase}; estado: {Status}; validación: {Validation}", phase, error.Status, error.Details is NexoPopulationProblem ? "integrity" : "contract"); throw; }
        catch (Exception error) when (error is not OperationCanceledException)
        { logger.LogWarning("Población de Competencias no disponible. Fase: {Phase}; tipo: {Type}; SQLSTATE: {SqlState}", phase, error.GetType().Name, (error as PostgresException)?.SqlState); throw Unavailable(); }
    }
    // Utilizado también por el diagnóstico explícito de sólo lectura. No transforma errores
    // del proveedor: permite identificar su SQLSTATE sin registrar mensajes con datos.
    public async Task<NexoPopulationInspection> InspectPopulation(string[] roles, string[] units, string[] validUnits, CancellationToken ct)
    {
            await ApplicationId(ct); // Cacheada en esta instancia; tampoco el diagnóstico omite esta comprobación.
            await using var connection = await connections.Open(ConnectionName, ct);
            await using var command = new NpgsqlCommand(PopulationIdentityCte + "\n" + """
                SELECT jsonb_build_object(
                    'issues', jsonb_build_object(
                        'canonical_id', (SELECT count(*) FROM eligible WHERE id IS NULL OR id<=0),
                        'employee', (SELECT count(*) FROM eligible WHERE employee IS NULL OR length(employee) NOT BETWEEN 1 AND 32 OR employee ~ '^\s*$'),
                        'unit_missing', (SELECT count(*) FROM eligible WHERE unit IS NULL),
                        'unit_or_chain', (SELECT count(*) FROM eligible WHERE unit IS NOT NULL AND NOT(unit=ANY($3))),
                        'origin_multiple', (SELECT count(*) FROM eligible WHERE origin='multiple'),
                        'origin_missing_or_unknown', (SELECT count(*) FROM eligible WHERE origin IS NULL OR origin NOT IN ('adscripcion','responsable_ur','asignacion_manual','multiple')),
                        'canonical_conflict', (SELECT count(*) FROM (SELECT id FROM identities GROUP BY id HAVING count(*)>1) x),
                        'employee_conflict', (SELECT count(*) FROM (SELECT employee FROM identities GROUP BY employee HAVING count(DISTINCT id)>1) x)),
                    'candidates', (SELECT count(DISTINCT id) FROM identities),
                    'people', COALESCE((SELECT jsonb_agg(jsonb_build_object('userId',i.id,'employee',i.employee,'unitId',i.unit,'email',i.email,
                        'employeeUnique',i.employee IS NOT NULL AND c.users=1))
                        FROM identities i LEFT JOIN (SELECT employee,count(DISTINCT id) users FROM identities GROUP BY employee) c ON c.employee=i.employee
                        WHERE i.unit=ANY($2)), '[]'::jsonb))::text
                """, connection);
            command.Parameters.AddWithValue(roles);
            command.Parameters.AddWithValue(units); command.Parameters.AddWithValue(validUnits);
            using var result = JsonDocument.Parse((string)(await command.ExecuteScalarAsync(ct))!);
            var people = result.RootElement.GetProperty("people").Deserialize<NexoPopulationPerson[]>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            var issues = result.RootElement.GetProperty("issues").Deserialize<Dictionary<string, int>>()!;
            // Misma normalización/validación de la cuenta canónica que LoadProfile. El correo no une personas.
            issues["email"] = 0;
            foreach (var person in people)
                try { if (person.Email is null || Email(person.Email) != person.Email) issues["email"]++; }
                catch (DomainProblem) { issues["email"]++; }
            return new(issues, people, result.RootElement.GetProperty("candidates").GetInt32());
    }
}
