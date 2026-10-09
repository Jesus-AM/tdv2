using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;
namespace Tdv2.Infrastructure.Configurations;

public sealed class FormExclusionConfiguration : IEntityTypeConfiguration<FormExclusion>
{
    public void Configure(EntityTypeBuilder<FormExclusion> b)
    {
        b.ToTable("formato_exclusiones_ilda");
        b.HasKey(x => new { x.UnitId, x.Year, x.RowId });
        b.Property(x => x.UnitId).HasColumnName("id_ur").HasMaxLength(32);
        b.Property(x => x.Year).HasColumnName("ejercicio");
        b.Property(x => x.RowId).HasColumnName("registro").HasMaxLength(64);
        b.Property(x => x.CreatedAt).HasColumnName("creado_en");
        b.Property(x => x.Actor).HasColumnName("actor").HasMaxLength(254);
        b.Property(x => x.Effective).HasColumnName("efectivo").HasMaxLength(254);
        b.HasOne<UnitForm>().WithMany().HasForeignKey(x => x.UnitId).HasPrincipalKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
