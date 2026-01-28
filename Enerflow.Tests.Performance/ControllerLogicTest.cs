using Enerflow.API.Controllers;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Entities;
using Enerflow.Domain.Enums;
using Enerflow.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enerflow.Tests.Performance;

public class ControllerLogicTest
{
    public class TestEnerflowDbContext : EnerflowDbContext
    {
        public TestEnerflowDbContext(DbContextOptions<EnerflowDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ignore JSON properties for InMemory test to avoid "not mapped" errors
            modelBuilder.Entity<Compound>().Ignore(c => c.ConstantProperties);
            modelBuilder.Entity<UnitOperation>().Ignore(u => u.ConfigParams);
            modelBuilder.Entity<MaterialStream>().Ignore(s => s.MolarCompositions);
            modelBuilder.Entity<Simulation>().Ignore(s => s.ResultJson);
        }
    }

    [Fact]
    public async Task ImportSimulation_ShouldAddEntitiesCorrectly_UsingAddRange()
    {
        // 1. Setup InMemory DbContext
        var options = new DbContextOptionsBuilder<EnerflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB name per test
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var context = new TestEnerflowDbContext(options);
        var logger = NullLogger<SimulationsController>.Instance;
        var controller = new SimulationsController(context, logger);

        // 2. Create Import Data
        var importDto = new SimulationExportDto
        {
            Name = "Test Sim",
            ThermoPackage = "PengRobinson",
            FlashAlgorithm = "NestedLoops",
            SystemOfUnits = "SI",
            Compounds = new List<CompoundExportDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Water", ConstantProperties = null },
                new() { Id = Guid.NewGuid(), Name = "Ethanol", ConstantProperties = null }
            },
            MaterialStreams = new List<MaterialStreamExportDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Inlet", Temperature = 300, Pressure = 100000, MassFlow = 10 }
            },
            EnergyStreams = new List<EnergyStreamExportDto>(),
            UnitOperations = new List<UnitOperationExportDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Mixer",
                    Type = UnitOperationType.Mixer.ToString(),
                    InputStreamIds = new List<Guid>(),
                    OutputStreamIds = new List<Guid>()
                }
            }
        };

        // 3. Act
        var result = await controller.ImportSimulation(importDto);

        // 4. Assert
        context.Simulations.Count().Should().Be(1);
        context.Compounds.Count().Should().Be(2);
        context.MaterialStreams.Count().Should().Be(1);
        context.UnitOperations.Count().Should().Be(1);

        var sim = await context.Simulations.FirstAsync();
        sim.Name.Should().Be("Test Sim");

        var compounds = await context.Compounds.ToListAsync();
        compounds.Should().Contain(c => c.Name == "Water");
        compounds.Should().Contain(c => c.Name == "Ethanol");
    }
}
