using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> b)
    {
        b.ToTable("activity_logs");
        b.HasKey(x => x.Id).HasName("activity_logs_pkey");
        b.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint").UseSerialColumn();
        b.Property(x => x.UserEmail).HasColumnName("user_email").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.UserName).HasColumnName("user_name").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.RoleId).HasColumnName("role_id").HasColumnType("bigint");
        b.Property(x => x.Ur).HasColumnName("ur").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.Ur2).HasColumnName("ur2").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.Entity).HasColumnName("entity").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.RecordId).HasColumnName("id_registro").HasColumnType("bigint");
        b.Property(x => x.Action).HasColumnName("action").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.Meta).HasColumnName("meta").HasColumnType("json");
        b.Property(x => x.Ip).HasColumnName("ip").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.UserAgent).HasColumnName("user_agent").HasColumnType("character varying(255)").HasMaxLength(255);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone");

    }
}
