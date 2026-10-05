namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en colaboraciones_ur; no se envía directamente a React.</summary>
public sealed class UnitCollaboration
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string EmployeeNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string OriginUnitId { get; set; } = "";
    public string ScopeUnitId { get; set; } = "";
    public string Kind { get; set; } = "";
    public long NexoGrantId { get; set; }
    public long NexoRoleId { get; set; }
    public string GrantedBy { get; set; } = "";
    public string GrantorUnitId { get; set; } = "";
    public DateTime? RevokedAt { get; set; }
    public bool CentralRemovalPending { get; set; } = false;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
