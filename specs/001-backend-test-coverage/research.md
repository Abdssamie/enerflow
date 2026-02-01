# Research: Backend Test Coverage & MVP Readiness

**Date**: 2025-01-30
**Feature**: Backend Test Coverage & MVP Readiness Assessment
**Phase**: 0 - Research & Technical Decisions

## Executive Summary

This research resolves 6 critical technical unknowns for implementing comprehensive backend test coverage. Key decisions: (1) Testcontainer blocker likely caused by connection string not propagating to Worker services - fix via explicit configuration override, (2) Performance targets set at p95 < 500ms for API, (3) Auth/authz deferred to post-MVP with API key placeholder, (4) Coverlet + ReportGenerator for coverage, (5) NBomber for performance testing (NET-native), (6) Implementation guidance provided for 4 incomplete features.

## 1. Critical Blocker: Testcontainer/MassTransit/Postgres Connection

### Problem Analysis

The functional test `SimulationFlowTests.cs` has been blocked since 2025-01-17 with error: "Connection refused" when Worker attempts to connect to Testcontainer Postgres. This is a common issue in .NET integration tests where:

1. **Connection String Propagation**: The test host (API) successfully connects to Testcontainer Postgres, but the Worker service (running in a separate process context within the test) uses a different connection string
2. **MassTransit Configuration**: Worker services registered with MassTransit may not inherit the test configuration overrides
3. **Service Registration Timing**: Testcontainer starts after service configuration, causing Worker to use default connection string

### Root Cause

Most likely: **Connection string not propagating to Worker services registered with MassTransit**. The `IntegrationTestWebAppFactory` configures the API's DbContext with the Testcontainer connection string, but Worker services (registered via MassTransit consumers) are configured before the test container starts, using the default connection string from configuration.

### Solution

**Decision**: Override Worker DbContext configuration explicitly in test factory

**Rationale**: Ensures Worker services use the same Testcontainer Postgres instance as the API by explicitly configuring the Worker's DbContext after container startup.

**Implementation**:

```csharp
// In IntegrationTestWebAppFactory.cs or similar test fixture

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    private PostgreSqlContainer? _postgresContainer;
    private RabbitMqContainer? _rabbitMqContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(async services =>
        {
            // Start containers FIRST
            _postgresContainer = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .WithDatabase("enerflow_test")
                .WithUsername("test")
                .WithPassword("test")
                .Build();

            await _postgresContainer.StartAsync();

            _rabbitMqContainer = new RabbitMqBuilder()
                .WithImage("rabbitmq:3-management-alpine")
                .Build();

            await _rabbitMqContainer.StartAsync();

            var connectionString = _postgresContainer.GetConnectionString();

            // Remove existing DbContext registrations (API and Worker)
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<EnerflowDbContext>))
                .ToList();
            foreach (var descriptor in descriptors)
      {
                services.Remove(descriptor);
            }

            // Re-register DbContext with Testcontainer connection string
            services.AddDbContext<EnerflowDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            // Override MassTransit configuration to use Testcontainer RabbitMQ
            services.AddMassTransit(x =>
            {
                x.AddConsumer<SimulationJobConsumer>();

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(_rabbitMqContainer.Hostname, _rabbitMqContainer.GetMappedPublicPort(5672), "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            // Ensure database is created and migrated
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EnerflowDbContext>();
            dbConttabase.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _postgresContainer?.DisposeAsync().AsTask().Wait();
            _rabbitMqContainer?.DisposeAsync().AsTask().Wait();
        }
        base.Dispose(disposing);
    }
}
```

**Alternatives Considered**:
- **Alternative 1**: Use in-memory database for tests - Rejected because it doesn't test real Postgres behavior (JSONB, transactions, concurrency)
- **Alternative 2**: Use separate test database instance - Rejected because it requires manuald doesn't provide test isolation
- **Alternative 3**: Mock database entirely - Rejected because it doesn't validate actual data persistence

## 2. Performance Benchmarks

### Industry Standards

Research on REST API performance standards for simulation/computation-heavy workloads:

