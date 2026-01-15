# Enerflow Agent Guidelines

This document serves as the primary instruction set for AI coding agents operating within the Enerflow repository. Adhere strictly to these patterns to maintain system integrity and thermodynamic accuracy.

## 1. Development Commands

### Build & Run
- **Build Solution:** `dotnet build`
- **Build Project:** `dotnet build Enerflow.API/Enerflow.API.csproj`
- **Run Web API:** `dotnet run --project Enerflow.API/Enerflow.API.csproj`

### Testing (TBD)
*Note: Test projects are not yet initialized. When creating tests, follow these patterns:*
- **Run All Tests:** `dotnet test`
- **Run Specific Test:** `dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"`
- **Watch Mode:** `dotnet watch test`

### Linting & Formatting
- **Check Formatting:** `dotnet format --verify-no-changes`
- **Fix Formatting:** `dotnet format`

## 2. DWSIM & Chemical Engineering Constraints

### Thermodynamic Integrity
- **SI Units Only:** Internal logic MUST use SI units (Kelvin, Pascal, kg/s, mol/s). Never pass Celsius or Bar directly to DWSIM DLLs without using the `UOMConverter` or manual conversion.
- **Degrees of Freedom:** When updating material streams, ensure you do not over-define the state. Exactly 2 state variables (usually T & P) + Composition + Flow are required.
- **Solver Safety:** Always wrap `CalculateFlowsheet2` calls in try-catch blocks. Monitor `flowsheet.SolverState` to prevent infinite recycle loops.

## 3. Code Style & Conventions

### C# / .NET 10.0 Guidelines
- **Modern Features:** Use File-scoped namespaces, Primary Constructors (where appropriate), and `var` for obvious types.
- **Naming Conventions:**
  - Classes/Methods: `PascalCase`
  - Interfaces: `IPascalCase`
  - Local Variables/Fields: `_camelCase` (for private fields), `camelCase` (for locals).
- **Imports:** Group `System` namespaces first, then third-party (DWSIM), then project namespaces. Remove unused usings.

### Architecture
- **Dependency Injection:** Use Constructor Injection. Register simulation-critical services (like `IDWSIMService`) as **Singletons** to preserve the heavy automation engine state.
- **Controllers:** Keep controllers thin. Delegate simulation logic to `Services`.
- **DWSIM Interaction:** Always cast flowsheet entities to their interfaces (e.g., `IMaterialStream`, `IUnitOperation`) from `DWSIM.Interfaces.dll` to ensure type safety.

## 4. Error Handling & Robustness

- **Domain Exceptions:** Throw specific exceptions (e.g., `FlowsheetNotFoundException`) rather than generic `Exception`.
- **API Responses:** Ensure the API returns structured error objects containing:
  - `StatusCode`: Appropriate HTTP code.
  - `Message`: Human-readable error.
  - `DWSIMError`: Specific error message from the solver if available.
- **Validation Gate:** All inputs destined for DWSIM must be "Clipped". 
  - T must be > 0.
  - P must be > 0.
  - Fractions must sum to 1.0.

## 5. DWSIM Binary Management

- **Binaries:** DLLs are located in `libs/dwsim_9.0.5/dwsim`.
- **References:** Reference DLLs via `HintPath` in the `.csproj`. Do NOT commit `bin/` or `obj/` folders.
- **Deployment:** The build process is configured to copy all DWSIM dependencies to the output folder. Ensure any new DWSIM-related DLLs are added to the `<None Update...>` glob in the `.csproj` if they are not picked up automatically.

## 6. Git Protocol

- **Branching:** Use issue-prefixed branch names (e.g., `mndsk-XX-feature-name`).
- **Commits:** Use Conventional Commits (`feat:`, `fix:`, `chore:`, `refactor:`).
- **Binary Safety:** Never use `git add .` without checking `git status` to ensure no massive DLLs or build artifacts are accidentally staged.

## 7. Context Memory

- DWSIM 9 is a cross-platform .NET 8+ compatible library.
- The project targets **.NET 10.0** for the latest runtime optimizations.
- The system is a hybrid: C# handles the "Physical" simulation, while a planned Python FastAPI layer will handle "Agentic" reasoning.
