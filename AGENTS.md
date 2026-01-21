# Enerflow Agent Guidelines

This document serves as the primary instruction set for AI coding agents operating within the Enerflow repository. Adhere strictly to these patterns to maintain system integrity and thermodynamic accuracy.

## 1. Development Commands

### Build & Run
```bash
dotnet build                                              # Build entire solution
dotnet build Enerflow.API/Enerflow.API.csproj             # Build API only
dotnet build Enerflow.Worker/Enerflow.Worker.csproj       # Build Worker only
dotnet run --project Enerflow.API/Enerflow.API.csproj     # Run API
dotnet run --project Enerflow.Worker/Enerflow.Worker.csproj  # Run Worker (MassTransit listener)
```

### Testing
```bash
dotnet test                                               # Run all tests
dotnet test --filter "FullyQualifiedName~ClassName"       # Run tests matching class name
dotnet test --filter "FullyQualifiedName=Namespace.Class.Method"  # Run single test
dotnet test Enerflow.Tests.DWSIM/Enerflow.Tests.DWSIM.csproj      # DWSIM API tests
dotnet test Enerflow.Tests.Unit/Enerflow.Tests.Unit.csproj        # Unit tests
dotnet test Enerflow.Tests.Functional/Enerflow.Tests.Functional.csproj  # Requires Docker
dotnet test --logger "console;verbosity=detailed"         # Verbose output
```

### DWSIM Tests (Special)
DWSIM tests run sequentially (single-threaded requirement). Do NOT modify `xunit.runner.json`:
```bash
dotnet test Enerflow.Tests.DWSIM --no-build --filter "FullyQualifiedName~Test01"
```

## 2. Project Structure

| Project | Purpose | DWSIM Reference |
|---------|---------|-----------------|
| `Enerflow.API` | HTTP API, MassTransit Producer | NO |
| `Enerflow.Worker` | Job Consumer, DWSIM Solver | YES |
| `Enerflow.Domain` | Entities, DTOs, Interfaces | NO |
| `Enerflow.Infrastructure` | EF Core, Migrations | NO |
| `Enerflow.Simulation` | DWSIM wrapper library | YES |
| `Enerflow.Tests.DWSIM` | DWSIM API isolation tests | YES |
| `libs/dwsim_9.0.5/dwsim` | DWSIM binaries (immutable) | - |

## 3. Architecture Rules

### Enterprise Worker Pattern
1. **API**: Orchestrator. Handles HTTP, DB, job submission. NEVER references DWSIM.
2. **Worker**: Executor. Consumes jobs via MassTransit. References DWSIM, runs simulations.
3. **Domain**: Shared kernel. Contains `Simulation`, `SimulationJob`, interfaces.

### DWSIM Integration Constraints
- **Headless Mode**: Set `DWSIM.GlobalSettings.Settings.AutomationMode = true` BEFORE any DWSIM call
- **Thread Safety**: DWSIM is NOT thread-safe. Worker uses `ConcurrentMessageLimit = 1`
- **Automation Class**: Use `DWSIM.Automation.Automation3`, NOT legacy `Automation`
- **Calculation**: `Automation.CalculateFlowsheet2(flowsheet)` returns VOID, not exceptions
- **Error Check**: Use `flowsheet.Solved` and `flowsheet.ErrorMessage` after calculation

### Messaging
- **Transport**: MassTransit with PostgreSQL Transport
- **Queue**: `simulation-jobs` (kebab-case)
- **Serialization**: System.Text.Json (camelCase)

## 4. Code Style & Conventions

### C# / .NET 10.0
```csharp
// File-scoped namespaces (REQUIRED)
namespace Enerflow.Domain.Entities;

// Primary constructors for simple classes
public class StreamData(string name, double temperature);

// Standard constructors for DI
public class SimulationService
{
    private readonly IDbContext _context;
    public SimulationService(IDbContext context) => _context = context;
}

// Required modifier for DTOs/Entities
public class Simulation
{
    public required string Name { get; set; }
    public required string ThermoPackage { get; set; }
}
```

### Sequential IDs (CRITICAL)
```csharp
// WRONG - Fragmented clustered index
public Guid Id { get; set; } = Guid.NewGuid();

// CORRECT - Sequential for DB performance
public Guid Id { get; set; } = Common.IdGenerator.NextGuid();
```

### Naming Conventions
| Element | Convention | Example |
|---------|------------|---------|
| Classes/Methods | PascalCase | `SimulationService`, `CalculateFlowsheet` |
| Private Fields | _camelCase | `_simulationService`, `_logger` |
| Local Variables | camelCase | `flowsheet`, `materialStream` |
| Interfaces | I prefix | `ISimulationService`, `IJobProducer` |
| Constants | PascalCase | `MaxRetryCount`, `DefaultTimeout` |

### Typing Rules
```csharp
// Use var for complex objects
var streams = new Dictionary<Guid, MaterialStream>();
var config = new FlowsheetConfiguration { Name = "Test" };

// Explicit types for primitives and return types
int count = 0;
string name = "Feed";
double temperature = 298.15;

public async Task<Simulation> GetSimulationAsync(Guid id)
```

