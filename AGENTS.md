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
# Run all tests
dotnet test

# Run specific test project
dotnet test Enerflow.Tests.Unit/Enerflow.Tests.Unit.csproj
dotnet test Enerflow.Tests.Integration/Enerflow.Tests.Integration.csproj
dotnet test Enerflow.Tests.Functional/Enerflow.Tests.Functional.csproj  # Requires Docker
dotnet test Enerflow.Tests.DWSIM/Enerflow.Tests.DWSIM.csproj

# Run single test by fully qualified name
dotnet test --filter "FullyQualifiedName=Enerflow.Tests.Functional.Scenarios.SimulationFlowTests.Can_Run_Simple_Mixer_Simulation"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~SimulationFlowTests"

# Run tests matching a pattern
dotnet test --filter "FullyQualifiedName~Mixer"

# Verbose output for debugging
dotnet test --logger "console;verbosity=detailed"

# Run with specific settings
dotnet test --no-build --no-restore  # Skip build/restore
```

### DWSIM Tests (Special)

DWSIM tests run sequentially (single-threaded requirement). Do NOT modify `xunit.runner.json`:

```bash
dotnet test Enerflow.Tests.DWSIM --no-build --filter "FullyQualifiedName~Test01"
```

### Functional Tests (Testcontainers)

Functional tests use Testcontainers with PostgreSQL and Redis. Key requirements:
- Docker must be running
- Tests use `IntegrationTestWebAppFactory` which starts PostgreSQL and Redis containers
- Containers start in parallel using `Task.WhenAll()` for performance
- Database schema created via `GenerateCreateScript()` (not `EnsureCreatedAsync()`)
- All Worker services must be registered in test factory (see IntegrationTestWebAppFactory.cs)

**Common Issues**:
- "Connection refused" errors → Check service registrations in test factory
- Missing DWSIM services → Ensure `DWSIM.Automation.Automation3` registered as concrete type
- Database creation fails → Use `GenerateCreateScript()` pattern (see section 4)
- Redis timeouts → Ensure Redis container is started and connection string is configured
- Slow tests → Check if containers are starting in parallel, not sequentially

## 2. Project Structure

| Project | Purpose | DWSIM Reference |
|---------|---------|-----------------|
| `Enerflow.API` | HTTP API, MassTransit Producer | NO |
| `Enerflow.Worker` | Job Consumer, DWSIM Solver | YES |
| `Enerflow.Domain` | Entities, DTOs, Interfaces | NO |
| `Enerflow.Infrastructure` | EF Core, Migrations | NO |
| `Enerflow.Simulation` | DWSIM wrapper library | YES |
| `Enerflow.Tests.Unit` | Unit tests (no external deps) | NO |
| `Enerflow.Tests.Integration` | Integration tests | NO |
| `Enerflow.Tests.Functional` | E2E tests with Testcontainers | YES |
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

### Service Registration (CRITICAL)

When registering Worker services (especially in tests), you MUST register ALL dependencies:

```csharp
// Flowsheet Managers (Singleton)
services.AddSingleton<ICompoundManager, CompoundManager>();
services.AddSingleton<IPropertyPackageManager, PropertyPackageManager>();
services.AddSingleton<IMaterialStreamFactory, MaterialStreamFactory>();
services.AddSingleton<IEnergyStreamFactory, EnergyStreamFactory>();
services.AddSingleton<IUnitOperationFactory, UnitOperationFactory>();
services.AddSingleton<IFlashAlgorithmManager, FlashAlgorithmManager>();

// DWSIM Automation (Singleton - register as CONCRETE TYPE)
// CRITICAL: DWSIMFlowsheetBuilder expects DWSIM.Automation.Automation3, not AutomationInterface
services.AddSingleton<DWSIM.Automation.Automation3>();

// Builders (Scoped)
services.AddScoped<IFlowsheetBuilder, DWSIMFlowsheetBuilder>();

