# Testing Patterns & Conventions

**Feature**: Backend Test Coverage & MVP Readiness Assessment  
**Date**: 2025-01-30  
**Phase**: 1 - Design

## Overview

This document defines testing patterns, conventions, and best practices for implementing comprehensive backend test coverage in the Enerflow project. All tests must follow these patterns to ensure consistency, maintainability, and reliability.

## Test Organization

### Directory Structure

```
Enerflow.Tests.Unit/
├── API/
│   ├── Controllers/
│   │   ├── SimulationsControllerTests.cs
│   │   ├── SimulationJobsControllerTests.cs
│   │   └── CatalogsControllerTests.cs
│   └── ...
├── Domain/
│   ├── Entities/
│   └── Extensions/
├── Services/
│   ├── CatalogServiceTests.cs
│   └── SimulationServiceTests.cs
└── ...

Enerflow.Tests.Integration/
├── Worker/
│   ├── Consumers/
│   │   └── SimulationJobConsumerTests.cs
│   ├── Solvers/
│   │   ├── DWSIMSolverTests.cs
│   │   └── ResultCollectorTests.cs
│   └── Mappers/
├── Infrastructure/
│   └── Persistence/
│       └── EnerflowDbContextTests.cs
└── ...

Enerflow.Tests.Functional/
├── Scenarios/
│   ├── SimulationFlowTests.cs
│   ├── JobProcessingTests.cs
│   └── EndToEndTests.cs
├── Fixtures/
│   ├── IntegrationTestWebAppFactory.cs
│   └── TestContainerFixture.cs
└── ...

Enerflow.Tests.Performance/
├── Scenarios/
│   ├── SimulationSubmissionLoadTests.cs
│   └── ConcurrentUserTests.cs
└── ...
```

### Naming Conventions

**Test Classes**:
- Format: `{ClassUnderTest}Tests`
- Example: `SimulationsControllerTests`, `DWSIMSolverTests`
- One test class per production class

**Test Methods**:
- Format: `{MethodName}_{Scenario}_{ExpectedBehavior}`
- Example: `CreateSimulation_WithValidData_ReturnsCreatedResult`
- Example: `ProcessJob_WhenDatabaseUnavailable_ThrowsException`

**Test Fixtures**:
- Format: `{Purpose}Fixture` or `{Purpose}TestFixture`
- Example: `DatabaseFixture`, `TestContainerFixture`

## Unit Testing Patterns

### Arrange-Act-Assert (AAA) Pattern

**Standard Structure**:

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange: Set up test data and dependencies
    var mockService = new Mock<IService>();
    mockService.Setup(s => s.GetData()).Returns(expectedData);
    var controller = new MyController(mockService.Object);
    var request = new MyRequest { /* ... */ };

    // Act: Execute the method under test
    var result = controller.MethodName(request);

    // Assert: Verify the outcome
    Assert.NotNull(result);
    Assert.Equal(expectedValue, result.Value);
    mockService.Verify(s => s.GetData(), Times.Once);
}
```

### Mocking Strategy

**When to Mock**:
- External dependencies (databases, APIs, message queues)
- Services with complex logic not under test
- Time-dependent operations (use `ISystemClock` abstraction)
- File system operations

**When NOT to Mock**:
- Domain entities (use real instances)
- DTOs and value objects
- Simple data structures
- The class under test itself

**Mocking Framework**: Moq (already in use)

```csharp
// Good: Mock external dependency
var mockDbContext = new Mock<EnerflowDbContext>();
mockDbContext.Setup(db => db.Simulations).Returns(mockDbSet.Object);

// Good: Mock service interface
var mockCatalogService = new Mock<ICatalogService>();
mockCatalogService.Setup(s => s.GetCompounds()).Returns(compounds);

