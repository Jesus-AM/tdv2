namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en sincronizacion_ejecuciones; no se envía directamente a React.</summary>
public sealed class SynchronizationRun
{
    public Guid Id { get; set; }
    public string Sources { get; set; } = "";
    public string Origin { get; set; } = "";
    public string RequestedBy { get; set; } = "";
    public string State { get; set; } = "";
    public string? Stage { get; set; }
    public string Result { get; set; } = "";
    public DateTime RequestedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}
