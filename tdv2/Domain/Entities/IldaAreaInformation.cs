namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en ilda_informacion_area; no se envía directamente a React.</summary>
public sealed class IldaAreaInformation
{
    public string Id { get; set; } = "";
    public string? Ur2 { get; set; }
    public string? Information { get; set; }
    public string Data { get; set; } = "";
    public bool Present { get; set; } = true;
    public DateTime SynchronizedAt { get; set; }
}
