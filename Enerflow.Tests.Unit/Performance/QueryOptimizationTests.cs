using System.Diagnostics;
using System.Text.Json;
using Enerflow.Domain.Entities;
using Enerflow.Domain.Enums;
using Enerflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit;
using Xunit.Abstractions;

namespace Enerflow.Tests.Unit.Performance;

public class TestDbContext : EnerflowDbContext
{
    public TestDbContext(DbContextOptions<EnerflowDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call base to set up entities
        base.OnModelCreating(modelBuilder);

        // Add converters for InMemory provider which doesn't support jsonb natively
        var jsonConverter = new ValueConverter<JsonDocument?, string>(
            v => v != null ? v.RootElement.GetRawText() : null,
            v => v != null ? JsonDocument.Parse(v, default) : null);

        modelBuilder.Entity<Compound>().Property(e => e.ConstantProperties).HasConversion(jsonConverter);
        modelBuilder.Entity<UnitOperation>().Property(e => e.ConfigParams).HasConversion(jsonConverter);
        modelBuilder.Entity<Simulation>().Property(e => e.ResultJson).HasConversion(jsonConverter);

        var dictConverter = new ValueConverter<Dictionary<string, double>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, double>());

        modelBuilder.Entity<MaterialStream>().Property(e => e.MolarCompositions).HasConversion(dictConverter);
    }
}

public class QueryOptimizationTests
{
    private readonly ITestOutputHelper _output;

    public QueryOptimizationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private TestDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<EnerflowDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task Benchmark_GetSimulation()
    {
        var dbName = Guid.NewGuid().ToString();
        var simulationId = Guid.NewGuid();

        // 1. Seed Data
        using (var context = CreateContext(dbName))
        {
            var simulation = new Simulation
            {
                Id = simulationId,
                Name = "Performance Test Simulation",
                ThermoPackage = "Peng-Robinson",
                FlashAlgorithm = "Nested Loops",
                SystemOfUnits = "SI",
                Status = SimulationStatus.Created,
                ResultJson = JsonDocument.Parse("{}")
            };

            // Create a large JSON blob (~10KB)
            var largeData = Enumerable.Range(0, 1000).ToDictionary(i => $"key_{i}", i => (object)$"value_{i}");
            var jsonContent = JsonSerializer.SerializeToDocument(largeData);

            // Add 100 Compounds with large properties
            for (int i = 0; i < 100; i++)
            {
                simulation.Compounds.Add(new Compound
                {
                    SimulationId = simulationId,
                    Name = $"Compound {i}",
                    ConstantProperties = jsonContent
                });
            }

            // Add some streams and units
             for (int i = 0; i < 50; i++)
            {
                simulation.MaterialStreams.Add(new MaterialStream
                {
                    SimulationId = simulationId,
                    Name = $"Stream {i}",
                    MolarCompositions = new Dictionary<string, double> { { "Water", 1.0 } }
                });
            }

            context.Simulations.Add(simulation);
            await context.SaveChangesAsync();
        }

        // 2. Measure Original Query
        long originalTimeMs = 0;
        using (var context = CreateContext(dbName))
        {
            // Warmup
            await context.Simulations.FirstOrDefaultAsync(s => s.Id == Guid.Empty);

            var sw = Stopwatch.StartNew();

            var simulation = await context.Simulations
                .Include(s => s.Compounds)
                .Include(s => s.MaterialStreams)
                .Include(s => s.EnergyStreams)
                .Include(s => s.UnitOperations)
                .FirstOrDefaultAsync(s => s.Id == simulationId);

            sw.Stop();
            originalTimeMs = sw.ElapsedMilliseconds;

            Assert.NotNull(simulation);
            Assert.Equal(100, simulation.Compounds.Count);
            // Verify large data is loaded
            Assert.NotNull(simulation.Compounds.First().ConstantProperties);
        }

        // 3. Measure Optimized Query
        long optimizedTimeMs = 0;
        using (var context = CreateContext(dbName))
        {
             // Warmup
            await context.Simulations.FirstOrDefaultAsync(s => s.Id == Guid.Empty);

            var sw = Stopwatch.StartNew();

            var simulation = await context.Simulations
                .Where(s => s.Id == simulationId)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.ThermoPackage,
                    s.FlashAlgorithm,
                    s.SystemOfUnits,
                    s.Status,
                    s.CreatedAt,
                    s.UpdatedAt,
                    Compounds = s.Compounds.Select(c => new { c.Id, c.Name }).ToList(),
                    MaterialStreams = s.MaterialStreams.Select(ms => new {
                        ms.Id,
                        ms.Name,
                        ms.Temperature,
                        ms.Pressure,
                        ms.MassFlow,
                        ms.MolarCompositions
                    }).ToList(),
                    EnergyStreams = s.EnergyStreams.Select(es => new { es.Id, es.Name, es.EnergyFlow }).ToList(),
                    UnitOperations = s.UnitOperations.Select(u => new {
                        u.Id,
                        u.Name,
                        u.Type,
                        u.InputStreamIds,
                        u.OutputStreamIds
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            sw.Stop();
            optimizedTimeMs = sw.ElapsedMilliseconds;

            Assert.NotNull(simulation);
            Assert.Equal(100, simulation.Compounds.Count);
            Assert.Equal("Performance Test Simulation", simulation.Name);
        }

        _output.WriteLine($"Original Query Time: {originalTimeMs}ms");
        _output.WriteLine($"Optimized Query Time: {optimizedTimeMs}ms");

        // Assert improvement (or at least valid execution)
        // Note: Asserting time is flaky in CI/Agents, so we primarily verify code correctness here.
        // But we expect optimized < original usually.
    }
}
