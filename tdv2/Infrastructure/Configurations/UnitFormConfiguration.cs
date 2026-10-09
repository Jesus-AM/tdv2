using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class UnitFormConfiguration : IEntityTypeConfiguration<UnitForm>
{
    public void Configure(EntityTypeBuilder<UnitForm> b)
    {
        b.ToTable("formatos_ur");
        b.HasKey(x => x.Id).HasName("formatos_ur_pkey");
        b.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint").UseSerialColumn();
        b.Property(x => x.UnitId).HasColumnName("id_ur").HasColumnType("character varying(32)").HasMaxLength(32).IsRequired();
        b.Property(x => x.Content).HasColumnName("contenido").HasColumnType("json").IsRequired();
        b.Property(x => x.Version).HasColumnName("version").HasColumnType("integer").HasDefaultValue(1);
        b.Property(x => x.Progress).HasColumnName("porcentaje").HasColumnType("smallint").HasDefaultValue((short)0);
        b.Property(x => x.UpdatedBy).HasColumnName("actualizado_por").HasColumnType("character varying(254)").HasMaxLength(254).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone");
        b.HasAlternateKey(x => x.UnitId).HasName("formatos_ur_id_ur_key");
        b.HasOne(x => x.Unit).WithOne(x => x.Form).HasForeignKey<UnitForm>(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("formatos_ur_id_ur_fkey");
        b.Property(x => x.Version).IsConcurrencyToken().HasSentinel(-1);
        b.Property(x => x.Year).HasColumnName("ejercicio");
        b.Property(x => x.ActiveStage).HasColumnName("etapa_activa").HasDefaultValue(1);
        b.Property(x => x.SubmittedAt).HasColumnName("enviado_en");
        b.Property(x => x.SubmittedBy).HasColumnName("enviado_por").HasMaxLength(254);
        b.Property(x => x.SubmittedEffective).HasColumnName("enviado_como").HasMaxLength(254);
        b.Property(x => x.SubmissionId).HasColumnName("envio_id");
        b.Property(x => x.SubmissionSnapshot).HasColumnName("instantanea_envio").HasColumnType("jsonb");
    }
}
