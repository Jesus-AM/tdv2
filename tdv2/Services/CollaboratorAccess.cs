using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
using Tdv2.Synchronization;
using Tdv2.Web;
namespace Tdv2.Services;

/// <summary>Interseca responsabilidad local con delegación vigente de Nexo; el rol administrador no basta.</summary>
public sealed class CollaboratorAccess(RequestAccess access, NexoOperations nexo)
{
    public async Task<Dictionary<string, Unit>> Roots(HttpContext http)
    {
        var context = await access.Resolve(http); var profile = context.Profile; var directory = context.Directory;
        var candidates = new Dictionary<string, Unit>(StringComparer.Ordinal);
        if (profile.Has("administrador"))
        {
            if (directory.AdministratorRoot(profile.User) is { } root) candidates.Add(root.Id, root);
        }
        else if (profile.Has("responsable_ur"))
        {
            var units = access.Selection is { Kind: "preview" } selection ? new[] { directory.Get(selection.UnitId)! } : directory.Responsibilities(profile.User);
            foreach (var unit in units.Where(u => u.Level == 2)) candidates.Add(unit.Id, unit);
        }
        if (candidates.Count == 0 && !profile.Has("administrador")) throw new DomainProblem(403, "No tienes autorización para administrar colaboradores.");
        if (access.Selection?.Kind == "preview" || candidates.Count == 0) return candidates;
        var rows = await nexo.Rows("delegacion", profile.User.Email, http.RequestAborted);
        var employee = UnitDirectory.Employee(profile.User);
        return candidates.Where(pair => rows.OfType<JsonObject>().Count(row => NexoOperations.Text(row, "id_ur") == pair.Key
            && employee is not null && NexoOperations.Text(row, "num_empleado") == employee && employee == pair.Value.Employee
            && NexoOperations.Number(row, "nivel_ur") == pair.Value.Level) == 1).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
    }
    public async Task<Unit> Root(HttpContext http, string unit) => (await Roots(http)).GetValueOrDefault(unit)
        ?? throw new DomainProblem(403, "El área no está habilitada para delegar en Nexo y TDV2.");
    public async Task<Dictionary<string, long>> Roles(CancellationToken ct)
    {
        var rows = await nexo.Rows("delegacion_roles", null, ct); var result = new Dictionary<string, long>();
        foreach (var (kind, key) in new[] { ("local", "colaborador_local"), ("dependencias", "colaborador_dependencias") })
        {
            var matches = rows.OfType<JsonObject>().Where(r => NexoOperations.Text(r, "clave") == key).ToArray();
            if (matches.Length > 1 || matches.Length == 1 && NexoOperations.Number(matches[0], "rol_id") <= 0) throw new DomainProblem(503, "Nexo no publicó roles de delegación válidos.");
            if (matches.Length == 1) result[kind] = NexoOperations.Number(matches[0], "rol_id");
        }
        return result;
    }
    public async Task<JsonArray> Call(HttpContext http, string operation, string unit, JsonObject data, object[] ownParameters)
    {
        if (access.Selection is not { Kind: "representation" } selection) return await nexo.Delegate(operation, ownParameters, http.RequestAborted);
        data["ur"] = unit; data["token"] = selection.Token;
        return await nexo.Represent(operation, access.Actor(http), data, http.RequestAborted) as JsonArray
            ?? throw new DomainProblem(503, "Nexo no confirmó el resultado de la operación delegada.");
    }
}
