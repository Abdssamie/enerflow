# Enerflow Agent Guidelines

This document serves as the primary instruction set for AI coding agents operating within the Enerflow repository. Adhere strictly to these patterns to maintain system integrity and thermodynamic accuracy.

## 1. Development Commands

### Build & Run
- **Build Solution:** `dotnet build`
- **Build API:** `dotnet build Enerflow.API/Enerflow.API.csproj`
- **Build Worker:** `dotnet build Enerflow.Worker/Enerflow.Worker.csproj`
- **Run API:** `dotnet run --project Enerflow.API/Enerflow.API.csproj`
- **Run Worker (Manual):** `dotnet run --project Enerflow.Worker/Enerflow.Worker.csproj -- --job <job.json> --output <result.json>`

### Testing
- **Run All Tests:** `dotnet test`
- **Run Specific Test:** `dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"`

## 2. Architecture & Design

### The "Enterprise Worker" Pattern
Enerflow uses a split architecture to ensure stability and isolation:
1.  **Enerflow.API:** The Orchestrator. Handles HTTP, DB, and Job Queueing. **NEVER** references DWSIM binaries directly for solving. It manages `SimulationJob` entities.
2.  **Enerflow.Worker:** The Executor. A transient Console App spawned as a child process. It references DWSIM binaries, loads the simulation, solves it, and dies.
3.  **Enerflow.Domain:** Shared Kernel. Contains Entities (`SimulationSession`) and DTOs (`SimulationJob`, `SimulationResult`) shared by API and Worker.

### DWSIM Integration Constraints
- **Binaries:** Located in `libs/dwsim_9.0.5/dwsim`. Treat them as immutable.
- **Headless Mode:** Always set `DWSIM.GlobalSettings.Settings.AutomationMode = true` immediately upon initialization to prevent UI crashes on Linux.
- **Solver Safety:** `CalculateFlowsheet2/3` in the patched binaries returns `void`. Check `flowsheet.Solved` and `flowsheet.ErrorMessage` to determine success.
- **SI Units:** All `StreamState` values (T, P, Flow) must be in SI Units (Kelvin, Pascal, kg/s).

## 3. Code Style & Conventions

### C# / .NET 10.0 Guidelines
- **Modern Features:** Use File-scoped namespaces, Primary Constructors, and `var` for obvious types.
- **Naming:** `PascalCase` for public members, `_camelCase` for private fields.
- **DTOs:** Use `record` or `class` with `required` properties for data transfer objects in the Domain.

### Error Handling
- **Worker Safety:** The Worker process must **never** crash silently. Wrap `Main` in a global try-catch and write a `FailureResult` JSON to disk before exiting with code 1.
- **API Resilience:** The API must assume the Worker might be killed (OOM, SegFault). Implement timeouts (default 60s) when waiting for the Worker process.

## 4. Workflows

### Running a Simulation
1.  **API:** Creates `SimulationJob` DTO (Input file path + Parameter Overrides).
2.  **API:** Serializes Job to JSON.
3.  **API:** Spawns `Enerflow.Worker` with paths to Job JSON and Output JSON.
4.  **Worker:** Loads Template -> Applies Overrides -> Solves -> Writes Result JSON.
5.  **API:** Reads Result JSON -> Updates DB.

### Modifying a Simulation
- **Template + Diffs:** Do not overwrite the original `.dwxmz` file on every run. Apply "Diffs" (Parameter Overrides) in memory within the Worker.

## 5. Git & Version Control
- **Binaries:** `libs/` is gitignored (mostly). Do not commit DLLs unless updating the core engine.
- **Output:** `output/` is gitignored.
- **Commits:** Use Conventional Commits (`feat:`, `fix:`, `chore:`).

## 6. Context Memory
- DWSIM 9 is patched for Headless Linux execution (AutomationMode fix).
- The project targets **.NET 10.0**.
- Architecture is "Job-Based" with process isolation.
