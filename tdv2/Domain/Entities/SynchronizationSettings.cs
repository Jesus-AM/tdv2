namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en sincronizacion_configuracion; no se envía directamente a React.</summary>
public sealed class SynchronizationSettings
{
    public short Id { get; set; }
    public bool Active { get; set; } = false;
    public int IntervalMinutes { get; set; } = 60;
    public string Time { get; set; } = "08:00";
    public string TimeZone { get; set; } = "America/Ciudad_Juarez";
    public bool IncludeIlda { get; set; } = false;
    public int Version { get; set; } = 1;
    public DateTime? NextAt { get; set; }
    public DateTime? ProcessorSeenAt { get; set; }
    public Guid? ActiveRunId { get; set; }
    public Guid? Owner { get; set; }
    public DateTime? ReservedUntil { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public SynchronizationRun? ActiveRun { get; set; }
}
