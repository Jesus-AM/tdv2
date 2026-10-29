using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class InstitutionalIdentityLinkConfiguration : IEntityTypeConfiguration<InstitutionalIdentityLink>
{
    public void Configure(EntityTypeBuilder<InstitutionalIdentityLink> b)
    {
        b.ToTable("tdv2_identity_links", t => t.HasCheckConstraint("identity_link_verified", "nexo_user_id > 0 AND length(trim(verified_by)) > 0 AND length(trim(verification_reference)) > 0"));
        b.HasKey(x => new { x.TenantId, x.ObjectId });
        b.Property(x => x.TenantId).HasColumnName("microsoft_tenant_id");
        b.Property(x => x.ObjectId).HasColumnName("microsoft_id");
        b.Property(x => x.NexoUserId).HasColumnName("nexo_user_id");
        b.HasIndex(x => x.NexoUserId).IsUnique();
        b.Property(x => x.VerifiedAt).HasColumnName("verified_at");
        b.Property(x => x.VerifiedBy).HasColumnName("verified_by").HasMaxLength(255).IsRequired();
        b.Property(x => x.VerificationReference).HasColumnName("verification_reference").HasMaxLength(512).IsRequired();
    }
}
