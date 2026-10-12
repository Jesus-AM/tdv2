namespace Tdv2.Domain;

/// <summary>Las mismas comprobaciones de concesión alimentan permisos y visibilidad; listar no autoriza escrituras.</summary>
public static class CollaborationRules
{
    public static string? Kind(string key) => key switch { "colaborador_local" => "local", "colaborador_dependencias" => "dependencias", _ => null };
    public static Unit? Central(UnitDirectory directory, Profile profile, Grant grant)
    {
        if (grant.Id <= 0 || grant.Origin != "central" || !ModuleAccess.Allows(profile, ModuleAccess.Procedures)
            || directory.Affiliation(profile.User) is not { } affiliation) return null;
        return Kind(profile.Roles.FirstOrDefault(r => r.Id == grant.RoleId)?.Key ?? "") switch
        {
            // Contrato publicado vigente: id_ur_acceso es origen histórico, no área asignada.
            "local" => directory.FormUnit(affiliation.Id),
            "dependencias" => directory.LevelTwo(affiliation.Id),
            _ => null
        };
    }
    public static bool Delegated(UnitDirectory directory, Profile profile, Collaboration link, Grant? grant)
    {
        var role = profile.Roles.FirstOrDefault(r => Kind(r.Key) == link.Kind);
        if (link.Revoked || link.Email != profile.User.Email || role is null || grant is null
            || role.Id != link.RoleId || grant.RoleId != link.RoleId || grant.Origin != "aplicacion"
            || grant.OriginUnit != link.Origin || directory.Affiliation(profile.User)?.Id != link.Origin
            || UnitDirectory.Employee(profile.User) != link.Employee
            || !directory.Within(link.Origin, link.Grantor) || !directory.Within(link.Scope, link.Grantor)
            || directory.Get(link.Scope) is not { } unit || !UnitDirectory.IsForm(unit) || !directory.Participates(unit)) return false;
        return link.Kind == "local" && directory.LocalCollaborationUnit(link.Origin, link.Grantor)?.Id == link.Scope
            || link.Kind == "dependencias" && unit.Level == 2 && link.Grantor == link.Scope;
    }
    public static bool Visible(UnitDirectory directory, IEnumerable<Unit> roots, string scope, string kind) =>
        kind is "local" or "dependencias" && directory.Get(scope) is not null && roots.Any(root =>
            root.Level == 2 && directory.LevelTwo(scope)?.Id == root.Id
            || root.Level == 3 && kind == "local" && root.Id == scope);
}
