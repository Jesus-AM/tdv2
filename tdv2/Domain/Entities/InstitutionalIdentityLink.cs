namespace Tdv2.Domain.Entities;

// Correspondencia acreditada por OIDC + UPN/SMTP de Graph + identidad Nexo única,
// o por el operador. No concede roles y no se modifica desde el login ni por HTTP.
public sealed class InstitutionalIdentityLink
{
    public Guid TenantId { get; set; }
    public Guid ObjectId { get; set; }
    public long NexoUserId { get; set; }
    public DateTimeOffset VerifiedAt { get; set; }
    public string VerifiedBy { get; set; } = "";
    public string VerificationReference { get; set; } = "";
}
