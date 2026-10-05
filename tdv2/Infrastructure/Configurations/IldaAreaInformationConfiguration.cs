using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class IldaAreaInformationConfiguration : IEntityTypeConfiguration<IldaAreaInformation>
{
    public void Configure(EntityTypeBuilder<IldaAreaInformation> b)
    {
        b.ToTable("ilda_informacion_area");
        b.HasKey(x => x.Id).HasName("ilda_informacion_area_pkey");
        b.Property(x => x.Id).HasColumnName("id_origen").HasColumnType("character varying(40)").HasMaxLength(40).IsRequired();
        b.Property(x => x.Ur2).HasColumnName("ur2").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.Information).HasColumnName("informacion_generada").HasColumnType("text");
        b.Property(x => x.Data).HasColumnName("datos").HasColumnType("json").IsRequired();
        b.Property(x => x.Present).HasColumnName("presente").HasColumnType("boolean").HasDefaultValue(true);
        b.Property(x => x.SynchronizedAt).HasColumnName("sincronizado_en").HasColumnType("timestamp without time zone");
        b.HasIndex(x => x.Ur2).HasDatabaseName("ilda_informacion_area_ur2_index");
        b.HasIndex(x => x.Present).HasDatabaseName("ilda_informacion_area_presente_index");
    }
}