// Bad: Don't mock domain entities
// var mockSimulation = new Mock<Simulation>(); // NO!
var simulation = new Simulation { /* real instance */ }; // YES!
```

### Test Data Builders

**Pattern**: Use builder pattern for complex test data

```csharp
public class SimulationBuilder
{
    private string _name = "Test Simulation";
    private List<string> _compounds = new() { "Water", "Ethanol" };
    private PropertyPackageType _propertyPackage = PropertyPackageType.NRTL;

    public SimulationBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public SimulationBuilder WithCompounds(params string[] compounds)
    {
        _compounds = compounds.ToList();
        return this;
    }

    public Simulation Build()
    {
        return new Simulation
        {
            Name = _name,
            Compounds = _compounds.Select(c => new Compound { Name = c }).ToList(),
            PropertyPackage = _propertyPackage
        };
    }
}

// Usage in tests
[Fact]
public void Test_WithCustomSimulation()
{
    // Arrange
    var simulation = new SimulationBuilder()
        .WithName("Custom Test")
        .WithCompounds("Methane", "Ethane")
        .Build();

    // Act & Assert...
}
```

## Integration Testing Patterns

### Database Testing with Testcontainers

**Pattern**: Use Testcontainers for isolated database testing

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    public EnerflowDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("enerflow_test")
            .Build();

        await _container.StartAsync();

        var options = new DbContextOBuilder<EnerflowDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        DbContext = new EnerflowDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}

// Usage in tests
public class EnerflowDbContextTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public EnerflowDbContextTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateSimulation_SavesToDatabase()
    {
        // Arrange
        var simulation = new Simulation { Name = "Test" };

        // Act
        _fixture.DbContext.Simulations.Add(simulation);
        await _fixture.DbContext.SaveChangesAsync();

        // Assert
        var saved = await _fixture.DbContext.Simulations.FindAsync(simulation.Id);
        Assert.NotNull(saved);
        Assert.Equal("Test", saved.Name);
    }
}
```

### Worker/Consumer Testing

**Pattern**: Test consumers with in-memory test harness

```csharp
public class SimulationJobConsumerTests
{
    [Fact]
    public async Task Consume_WithValidJob_ProcessesSuccessfully()
    {
        // Arrange
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer<SimulationJobConsumer>();

        await harness.Start();

        try
        {
            var job = new SimulationJob { /* ... */ };

            // Act
            await harness.InputQueueSendEndpoint.Send(job);

            // Assert
            Assert.True(await harness.Consumed.Any<SimulationJob>());
            Assert.True(await consumerHarnsumed.Any<SimulationJob>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
```

## Functional Testing Patterns

### End-to-End Testing with WebApplicationFactory

**Pattern**: Use WebApplicationFactory for full integration tests

```csharp
public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private RabbitMqContainer? _rabbitMqContainer;

    public async Task InitializeAsync()
    {
        // Start containers
        _postgresContainer = new PostgreSqlBuilder().Build();
        await _postgresContainer.StartAsync();

        _rabbitMqContainer = new RabbitMqBuilder().Build();
        await _rabbitMqContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Override DbContext with Testcontainer connection
            var connectionString = _postgresContainer!.GetConnectionString();
            services.AddDbContext<EnerflowDbContext>(options =>
                options.UseNpgsql(connectionString));

            // OverriassTransit with Testcontainer RabbitMQ
            services.AddMassTransit(x =>
            {
                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(_rabbitMqContainer!.Hostname, 
                        _rabbitMqContainer.GetMappedPublicPort(5672), "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });
                });
            });
        });
    }

    public async Task DisposeAsync()
    {
        if (_postgresContainer != null)
            await _postgresContainer.DisposeAsync();
        if (_rabbitMqContainer != null)
            await _rabbitMqContainer.DisposeAsync();
    }
}

// Usage in tests
public class SimulationFlowTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly IntegrationTestWebAppFactory _factory;

    public SimulationFlowTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SubmitSimulation_ProcessesAndReturnsResults()
    {
        // Arrange
        var request = new CreateSimulationRequest { /* ... */ };

        // Act: Submit simulation
        var response = await _client.PostAsJsonAsync("/api/simulations", request);
        response.EnsureSuccessStatusCode();
        var simulation = await response.Content.ReadFromJsonAsync<Simulation>();

        // Wait for processing (poll status endpoint)
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Act: Get results
        var resultsResponse = await _client.GetAsync($"/api/simulations/{simulation!.Id}/results");
        resultsResponse.EnsureSuccessStatusCode();
        var results = await resultsResp.Content.ReadFromJsonAsync<SimulationResults>();

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results.Streams);
    }
}
```

