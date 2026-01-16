# Enerflow Agent Guidelines

This document serves as the primary instruction set for AI coding agents operating within the Enerflow repository. Adhere strictly to these patterns to maintain system integrity and thermodynamic accuracy.

## 1. Development Commands

### Build & Run
- **Build Solution:** `dotnet build`
- **Build API:** `dotnet build Enerflow.API/Enerflow.API.csproj`
- **Build Worker:** `dotnet build Enerflow.Worker/Enerflow.Worker.csproj`
- **Run API:** `dotnet run --project Enerflow.API/Enerflow.API.csproj`
- **Run Worker:** `dotnet run --project Enerflow.Worker/Enerflow.Worker.csproj` (Runs as a Hosted Service listening to MassTransit)

### Testing
- **Run All Tests:** `dotnet test`
- **Run Specific Test:** `dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"`
- **Run Functional Tests:** `dotnet test Enerflow.Tests.Functional/Enerflow.Tests.Functional.csproj` (Requires Docker for Testcontainers)

## 2. Enerflow Vibe Coding (Opencode)
Adhere to these principles to maintain flow and quality within our specific architecture:
1.  **Domain-First Intent:** Describe code in terms of `MaterialStream`, `UnitOperation`, and `Topology`. Avoid generic "data processing" language.
2.  **Lifecycle Chunking:** Implement features by following the Simulation Lifecycle: **Map -> Build -> Solve -> Collect**.
3.  **Hybrid Data Model:** Enforce strict typing for Relations/Topology (Guids), but embrace `JsonDocument` for flexible physical properties.
4.  **Stateless Execution:** The Worker is stateless. Ensure every `SimulationJob` contains *everything* needed to run.
5.  **Fail Safe:** DWSIM is fragile. Always wrap solver logic in robust error handling to protect the Worker process.

## 3. Architecture & Design

### The "Enterprise Worker" Pattern
Enerflow uses a split architecture to ensure stability and isolation:
1.  **Enerflow.API:** The Orchestrator. Handles HTTP, DB, and Job Submission. **NEVER** references DWSIM binaries directly. It communicates with the Worker via MassTransit.
2.  **Enerflow.Worker:** The Executor. A Hosted Service that consumes `SimulationJob` messages. It references DWSIM binaries, manages the automation engine, and executes simulations.
3.  **Enerflow.Domain:** Shared Kernel. Contains Entities (`Simulation`), DTOs (`SimulationJob`), and Interfaces (`ISimulationService`).

### Messaging & Transport
- **Transport:** MassTransit using **PostgreSQL Transport** (SQL Transport).
- **Queues:** Worker listens on `simulation-jobs` (configured via kebab-case formatter).
- **Serialization:** System.Text.Json (CamelCase).

### DWSIM Integration Constraints
- **Binaries:** Located in `libs/dwsim_9.0.5/dwsim`. Treat as immutable.
- **Headless Mode:** `DWSIM.GlobalSettings.Settings.AutomationMode = true` must be set **before** any other DWSIM call.
- **Thread Safety:** DWSIM Automation is **NOT** thread-safe.
    - The Worker enforces `ConcurrentMessageLimit = 1` via `SimulationJobConsumerDefinition`.
    - **NEVER** remove this concurrency limit.
- **Solver:** Use `flowsheet.RequestCalculation()`. `CalculateFlowsheet2` is deprecated/void in patched binaries.

## 3. Code Style & Conventions

### C# / .NET 10.0 Guidelines
- **Namespaces:** Use File-scoped namespaces (`namespace Enerflow.Domain;`).
- **Constructors:** Use Primary Constructors where appropriate, or standard constructors for DI injection.
- **Sequential IDs:** Use `Enerflow.Domain.Common.IdGenerator.NextGuid()` for generating new identifiers. **NEVER** use `Guid.NewGuid()`. Sequential IDs (NewId) are required for database performance and clustered index stability.
- **Properties:** Use `required` modifier for DTOs and Entities to ensure validity.
- **Typing:** Use `var` for complex object creation (`new Dictionary<...>`), explicit types for primitives (`int`, `string`) and return types.
- **Async:** Always use `async/await`. Avoid `.Result` or `.Wait()`. Use `CancellationToken` where available.

### Naming Conventions
- **Classes/Methods:** `PascalCase`
- **Private Fields:** `_camelCase` (e.g., `_simulationService`)
- **Local Variables:** `camelCase`
- **Interfaces:** `I` prefix (e.g., `ISimulationService`)

### Error Handling
- **Worker Safety:** The Worker process must handle exceptions gracefully.
    - `SimulationJobConsumer` catches all exceptions during execution.
    - On failure: Update `Simulation.Status` to `Failed`, save `ErrorMessage`, and persist to DB.
    - The process should **not** crash; it should ack the message (or move to error queue) and be ready for the next job.

## 4. Workflows

### Simulation Execution
1.  **API:** Receives request -> Creates/Updates `Simulation` Entity -> Publishes `SimulationJob` via MassTransit.
2.  **Worker:** Consumes message (Serialized execution) -> Maps DTO to DWSIM Objects -> Solves Flowsheet -> Collects Results.
3.  **Worker:** Updates `Simulation` Entity (Status, ResultJson) and `MaterialStream` Entities directly in DB.
4.  **API:** Polls/Reads updated Entities to show results to user.

### Data Access
- **ORM:** Entity Framework Core with Npgsql.
- **JSON:** Heavy/Dynamic data (Compositions, Unit Configs, Full Results) is stored in `jsonb` columns using `JsonDocument`.
- **Arrays:** Native PostgreSQL arrays (`uuid[]`) used for Topology (Input/Output IDs).

## 5. Git & Version Control
- **Binaries:** `libs/` is gitignored.
- **Commits:** Use Conventional Commits (`feat:`, `fix:`, `chore:`, `refactor:`).
- **Configuration:** `appsettings.json` is gitignored; use `appsettings.Development.json` or environment variables.

## 6. Project Structure
- `Enerflow.API`: Web API (Controllers, MassTransit Producer).
- `Enerflow.Worker`: Hosted Service (Consumer, DWSIM Mapper, Solver).
- `Enerflow.Domain`: Entities, Enums, DTOs, Interfaces.
- `Enerflow.Infrastructure`: EF Core Context, Migrations.
- `libs/`: External DWSIM dependencies.

