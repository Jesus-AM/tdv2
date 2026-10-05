using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class CatalogSynchronizationConfiguration : IEntityTypeConfiguration<CatalogSynchronization>
{
    public void Configure(EntityTypeBuilder<CatalogSynchronization> b)
    {
        b.ToTable("sincronizacion_catalogos");
        b.HasKey(x => x.Id).HasName("sincronizacion_catalogos_pkey");
        b.Property(x => x.Id).HasColumnName("fuente").HasColumnType("character varying(12)").HasMaxLength(12).IsRequired();
        b.Property(x => x.Records).HasColumnName("registros").HasColumnType("integer");
        b.Property(x => x.CompletedAt).HasColumnName("completada_en").HasColumnType("timestamp without time zone");

    }
}