### Async/Await
```csharp
// CORRECT - Always async/await with CancellationToken
public async Task<Result> ProcessAsync(CancellationToken ct)
{
    var data = await _repository.GetAsync(id, ct);
    return await _solver.SolveAsync(data, ct);
}

// WRONG - Blocking calls
var result = _repository.GetAsync(id).Result;  // NEVER
_solver.SolveAsync(data).Wait();               // NEVER
```

## 5. Error Handling

### Worker Safety Pattern
```csharp
public async Task Consume(ConsumeContext<SimulationJob> context)
{
    var job = context.Message;
    try
    {
        // Execute simulation
        var result = await _solver.SolveAsync(job);
        await UpdateStatus(job.SimulationId, SimulationStatus.Completed, result);
    }
    catch (Exception ex)
    {
        // NEVER crash - update status and continue
        await UpdateStatus(job.SimulationId, SimulationStatus.Failed, ex.Message);
        _logger.LogError(ex, "Simulation {Id} failed", job.SimulationId);
    }
}
```

### DWSIM Exception Handling
```csharp
Automation.CalculateFlowsheet2(flowsheet);

if (!flowsheet.Solved)
{
    throw new SimulationException(flowsheet.ErrorMessage ?? "Unknown solver error");
}

// Check individual objects
foreach (var obj in flowsheet.SimulationObjects.Values)
{
    if (!string.IsNullOrEmpty(obj.ErrorMessage))
        _logger.LogWarning("{Name}: {Error}", obj.Name, obj.ErrorMessage);
}
```

## 6. DWSIM API Pitfalls

### DO NOT Call AddCompoundsToMaterialStream
```csharp
// WRONG - Causes duplicate key exception
var stream = flowsheet.AddObject(ObjectType.MaterialStream, 100, 100, "Feed");
flowsheet.AddCompoundsToMaterialStream(stream);  // THROWS!

// CORRECT - AddObject already adds compounds
var stream = flowsheet.AddObject(ObjectType.MaterialStream, 100, 100, "Feed");
stream.Phases[0].Compounds["Methane"].MoleFraction = 1.0;
```

### Set CalcMode BEFORE Values
```csharp
// Valve
valve.CalcMode = Valve.CalculationMode.OutletPressure;
valve.OutletPressure = 500000;

// Heater/Cooler
heater.CalcMode = Heater.CalculationMode.OutletTemperature;
heater.OutletTemperature = 348.15;

// Compressor
compressor.CalcMode = Compressor.CalculationMode.OutletPressure;
compressor.POut = 2000000;
```

### Property Name Case Sensitivity
```csharp
// VB.NET origin - some properties are lowercase
stream.Phases[0].Properties.molarfraction = 0.5;  // NOT MolarFraction
stream.SpecType = StreamSpec.Pressure_and_VaporFraction;  // underscore
```

## 7. Data Access

- **ORM**: Entity Framework Core with Npgsql
- **JSON Columns**: Use `JsonDocument` for flexible data (compositions, configs, results)
- **Arrays**: Native PostgreSQL `uuid[]` for topology (InputIds, OutputIds)

## 8. Git & Version Control

- **Commits**: Conventional Commits (`feat:`, `fix:`, `chore:`, `refactor:`, `test:`)
- **Binaries**: `libs/` is gitignored - never commit DWSIM binaries
- **Config**: `appsettings.json` gitignored - use `appsettings.Development.json`

## 9. Domain Terminology

Use domain language in code and comments:
- `MaterialStream` not "data stream" or "pipe"
- `UnitOperation` not "processor" or "node"
- `Topology` not "graph" or "connections"
- `PropertyPackage` not "thermodynamic model"
- Lifecycle: **Map -> Build -> Solve -> Collect**

## 10. Agent Resources

### Directory Structure
```
.agent/                    # Antigravity/Cursor IDE
  rules/                   # Coding rules
  skills/                  # Loadable skills (dwsim-api-verification, etc.)
  workflows/               # APM workflow definitions
  prompts/                 # Code review prompts (security, architecture, etc.)

.opencode/                 # OpenCode IDE
  command/                 # Slash commands

.apm/                      # Agentic Project Management (separate system)
  guides/                  # APM methodology guides
  Memory/                  # Task logs and handovers

docs/DWSIM/                # DWSIM reference documentation
  DWSIM_API_MAP.md         # Authoritative API surface
  IPhaseProperties.cs      # Property interface reference

libs/                      # External dependencies (gitignored binaries)
  dwsim_9.0.5/dwsim/       # DWSIM runtime binaries
  dwsim_src/               # DWSIM source for API verification
```

### Skills & Verification
Before using DWSIM APIs, load the verification skill:
```
/skill dwsim-api-verification
```

Or manually check: `.agent/skills/dwsim-api-verification/SKILL.md`

### Code Review Prompts
Available in `.agent/prompts/`:
- `security-audit.md` - Input validation, injection, secrets
- `architecture-check.md` - Enterprise Worker pattern compliance
- `thermodynamic-integrity.md` - Units, DWSIM API, physics
- `bug-hunter.md` - Null refs, edge cases, resource leaks
- `concurrency-check.md` - Thread safety, async/await, DWSIM constraints
