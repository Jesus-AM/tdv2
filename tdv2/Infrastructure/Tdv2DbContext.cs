using Microsoft.EntityFrameworkCore;
using Tdv2.Domain.Entities;

namespace Tdv2.Infrastructure;

/// <summary>Únicamente el esquema local TDV2. Nexo, SII e ILDA no forman parte del modelo EF.</summary>
public sealed class Tdv2DbContext(DbContextOptions<Tdv2DbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<GraphToken> GraphTokens => Set<GraphToken>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<ResponsibleUnit> ResponsibleUnits => Set<ResponsibleUnit>();
    public DbSet<UnitForm> UnitForms => Set<UnitForm>();
    public DbSet<FormBlock> FormBlocks => Set<FormBlock>();
    public DbSet<FormMutation> FormMutations => Set<FormMutation>();
    public DbSet<FormPosition> FormPositions => Set<FormPosition>();
    public DbSet<UnitCollaboration> UnitCollaborations => Set<UnitCollaboration>();
    public DbSet<SessionTicket> SessionTickets => Set<SessionTicket>();
    public DbSet<OAuthAttempt> OAuthAttempts => Set<OAuthAttempt>();
    public DbSet<AccessContext> AccessContexts => Set<AccessContext>();
    public DbSet<InstitutionalSynchronization> InstitutionalSynchronizations => Set<InstitutionalSynchronization>();
    public DbSet<CatalogSynchronization> CatalogSynchronizations => Set<CatalogSynchronization>();
    public DbSet<IldaAreaInformation> IldaAreaInformations => Set<IldaAreaInformation>();
    public DbSet<SynchronizationRun> SynchronizationRuns => Set<SynchronizationRun>();
    public DbSet<SynchronizationSettings> SynchronizationSettings => Set<SynchronizationSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Tdv2DbContext).Assembly);
    }
}
