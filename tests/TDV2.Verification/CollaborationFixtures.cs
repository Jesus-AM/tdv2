using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Tdv2.Domain;
using Tdv2.Domain.Entities;
using Tdv2.Infrastructure;
using Tdv2.Services;
using Tdv2.Web;
namespace Tdv2.Verification;

internal sealed class MemoryCollaborationDirectory : ICollaborationListingStore, INexoCollaborationDirectory
{
    internal List<UnitCollaboration> Links = [];
    internal List<CollaborationIdentity> People = [];
    internal bool Outage;
    internal bool DelegationEnabled = true;
    internal int Reads;
    internal string[] Affiliations = [];
    public Task<IReadOnlyList<UnitCollaboration>> Candidates(string[] scopes, string[] grantors, CancellationToken cancellation) =>
        Task.FromResult<IReadOnlyList<UnitCollaboration>>(Links.Where(c => scopes.Contains(c.ScopeUnitId) && c.RevokedAt is null
            || grantors.Contains(c.GrantorUnitId) && c.CentralRemovalPending).ToArray());
    public Task<IReadOnlyList<CollaborationIdentity>> Collaborators(string[] affiliations, string[] linkedEmails, CancellationToken cancellation)
    {
        Reads++; Affiliations = affiliations;
        if (Outage) throw new DomainProblem(503, "No se pudo comprobar la operación con Nexo. Inténtalo más tarde.");
        // Entrega también candidatos ajenos para comprobar el filtro final en el servicio real.
        return Task.FromResult<IReadOnlyList<CollaborationIdentity>>(People);
    }
}
internal sealed class SyntheticCollaboratorAccess(RequestAccess access, ICollaborationListingStore data) : ICollaboratorAccess
{
    public async Task<Dictionary<string, Unit>> Roots(HttpContext http)
    {
        var context = await access.Resolve(http);
        if (!context.Profile.Responsible && !context.Profile.Has("administrador")) throw new DomainProblem(403, "Sin delegación.");
        if (data is MemoryCollaborationDirectory { DelegationEnabled: false }) return [];
        // Doble de la publicación Nexo: sólo A y A3 tienen delegación en estos casos.
        IEnumerable<Unit?> units = context.Profile.Has("administrador") ? new[] { context.Directory.AdministratorRoot(context.Profile.User) }
            : context.Directory.Responsibilities(context.Profile.User).Cast<Unit?>();
        return units.Where(u => u is not null && u.Id is "A" or "A3" && u.Employee == UnitDirectory.Employee(context.Profile.User))
            .ToDictionary(u => u!.Id, u => u!);
    }
    public async Task<Unit> Root(HttpContext http, string unit) => (await Roots(http)).GetValueOrDefault(unit) ?? throw new DomainProblem(403, "Área ajena.");
    public Task<Dictionary<string, long>> Roles(CancellationToken ct) => Task.FromResult(new Dictionary<string, long> { ["local"] = 30, ["dependencias"] = 31 });
    public Task<JsonArray> Call(HttpContext http, string operation, string unit, JsonObject data, object[] ownParameters) => throw new InvalidOperationException("No hay operaciones centrales reales en este doble.");
}