## Performance Testing Patterns

### Load Testing with NBomber

**Pattern**: Define scenarios with clear performance targets

```csharp
public class SimulationSubmissionLoadTests
{
    [Fact]
    public void LoadTest_SimulationSubmission_MeetsP95Target()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
        var request = new CreateSimulationRequest { /* ... */ };

        var scenario = Scenario.Create("simulation_submission", async context =>
        {
            var httpRequest = Http.CreateRequest("POST", "/api/simulations")
                .WithHeader("Content-Type", "application/json")
                .WithJsonBody(request);

            var response = await Http.Send(httpClient, httpRequest);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
      n
        // Act
        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        var p95Latency = stats.ScenarioStats[0].Ok.Latency.Percent95;
        Assert.True(p95Latency < 500, $"p95 latency {p95Latency}ms exceeds 500ms target");
    }
}
```

## Test Data Management

### Test Data Isolation

**Pattern**: Each test should have isolated data

```csharp
// Good: Create fresh data per test
[Fact]
public async Task Test1()
{
    var simulation = new Simulation { Name = "Test1" };
    _dbContext.Simulations.Add(simulation);
    await _dbContext.SaveChangesAsync();
    // Test uses only this simulation
}

[Fact]
public async Task Test2()
{
    var simulation = new Simulation { Name = "Test2" };
    _dbContext.Simulations.Add(simulation);
    await _dbContext.SaveChangesAsync();
    // Test uses only this simulation
}

// Bad: Shared data between tests
private static Simulation _sharedSimulation = new(); // NO!
```

### Test Data Cleanup

**Pattern**: Use IAsyncLifetime or IDisposable for cleanup

```csharp
public class MyTests : IAsyncLifetime
{
    private EnerflowDbContext _dbContext = null!;

   ic async Task InitializeAsync()
    {
    
        _dbContext = CreateDbContext();
    }

    public async Task DisposeAsync()
    {
        // Cleanup
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task MyTest()
    {
        // Test code
    }
}
```

## Assertion Patterns

### Fluent Assertions (Optional)

**Pattern**: Use FluentAssertions for readable assertions

```csharp
// Standard xUnit assertions
Assert.NotNull(result);
Assert.Equal(expected, result.Value);
Assert.True(result.IsSuccess);

// FluentAssertions (more readable)
result.Should().NotBeNull();
result.Value.Should().Be(expnresult.IsSuccess.Should().BeTrue();

// Collection assertions
results.Should().HaveCount(3);
results.Should().Contain(x => x.Name == "Test");
results.Should().AllSatisfy(x => x.IsValid.Should().BeTrue());
```

### Exception Assertions

**Pattern**: Test exception scenarios explicitly

```csharp
[Fact]
public async Task ProcessJob_WhenDatabaseUnavailable_ThrowsException()
{
    // Arrange
    var mockDbContext = new Mock<EnerflowDbContext>();
    mockDbContext.Setup(db => db.SaveChangesAsync(default))
        .ThrowsAsync(new DbUpdateException());
    var consumer = new SimulationJobConsumer(mockDbContext.Object);

    // Act & Assert
    await Assert.ThrowsAsync<DbUpdateException>(() => 
        consumer.ProcessJob(new SimulationJob()));
}
```

## Test Categories and Traits

**Pattern**: Use traits to categorize tests

