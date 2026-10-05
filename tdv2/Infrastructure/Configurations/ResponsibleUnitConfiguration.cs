using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class ResponsibleUnitConfiguration : IEntityTypeConfiguration<ResponsibleUnit>
{
    public void Configure(EntityTypeBuilder<ResponsibleUnit> b)
    {
        b.ToTable("unidades_responsables_poa");
        b.HasKey(x => x.Id).HasName("unidades_responsables_poa_pkey");
        b.Property(x => x.Id).HasColumnName("id_ur").HasColumnType("character varying(32)").HasMaxLength(32).IsRequired();
        b.Property(x => x.Year).HasColumnName("ejercicio").HasColumnType("integer");
        b.Property(x => x.Code).HasColumnName("cve_ur").HasColumnType("character varying(32)").HasMaxLength(32).IsRequired();
        b.Property(x => x.Description).HasColumnName("desc_ur").HasColumnType("character varying(500)").HasMaxLength(500);
        b.Property(x => x.EmployeeNumber).HasColumnName("num_empleado").HasColumnType("character varying(32)").HasMaxLength(32);
        b.Property(x => x.Manager).HasColumnName("encargado").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.ParentId).HasColumnName("id_ur_pertenece").HasColumnType("character varying(32)").HasMaxLength(32);
        b.Property(x => x.Kind).HasColumnName("tipo_ur").HasColumnType("character varying(32)").HasMaxLength(32);
        b.Property(x => x.Level).HasColumnName("nivel_ur").HasColumnType("integer");
        b.Property(x => x.Status).HasColumnName("estatus_ur").HasColumnType("character varying(32)").HasMaxLength(32);
        b.Property(x => x.Present).HasColumnName("presente").HasColumnType("boolean").HasDefaultValue(true);
        b.Property(x => x.SynchronizedAt).HasColumnName("sincronizado_en").HasColumnType("timestamp without time zone");
        // Empleado y jerarquía son claves textuales de la réplica SII, no permisos locales.
        b.HasIndex(x => x.EmployeeNumber).HasDatabaseName("unidades_responsables_poa_num_empleado_index");
        b.HasIndex(x => x.ParentId).HasDatabaseName("unidades_responsables_poa_id_ur_pertenece_index");
    }
}
