using Enerflow.Domain.Entities;
using Enerflow.Domain.Entities.Streams;
using Enerflow.Domain.Entities.UnitOperations;
using Enerflow.Domain.Enums;
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
    public DbSet<UnitOperationObject> UnitOperations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Simulation (Aggregate Root)
        modelBuilder.Entity<Simulation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasValueGenerator<SequentialGuidValueGenerator>();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.ThermoPackage).IsRequired();
            entity.Property(e => e.FlashAlgorithm).IsRequired();
            entity.Property(e => e.SystemOfUnits).IsRequired();

            // Status stored as string for readability
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .IsRequired();

            // Results stored as JSONB
            entity.Property(e => e.ResultJson).HasColumnType("jsonb");

            // Cascade delete behavior
            entity.HasMany(e => e.Compounds).WithOne().HasForeignKey(c => c.SimulationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.MaterialStreams).WithOne().HasForeignKey(s => s.SimulationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.EnergyStreams).WithOne().HasForeignKey(s => s.SimulationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.UnitOperations).WithOne().HasForeignKey(u => u.SimulationId).OnDelete(DeleteBehavior.Cascade);
        });

        // Compound
        modelBuilder.Entity<Compound>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasValueGenerator<SequentialGuidValueGenerator>();
            entity.Property(e => e.Name).IsRequired();
            // Map ConstantProperties to JSONB
            entity.Property(e => e.ConstantProperties).HasColumnType("jsonb");
        });

        // MaterialStream
        modelBuilder.Entity<MaterialStream>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasValueGenerator<SequentialGuidValueGenerator>();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.SimulationId).IsRequired();
            
            // Map Position as Complex Property (Value Type)
            entity.ComplexProperty(e => e.Position);

            // Map Composition to JSONB
            entity.Property(e => e.Composition).HasColumnType("jsonb");
            
            // Map Phase enum
            entity.Property(e => e.Phase).HasConversion<string>();
        });

        // EnergyStream
        modelBuilder.Entity<EnergyStream>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasValueGenerator<SequentialGuidValueGenerator>();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.SimulationId).IsRequired();
            
            // Map Position as Complex Property
            entity.ComplexProperty(e => e.Position);
        });

        // UnitOperation
        modelBuilder.Entity<UnitOperationObject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasValueGenerator<SequentialGuidValueGenerator>();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.SimulationId).IsRequired();
            entity.Property(e => e.ConfigParams).HasColumnType("jsonb");
            
            // Map Position as Complex Property
            entity.ComplexProperty(e => e.Position);

            // Discriminator for Inheritance (Heater, Cooler, etc.)
            entity.HasDiscriminator<string>("UnitType")
                .HasValue<HeaterObject>(nameof(UnitOperationType.Heater))
                .HasValue<CoolerObject>(nameof(UnitOperationType.Cooler))
                .HasValue<RecycleObject>(nameof(UnitOperationType.Recycle));
                
            // Ignore the abstract Type property as it's computed from class
            entity.Ignore(e => e.Type);

            // Npgsql maps List<Guid> to uuid[] automatically
        });
        
        // Heater
        modelBuilder.Entity<HeaterObject>(entity =>
        {
            entity.Property(e => e.CalcMode).HasConversion<string>();
        });

        // Cooler
        modelBuilder.Entity<CoolerObject>(entity =>
        {
            entity.Property(e => e.CalcMode).HasConversion<string>();
        });

        // Recycle
        modelBuilder.Entity<RecycleObject>(entity =>
        {
            entity.Property(e => e.Acceleration).HasConversion<string>();
        });
    }
}
