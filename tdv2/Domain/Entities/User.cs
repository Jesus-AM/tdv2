namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en users; no se envía directamente a React.</summary>
public sealed class User
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime? EmailVerifiedAt { get; set; }
    public string Password { get; set; } = "";
    public string? RememberToken { get; set; }
    public Guid? MicrosoftTenantId { get; set; }
    public Guid? MicrosoftId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
