using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class InstitutionalSynchronizationConfiguration : IEntityTypeConfiguration<InstitutionalSynchronization>
{
    public void Configure(EntityTypeBuilder<InstitutionalSynchronization> b)
    {
        b.ToTable("sincronizaciones_institucionales");
        b.HasKey(x => x.Id).HasName("sincronizaciones_institucionales_pkey");
        b.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint").UseSerialColumn();
        b.Property(x => x.Summary).HasColumnName("resumen").HasColumnType("json").IsRequired();
        b.Property(x => x.CompletedAt).HasColumnName("completada_en").HasColumnType("timestamp without time zone");

    }
}