```csharp
[Fact]
[Trait("Category", "Unit")]
[Trait("Layer", "API")]
public void UnitTest_Example() { }

[Fact]
[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
public void IntegrationTest_Example() { }

[Fact]
[Traegory", "Functional")]
[Trait("Speed", "Slow")]
public void FunctionalTest_Example() { }

// Run specific categories
// dotnet test --filter "Category=Unit"
// dotnet test --filter "Layer=API"
```

## Async Testing Best Practices

**Pattern**: Always use async/await for async operations

```csharp
// Good: Proper async testing
[Fact]
public async Task AsyncMethod_ReturnsExpectedResult()
{
    var result = await service.GetDataAsync();
    Assert.NotNull(result);
}

// Bad: Blocking on async
[Fact]
public void AsyncMethod_ReturnsExpectedResult()
{
    var result = service.GetDataAsync().Result; // NO!
    Assert.NotNull(result)n// Bad: Not awaiting
[Fact]
public async Task AsyncMethod_ReturnsExpectedResult()
{
    service.GetDataAsync(); // NO! Missing await
    // Test completes before async operation finishes
}
```

## Test Execution Order

**Pattern**: Tests should be independent and order-agnostic

```csharp
// Good: Each test is independent
[Fact]
public void Test1() { /* Independent */ }

[Fact]
public void Test2() { /* Independent */ }

// Bad: Tests depend on execution order
[Fact, TestPriority(1)]
public void Test1_MustRunFirst() { /* Sets up state */ }

[Fact, TestPriority(2)]
public void Test2_DependsOnTest1() { /* Uses state from Test1 */ } // NO!
```

## Flaky Test Prevention

**Patterns to Avoid Flaky Tests**:

1. **No Thread.Sleep**: Use proper async/await or polling
2. **No DateTime.Now**: Use `ISystemClock` abstraction
3. **No Random Data**: Use deterministic test data
4. **No External Dependencies**: Mock or use Testcontainers
5. **No Shared State**: Isolate test data

```csharp
// Bad: Flaky due to timing
[Fact]
public async Task Test_WithDelay()
{
    await service.StartAsync();
    Thread.Sleep(1000); // NO! Timing-dependent
    var result = service.GetResult();
}

// Good: Proper async waiting
[Fact]
public async Task Test_WithProperWait()
{
    await service.StartAsync();
    var result = await service.GetResultAsync(); // Waits until complete
}

// Bad: Flaky due to DateTime
[Fact]
public void Test_WithDateTime()
{
    var entity = new Entity { CreatedAt = DateTime.Now }; // NO! Non-deterministic
}

// Good: Deterministic time
[Fact]
public void Test_WithFixedDateTime()
{
    var fixedTime = new DateTime(2025, 1, 30, 12, 0, 0, DateTimeKind.Utc);
    var entity = new Entity { CreatedAt = fixedTime };
}
```

## Code Coverage Measurement

**Pattern**: Run tests with coverage collection

```bash
# Run all tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Run specific test project
dotnet test Enerflow.Tests.Unit/Enerflow.Tests.Unit.csproj /p:CollectCoverage=true

# Exclude test projects from coverage
dotnet test /p:CollectCoverage=true /p:Exclude="[Enerflow.Tests.*]*"

# Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html
```

## Summary

**Key Principles**:
1. **AAA Pattern**: Arrange-Act-Assert for clarity
2. **Isolation**: Each test is independent
3. **Deterministic**: No flaky tests
4. **Fast**: Unit tests < 100ms, integration tests < 1s
5. **Readable**: Clear naming and structure
6. **Maintainable**: DRY with builders and fixtures

**Next Steps**:
- Review coverage-targets.md for layer-specific requirements
- Review incomplete-features.md for feature implementation contracts
- Begin implementing tests following these patterns

---

**Status**: ✅ Complete  
**Next**: Create coverage-targets.md and incomplete-features.md
