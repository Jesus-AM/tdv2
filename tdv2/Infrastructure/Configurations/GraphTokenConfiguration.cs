using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class GraphTokenConfiguration : IEntityTypeConfiguration<GraphToken>
{
    public void Configure(EntityTypeBuilder<GraphToken> b)
    {
        b.ToTable("ms_graph_tokens");
        b.HasKey(x => x.Id).HasName("ms_graph_tokens_pkey");
        b.Property(x => x.Id).HasColumnName("id").HasColumnType("integer").UseSerialColumn();
        b.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("integer");
        b.Property(x => x.Email).HasColumnName("email").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.AccessToken).HasColumnName("access_token").HasColumnType("text").IsRequired();
        b.Property(x => x.RefreshToken).HasColumnName("refresh_token").HasColumnType("text");
        b.Property(x => x.Expires).HasColumnName("expires").HasColumnType("character varying(255)").HasMaxLength(255).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone");
        // El esquema histórico usa integer, mientras users.id es bigint; no inventar una FK ni truncar IDs.
        b.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("ms_graph_tokens_user_id_key");
    }
}
