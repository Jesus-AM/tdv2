using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id).HasName("users_pkey");
        b.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint").UseSerialColumn();
        b.Property(x => x.Name).HasColumnName("name").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired();
        b.Property(x => x.Email).HasColumnName("email").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired();
        b.Property(x => x.EmailVerifiedAt).HasColumnName("email_verified_at").HasColumnType("timestamp without time zone");
        b.Property(x => x.Password).HasColumnName("password").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired();
        b.Property(x => x.RememberToken).HasColumnName("remember_token").HasColumnType("character varying(100)").HasMaxLength(100);
        b.Property(x => x.MicrosoftTenantId).HasColumnName("microsoft_tenant_id").HasColumnType("uuid");
        b.Property(x => x.MicrosoftId).HasColumnName("microsoft_id").HasColumnType("uuid");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone");
        b.HasAlternateKey(x => x.Email).HasName("users_email_key");
        b.HasIndex(x => new { x.MicrosoftTenantId, x.MicrosoftId }).IsUnique().HasDatabaseName("users_microsoft_identity_unique");
    }
}
