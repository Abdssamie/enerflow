using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Enerflow.Domain.Common;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Entities;
using Enerflow.Domain.Enums;
using Enerflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit;
using Xunit.Abstractions;

namespace Enerflow.Tests.Unit.Performance;

public class AddStreamBenchmark
{
    private readonly ITestOutputHelper _output;

    public AddStreamBenchmark(ITestOutputHelper output)
    {
        _output = output;
    }

    private EnerflowDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<EnerflowDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new TestEnerflowDbContext(options);
        return context;
    }

    // Subclass to handle JSON properties in InMemory
    public class TestEnerflowDbContext : EnerflowDbContext
    {
        public TestEnerflowDbContext(DbContextOptions<EnerflowDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add value converters for JSON properties
            var jsonDocumentConverter = new ValueConverter<JsonDocument?, string>(
                v => v != null ? v.RootElement.GetRawText() : "null",
                v => v != "null" ? JsonDocument.Parse(v, default) : null);

            var dictionaryConverter = new ValueConverter<Dictionary<string, double>?, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v != null ? JsonSerializer.Deserialize<Dictionary<string, double>>(v, (JsonSerializerOptions?)null) : null);

            modelBuilder.Entity<Simulation>().Property(e => e.ResultJson).HasConversion(jsonDocumentConverter);
            modelBuilder.Entity<Compound>().Property(e => e.ConstantProperties).HasConversion(jsonDocumentConverter);
            modelBuilder.Entity<MaterialStream>().Property(e => e.MolarCompositions).HasConversion(dictionaryConverter);
            modelBuilder.Entity<UnitOperation>().Property(e => e.ConfigParams).HasConversion(jsonDocumentConverter);
        }
    }

    [Fact]
    public async Task Benchmark_AddStream_Performance()
    {
        int iterations = 500;
        var simulationId = Guid.NewGuid();

        // 1. Measure Original
        long originalMs = 0;
        using (var context = CreateContext("BenchmarkDb_Original"))
        {
             var sim = new Simulation
             {
                 Id = simulationId,
                 Name = "Sim",
                 ThermoPackage = "A",
                 FlashAlgorithm = "B",
                 SystemOfUnits = "C",
                 ResultJson = JsonDocument.Parse("{\"data\":\"heavy payload to simulate cost\"}")
             };
             context.Simulations.Add(sim);
             await context.SaveChangesAsync();

             var sw = Stopwatch.StartNew();
             for (int i = 0; i < iterations; i++)
             {
                 context.ChangeTracker.Clear();
                 await AddStream_Original(context, simulationId, i);
             }
             sw.Stop();
             originalMs = sw.ElapsedMilliseconds;
        }

        // 2. Measure Optimized
        long optimizedMs = 0;
        using (var context = CreateContext("BenchmarkDb_Optimized"))
        {
             var sim = new Simulation
             {
                 Id = simulationId,
                 Name = "Sim",
                 ThermoPackage = "A",
                 FlashAlgorithm = "B",
                 SystemOfUnits = "C",
                 ResultJson = JsonDocument.Parse("{\"data\":\"heavy payload to simulate cost\"}")
             };
             context.Simulations.Add(sim);
             await context.SaveChangesAsync();

             var sw = Stopwatch.StartNew();
             for (int i = 0; i < iterations; i++)
             {
                 context.ChangeTracker.Clear();
                 await AddStream_Optimized(context, simulationId, i);
             }
             sw.Stop();
             optimizedMs = sw.ElapsedMilliseconds;
        }

        _output.WriteLine($"Iterations: {iterations}");
        _output.WriteLine($"Original Total Time: {originalMs} ms");
        _output.WriteLine($"Optimized Total Time: {optimizedMs} ms");

        if (originalMs > 0)
        {
            var improvement = (double)(originalMs - optimizedMs) / originalMs * 100;
            _output.WriteLine($"Improvement: {improvement:F2}%");
        }

        // Assert functional correctness is assumed if no exception thrown.
        // We can verify the last update actually worked.
        using (var context = CreateContext("BenchmarkDb_Optimized"))
        {
            var sim = await context.Simulations.FindAsync(simulationId);
            Assert.NotNull(sim);
            // UpdatedAt should be recent
            Assert.True((DateTime.UtcNow - sim.UpdatedAt).TotalSeconds < 10);

            var streams = await context.MaterialStreams.CountAsync(s => s.SimulationId == simulationId);
            Assert.Equal(iterations, streams);
        }

    }

    private async Task AddStream_Original(EnerflowDbContext context, Guid simulationId, int i)
    {
        var simulation = await context.Simulations.FindAsync(simulationId);
        if (simulation == null) throw new Exception("Not found");

        var stream = new MaterialStream
        {
            Id = Guid.NewGuid(),
            SimulationId = simulationId,
            Name = $"Stream {i}",
            Temperature = 300,
            Pressure = 101325,
            MassFlow = 1.0,
            MolarCompositions = new Dictionary<string, double>()
        };

        context.MaterialStreams.Add(stream);
        simulation.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private async Task AddStream_Optimized(EnerflowDbContext context, Guid simulationId, int i)
    {
        var exists = await context.Simulations.AnyAsync(s => s.Id == simulationId);
        if (!exists) throw new Exception("Not found");

        var stream = new MaterialStream
        {
            Id = Guid.NewGuid(),
            SimulationId = simulationId,
            Name = $"Stream {i}",
            Temperature = 300,
            Pressure = 101325,
            MassFlow = 1.0,
            MolarCompositions = new Dictionary<string, double>()
        };

        context.MaterialStreams.Add(stream);

        // Update timestamp via stub
        var simulationStub = new Simulation
        {
            Id = simulationId,
            Name = null!,
            ThermoPackage = null!,
            FlashAlgorithm = null!,
            SystemOfUnits = null!
        };

        context.Attach(simulationStub);
        context.Entry(simulationStub).Property(p => p.UpdatedAt).CurrentValue = DateTime.UtcNow;
        context.Entry(simulationStub).Property(p => p.UpdatedAt).IsModified = true;

        await context.SaveChangesAsync();
    }
}
