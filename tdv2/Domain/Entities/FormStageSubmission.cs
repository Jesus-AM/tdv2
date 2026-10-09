namespace Tdv2.Domain.Entities;

/// <summary>Envío inmutable de una etapa. Los envíos globales anteriores permanecen en UnitForm.</summary>
public sealed class FormStageSubmission
{
    public string UnitId { get; set; } = "";
    public int Stage { get; set; }
    public int Year { get; set; }
    public int Version { get; set; }
    public Guid OperationId { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public string Actor { get; set; } = "";
    public string Effective { get; set; } = "";
    public string Name { get; set; } = "";
    public string Snapshot { get; set; } = "";
    public UnitForm Form { get; set; } = null!;
}
