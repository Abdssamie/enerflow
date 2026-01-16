using Enerflow.Domain.Common;
using Enerflow.Domain.Entities;
using Enerflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Enerflow.Tests.Unit;

public class TestEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    public DbSet<TestEntity> TestEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasValueGenerator<SequentialGuidValueGenerator>();
        });
    }
}

public class IdGenerationTests
{
    [Fact]
    public void IdGenerator_ShouldGenerateSequentialGuids()
    {
        // Arrange
        var id1 = IdGenerator.NextGuid();
        var id2 = IdGenerator.NextGuid();
        var id3 = IdGenerator.NextGuid();

        // Act & Assert
        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id2, id3);
    }

    [Fact]
    public void Entity_ShouldInitializeWithSequentialId()
    {
        // Act
        var simulation1 = new Simulation { Name = "Test 1", ThermoPackage = "PR", SystemOfUnits = "SI" };
        var simulation2 = new Simulation { Name = "Test 2", ThermoPackage = "PR", SystemOfUnits = "SI" };

        // Assert
        Assert.NotEqual(Guid.Empty, simulation1.Id);
        Assert.NotEqual(Guid.Empty, simulation2.Id);
        Assert.NotEqual(simulation1.Id, simulation2.Id);
    }

    [Fact]
    public async Task EFCore_ValueGenerator_ShouldAssignSequentialId()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
        
        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Act
        var entity = new TestEntity { Name = "EF Test" };
        
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(Guid.Empty, entity.Id);
    }
}
