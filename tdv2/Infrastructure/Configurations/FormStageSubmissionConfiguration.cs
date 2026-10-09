using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;
namespace Tdv2.Infrastructure.Configurations;

public sealed class FormStageSubmissionConfiguration : IEntityTypeConfiguration<FormStageSubmission>
{
    public void Configure(EntityTypeBuilder<FormStageSubmission> b)
    {
        b.ToTable("formato_envios_etapas", t => t.HasCheckConstraint("formato_envios_etapas_valores_ck", "etapa > 0 AND version >= 0"));
        b.HasKey(x => new { x.UnitId, x.Stage });
        b.Property(x => x.UnitId).HasColumnName("id_ur").HasMaxLength(32);
        b.Property(x => x.Stage).HasColumnName("etapa");
        b.Property(x => x.Year).HasColumnName("ejercicio");
        b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.OperationId).HasColumnName("operacion");
        b.Property(x => x.SubmittedAt).HasColumnName("enviado_en");
        b.Property(x => x.Actor).HasColumnName("actor").HasMaxLength(254);
        b.Property(x => x.Effective).HasColumnName("efectivo").HasMaxLength(254);
        b.Property(x => x.Name).HasColumnName("nombre").HasMaxLength(512);
        b.Property(x => x.Snapshot).HasColumnName("instantanea").HasColumnType("jsonb");
        b.HasIndex(x => new { x.UnitId, x.OperationId }).IsUnique();
        b.HasOne(x => x.Form).WithMany(x => x.StageSubmissions).HasForeignKey(x => x.UnitId)
            .HasPrincipalKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
