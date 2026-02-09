# Cleanup Unnecessary Code & Reduce Maintenance Overhead

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove unused, redundant, and over-engineered code that adds maintenance overhead without providing value.

**Context:**
After migrating to `CalculateFlowsheet4()`, several components are now obsolete:
1. Custom solver loop logic (replaced by DWSIM's built-in solver)
2. ErrorCalculator (not used by CalculateFlowsheet4)
3. ConvergenceConfig (not used by CalculateFlowsheet4)
4. PostConnectionConfigurator (minimal value, adds complexity)
5. Unused convergence acceleration code

**Tech Stack:** C#, DWSIM API

---

## Task 1: Remove Unused Convergence Infrastructure

**Files:**
- Delete: `Enerflow.Worker/Convergence/ErrorCalculator.cs`
- Delete: `Enerflow.Worker/Convergence/WegsteinAccelerator.cs` (if exists)
- Modify: `Enerflow.Worker/Solvers/ISimulationSolver.cs` (remove ConvergenceConfig)
- Modify: `Enerflow.Worker/Solvers/DWSIMSolver.cs` (remove ErrorCalculator dependency)

**Step 1: Verify ErrorCalculator is not used**

```bash
cd /home/abdssamie/ChemforgeProjects/enerflow
rg "ErrorCalculator" --type cs
```

Expected: Only found in DWSIMSolver constructor and Convergence folder

**Step 2: Remove ErrorCalculator from DWSIMSolver**

In `Enerflow.Worker/Solvers/DWSIMSolver.cs`:
- Remove `ErrorCalculator` from constructor parameters
- Remove `_errorCalculator` field
- Remove any calls to `_errorCalculator.CalculateError()`

**Step 3: Delete convergence files**

```bash
rm -rf Enerflow.Worker/Convergence/
```

**Step 4: Remove ConvergenceConfig**

In `Enerflow.Worker/Solvers/ISimulationSolver.cs`:
- Remove `ConvergenceConfig` class definition
- Change `Solve` signature from:
  ```csharp
  SimulationResult Solve(Simulation simulation, ConvergenceConfig? config = null);
  ```
  To:
  ```csharp
  SimulationResult Solve(Simulation simulation);
  ```

**Step 5: Update DWSIMSolver.Solve method**

Remove:
```csharp
config ??= new ConvergenceConfig();
```

**Step 6: Verify build**

```bash
dotnet build Enerflow.Worker
```

**Step 7: Run tests**

```bash
dotnet test Enerflow.Tests.Functional --filter "Can_Run_Simple_Mixer_Simulation"
dotnet test Enerflow.Tests.DWSIM
```

Expected: All tests PASS

**Step 8: Commit**

```bash
git add -A
git commit -m "refactor: remove unused convergence infrastructure (ErrorCalculator, ConvergenceConfig)

- Removed ErrorCalculator (not needed with CalculateFlowsheet4)
- Removed ConvergenceConfig (DWSIM handles convergence internally)
- Simplified DWSIMSolver.Solve signature
- Deleted Convergence/ folder

Rationale: CalculateFlowsheet4() handles convergence internally, making
custom convergence logic redundant and adding unnecessary maintenance overhead."
```

---

## Task 2: Evaluate PostConnectionConfigurator

**Files:**
- Read: `Enerflow.Worker/Mappers/PostConnectionConfigurator.cs`
- Read: `Enerflow.Worker/Solvers/DWSIMSolver.cs` (check usage)

**Step 1: Analyze what PostConnectionConfigurator does**

```bash
cat Enerflow.Worker/Mappers/PostConnectionConfigurator.cs
```

**Step 2: Check if it's actually needed**

Questions to answer:
- Does it do anything that can't be done in UnitOperationMapper?
- Is it just setting splittos? (That should be in UnitOperationMapper)
- Does it add value or just add an extra layer?

**Step 3: Decision**

If PostConnectionConfigurator only sets splitter ratios:
- **Action:** Move that logic into `UnitOperationMapper.MapSplitter()`
- **Delete:** `PostConnectionConfigurator.cs` and interface
- **Update:** `DWSIMSolver.cs` to remove the configurator step

If it does more complex post-connection logic:
- **Keep it** but document why it's needed

**Step 4: Implement decision**

If removing:
```bash
# Move splitter ratio logic to UnitOperationMapper
# Delete PostConnectionConfigurator
rm Enerflow.Worker/Mappers/PostConnectionConfigurator.cs
rm Eker/Mappers/IPostConnectionConfigurator.cs

# Update DWSIMSolver constructor and Solve method
# Remove _postConfigurator field and calls
```

**Step 5: Verify tests**

```bash
dotnet test Enerflow.Tests.Functional
dotnet test Enerflow.Tests.DWSIM
```

**Step 6: Commit**

```bash
git add -A
git commit -m "refactor: remove PostConnectionConfigurator and move logic to UnitOperationMapper

- Moved splitter ratio configuration into MapSplitter()
- Removed unnecessary abstraction layer
- Simplified DWSIMSolver pipeline

Rationale: PostConnectionConfigurator added complexity without clear benefit.
Configuration should happen in the mapper that handles that unit type."
```

---

## Task 3: Remove ConvergenceException (if unused)

**Files:**
- Check: `Enerflow.Worker/Solvers/DWSIMSolver.cs` (bottom of file)

**Step 1: Check if ConvergenceException is used**

```bash
rg "ConvergenceException" --type cs
```

**Step 2: If only defined but never thrown, delete it**

Remove from `DWSIMSolver.cs`:
```csharp
public class ConvergenceException : Exception
{
    public ConvergenceException(string message) : base(message)
    {
    }
}
```

**Step 3: Commit**

```bash
git add Enerflow.Worker/Solvers/DWSIMSolver.cs
git commit -m "refactor: remove unused ConvergenceException class"
```

---

## Task 4: Simplify DI Registration

**Files:**
- Modify: `Enerflow.Worker/Program.cs` (or wherever DI is configured)

**Step 1: Remove registrations for deleted services**

Remove:
```csharp
services.AddSingleton<ErrorCalculator>();
services.AddScoped<IPostConnectionConfigurator, PostConnectionConfigurator>();
```

**Step 2: Verify build**

```bash
dotnet build Enerflow.Worker
```

**Step 3: Commit**

```bash
git add Enerflow.Worker/Program.cs
git commit -m "refactor: remove DI registrations for deleservices"
```

---

## Task 5: Document Simplified Architecture

**Files:**
- Create: `docs/ARCHITECTURE/SOLVER_PIPELINE.md`

**Step 1: Document the simplified pipeline**

```markdown
# Solver Pipeline Architecture

## Overview

The Enerflow solver uses a clean, linear pipeline to convert domain entities into DWSIM flowsheets and execute simulations.

## Pipeline Steps

1. **Build** (`DWSIMFlowsheetBuilder`)
   - Creates DWSIM flowsheet
   - Adds compounds and property package
   - Creates all simulation objects (streams, units)
   - Validates flowsheet structure

2. **Map Streams** (`StreamMapper`)
   - Configures material stream properties (T, P, flow, composition)
   - Configures energy stream properties

3. **Map Unit Operations** (`UnitOperationMapper`)
   - Configures unit operation parameters (calc mode, setpoints, etc.)
   - Uses lookup (not creation) to find objects created by Builder

4. **Connect** (`ConnectionMapper`)
   - Wires streams to unit operations
   - Uses DWSIM's `ConnectObjects()` API

5. **Solve** (`Automation.CalculateFlowsheet4()`)
   - DWSIM's built-in solver handles:
     - Calculation order
     - Convergence loops
     - Recycle convergence
     - Error handling
   - Returns `List<Exception>` and sets `flowsheet.Solved` flag

6. **Extract Results** (`ResultCollector`)
   - Reads calculated properties from streams and units
   - Converts to domain DTOs

## Key Principles

- **Single Responsibility:** Each component does one thing
- **No Duplication:** Objects created once, configured once, connected once
- **Trust DWSIM:** Use DWSIM's built-in solver instead of reimplementing
- **Fail Fast:** Validate early, fail with clear errors

## What We Removed

- ❌ Custom convergence loop (DWSIM handles this)
- ❌ ErrorCalculator (not needed with CalculateFlowsheet4)
- ❌ ConvergenceConfig (DWSIM has its own setti
- ❌ Wegstein acceleration (DWSIM has better algorithms)
- ❌ PostConnectionConfigurator (moved to UnitOperationMapper)

## Benefits

- ✅ Simpler code (fewer classes, less abstraction)
- ✅ Easier to understand and maintain
- ✅ Leverages DWSIM's proven solver
- ✅ Fewer bugs (less custom logic)
- ✅ Better performance (DWSIM's optimized algorithms)
```

**Step 2: Commit**

```bash
git add docs/ARCHITECTURE/SOLVER_PIPELINE.md
git commit -m "docs: document simplified solver pipeline architecture"
```

---

## Task 6: Update Plan Status

**Files:**
- Modify: `docs/plans/2026-02-09-fix-unit-operation-mapper-bug.md`
- Modify: `docs/plans/2026-02-09-architecture-cleanup.md`

**Step 1: Mark completed plans**

Add to top of completed plans:
```markdown
## ✅ STATUS: COMPLETED (2026-02-09)

This plan has been fully implemented. See commits:
- `f40f156` - fix: resolve DWSIM solver hang caused by duplicate object creation
- `9970bab` - fix: use lookup instead of AddObject in mappers
- `14ec62a` - refactor: migrate to modern CalculateFlowsheet4() API
```

**Step 2: Commit**

```bash
git add docs/plans/
git commit -m "docs: mark completed plans as done"
```

---

## Expected Outcome

After this cleanup:

**Deleted:**
- `Enerflow.Worker/Convergence/` (entire folder)
- `Enerflow.Worker/Mappers/PostConnectionConfigurator.cs`
- `Enerflow.Worker/Mappers/IPostConnectionConfigurator.cs`
- `ConvergenceConfig` class
- `ConvergenceException` class

**Simplified:**
- `DWSIMSolver.cs` - Cleaner constructor, simpler Solve method
- `ISimulationSolver.cs` - Simpler interface
- `Program.cs` - Fewer DI registrations

**Result:**
- ✅ All tests still pass
- ✅ ~500+ lines of code removed
- ✅ Fewer abstractions to maintain
- ✅ Clearer architecture
- ✅ Better performance (using DWSIM's optimized solver)
