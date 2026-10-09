namespace Tdv2.Domain.Entities;

/// <summary>Réplica local; Present indica disponibilidad observada, no ACTIVO institucional.</summary>
public sealed class SiiModule
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public bool Present { get; set; }
    public DateTime SynchronizedAt { get; set; }
}
