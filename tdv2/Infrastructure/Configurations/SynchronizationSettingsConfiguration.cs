using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure.Configurations;

public sealed class SynchronizationSettingsConfiguration : IEntityTypeConfiguration<SynchronizationSettings>
{
    public void Configure(EntityTypeBuilder<SynchronizationSettings> b)
    {
        b.ToTable("sincronizacion_configuracion", t => t.HasCheckConstraint("sincronizacion_configuracion_id_check", "id = 1"));
        b.HasKey(x => x.Id).HasName("sincronizacion_configuracion_pkey");
        b.Property(x => x.Id).HasColumnName("id").HasColumnType("smallint");
        b.Property(x => x.Active).HasColumnName("activa").HasColumnType("boolean").HasDefaultValue(false);
        b.Property(x => x.IntervalMinutes).HasColumnName("intervalo_minutos").HasColumnType("integer").HasDefaultValue(60);
        b.Property(x => x.Time).HasColumnName("hora").HasColumnType("character varying(5)").HasMaxLength(5).IsRequired().HasDefaultValue("08:00");
        b.Property(x => x.TimeZone).HasColumnName("zona_horaria").HasColumnType("character varying(64)").HasMaxLength(64).IsRequired().HasDefaultValue("America/Ciudad_Juarez");
        b.Property(x => x.IncludeIlda).HasColumnName("incluir_ilda").HasColumnType("boolean").HasDefaultValue(false);
        b.Property(x => x.Version).HasColumnName("version").HasColumnType("integer").HasDefaultValue(1);
        b.Property(x => x.NextAt).HasColumnName("proxima_en").HasColumnType("timestamp without time zone");
        b.Property(x => x.ProcessorSeenAt).HasColumnName("procesador_visto_en").HasColumnType("timestamp without time zone");
        b.Property(x => x.ActiveRunId).HasColumnName("ejecucion_activa").HasColumnType("uuid");
        b.Property(x => x.Owner).HasColumnName("propietario").HasColumnType("uuid");
        b.Property(x => x.ReservedUntil).HasColumnName("reserva_hasta").HasColumnType("timestamp without time zone");
        b.Property(x => x.UpdatedBy).HasColumnName("actualizado_por").HasColumnType("character varying(254)").HasMaxLength(254);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone");
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasOne(x => x.ActiveRun).WithMany().HasForeignKey(x => x.ActiveRunId)
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("sincronizacion_configuracion_ejecucion_activa_fkey");
        // Índice de la relación; sólo existe una fila de configuración.
        b.HasIndex(x => x.ActiveRunId).HasDatabaseName("IX_sincronizacion_configuracion_ejecucion_activa");
    }
}
