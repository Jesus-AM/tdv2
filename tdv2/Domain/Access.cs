namespace Tdv2.Domain;

public sealed record Unit(string Id, string Code, string? Description, int Level, string? Parent,
    int Year, string? Employee = null, bool Present = true, string Status = "Activo", string? Kind = null)
{
    public object Public() => new
    {
        id_ur = Id,
        cve_ur = Code,
        desc_ur = Description ?? Code,
        nivel_ur = Level,
        tipo_ur = Kind,
        id_ur_pertenece = Parent,
        ejercicio = Year
    };
}
public sealed record Person(string Email, string Name, string? Employee, string? UnitId,
    string Origin = "adscripcion", string AccountType = "individual");
public sealed record Role(long Id, string Key, string Name);
public sealed record Module(long Id, string Key, string Name, string Path, long? Parent = null, string Icon = "mdi-view-grid-outline");
public sealed record Profile(Person User, IReadOnlyList<Role> Roles, IReadOnlyList<Module> Modules)
{
    public bool Has(string role) => Roles.Any(r => r.Key == role);
    // Alias temporal: conserva las concesiones anteriores durante la transición en Nexo.
    // El rol genérico «supervisor» no se eleva: puede pertenecer a otro catálogo compartido.
    public bool SupervisingResponsible => Has("responsable_ur_supervisor") || Has("responsable_ur_institucional");
    public bool Responsible => Has("responsable_ur") || SupervisingResponsible;
    public bool InstitutionalRead => Has("administrador") || Has("consulta_institucional") || SupervisingResponsible;
}
public sealed record Collaboration(string Email, string Employee, string Origin, string Scope,
    string Kind, long GrantId, long RoleId, string Grantor, bool Revoked = false);
public sealed record Grant(long Id, long RoleId, string OriginUnit, string Origin = "aplicacion");
public sealed record Scope(Unit Unit, bool Edit, bool Own = false);

/// <summary>Resuelve jerarquía y responsabilidades sólo entre UR presentes y activas.</summary>
public sealed class UnitDirectory(IEnumerable<Unit> units)
{
    public IReadOnlyDictionary<string, Unit> Units { get; } = units
        .Where(u => u.Present && u.Status.Trim().Equals("activo", StringComparison.OrdinalIgnoreCase))
        .ToDictionary(u => u.Id, StringComparer.Ordinal);
    public Unit? Get(string? id) => id is not null && Units.TryGetValue(id, out var unit) ? unit : null;
    // TIPO_UR es independiente del nivel y de CVE_UR. Los nodos auxiliares siguen en Units.
    public static bool IsEligible(Unit unit) => !int.TryParse(unit.Kind, out var kind) || kind != 0;
    public static bool IsForm(Unit unit) => IsEligible(unit) && unit.Level is 2 or 3;
    private IEnumerable<Unit> Ancestors(string? id)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (Get(id) is { } unit && seen.Add(unit.Id)) { yield return unit; id = unit.Parent; }
    }
    public bool Within(string id, string root) => Ancestors(id).Any(u => u.Id == root);
    public Unit? FormUnit(string? id) => Ancestors(id).FirstOrDefault(IsForm);
    public Unit? LevelTwo(string? id) => Ancestors(id).FirstOrDefault(u => u.Level == 2 && IsEligible(u));
    public string? DirectoryParent(Unit unit) => unit.Level == 2 ? null : LevelTwo(unit.Parent)?.Id;
    public Unit? LocalCollaborationUnit(string? origin, string grantor)
    {
        var root = Get(grantor);
        if (root is null || !IsForm(root) || origin is null || !Within(origin, grantor)) return null;
        // El encargado nivel 3 recibe ayuda exclusivamente en su propio formato.
        var owner = root.Level == 3 ? root : FormUnit(origin);
        return owner is not null && Within(owner.Id, grantor) ? owner : null;
    }
    // El empleado es una clave textual institucional: convertirlo a número perdería ceros y ampliaría accesos.
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
        (object)new
        {
            id = m.Id,
            key = m.Key,
            name = m.Name,
            route = m.Path,
            parent = Known[m.Key].Parent,
            icon = System.Text.RegularExpressions.Regex.IsMatch(m.Icon, "\\Amdi-[a-z0-9-]+\\z") ? m.Icon : "mdi-view-grid-outline"
        }).ToArray();
}

/// <summary>Calcula lectura y edición por UR a partir de identidad y concesiones revalidadas en Nexo.</summary>
public sealed class FormAccess(UnitDirectory directory)
{
    public bool CanSubmit(Profile profile, Unit unit) =>
        (profile.Responsible || profile.Has("administrador"))
        && directory.Responsibilities(profile.User).Any(root => root.Id == unit.Id
            || root.Level == 2 && directory.Within(unit.Id, root.Id));
    public IReadOnlyDictionary<string, Scope> Scopes(Profile profile,
        IEnumerable<Collaboration>? collaborations = null, IEnumerable<Grant>? grants = null)
    {
        var valid = new List<(string Kind, Unit Unit)>();
        if (!profile.Has("administrador"))
        {
            // Ni el vínculo local ni la concesión central bastan por separado. Deben coincidir
            // empleado, adscripción, rol, alcance y otorgante para resistir revocaciones y traslados.
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
                    || directory.Get(link.Scope) is not { } unit || !UnitDirectory.IsForm(unit)) continue;
                if (link.Kind == "local" && directory.LocalCollaborationUnit(link.Origin, link.Grantor)?.Id == link.Scope
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
        void Add(Unit unit, bool edit, bool own = true) => result[unit.Id] = new(unit,
            edit || result.GetValueOrDefault(unit.Id)?.Edit == true, own || result.GetValueOrDefault(unit.Id)?.Own == true);
        if (profile.Has("administrador"))
        {
            var root = directory.AdministratorRoot(profile.User);
            foreach (var unit in directory.Units.Values.Where(UnitDirectory.IsForm))
                Add(unit, root is not null && directory.Within(unit.Id, root.Id), root is not null && directory.Within(unit.Id, root.Id));
            return result; // La rama del administrador limita la edición incluso al combinar otros roles.
        }
        if (profile.Responsible)
            foreach (var root in responsibilities)
            {
                Add(root, true);
                if (root.Level == 2)
                    foreach (var unit in directory.Units.Values.Where(u => UnitDirectory.IsForm(u) && directory.Within(u.Id, root.Id))) Add(unit, true);
            }
        foreach (var link in links)
            if (link.Kind == "local") Add(link.Unit, true);
            else foreach (var unit in directory.Units.Values.Where(u => UnitDirectory.IsForm(u) && directory.Within(u.Id, link.Unit.Id))) Add(unit, true);
        if (profile.InstitutionalRead) foreach (var unit in directory.Units.Values.Where(UnitDirectory.IsForm)) Add(unit, false, false);
        return result;
    }
}
