namespace Tdv2.Domain;

public sealed record Unit(string Id, string Code, string? Description, int Level, string? Parent,
    int Year, string? Employee = null, bool Present = true, string Status = "Activo")
{
    public object Public() => new { id_ur = Id, cve_ur = Code, desc_ur = Description ?? Code,
        nivel_ur = Level, id_ur_pertenece = Parent, ejercicio = Year };
}
public sealed record Person(string Email, string Name, string? Employee, string? UnitId,
    string Origin = "adscripcion", string AccountType = "individual");
public sealed record Role(long Id, string Key, string Name);
public sealed record Module(long Id, string Key, string Name, string Path, long? Parent = null, string Icon = "mdi-view-grid-outline");
public sealed record Profile(Person User, IReadOnlyList<Role> Roles, IReadOnlyList<Module> Modules)
{
    public bool Has(string role) => Roles.Any(r => r.Key == role);
}
public sealed record Collaboration(string Email, string Employee, string Origin, string Scope,
    string Kind, long GrantId, long RoleId, string Grantor, bool Revoked = false);
public sealed record Grant(long Id, long RoleId, string OriginUnit, string Origin = "aplicacion");
public sealed record Scope(Unit Unit, bool Edit);

public sealed class UnitDirectory(IEnumerable<Unit> units)
{
    public IReadOnlyDictionary<string, Unit> Units { get; } = units
        .Where(u => u.Present && u.Status.Trim().Equals("activo", StringComparison.OrdinalIgnoreCase))
        .ToDictionary(u => u.Id, StringComparer.Ordinal);
    public Unit? Get(string? id) => id is not null && Units.TryGetValue(id, out var unit) ? unit : null;
    public static bool IsForm(Unit unit) => unit.Level is 2 or 3;
    private IEnumerable<Unit> Ancestors(string? id)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (Get(id) is { } unit && seen.Add(unit.Id)) { yield return unit; id = unit.Parent; }
    }
    public bool Within(string id, string root) => Ancestors(id).Any(u => u.Id == root);
    public Unit? FormUnit(string? id) => Ancestors(id).FirstOrDefault(IsForm);
    public Unit? LevelTwo(string? id) => Ancestors(id).FirstOrDefault(u => u.Level == 2);
    public static string? Employee(Person user) => user.AccountType == "individual"
        && !string.IsNullOrWhiteSpace(user.Employee) && user.Employee.Length <= 32 ? user.Employee : null;
    public Unit? Affiliation(Person user) => Employee(user) is not null
        && user.Origin is "asignacion_manual" or "responsable_ur" or "adscripcion" ? Get(user.UnitId) : null;
    public Unit? AdministratorRoot(Person user) => LevelTwo(Affiliation(user)?.Id);
    public IEnumerable<Unit> Responsibilities(Person user) => Employee(user) is { } employee
        ? Units.Values.Where(u => IsForm(u) && u.Employee == employee) : [];
}

public static class ModuleAccess
{
    private static readonly Dictionary<string, (string Path, string? Parent, bool Admin)> Known = new()
    {
        ["procesos_operativos"] = ("/inicio", null, false),
        ["configuracion"] = ("/configuracion", null, true),
        ["sincronizaciones"] = ("/configuracion/sincronizaciones", "configuracion", true),
        ["pruebas_acceso"] = ("/configuracion/pruebas-acceso", "configuracion", true)
    };
    public static bool Allows(Profile profile, string key)
    {
        if (!Known.TryGetValue(key, out var local) || local.Admin && !profile.Has("administrador")) return false;
        var parent = local.Parent is null ? null : profile.Modules.FirstOrDefault(m => m.Key == local.Parent);
        if (local.Parent is not null && (parent is null || !Allows(profile, local.Parent))) return false;
        return profile.Modules.Any(m => m.Key == key && m.Path == local.Path && (parent is null || m.Parent == parent.Id));
    }
    public static object[] Navigation(Profile profile) => profile.Modules.Where(m => Allows(profile, m.Key)).Select(m =>
        (object)new { id = m.Id, key = m.Key, name = m.Name, route = m.Path, parent = Known[m.Key].Parent,
            icon = System.Text.RegularExpressions.Regex.IsMatch(m.Icon, "\\Amdi-[a-z0-9-]+\\z") ? m.Icon : "mdi-view-grid-outline" }).ToArray();
}

