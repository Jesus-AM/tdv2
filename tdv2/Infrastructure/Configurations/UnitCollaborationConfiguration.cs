using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class UnitCollaborationConfiguration : IEntityTypeConfiguration<UnitCollaboration>
{
    public void Configure(EntityTypeBuilder<UnitCollaboration> b)
    {
        b.ToTable("colaboraciones_ur");
        b.HasKey(x => x.Id).HasName("colaboraciones_ur_pkey");
        b.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint").UseSerialColumn();
        b.Property(x => x.Email).HasColumnName("email").HasColumnType("character varying(254)").HasMaxLength(254).IsRequired();
        b.Property(x => x.EmployeeNumber).HasColumnName("num_empleado").HasColumnType("character varying(32)").HasMaxLength(32).IsRequired();
        b.Property(x => x.Name).HasColumnName("nombre").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired();
        b.Property(x => x.OriginUnitId).HasColumnName("id_ur_origen").HasColumnType("character varying(32)").HasMaxLength(32).IsRequired();
        b.Property(x => x.ScopeUnitId).HasColumnName("id_ur_alcance").HasColumnType("character varying(32)").HasMaxLength(32).IsRequired();
        b.Property(x => x.Kind).HasColumnName("tipo").HasColumnType("character varying(24)").HasMaxLength(24).IsRequired();
        b.Property(x => x.NexoGrantId).HasColumnName("nexo_concesion_id").HasColumnType("bigint");
        b.Property(x => x.NexoRoleId).HasColumnName("nexo_rol_id").HasColumnType("bigint");
        b.Property(x => x.GrantedBy).HasColumnName("otorgado_por").HasColumnType("character varying(254)").HasMaxLength(254).IsRequired();
        b.Property(x => x.GrantorUnitId).HasColumnName("ur_otorgante").HasColumnType("character varying(32)").HasMaxLength(32).IsRequired();
        b.Property(x => x.RevokedAt).HasColumnName("revocada_en").HasColumnType("timestamp without time zone");
        b.Property(x => x.CentralRemovalPending).HasColumnName("retiro_central_pendiente").HasColumnType("boolean").HasDefaultValue(false);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone");
        // La relación con Nexo es externa y se revalida por solicitud; EF no administra ese esquema.
        b.HasAlternateKey(x => new { x.NexoGrantId, x.ScopeUnitId }).HasName("colaboracion_concesion_alcance_unique");
    }
}
