using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class AccessContextConfiguration : IEntityTypeConfiguration<AccessContext>
{
    public void Configure(EntityTypeBuilder<AccessContext> b)
    {
        b.ToTable("tdv2_access_contexts");
        b.HasKey(x => x.Id).HasName("tdv2_access_contexts_pkey");
        b.Property(x => x.Id).HasColumnName("session_hash").HasColumnType("character(64)").HasMaxLength(64).IsFixedLength().IsRequired();
        b.Property(x => x.Revision).HasColumnName("revision").HasColumnType("bigint").HasDefaultValue(0L);
        b.Property(x => x.Selection).HasColumnName("selection").HasColumnType("text");
        b.Property(x => x.Revision).IsConcurrencyToken();
        b.HasOne(x => x.Session).WithOne(x => x.Context).HasForeignKey<AccessContext>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade).HasConstraintName("tdv2_access_contexts_session_hash_fkey");
    }
}
