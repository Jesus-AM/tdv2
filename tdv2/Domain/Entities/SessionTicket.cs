namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en tdv2_sessions; no se envía directamente a React.</summary>
public sealed class SessionTicket
{
    public string Id { get; set; } = "";
    public string Ticket { get; set; } = "";
    public DateTime ExpiresAt { get; set; }

    public AccessContext? Context { get; set; }
}
