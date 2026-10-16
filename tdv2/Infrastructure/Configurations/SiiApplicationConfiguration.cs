using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;
namespace Tdv2.Infrastructure.Configurations;

public sealed class SiiMobileApplicationConfiguration : IEntityTypeConfiguration<SiiMobileApplication>
{
    public void Configure(EntityTypeBuilder<SiiMobileApplication> b)
    {
        b.ToTable("sii_aplicaciones_moviles");
        b.HasKey(x => x.Id).HasName("sii_aplicaciones_moviles_pkey");
        b.Property(x => x.Id).HasColumnName("id_aplicacion_movil").HasColumnType("integer").ValueGeneratedNever();
        b.Property(x => x.Name).HasColumnName("nombre").HasMaxLength(200).IsRequired();
        b.Property(x => x.Definition).HasColumnName("definicion").HasMaxLength(200);
        b.Property(x => x.EmployeeResponsible).HasColumnName("emp_responsable").HasColumnType("integer");
        b.Property(x => x.ProgrammerResponsible).HasColumnName("prog_responsable").HasColumnType("integer");
        b.Property(x => x.UnitId).HasColumnName("id_ur").HasColumnType("integer");
        b.Property(x => x.Active).HasColumnName("activa").HasColumnType("boolean");
        b.Property(x => x.GeneralDirectorEmployee).HasColumnName("num_empleado_dir_gral").HasColumnType("integer");
        b.Property(x => x.Present).HasColumnName("presente");
        b.Property(x => x.SynchronizedAt).HasColumnName("sincronizado_en").HasColumnType("timestamp without time zone");
    }
}

public sealed class SiiWebApplicationConfiguration : IEntityTypeConfiguration<SiiWebApplication>
{
    public void Configure(EntityTypeBuilder<SiiWebApplication> b)
    {
        b.ToTable("sii_aplicaciones_web");
        b.HasKey(x => x.Id).HasName("sii_aplicaciones_web_pkey");
        b.Property(x => x.Id).HasColumnName("id_aplicacion_web").HasColumnType("integer").ValueGeneratedNever();
        b.Property(x => x.Name).HasColumnName("nombre").HasMaxLength(200).IsRequired();
        b.Property(x => x.Definition).HasColumnName("definicion").HasMaxLength(8000);
        b.Property(x => x.EmployeeResponsible).HasColumnName("emp_responsable").HasColumnType("integer");
        b.Property(x => x.ProgrammerResponsible).HasColumnName("prog_responsable").HasColumnType("integer");
        b.Property(x => x.Url).HasColumnName("url").HasMaxLength(500);
        b.Property(x => x.Active).HasColumnName("activa").HasColumnType("boolean");
        b.Property(x => x.GeneralDirectorEmployee).HasColumnName("num_empleado_dir_gral").HasColumnType("integer");
        b.Property(x => x.ParentApplicationId).HasColumnName("id_aplicacion_web_pertenece").HasColumnType("integer");
        b.Property(x => x.Present).HasColumnName("presente");
        b.Property(x => x.SynchronizedAt).HasColumnName("sincronizado_en").HasColumnType("timestamp without time zone");
    }
}