public sealed class FormAccess(UnitDirectory directory)
{
    public IReadOnlyDictionary<string, Scope> Scopes(Profile profile,
        IEnumerable<Collaboration>? collaborations = null, IEnumerable<Grant>? grants = null)
    {
        var valid = new List<(string Kind, Unit Unit)>();
        if (!profile.Has("administrador"))
        {
            foreach (var link in collaborations ?? [])
            {
                var roleKey = link.Kind switch { "local" => "colaborador_local", "dependencias" => "colaborador_dependencias", _ => "" };
                var role = profile.Roles.FirstOrDefault(r => r.Key == roleKey);
                var grant = (grants ?? []).FirstOrDefault(g => g.Id == link.GrantId);
                if (link.Revoked || link.Email != profile.User.Email || role is null || grant is null
                    || role.Id != link.RoleId || grant.RoleId != link.RoleId || grant.Origin != "aplicacion"
                    || grant.OriginUnit != link.Origin || directory.Affiliation(profile.User)?.Id != link.Origin
                    || UnitDirectory.Employee(profile.User) != link.Employee
                    || !directory.Within(link.Origin, link.Grantor) || !directory.Within(link.Scope, link.Grantor)
                    || directory.Get(link.Scope) is not { } unit) continue;
                if (link.Kind == "local" && directory.FormUnit(link.Origin)?.Id == link.Scope
                    || link.Kind == "dependencias" && unit.Level == 2 && link.Grantor == link.Scope)
                    valid.Add((link.Kind, unit));
            }
        }
        return Compose(profile, directory.Responsibilities(profile.User), valid);
    }
    public IReadOnlyDictionary<string, Scope> Scenario(Profile profile, Unit origin)
    {
        var links = new List<(string, Unit)>();
        if (profile.Has("colaborador_local") && directory.FormUnit(origin.Id) is { } local) links.Add(("local", local));
        if (profile.Has("colaborador_dependencias") && directory.LevelTwo(origin.Id) is { } root) links.Add(("dependencias", root));
        return Compose(profile, [origin], links);
    }
    private Dictionary<string, Scope> Compose(Profile profile, IEnumerable<Unit> responsibilities, IEnumerable<(string Kind, Unit Unit)> links)
    {
        var result = new Dictionary<string, Scope>(StringComparer.Ordinal);
        void Add(Unit unit, bool edit) => result[unit.Id] = new(unit, edit || result.GetValueOrDefault(unit.Id)?.Edit == true);
        if (profile.Has("administrador"))
        {
            var root = directory.AdministratorRoot(profile.User);
            foreach (var unit in directory.Units.Values.Where(UnitDirectory.IsForm))
                Add(unit, root is not null && directory.Within(unit.Id, root.Id));
            return result; // Explicit administrator branch limit wins over every other role.
        }
        if (profile.Has("responsable_ur"))
            foreach (var root in responsibilities)
            {
                Add(root, true);
                if (root.Level == 2)
                    foreach (var unit in directory.Units.Values.Where(u => UnitDirectory.IsForm(u) && directory.Within(u.Id, root.Id))) Add(unit, false);
            }
        foreach (var link in links)
            if (link.Kind == "local") Add(link.Unit, true);
            else foreach (var unit in directory.Units.Values.Where(u => UnitDirectory.IsForm(u) && directory.Within(u.Id, link.Unit.Id))) Add(unit, true);
        if (profile.Has("consulta_institucional")) foreach (var unit in directory.Units.Values.Where(UnitDirectory.IsForm)) Add(unit, false);
        return result;
    }
}
