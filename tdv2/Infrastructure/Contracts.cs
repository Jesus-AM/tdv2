using System.Text.Json.Nodes;
using Tdv2.Domain;
namespace Tdv2.Infrastructure;

public sealed record StoredForm(string UnitId, JsonObject Content, int Version, int Progress, DateTimeOffset UpdatedAt, string UpdatedBy,
    DateTimeOffset? SubmittedAt = null, string? SubmissionSnapshot = null, int Year = 0, string? SubmittedEffective = null);
public sealed record SaveForm(int? Version, JsonObject? Contenido);
public interface IFormStore
{
    Task<IReadOnlyList<Unit>> Units(CancellationToken cancellation);
    Task<IReadOnlyList<Collaboration>> Collaborations(string email, CancellationToken cancellation);
    Task<StoredForm?> Get(string unit, CancellationToken cancellation);
}
public interface INexoProfiles
{
    Task<Profile> ForEmail(string email, CancellationToken cancellation);
    Task<IReadOnlyList<Grant>> Grants(string email, CancellationToken cancellation);
}