- **General REST APIs**: p95 < 200ms for simple CRUD, p95 < 1000ms for complex operations
- **Computation APIs** (AWS Lambda, Azure Functions): p95 < 3000ms for compute-heavy tasks
- **Chemical Simulation Tools**: Aspen Plus, HYSYS typically take seconds to minutes for complex simulations
- **Microservice Best Practices**: p95 < 500ms for synchronous APIs, async for long-running tasks

### Decision

**Performance Targets for MVP**:

| Metric | Target | Rationale |
|--------|--------|-----------|
| **API Response Time (Submission)** | p50 < 100ms, p95 < 500ms, p99 < 1000ms | Simulation submission is async (returns job ID), should be fast |
| **API Response Time (Status/Results)** | p50 < 50ms, p95 < 200ms, p99 < 500ms | Simple database queries, should be very fast |
| **Worker Throughput** | Process 10 simulations/minute (simple), 2 simulations/minute (complex) | Based on DWSIM executime, varies by complexity |
| **Concurrent Users** | Support 50 concurrent API requests | Reasonable for MVP, can scale horizontally |
| **Test Suite Execution** | < 10 minutes | Enables rapid feedback loop for developers |
| **Database Operations** | p95 < 100ms for CRUD | Standard database performance |

**Rationale**: 
- Simulation submission is async, so API should respond quickly with job ID
- Actual simulation execution happens in Worker (can take seconds to minutes)
- Targets are achievable for MVP while leaving room for optimization
- Aligned with microservice best practices for synchronous APIs

**Alternatives Considered**:
- **Stricter targets** (p95 < 100ms for all): Rejected because simulation domain inherently involves computation time
- **Looser targets** (p95 < 2000ms): Rejected because users expect responsive APIs even for async operations

## 3. Authentication/Authorization

### Security Requirements Analysis

**Current State**: No authentication/authorization implemented in codebase

**MVP Security Considerations**:
- **Deployment Environment**: Likely internal/private network initially
- **Data Sensitivity**: Simulation data may contain proprietary procesnformation
- **User Management**: Multi-user support not yet implemented
- **Timeline Impact**: Full auth/authz implementation adds 1-2 weeks

**Security Best Practices**:
- **Minimum**: API keys for service-to-service communication
- **Standard**: JWT tokens with user authentication
- **Enterprise**: OAuth2/OIDC with identity provider integration

### Decision

**Decision**: **Defer authentication/authorization to post-MVP**

**Rationale**:
1. **Timeline**: Adding auth/authz would extend MVP timeline by 1-2 weeks
2. **Deployment**: MVP likely deployed in controlled/internal environment
3. **Scope**: Focus on core functionality (testing) before adding security layer
4. **Incremental**: Easier to add auth after testing infrastructure is solid

**MVP Security Mitigation**:
- Deploy behind VPN or internal network
- Use network-level security (firewall rules, security groups)
- Add API key validation as placeholder (simple header check)
- Document security limitations in MVP release notes

**Post-MVP Implementation Plan**:
- Phase 1: API key authentication (1-2 days)
- Phase 2: JWT token authentication with user management (1 week)
- Phase 3: OAuth2/OIDC integration if needed (1 week)

**Alternatives Considered**:
- **API Keys Now**: Simple but adds scope, deferred to post-MVP Phase 1
- **JWT Tokens Now**: More robust but adds 1 week, deferred to post-MVP Phase 2
- **OAuth2 Now**: Enterprise-grade but adds 2 weeks, deferred to post-MVP Phase 3
- **No Auth Ever**: Rejected due to security concerns for production deployment

## 4. Test Coverage Tools

### Decision

