namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en sincronizaciones_institucionales; no se envía directamente a React.</summary>
public sealed class InstitutionalSynchronization
{
    public long Id { get; set; }
    public string Summary { get; set; } = "";
    public DateTime CompletedAt { get; set; }
}
