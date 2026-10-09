using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;
namespace Tdv2.Infrastructure.Configurations;

public sealed class SiiModuleConfiguration : IEntityTypeConfiguration<SiiModule>
{
    public void Configure(EntityTypeBuilder<SiiModule> b)
    {
        b.ToTable("sii_modulos");
        b.HasKey(x => x.Id).HasName("sii_modulos_pkey");
        b.Property(x => x.Id).HasColumnName("id_modulo").HasMaxLength(40).ValueGeneratedNever();
        b.Property(x => x.Description).HasColumnName("desc_modulo").HasColumnType("text").IsRequired();
        b.Property(x => x.Present).HasColumnName("presente");
        b.Property(x => x.SynchronizedAt).HasColumnName("sincronizado_en").HasColumnType("timestamp without time zone");
    }
}