**Tools Selected**:
- **Code Coverage**: Coverlet (industry standard for .NET)
- **Report Generation**: ReportGenerator (integrates with Coverl- **CI Integration**: GitHub Actions / Azure DevOps (standard CI/CD platforms)

**Configuration**:

```xml
<!-- Add to test project .csproj files -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.msbuild" Version="6.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
      <PrivateAssets>alteAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

**Running Coverage Locally**:

```bash
# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/

# Generate HTML report
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/TestResults/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html

# Open report
open TestResults/CoverageReport/index.html
```

**CI/CD Integration** (GitHub Actions example):

```yaml
name: Test Coverage

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Run tests with coverage
        run: dotnet test --no-restore --verbosity normal /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
      
      - name: Generate coverage report
        run: |
          dotnet tool install -g dotnet-reportgeneratorn          reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"Html;Badges"
      
      - name: Upload coverage report
        uses: actions/upload-artifact@v3
        with:
          name: coverage-report
          path: CoverageReport/
      
      - name: Check coverage thresholds
        run: |
          # Fail if coverage below targets
          # API: 80%, Worker: 80%, Service: 70%, Infrastructure: 70%
```

**Rationale**: 
- Coverlet is the de facto standard for .NET code coverage
- ReportGenerator provides excellent HTML reports with drill-down
- Both tools are free, open-source, and well-man- Excellent CI/CD integration support

**Alternatives Considered**:
- **dotCover** (JetBrains): Commercial tool, rejected due to cost
- **OpenCover**: Older tool, less maintained, rejected in favor of Coverlet
- **Built-in VS Code Coverage**: Limited reporting, rejected in favor of ReportGenerator

## 5. Performance Testing Tools

### Tool Comparison

| Tool | Pros | Cons | .NET Integration |
|------|------|------|------------------|
| **NBomber** | .NET-native (C#), excellent .NET integration, type-safe | Smaller community | ⭐⭐⭐⭐⭐ Excellent |
| **k6** | Popular, great docs, JavaScript-based | Requires separate runtime | ⭐⭐⭐ Good (HTTP only) |
| **JMeter** | Mature, GUI, extensive plugins | Java-based, heavy, complex | ⭐⭐ Fair (HTTP only) |

### Decision

**Tool Selected**: **NBomber**

**Rationale**:
1. **.NET-Native**: Written in C#, runs in .NET runtime, no additional dependencies
2. **Type Safety**: Can reference actual DTOs from codebase for request/response validation
3. **Developer Experience**: Familiar syntax for .NET developers
4. **CI Integration**: Runs as standard .NET test, easy CI/CD integration
5. **Reporting**: Built-in HTML reports, metrics export

**Sample Performance Test**:

```csharp
using NBomber.CSharp;
using NBomber.Http.CSharp;

public class SimulationApiLoadTests
{
    [Fact]
    public void LoadTest_SimulationSubmission_MeetsPerformanceTargets()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

        var scenario = Scenario.Create("simulation_submission", async context =>
        {
            var request = Http.CreateRequest("POST", "/api/simulations")
                .WithHeader("Content-Type", "application/json")
                .WithBody(new StringContent("""
                    {
                        "name": "Load Test Simulation",
                        "compounds": ["Water", "Ethanol"],
                        "propertyPackage": "NRTL"
                    }
                    """, Encoding.UTF8, "application/json"));

            var response = await Http.Send(httpClient, request);
            return response;
        })
        .WithLoadSimulations(
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
  .Run();

        var p95Latency = stats.ScenarioStats[0].Ok.Latency.Percent95;
        
        // Assert p95 < 500ms target
        Assert.True(p95Latency < 500, $"p95 latency {p95Latency}ms exceeds 500ms target");
    }
}
```

**Alternatives Considered**:
- **k6**: Excellent tool but requires JavaScript, adds complexity for .NET team - Rejected
- **JMeter**: Mature but heavy and Java-based, poor developer experience - Rejected
- **Apache Bench (ab)**: Too simple, no scenario support - Rejected

## 6. Incomplete Feature Implementation

### 6.1 Mass Balance Validation

**Current State**: `SimulationService.cs:497` - Method returns `true` (placeholder)

**Approach**: Implement rigorous mass balance check by comparing total mass in vs total mass out

**DWSIM API Usage**:
```csharp
// Access material streams
var materialStreams = flowsheet.SimulationObjects.Values
    .OfType<DWSIM.UnitOperations.Streams.MaterialStream>();

// Get mass flow rate
double massFlow = stream.Phases[0].Properties.massflow.GetValueOrDefault();

// Sum inlet vs outlet streams
```

**Validation Logic**:
```csharp
public bool ValidateMassBalance(Flowsheet flowsheet, double tolerance = 0.01)
{
    var streams = flowsheet.SimulationObjects.ValueOfType<DWSIM.UnitOperations.Streams.MaterialStream>()
        .ToList();

    // Identify inlet streams (no source unit operation)
    var inletStreams = streams.Where(s => string.IsNullOrEmpty(s.GraphicObject.InputConnectors[0].AttachedConnector?.AttachedFrom?.Name));
    
    // Identify outlet streams (no destination unit operation)
    var outletStreams = streams.Where(s => string.IsNullOrEmpty(s.GraphicObject.OutputConnectors[0].AttachedConnector?.AttachedTo?.Name));

    double totalMassIn = inletStreams.Sum(s => s.Phases[0].Properties.massflow.GetValueOrDefault());
    double totalMassOut = outletStreams.Sum(s => s.Phases[0].Properties.massflow.GetValueOrDefault());

    double imbalance = Math.Abs(totalMassIn - totalMassOut) / totalMassIn;
    
    return imbalance <= tolerance; // 1% tolerance
}
```

**Test Strategy**: Create test simulations with known mass flows, verify validation detects imbalances

### 6.2 Unit Operation Parameter Configuration

**Current State**: `DWSIMFlowsheetBuilder.cs:146` - Unit operations created but parameters not configured from entity

**Approach**: Map entity properties to DWSIM unit operation parameters based on unit type

**Pmeter Mapping**:
```csharp
prte void ConfigureUnitOperationParameters(UnitOperation entity, DWSIM.SharedClasses.UnitOperations.BaseClass dwsimUnit)
{
    switch (entity.Type)
    {
        case UnitOperationType.Heater:
            if (entity.Parameters.TryGetValue("OutletTemperature", out var temp))
            {
                ((DWSIM.UnitOperations.UnitOperations.Heater)dwsimUnit).CalcMode = 
                    DWSIM.UnitOperations.UnitOperations.Heater.CalculationMode.OutletTemperature;
                ((DWSIM.UnitOperations.UnitOperations.Heater)dwsimUnit).OutletTemperature = Convert.ToDouble(temp);
            }
            break;

        case UnitOperationType.Compressor:
            if (entity.Parameters.TryGetValue("OutletPressure", out var pressure))
            {
                ((DWSIM.UnitOperations.UnitOperations.Compressor)dwsimUnit).POut = Convert.ToDouble(pressure);
            }
            if (entity.Parameters.TryGetValue("Efficiency", out var eff))
            {
                ((DWSIM.UnitOperations.UnitOperations.Compressor)dwsimUnit).Eficiencia = Convert.ToDouble(eff);
            }
            break;

        case UnitOperationType.Mixer:
            if (entity.Parameters.TryGetValue("OutletPressure", out var mixerP))
            {
                ((DWSIM.UnitOperations.UnitOperations.Mixer)dwsimUnit).OutletPressure = Convert.ToDouble(mixerP);
            }
            break;

        // Add cases for other unit types...
    }
}
```

**Test Strategy**: Create unit operations with specific parameters, verify DWSIM units configured correctly

### 6.3 Result Extraction Enhancement

**Current State**: `ResultCollector.cs:78` - Generic extraction, unit-specific properties not extracted

**Approach**: Extract unit-specific properties based on unit type

**Unit-Specific Properties**:
```csharp
private Dictionary<string, object> ExtractUnitSpecificProperties(DWSIM.SharedClasses.UnitOperations.BaseClass dwsimUnit)
{
    var properties = new Dictionary<string, object>();

    switch (dwsimUnit)
    {
        case DWSIM.UnitOperations.UnitOperations.Heater heater:
            properties["HeatDuty"] = heater.DeltaQ.GetValueOrDefault();
            properties["OutletTemperature"] = heater.OutletTemperature;
            break;

        case DWSIM.UnitOperations.UnitOperations.Compressor compressor:
            properties["Power"] = compressor.DeltaQ.GetValueOrDefault();
            properties["OutletPressure"] = compressor.POut;
            properties["Efficiency"] = compressor.Eficiencia;
            break;

        case DWSIM.UnitOperations.UnitOperations.HeatExchanger heatExchanger:
            properties["HeatTransferred"] = heatExchanger.Q.GetValueOrDefault();
            properties["LMTD"] = heatExchanger.LMTD.GetValueOrDefault();
            properties["OverallHeatTransferCoefficient"] = heatExchanger.OverallCoefficient.GetValueOrDefault();
            break;

        case DWSIM.UnitOperations.UnitOperations.DistillationColumn column:
            properties["Reflux Ratio"] = column.RefluxRatio;
            properties["NumberOfStages"] = column.NumberOfStages;
            properties["CondenserDuty"] = column.CondenseretValueOrDefault();
            properties["ReboilerDuty"] = column.ReboilerDuty.GetValueOrDefault();
            break;

        // Add cases for other unit types...
    }

    return properties;
}
```

**Test Strategy**: Run simulations with various unit types, verify all expected properties extracted

### 6.4 Wegstein Acceleration

**Current State**: `DWSIMSolver.cs:138` - Convergence loop exists, tear stream identification incomplete

**Approach**: Identify recycle streams (tear streams) and apply Wegstein acceleration

**Tear Stream Identification**:
```csharp
private List<string> IdentifyTearStreams(Flowsheet flowsheet)
{
    var tearStreams = new List<string>();
    
    // Build dependency graph
    var graph = BuildDependencyGraph(flowsheet);
    
    // Detect cycles using DFS
    var cycles = DetectCycles(graph);
    
    // For each cycle, identify the stream to "tear" (break the cycle)
    foreach (var cycle in cycles)
    {
        // Choose stream with least impact (heuristic: stream with lowest mass flow)
        var cycleStreams = cycle.SelectMany(unit => GetOutputStreams(unit, flowsheet));
        var tearStream = cycleStreams.OrderBy(s => GetMassFlow(s, flowsheet)).First();
        tearStreams.AarStream);
    }
    
    return tearStreams;
}

