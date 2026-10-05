using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class OAuthAttemptConfiguration : IEntityTypeConfiguration<OAuthAttempt>
{
    public void Configure(EntityTypeBuilder<OAuthAttempt> b)
    {
        b.ToTable("tdv2_oauth_attempts");
        b.HasKey(x => x.Id).HasName("tdv2_oauth_attempts_pkey");
        b.Property(x => x.Id).HasColumnName("state_hash").HasColumnType("character(64)").HasMaxLength(64).IsFixedLength().IsRequired();
        b.Property(x => x.BrowserHash).HasColumnName("browser_hash").HasColumnType("character(64)").HasMaxLength(64).IsFixedLength().IsRequired();
        b.Property(x => x.Verifier).HasColumnName("verifier").HasColumnType("text").IsRequired();
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
        b.HasIndex(x => x.ExpiresAt).HasDatabaseName("tdv2_oauth_attempts_expiry");
    }
}
