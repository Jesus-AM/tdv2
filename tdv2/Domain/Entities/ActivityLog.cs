namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en activity_logs; no se envía directamente a React.</summary>
public sealed class ActivityLog
{
    public long Id { get; set; }
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
    public long? RoleId { get; set; }
    public string? Ur { get; set; }
    public string? Ur2 { get; set; }
    public string? Entity { get; set; }
    public long? RecordId { get; set; }
    public string? Action { get; set; }
    public string? Meta { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
