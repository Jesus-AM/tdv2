using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class SessionTicketConfiguration : IEntityTypeConfiguration<SessionTicket>
{
    public void Configure(EntityTypeBuilder<SessionTicket> b)
    {
        b.ToTable("tdv2_sessions");
        b.HasKey(x => x.Id).HasName("tdv2_sessions_pkey");
        b.Property(x => x.Id).HasColumnName("id_hash").HasColumnType("character(64)").HasMaxLength(64).IsFixedLength().IsRequired();
        b.Property(x => x.Ticket).HasColumnName("ticket").HasColumnType("text").IsRequired();
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
        b.HasIndex(x => x.ExpiresAt).HasDatabaseName("tdv2_sessions_expiry");
    }
}