private void ApplyWegsteinAcceleration(string streamId, double[] previousValues, double[] currentValues)
{
    // Wegstein acceleration formula
    // x_new = x_current + q * (x_current - x_previous)
    // where q is the acceleration factor
    
    double q = CalculateAccelerationFactor(previousValues, currentValues);
    
    for (int i = 0; i < currentValues.Length; i++)
    {
        currentValues[i] = currentValues[i] + q * (currentValues[i] - previousValues[i]);
    }
}
```

**Test Strategy**: Create simulations with recycle loops, verify convergence is faster with acceleration

## Summary of Decisionsion Area | Choice | Impact | Timeline |
|---------------|--------|--------|----------|
| **Testcontainer Fix** | Override DbContext configuration in test factory | Unblocks functional tests | 1-2 days |
| **Performance Targets** | p95 < 500ms (API), 10 sim/min (Worker) | Defines MVP acceptance criteria | N/A |
| **Auth/Authz** | Defer to post-MVP | Saves 1-2 weeks, deploy in secure network | Post-MVP |
| **Coverage Tools** | Coverlet + ReportGenerator | Standard .NET tooling, excellent CI integration | N/A |
| **Performance Tool** | NBomber | .NET-native, type-safe, easy integration | N/A |
| **Feature ComImplement 4 features with provided guidance | Completes all TODOs, eliminates technical debt | 5 days |

## Next Steps

1. **Immediate**: Implement Testcontainer fix (Phase 0, Task 0.1)
2. **Validate**: Run at least one functional test end-to-end (Phase 0, Task 0.2)
3. **Document**: Update functional test setup guide (Phase 0, Task 0.3)
4. **Proceed**: Move to Phase 1 design (data-model.md, contracts/)
5. **Begin**: Start Phase 1 implementation (API tests)

---

**Research Status**: ✅ Complete  
**All NEEDS CLARIFICATION Resolved**: Yes  
**Ready for Phase 1**: Yes  
**Critical Blocker**: Solution identified, ready for implementation
