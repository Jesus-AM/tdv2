using Tdv2.Domain.Entities;
namespace Tdv2.Domain;

public sealed record CollaborationIdentity(Profile Profile, IReadOnlyList<Grant> Grants);
public sealed record CollaborationListItem(string key, long? id, string nombre, string email, string tipo,
    string? ur_otorgante, string id_ur_alcance, string alcance, string origen, bool revocada, bool pendiente, bool puedeRetirar);

/// <summary>Proyección por asignación y jerarquía actual, sin crear vínculos locales para las altas centrales.</summary>
public static class CollaborationListing
{
    public static IReadOnlyList<CollaborationListItem> Build(UnitDirectory directory, IReadOnlyDictionary<string, Unit> roots,
        IEnumerable<UnitCollaboration> links, IEnumerable<CollaborationIdentity> identities, bool readOnly,
        IReadOnlySet<string>? managementRoots = null)
    {
        managementRoots ??= roots.Keys.ToHashSet(StringComparer.Ordinal);
        var people = identities.ToDictionary(i => i.Profile.User.Email, StringComparer.Ordinal);
        var result = new List<CollaborationListItem>();
        foreach (var row in links)
        {
            var pending = row.RevokedAt is not null && row.CentralRemovalPending;
            // El retiro pendiente es una operación del otorgante, nunca una asignación efectiva.
            if (pending && !managementRoots.Contains(row.GrantorUnitId)) continue;
            if (!pending && !CollaborationRules.Visible(directory, roots.Values, row.ScopeUnitId, row.Kind)) continue;
            people.TryGetValue(row.Email, out var identity);
            var link = new Collaboration(row.Email, row.EmployeeNumber, row.OriginUnitId, row.ScopeUnitId, row.Kind,
                row.NexoGrantId, row.NexoRoleId, row.GrantorUnitId, row.RevokedAt is not null);
            if (!pending && (identity is null || !ModuleAccess.Allows(identity.Profile, ModuleAccess.Procedures)
                || !CollaborationRules.Delegated(directory, identity.Profile, link, identity.Grants.FirstOrDefault(g => g.Id == row.NexoGrantId)))) continue;
            result.Add(new($"aplicacion:{row.NexoGrantId}:{row.ScopeUnitId}", row.Id, identity?.Profile.User.Name ?? row.Name, row.Email,
                row.Kind, row.GrantorUnitId, row.ScopeUnitId, Label(directory, row.ScopeUnitId), "aplicacion", pending, pending,
                !readOnly && managementRoots.Contains(row.GrantorUnitId)));
        }
        foreach (var identity in people.Values)
            foreach (var grant in identity.Grants.Where(g => g.Origin == "central"))
            {
                var kind = CollaborationRules.Kind(identity.Profile.Roles.FirstOrDefault(r => r.Id == grant.RoleId)?.Key ?? "");
                var scope = CollaborationRules.Central(directory, identity.Profile, grant);
                if (scope is null || kind is null || !CollaborationRules.Visible(directory, roots.Values, scope.Id, kind)) continue;
                result.Add(new($"central:{grant.Id}:{scope.Id}", null, identity.Profile.User.Name, identity.Profile.User.Email,
                    kind, null, scope.Id, Label(directory, scope.Id), "central", false, false, false));
            }
        return result.DistinctBy(r => r.key).OrderBy(r => r.nombre, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.alcance).ThenBy(r => r.tipo).ThenBy(r => r.key).ToArray();
    }
    private static string Label(UnitDirectory directory, string scope) => directory.Get(scope) is { } unit
        ? unit.Code + " · " + (unit.Description ?? unit.Code) : scope;
}