// Mappers (Scoped)
services.AddScoped<IStreamMapper, StreamMapper>();
services.AddScoped<IUnitOperationMapper, UnitOperationMapper>();
services.AddScoped<IConnectionMapper, ConnectionMapper>();
services.AddScoped<IPostConnectionConfigurator, PostConnectionConfigurator>();

// Convergence & Solvers (Scoped)
services.AddScoped<ErrorCalculator>();
services.AddScoped<IConvergenceAccelerator, WegsteinAccelerator>();
services.AddScoped<IResultCollector, ResultCollector>();
services.AddScoped<ISimulationSolver, DWSIMSolver>();
```

**Reference**: Always check `Enerflow.Worker/Program.cs` for the canonical service registration pattern.

## 4. Database Patterns

### Schema Creation in Tests

When using Testcontainers with MassTransit, use `GenerateCreateScript()` instead of `EnsureCreatedAsync()`:

```csharp
// WRONG - Fails when MassTransit tables already exist
await dbContext.Database.EnsureCreatedAsync();

// CORRECT - Generates and executes SQL script
var script = dbContext.Database.GenerateCreateScript();
await dbContext.Database.ExecuteSqlRawAsync(script);
```

**Why**: MassTransit creates tables in the `transport` schema. EF Core's `EnsureCreatedAsync()` checks if ANY tables exist in the database and skips creation if true. Using `GenerateCreateScript()` bypasses this check.

### Testcontainers Best Practices

**Multiple Containers**: Start containers in parallel for better performance:

```csharp
public async Task InitializeAsync()
{
    // GOOD - Parallel startup
    await Task.WhenAll(
        _dbContainer.StartAsync(),
        _redisContainer.StartAsync()
    );
}

// BAD - Sequential startup (slower)
await _dbContainer.StartAsync();
await _redisContainer.StartAsync();
```

**Container Configuration**: Use specific images and proper builders:

```csharp
// PostgreSQL
private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-bookworm")
    .Build();

// Redis
private readonly RedisContainer _redisContainer = new RedisBuilder("redis:alpine")
    .Build();
```

**Connection String Injection**: Override settings in `ConfigureWebHost()`:

```csharp
builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
builder.UseSetting("RedisConfiguration", _redisContainer.GetConnectionString());
```

### Schema Separation

- **MassTransit**: Uses `transport` schema for message queues
- **EF Core**: Uses `public` schema for application tables
- Configure explicitly in tests:

```csharp
options.UseNpgsql(dataSource, npgsqlOptions => 
{
    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
});
```

## 5. Code Style & Conventions

### Import Organization

Organize imports in this order:
1. System namespaces
2. Third-party packages (MassTransit, Microsoft.*, etc.)
3. Project namespaces (Enerflow.*)
4. DWSIM namespaces (last, if needed)

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Enerflow.Domain.Entities;
using Enerflow.Infrastructure.Persistence;
using DWSIM.Automation;
```

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

## 6. Error Handling

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

## 7. DWSIM API Pitfalls

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

### Connection Management

- **Method**: Use `flowsheet.ConnectObjects(fromObj.GraphicObject, toObj.GraphicObject, fromIdx, toIdx)`
- **Do NOT Use**: `AttachInputStream`, `AttachOutputStream` (these do not exist in the API)
- **Indices**:
  - Streams usually have index 0 for both input and output ports.
  - Unit Operations use 0-based indices corresponding to their input/output collections.

## 8. Data Access

- **ORM**: Entity Framework Core with Npgsql
- **JSON Columns**: Use `JsonDocument` for flexible data (compositions, configs, results)
- **Arrays**: Native PostgreSQL `uuid[]` for topology (InputIds, OutputIds)

## 9. Git & Version Control

- **Commits**: Conventional Commits (`feat:`, `fix:`, `chore:`, `refactor:`, `test:`)
- **Binaries**: `libs/` is gitignored - never commit DWSIM binaries
- **Config**: `appsettings.json` gitignored - use `appsettings.Development.json`

## 10. Domain Terminology

Use domain language in code and comments:

