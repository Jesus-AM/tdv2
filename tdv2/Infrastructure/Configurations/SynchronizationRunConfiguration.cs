using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class SynchronizationRunConfiguration : IEntityTypeConfiguration<SynchronizationRun>
{
    public void Configure(EntityTypeBuilder<SynchronizationRun> b)
    {
        b.ToTable("sincronizacion_ejecuciones");
        b.HasKey(x => x.Id).HasName("sincronizacion_ejecuciones_pkey");
        b.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        b.Property(x => x.Sources).HasColumnName("fuentes").HasColumnType("character varying(12)").HasMaxLength(12).IsRequired();
        b.Property(x => x.Origin).HasColumnName("origen").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
        b.Property(x => x.RequestedBy).HasColumnName("solicitado_por").HasColumnType("character varying(254)").HasMaxLength(254).IsRequired();
        b.Property(x => x.State).HasColumnName("estado").HasColumnType("character varying(20)").HasMaxLength(20).IsRequired();
        b.Property(x => x.Stage).HasColumnName("etapa").HasColumnType("character varying(12)").HasMaxLength(12);
        b.Property(x => x.Result).HasColumnName("resultado").HasColumnType("json").IsRequired();
        b.Property(x => x.RequestedAt).HasColumnName("solicitada_en").HasColumnType("timestamp without time zone");
        b.Property(x => x.StartedAt).HasColumnName("iniciada_en").HasColumnType("timestamp without time zone");
        b.Property(x => x.FinishedAt).HasColumnName("terminada_en").HasColumnType("timestamp without time zone");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.State).HasDatabaseName("sincronizacion_ejecuciones_estado_index");
    }
}
