using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;
namespace Tdv2.Infrastructure.Configurations;

public sealed class FormBlockConfiguration : IEntityTypeConfiguration<FormBlock>
{
    public void Configure(EntityTypeBuilder<FormBlock> b)
    {
        b.ToTable("formato_bloques", t => t.HasCheckConstraint("formato_bloques_version_ck", "version >= 0"));
        b.HasKey(x => new { x.UnitId, x.Key });
        b.Property(x => x.UnitId).HasColumnName("id_ur").HasMaxLength(32);
        b.Property(x => x.Key).HasColumnName("bloque").HasMaxLength(512);
        b.Property(x => x.Version).HasColumnName("version");
        b.Property(x => x.LeaseId).HasColumnName("reserva_id");
        b.Property(x => x.SessionHash).HasColumnName("sesion").HasMaxLength(64);
        b.Property(x => x.TabId).HasColumnName("pestana");
        b.Property(x => x.ContextRevision).HasColumnName("revision_contexto");
        b.Property(x => x.Holder).HasColumnName("titular").HasMaxLength(512);
        b.Property(x => x.Participant).HasColumnName("participante").HasMaxLength(64);
        b.Property(x => x.ParticipantUserId).HasColumnName("usuario_participante_id");
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ParticipantUserId).OnDelete(DeleteBehavior.SetNull);
        b.Property(x => x.Color).HasColumnName("color");
        b.Property(x => x.ExpiresAt).HasColumnName("vence_en");
        b.HasIndex(x => new { x.UnitId, x.ExpiresAt });
        b.HasOne<UnitForm>().WithMany().HasForeignKey(x => x.UnitId).HasPrincipalKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class FormMutationConfiguration : IEntityTypeConfiguration<FormMutation>
{
    public void Configure(EntityTypeBuilder<FormMutation> b)
    {
        b.ToTable("formato_operaciones"); b.HasKey(x => new { x.UnitId, x.Id });
        b.Property(x => x.UnitId).HasColumnName("id_ur").HasMaxLength(32);
        b.Property(x => x.Id).HasColumnName("operacion");
        b.Property(x => x.SessionHash).HasColumnName("sesion").HasMaxLength(64);
        b.Property(x => x.ContextRevision).HasColumnName("revision_contexto");
        b.Property(x => x.TabId).HasColumnName("pestana");
        b.Property(x => x.Fingerprint).HasColumnName("huella").HasMaxLength(64);
        b.Property(x => x.Response).HasColumnName("respuesta").HasColumnType("jsonb");
        b.Property(x => x.CreatedAt).HasColumnName("creado_en");
        b.HasOne<UnitForm>().WithMany().HasForeignKey(x => x.UnitId).HasPrincipalKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class FormPositionConfiguration : IEntityTypeConfiguration<FormPosition>
{
    public void Configure(EntityTypeBuilder<FormPosition> b)
    {
        b.ToTable("formato_posiciones"); b.HasKey(x => new { x.UnitId, x.Actor, x.Effective });
        b.Property(x => x.UnitId).HasColumnName("id_ur").HasMaxLength(32);
        b.Property(x => x.Actor).HasColumnName("actor").HasMaxLength(254);
        b.Property(x => x.Effective).HasColumnName("efectivo").HasMaxLength(254);
        b.Property(x => x.Section).HasColumnName("seccion").HasMaxLength(32);
        b.HasOne<UnitForm>().WithMany().HasForeignKey(x => x.UnitId).HasPrincipalKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
