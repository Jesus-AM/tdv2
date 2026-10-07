using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;
namespace Tdv2.Infrastructure.Configurations;

public sealed class ProcessSettingsConfiguration : IEntityTypeConfiguration<ProcessSettings>
{
    public void Configure(EntityTypeBuilder<ProcessSettings> b)
    {
        b.ToTable("configuracion_procesos", t => t.HasCheckConstraint("configuracion_procesos_unica_ck", "id = 1 AND version > 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        b.Property(x => x.Levels).HasColumnName("niveles").HasColumnType("integer[]");
        b.Property(x => x.ExcludedTypes).HasColumnName("tipos_excluidos").HasColumnType("text[]");
        b.Property(x => x.UpdatedAt).HasColumnName("actualizado_en");
        b.HasData(new ProcessSettings());
    }
}
