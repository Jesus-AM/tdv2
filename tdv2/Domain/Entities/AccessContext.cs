namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en tdv2_access_contexts; no se envía directamente a React.</summary>
public sealed class AccessContext
{
    public string Id { get; set; } = "";
    public long Revision { get; set; } = 0L;
    public string? Selection { get; set; }

    public SessionTicket Session { get; set; } = null!;
}