- `MaterialStream` not "data stream" or "pipe"
- `UnitOperation` not "processor" or "node"
- `Topology` not "graph" or "connections"
- `PropertyPackage` not "thermodynamic model"
- Lifecycle: **Map -> Build -> Solve -> Collect**

## 11. Agent Resources

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

## 12. Strict Testing Protocols (CRITICAL)

**NEVER compromise production code to facilitate testing.**

1. **Production Purity**: Do not modify production code (e.g., `DbContext`, Controllers, Services) to support test-specific constraints (e.g., EF Core InMemory provider quirks).
2. **Database Strategy**: If a test requires database features not supported by the InMemory provider (like `jsonb` or `uuid[]`), **DO NOT** add conditional logic to the production `DbContext`. Instead:
    - **PREFERRED**: Use Testcontainers with the actual database engine (PostgreSQL, Redis, etc.)
    - Use a separate `TestDbContext` that inherits from the production context but overrides configuration *only* for tests.
    - Mock the repository/context layer entirely.
    - **STOP and ask the user** for guidance if stuck.
3. **Infrastructure Dependencies**: Always use Testcontainers for infrastructure (databases, caches, message queues). Never mock infrastructure - it hides real integration issues.
4. **No "Passing at all costs"**: It is better to fail a test and report the architectural constraint than to implement a hack that technically passes the test but corrupts the codebase design.
5. **Escalation**: If you encounter a hard constraint where the only solution seems to be a non-production hack, you **MUST** pause and consult the user.

### Testing Anti-Patterns to Avoid

```csharp
// ❌ BAD - Mocking infrastructure
var mockDb = new Mock<IDbConnection>();
var mockRedis = new Mock<IConnectionMultiplexer>();

// ✅ GOOD - Real infrastructure in containers
var dbContainer = new PostgreSqlBuilder().Build();
var redisContainer = new RedisBuilder().Build();

// ❌ BAD - Conditional production code for tests
if (Environment.GetEnvironmentVariable("TESTING") == "true")
{
    // Special test behavior
}

// ✅ GOOD - Test-specific configuration in test factory
builder.ConfigureTestServices(services => 
{
    // Override only in tests
});
```

## 13. Beads Workflow (Task Management)

This project uses **Beads** for persistent task tracking across sessions. Use `bd` commands via the `bash` tool.

### Essential Commands

```bash
# Finding work
bd ready --json                    # Show issues ready to work (no blockers)
bd list --status=open --json       # All open issues
bd show <id> --json                # Detailed issue view

# Creating & updating
bd create --title="Fix bug X" --type=bug --priority=0 --json  # P0=critical, P2=medium, P4=backlog
bd update <id> --status=in_progress --json
bd close <id> --json
bd close <id1> <id2> <id3> --json  # Close multiple at once

# Dependencies
bd dep add <issue> <depends-on> --json  # issue depends on depends-on
bd blocked --json                       # Show all blocked issues

# Sync (CRITICAL at session end)
bd sync --from-main                # Pull beads updates from main branch
```

### When to Use Beads

- **Multi-session work**: Tasks that span multiple coding sessions
- **Dependencies**: When task B depends on task A
- **Discovered work**: Found a bug while working on something else? Create a bead
- **Strategic planning**: Breaking down large features into trackable tasks

### When NOT to Use Beads

- **Single-session simple tasks**: Use TodoWrite tool instead
- **Trivial operations**: "Run tests", "Fix typo" - just do it

### Session Close Protocol

Before ending a session, ALWAYS run:
```bash
bd sync --from-main    # Pull latest beads
git add .
git commit -m "..."
# Merge to main when ready
```

## 14. Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds
3. **Update issue status** - Close finished work, update in-progress items
4. **PUSH TO REMOTE** - This is MANDATORY:

   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```

5. **Clean up** - Clear stashes, prune remote branches
6. **Verify** - All changes committed AND pushed
7. **Hand off** - Provide context for next session

**CRITICAL RULES:**

- Work is NOT complete until `git push` succeeds, but always do a review first
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds
Use 'bd' for task tracking
