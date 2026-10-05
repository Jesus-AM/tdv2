namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en unidades_responsables_poa; no se envía directamente a React.</summary>
public sealed class ResponsibleUnit
{
    public string Id { get; set; } = "";
    public int Year { get; set; }
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? Manager { get; set; }
    public string? ParentId { get; set; }
    public string? Kind { get; set; }
    public int? Level { get; set; }
    public string? Status { get; set; }
    public bool Present { get; set; } = true;
    public DateTime? SynchronizedAt { get; set; }

    public UnitForm? Form { get; set; }
}
