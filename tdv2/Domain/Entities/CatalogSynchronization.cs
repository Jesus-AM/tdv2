namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en sincronizacion_catalogos; no se envía directamente a React.</summary>
public sealed class CatalogSynchronization
{
    public string Id { get; set; } = "";
    public int Records { get; set; }
    public DateTime CompletedAt { get; set; }
}
