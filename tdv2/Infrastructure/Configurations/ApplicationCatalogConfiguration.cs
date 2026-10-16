using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;
namespace Tdv2.Infrastructure.Configurations;

public sealed class ApplicationCatalogEntryConfiguration : IEntityTypeConfiguration<ApplicationCatalogEntry>
{
    public void Configure(EntityTypeBuilder<ApplicationCatalogEntry> b)
    {
        b.ToTable("aplicaciones_catalogo");
        b.HasKey(x => new { x.Category, x.Id });
        b.Property(x => x.Category).HasColumnName("categoria").HasMaxLength(12);
        b.Property(x => x.Id).HasColumnName("id").HasMaxLength(64).ValueGeneratedNever();
        b.Property(x => x.Name).HasColumnName("nombre").HasMaxLength(4000).IsRequired();
        b.Property(x => x.ListKey).HasColumnName("clave_lista").HasMaxLength(64);
        b.Property(x => x.Available).HasColumnName("disponible");
        b.HasIndex(x => new { x.Category, x.ListKey }).IsUnique();
    }
}
public sealed class ApplicationCatalogImportConfiguration : IEntityTypeConfiguration<ApplicationCatalogImport>
{
    public void Configure(EntityTypeBuilder<ApplicationCatalogImport> b)
    {
        b.ToTable("aplicaciones_importaciones"); b.HasKey(x => x.Category);
        b.Property(x => x.Category).HasColumnName("categoria").HasMaxLength(12);
        b.Property(x => x.Edition).HasColumnName("edicion").HasMaxLength(64).IsRequired();
        b.Property(x => x.Hash).HasColumnName("sha256").HasMaxLength(64).IsRequired();
        b.Property(x => x.ImportedAt).HasColumnName("importado_en").HasColumnType("timestamp without time zone");
        b.Property(x => x.Result).HasColumnName("resultado").HasColumnType("json").IsRequired();
    }
}
