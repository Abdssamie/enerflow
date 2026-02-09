# Architecture Cleanup & Legacy Removal

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Remove the legacy "Zombie" SimulationService and cleanup architecture to enforce the Worker Pipeline pattern.

**Context:**
The scan revealed that `Enerflow.Simulation/Services/SimulationService.cs` is a legacy monolithic implementation that conflicts with the modern `Enerflow.Worker` architecture. It contains the "stub" mass balance check (TODO #3). Deleting this file solves the TODO by removing the dead code.

**Tech Stack:** C#

---

## Task 1: Remove Legacy Simulation Service

**Files:**
- Delete: `Enerflow.Simulation/Services/SimulationService.cs`
- Verify: `Enerflow.Simulation/Enerflow.Simulation.csproj` (Check for dangling references)

**Step 1: Delete the file**
```bash
rm Enerflow.Simulation/Services/SimulationService.cs
```

**Step 2: Verify Build**
Ensure that removing this class doesn't break the build (checking for references in API or Tests).
```bash
dotnet build Enerflow.Simulation
dotnet build Enerflow.API
```

**Step 3: Commit**
```bash
git add .
git commit -m "chore: remove legacy SimulationService to enforce Worker architecture"
```

---

## Task 2: Remove Redundant Wiring Logic from Builder

**Files:**
- Modify: `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs`

**Context:**
The Builder is currently creating connections (Lines 155-187) *AND* the `ConnectionMapper` is also creating connections. This double-wiring is dangerous. The Builder should only *create* objects. The `ConnectionMapper` should connect them.

**Step 1: Remove connection logic**
Remove the loop in `BuildFlowsheet` that calls `flowsheet.ConnectObjects`.

**Step 2: Verify Tests**
Run unit tests to ensure no regression (though tests might fail if they relied on this side effect, they should be relying on ConnectionMapper).

```bash
dotnet test Enerflow.Tests.Unit
```

**Step 3: Commit**
```bash
git add Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs
git commit -m "refactor: remove redundant connection logic from flowsheet builder"
```
