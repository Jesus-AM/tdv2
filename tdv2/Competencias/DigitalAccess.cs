using System.Security.Claims;
using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Integrations.Nexo;
using Tdv2.Security;
using Tdv2.Web;

namespace Tdv2.Competencias;

public static class DigitalRoutes
{
    public const string Base = "/competencias-digitales";
    public static readonly string[] Slugs = ["avance-dependencia", "resultados-generales", "encuesta", "mi-informe-ruta", "configuracion"];
    public static readonly string[] Names = ["Avance por dependencia", "Resultados generales", "Encuesta", "Mi informe y ruta", "Configuración"];
    public static bool Page(PathString path) => path == Base || Slugs.Any(s => path == Base + "/" + s);
}
public interface IDigitalNexo : INexoProfiles
{
    Task<JsonNode> Represent(string action, string actor, JsonObject? data, CancellationToken ct);
}
public sealed class DigitalNexo : IDigitalNexo
{
    private readonly PostgresNexoProfiles profiles;
    private readonly NexoOperations operations;
    public DigitalNexo(DatabaseConnections connections, IConfiguration config, ILogger<PostgresNexoProfiles> logger, ILogger<NexoOperations> operationsLogger)
    {
        profiles = new(connections, logger, "CompetenciasDigitalesNexo", long.TryParse(config["CompetenciasDigitales:Nexo:ApplicationId"], out var id) ? id : null);
        operations = new(connections, profiles, operationsLogger);
    }
    public Task<Profile> ForEmail(string email, CancellationToken ct) => profiles.ForEmail(email, ct);
    public Task<IReadOnlyList<Grant>> Grants(string email, CancellationToken ct) => profiles.Grants(email, ct);
    public Task<JsonNode> Represent(string action, string actor, JsonObject? data, CancellationToken ct) => operations.Represent(action, actor, data, ct);
}
public sealed record DigitalUnitOption(string Id, string Code, string? Description, int Level, string? Parent, bool LevelKnown)
{
    public static DigitalUnitOption From(Unit unit) => new(unit.Id, unit.Code, unit.Description, unit.Level, unit.Parent, unit.LevelKnown);
}
public sealed record DigitalInstitution(string Status, string Name, string Employee, DigitalUnitOption? Unit, DigitalUnitOption? Level2, DigitalUnitOption? Level3, string Origin, DigitalUnitOption[] Ancestors);
public sealed class DigitalScope(Profile profile, UnitDirectory directory)
{
    private Unit[]? roots;
    private Unit[]? allowed;
    public Profile Profile { get; } = profile;
    public UnitDirectory Directory { get; } = directory;
    public bool Institutional => Profile.Has("administrador") || Profile.Has("responsable_ur_supervisor");
    public Unit[] Roots => roots ??= Institutional ? Directory.Units.Values.Where(u => u.Parent is null || Directory.Get(u.Parent) is null).ToArray()
        : Profile.Has("responsable_ur") ? Directory.Units.Values.Where(u => u.LevelKnown && u.Level is 2 or 3 && u.Employee == UnitDirectory.Employee(Profile.User) && u.Employee is not null && Chain(u.Id) is not null).ToArray() : [];
    public Unit[] Allowed => allowed ??= Directory.Units.Values.Where(u => Institutional || Roots.Any(root => Directory.Within(u.Id, root.Id)) && Chain(u.Id) is not null).ToArray();
    public Unit[]? Chain(string? id)
    {
        if (id is null) return null;
        var result = new List<Unit>(); var visited = new HashSet<string>();
        while (id is not null)
        {
            var unit = Directory.Get(id);
            if (unit is null || !visited.Add(id) || !unit.LevelKnown) return null;
            if (result.Count > 0 && unit.Level >= result[^1].Level) return null;
            result.Add(unit); id = string.IsNullOrWhiteSpace(unit.Parent) ? null : unit.Parent;
        }
        return result.ToArray();
    }
    public DigitalInstitution Institution()
    {
        var user = Profile.User;
        var chain = user.Origin is "adscripcion" or "asignacion_manual" or "responsable_ur" ? Chain(user.UnitId) : null;
        var safe = chain?.Select(DigitalUnitOption.From).ToArray();
        return new(chain is null ? "incomplete" : "available", user.Name, UnitDirectory.Employee(user)!, safe?.FirstOrDefault(),
            safe?.SingleOrDefault(u => u.Level == 2), safe?.SingleOrDefault(u => u.Level == 3), user.Origin, safe ?? []);
    }
    public bool Module(int index) => Profile.Modules.Any(m => m.Path == DigitalRoutes.Base + "/" + DigitalRoutes.Slugs[index])
        && (index is 2 or 3 || index == 4 && Profile.Has("administrador") || index is 0 or 1 && (Institutional || Roots.Length > 0));
    // PostgresNexoProfiles ya entrega m.orden, m.id; filtrar permisos conserva ese orden publicado.
    public IEnumerable<int> MenuPages => Profile.Modules.Select(m => Array.FindIndex(DigitalRoutes.Slugs, s => m.Path == DigitalRoutes.Base + "/" + s))
        .Where(i => i >= 0 && Module(i)).Distinct();
    public string[] Filter(string? unitId)
    {
        var allowed = Allowed;
        if (unitId is not null && !allowed.Any(u => u.Id == unitId)) throw new DomainProblem(403, "La unidad está fuera de tu alcance autorizado.");
        return allowed.Where(u => unitId is null || Directory.Within(u.Id, unitId)).Select(u => u.Id).Distinct(StringComparer.Ordinal).ToArray();
    }
}
public sealed class DigitalAccess(IDigitalNexo nexo, IFormStore catalogs, ISessionIdentity identity, AccessState state, OperationAudit audit)
{
    public Profile? Real { get; private set; }
    public DigitalScope? Current { get; private set; }
    public AccessSelection? Selection { get; private set; }
    public async Task<DigitalScope> Resolve(HttpContext http)
    {
        if (Current is not null) return Current;
        var email = http.User.FindFirstValue(ClaimTypes.Email);
        if (email is null || !await identity.Matches(http.User, http.RequestAborted)) throw new DomainProblem(401, "Inicia sesión con tu cuenta institucional.");
        Real = await nexo.ForEmail(email, http.RequestAborted); ValidateEmployee(Real, email);
        var snapshot = await state.Load(http.RequestAborted); Selection = snapshot.Selection;
        var safe = HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method);
        if (!safe && (http.Request.Headers["X-TDV2-Context"].FirstOrDefault() ?? "own") != snapshot.Key)
            throw new DomainProblem(409, "El usuario activo cambió. Recarga antes de continuar.");
        var profile = Real;
        if (Selection is { } selection)
        {
            if (selection.Application != "competencias-digitales" || selection.Kind != "representation") throw new DomainProblem(409, "Sal del contexto activo antes de entrar a Competencias digitales.");
            if (!safe) throw new DomainProblem(403, "La representación permite únicamente consultas.");
            if (!Real.Has("administrador") || !Real.Modules.Any(m => m.Path == DigitalRoutes.Base + "/configuracion") || selection.ActorEmail != email || selection.ExpiresAt <= DateTimeOffset.UtcNow)
                throw new DomainProblem(403, "La representación ya no está autorizada. Vuelve a tu usuario.");
            var result = (await nexo.Represent("validar", email, new() { ["token"] = selection.Token, ["requiere_escritura"] = false }, http.RequestAborted)).AsObject();
            var validated = RequestAccess.ParseRepresentation(result, email, selection.Email);
            if (validated.Id != selection.Id || validated.ExpiresAt > selection.ExpiresAt.AddSeconds(1)) throw new DomainProblem(409, "La representación cambió. Vuelve a tu usuario.");
            profile = await nexo.ForEmail(selection.Email, http.RequestAborted); ValidateEmployee(profile, selection.Email);
        }
        audit.Effective = profile; audit.Resolved = Selection;
        return Current = new(profile, new UnitDirectory(await catalogs.Units(http.RequestAborted)));
    }
    public static void ValidateEmployee(Profile profile, string email)
    {
        if (profile.User.Email != email || UnitDirectory.Employee(profile.User) is null || !profile.Roles.Any(r => r.Key is "administrador" or "responsable_ur_supervisor" or "responsable_ur" or "participante"))
            throw new DomainProblem(403, "No se confirmó tu acceso como empleado a Competencias digitales en Nexo.");
    }
    public async Task<DigitalScope> Require(HttpContext http, int module)
    {
        var scope = await Resolve(http);
        if (!scope.Module(module)) throw new DomainProblem(403, "No tienes acceso a este módulo de Competencias digitales.");
        return scope;
    }
}
