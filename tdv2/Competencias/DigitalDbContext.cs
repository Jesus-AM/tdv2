using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Tdv2.Domain;

namespace Tdv2.Competencias;

// Referencias institucionales textuales; ninguna FK atraviesa bases de datos.
public sealed class DigitalSurvey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Employee { get; set; } = "";
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string? UnitId { get; set; }
    public string InstitutionJson { get; set; } = "{}";
    public string ProfileJson { get; set; } = "{}";
    public string AnswersJson { get; set; } = "{}";
    public string InstrumentVersion { get; set; } = "";
    public string InstrumentJson { get; set; } = "{}";
    public string? ReportJson { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
public sealed class DigitalConsent
{
    public string Employee { get; set; } = "";
    public string NoticeVersion { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTimeOffset AcceptedAt { get; set; }
}
public sealed class DigitalTarget
{
    public string UnitId { get; set; } = "";
    public int Value { get; set; }
    public int Version { get; set; } = 1;
    public DateTimeOffset UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = "";
}
public sealed class DigitalControl
{
    public int Id { get; set; } = 1;
    public Guid Generation { get; set; }
    public long Revision { get; set; }
}
public sealed class DigitalResetOperation
{
    public Guid Id { get; set; }
    public string Actor { get; set; } = "";
    public string Employee { get; set; } = "";
    public Guid Generation { get; set; }
    public long Revision { get; set; }
    public bool IncludeTargets { get; set; }
    public string SummaryJson { get; set; } = "{}";
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? GenerationAfter { get; set; }
}

public sealed class DigitalDbContext(DbContextOptions<DigitalDbContext> options) : DbContext(options)
{
    public DbSet<DigitalSurvey> Surveys => Set<DigitalSurvey>();
    public DbSet<DigitalConsent> Consents => Set<DigitalConsent>();
    public DbSet<DigitalTarget> Targets => Set<DigitalTarget>();
    public DbSet<DigitalControl> Control => Set<DigitalControl>();
    public DbSet<DigitalResetOperation> ResetOperations => Set<DigitalResetOperation>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        var s = model.Entity<DigitalSurvey>();
        s.ToTable("cd_surveys", t => t.HasCheckConstraint("cd_final_consistent", "(\"CompletedAt\" IS NULL) = (\"ReportJson\" IS NULL)"));
        s.HasKey(x => x.Id); s.HasIndex(x => x.Employee).IsUnique(); s.HasIndex(x => x.UnitId);
        s.Property(x => x.Employee).HasMaxLength(32); s.Property(x => x.Email).HasMaxLength(254);
        s.Property(x => x.Name).HasMaxLength(500); s.Property(x => x.UnitId).HasMaxLength(32);
        s.Property(x => x.InstrumentVersion).HasMaxLength(80);
        foreach (var key in new[] { nameof(DigitalSurvey.InstitutionJson), nameof(DigitalSurvey.ProfileJson), nameof(DigitalSurvey.AnswersJson), nameof(DigitalSurvey.InstrumentJson), nameof(DigitalSurvey.ReportJson) }) s.Property<string>(key).HasColumnType("jsonb");
        s.Property(x => x.Version).IsConcurrencyToken();
        var c = model.Entity<DigitalConsent>(); c.ToTable("cd_consents"); c.HasKey(x => new { x.Employee, x.NoticeVersion });
        c.Property(x => x.Employee).HasMaxLength(32); c.Property(x => x.NoticeVersion).HasMaxLength(80); c.Property(x => x.Email).HasMaxLength(254);
        var t = model.Entity<DigitalTarget>(); t.ToTable("cd_targets", b => b.HasCheckConstraint("cd_target_nonnegative", "\"Value\" >= 0"));
        t.HasKey(x => x.UnitId); t.Property(x => x.UnitId).HasMaxLength(32); t.Property(x => x.Version).IsConcurrencyToken();
        t.Property(x => x.UpdatedBy).HasMaxLength(254);
        var control = model.Entity<DigitalControl>(); control.ToTable("cd_control", b => b.HasCheckConstraint("cd_control_singleton", "\"Id\" = 1"));
        control.HasKey(x => x.Id); control.Property(x => x.Id).ValueGeneratedNever();
        var reset = model.Entity<DigitalResetOperation>(); reset.ToTable("cd_reset_operations"); reset.HasKey(x => x.Id);
        reset.Property(x => x.Id).ValueGeneratedNever(); reset.Property(x => x.Actor).HasMaxLength(254); reset.Property(x => x.Employee).HasMaxLength(32);
        reset.Property(x => x.SummaryJson).HasColumnType("jsonb");
    }
    public static void Configure(DbContextOptionsBuilder options, IConfiguration config)
    {
        var raw = config.GetConnectionString("CompetenciasDigitales");
        if (string.IsNullOrWhiteSpace(raw)) throw new DomainProblem(503, "Competencias digitales aún no tiene configurada su base de datos.");
        var own = new NpgsqlConnectionStringBuilder(raw) { IncludeErrorDetail = false, LogParameters = false };
        if (config.GetConnectionString("Tdv2") is { Length: > 0 } existing)
        {
            var tdv2 = new NpgsqlConnectionStringBuilder(existing);
            if (string.Equals(own.Host, tdv2.Host, StringComparison.OrdinalIgnoreCase) && own.Port == tdv2.Port && own.Database == tdv2.Database)
                throw new DomainProblem(503, "Competencias digitales requiere una base de datos independiente.");
        }
        options.UseNpgsql(own.ConnectionString, p => p.MigrationsHistoryTable("__CDMigrationsHistory"));
    }
}
public sealed class DigitalDbContextFactory : IDesignTimeDbContextFactory<DigitalDbContext>
{
    public DigitalDbContext CreateDbContext(string[] args)
    {
        var directory = Directory.GetCurrentDirectory();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args,
            ContentRootPath = File.Exists(Path.Combine(directory, "tdv2.csproj")) ? directory : Path.Combine(directory, "tdv2"),
            ApplicationName = typeof(DigitalDbContext).Assembly.GetName().Name });
        var options = new DbContextOptionsBuilder<DigitalDbContext>();
        DigitalDbContext.Configure(options, builder.Configuration); options.UseLoggerFactory(NullLoggerFactory.Instance);
        return new(options.Options);
    }
}
