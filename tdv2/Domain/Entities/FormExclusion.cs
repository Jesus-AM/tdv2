namespace Tdv2.Domain.Entities;

/// <summary>Retiro del formato, nunca de la réplica ILDA. Conserva la identidad del inventario y del ejercicio.</summary>
public sealed class FormExclusion
{
    public string UnitId { get; set; } = "";
    public int Year { get; set; }
    public string RowId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string Actor { get; set; } = "";
    public string Effective { get; set; } = "";
}
