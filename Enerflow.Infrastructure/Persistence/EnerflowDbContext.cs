using Enerflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Enerflow.Infrastructure.Persistence;

public class EnerflowDbContext : DbContext
{
    public EnerflowDbContext(DbContextOptions<EnerflowDbContext> options) : base(options)
    {
    }

    public DbSet<Simulation> Simulations { get; set; }
    public DbSet<Compound> Compounds { get; set; }
    public DbSet<MaterialStream> MaterialStreams { get; set; }
    public DbSet<EnergyStream> EnergyStreams { get; set; }
    public DbSet<UnitOperation> UnitOperations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Simulation (Aggregate Root)
        modelBuilder.Entity<Simulation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.ThermoPackage).IsRequired();
            entity.Property(e => e.SystemOfUnits).IsRequired();
            
            // Cascade delete behavior
            entity.HasMany<Compound>().WithOne().HasForeignKey(c => c.SimulationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany<MaterialStream>().WithOne().HasForeignKey(s => s.SimulationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany<EnergyStream>().WithOne().HasForeignKey(s => s.SimulationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany<UnitOperation>().WithOne().HasForeignKey(u => u.SimulationId).OnDelete(DeleteBehavior.Cascade);
        });

        // Compound
        modelBuilder.Entity<Compound>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            // Map ConstantProperties to JSONB
            entity.Property(e => e.ConstantProperties).HasColumnType("jsonb");
        });

        // MaterialStream
        modelBuilder.Entity<MaterialStream>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            
            // Map MolarCompositions to JSONB
            entity.Property(e => e.MolarCompositions).HasColumnType("jsonb");
        });

        // EnergyStream
        modelBuilder.Entity<EnergyStream>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
        });

        // UnitOperation
        modelBuilder.Entity<UnitOperation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Type).IsRequired(); // Kept as string for now to match Entity definition

            // Npgsql maps List<Guid> to uuid[] automatically

            // Map ConfigParams to JSONB
            entity.Property(e => e.ConfigParams).HasColumnType("jsonb");
        });
    }
}
