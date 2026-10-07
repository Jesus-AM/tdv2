namespace Tdv2.Domain.Entities;

/// <summary>Una versión por bloque y una reserva con testigo nuevo después de cada vencimiento.</summary>
public sealed class FormBlock
{
    public string UnitId { get; set; } = "";
    public string Key { get; set; } = "";
    public int Version { get; set; }
    public Guid? LeaseId { get; set; }
    public string? SessionHash { get; set; }
    public Guid? TabId { get; set; }
    public long ContextRevision { get; set; }
    public string? Holder { get; set; }
    public string? Participant { get; set; }
    public long? ParticipantUserId { get; set; }
    public int Color { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public sealed class FormMutation
{
    public string UnitId { get; set; } = "";
    public Guid Id { get; set; }
    public string SessionHash { get; set; } = "";
    public long ContextRevision { get; set; }
    public Guid TabId { get; set; }
    public string Fingerprint { get; set; } = "";
    public string Response { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class FormPosition
{
    public string UnitId { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Effective { get; set; } = "";
    public string Section { get; set; } = "contexto";
}
