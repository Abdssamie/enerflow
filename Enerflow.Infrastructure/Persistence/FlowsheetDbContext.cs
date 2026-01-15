using Enerflow.Domain.Entities;
using Enerflow.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Enerflow.Infrastructure.Persistence;

public class FlowsheetDbContext : DbContext
{
    public FlowsheetDbContext(DbContextOptions<FlowsheetDbContext> options) : base(options)
    {
    }

    public DbSet<Flowsheet> Flowsheets { get; set; }
    public DbSet<UnitOperation> UnitOperations { get; set; }
    public DbSet<MaterialStream> MaterialStreams { get; set; }
    public DbSet<EnergyStream> EnergyStreams { get; set; }
    public DbSet<SimulationRun> SimulationRuns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlowsheetDbContext).Assembly);
    }
}
