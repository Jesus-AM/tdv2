namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en formatos_ur; no se envía directamente a React.</summary>
public sealed class UnitForm
{
    public long Id { get; set; }
    public string UnitId { get; set; } = "";
    public string Content { get; set; } = "";
    public int Version { get; set; } = 1;
    public short Progress { get; set; } = (short)0;
    public string UpdatedBy { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int Year { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public string? SubmittedBy { get; set; }
    public string? SubmittedEffective { get; set; }
    public Guid? SubmissionId { get; set; }
    public string? SubmissionSnapshot { get; set; }

    public ResponsibleUnit Unit { get; set; } = null!;
}
