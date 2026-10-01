using System.Text.Json.Nodes;
using Tdv2.Domain;
namespace Tdv2.Infrastructure;

public sealed record StoredForm(string UnitId, JsonObject Content, int Version, int Progress, DateTimeOffset UpdatedAt, string UpdatedBy);
public sealed record SaveForm(int? Version, JsonObject? Contenido);
public interface IFormStore
{
    Task<IReadOnlyList<Unit>> Units(CancellationToken cancellation);
    Task<IReadOnlyList<Collaboration>> Collaborations(string email, CancellationToken cancellation);
    Task<StoredForm?> Get(string unit, CancellationToken cancellation);
    Task<StoredForm> Save(string unit, int expectedVersion, JsonObject content, string actor, CancellationToken cancellation);
}
public interface INexoProfiles
{
    Task<Profile> ForEmail(string email, CancellationToken cancellation);
    Task<IReadOnlyList<Grant>> Grants(string email, CancellationToken cancellation);
}
