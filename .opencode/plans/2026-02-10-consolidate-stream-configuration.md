# Consolidate Stream Configuration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Eliminate redundant stream configuration by moving Mapper logic into Factory, making Factory the single source of truth for all DWSIM stream configuration.

**Architecture:** Factory (Enerflow.Simulation) handles all DWSIM stream configuration from both DTOs and Domain Entities. Builder (Enerflow.Worker) orchestrates by calling Factory methods. Mapper becomes obsolete.

**Tech Stack:** C# .NET, DWSIM Automation API, Dependency Injection

---

## Problem Analysis

**Current Flow (Redundant):**
```
Builder: Create stream → Configure via Factory (DTO)
Solver: Find same stream → Re-configure via Mapper (Entity)
```

**Target Flow (Clean):**
```
Builder: Create stream → Configure via Factory (Entity)
Solver: Use pre-configured streams
```

**Key Principle:** Factory encapsulates all DWSIM property manipulation. Builder remains high-level orchestration.

**Files Affected:**
- Modify: `Enerflow.Simulation/Flowsheet/Streams/IMaterialStreamFactory.cs` (add Entity overloads)
- Modify: `Enerflow.Simulation/Flowsheet/Streams/MaterialStreamFactory.cs` (add Entity configuration)
- Modify: `Enerflow.Simulation/Flowsheet/Streams/IEnergyStreamFactory.cs` (add Entity overloads)
- Modify: `Enerflow.Simulation/Flowsheet/Streams/EnergyStreamFactory.cs` (add Entity configuration)
- Modify: `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs` (use Entity overloads)
- Delete: `Enerflow.Worker/Mappers/StreamPropertiesMapper.cs`
- Delete: `Enerflow.Worker/Mappers/IStreamPropertiesMapper.cs`
- Modify: `Enerflow.Worker/Solvers/DWSIMSolver.cs` (remove Mapper calls)
- Modify: `Enerflow.Worker/Program.cs` (remove Mapper DI)
- Modify: `Enerflow.Tests.Functional/IntegrationTestWebAppFactory.cs` (remove Mapper DI)

---

## Task 1: Add Entity Configuration to MaterialStreamFactory

**Files:**
- Modify: `Enerflow.Simulation/Flowsheet/Streams/IMaterialStreamFactory.cs`
- Modify: `Enerflow.Simulation/Flowsheet/Streams/MaterialStreamFactory.cs`

**Steps:**

**Step 1: Add interface method for Entity configuration**
- Add `Configure(MaterialStream dwsimStream, Domain.Entities.Streams.MaterialStream entity)` to interface

**Step 2: Implement Entity configuration in Factory**
- Copy logic from StreamPropertiesMapper.MapMaterialStream()
- Set temperature, pressure, massflow from entity properties (assume SI units)
- Iterate composition and set MoleFraction
- Add logging and error handling

**Step 3: Run tests**
- Command: `dotnet test --filter "FullyQualifiedName~MaterialStreamFactory"`

**Step 4: Commit**
```bash
git add Enerflow.Simulation/Flowsheet/Streams/
git commit -m "feat: add Entity configuration to MaterialStreamFactory"
```
Builder: Create stream → Configure via Factory (DTO)
Solver: Find same stream → Re-configure via Mapper (Entity)
```

**Target Flow (Clean):**
```
Builder: Create stream → Configure directly (Entity)
Solver: Use pre-configured streams
```

**Files Affected:**
- Modify: `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs`
- Delete: `Enerflow.Worker/Mappers/StreamPropertiesMapper.cs`
- Delete: `Enerflow.Worker/Mappers/IStreamPropertiesMapper.cs`
- Modify: `Enerflow.Worker/Solvers/DWSIMSolver.cs`
- Modify: `Enerflow.Worker/Program.cs` (DI registration)
- Modify: `Enerflow.Tests.Functional/IntegrationTestWebAppFactory.cs` (DI registration)
- Review: `Enerflow.Simulation/Flowsheet/Streams/MaterialStreamFactory.cs` (may deprecate later)
- Review: `Enerflow.Simulation/Flowsheet/Streams/IMaterialStreamFactory.cs` (may deprecate later)

---

## Task 1: Update Builder to Configure Streams Directly

**Files:**
- Modify: `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs:82-136`

**Steps:**

**Step 1: Remove Factory dependency from Builder constructor**
- Remove `IMaterialStreamFactory` and `IEnergyStreamFactory` from constructor parameters
- Remove corresponding private fields

**Step 2: Update Material Stream creation logic**
- In `BuildFlowsheet()` method, replace Factory.Configure() call with direct property assignment
- Set `temperature`, `pressure`, `massflow` directly from `stream.Temperature`, `stream.Pressure`, `stream.MassFlow`
- Iterate `stream.Composition` and set `MoleFraction` for each compound
- Add null/bounds checking and logging

**Step 3: Update Energy Stream creation logic**
- Replace Factory.Configure() call with direct `EnergyFlow` assignment
- Add logging

**Step 4: Run existing Builder tests**
- Command: `dotnet test --filter "FullyQualifiedName~FlowsheetBuilder"`
- Expected: Tests should still pass (Builder already had configuration logic via Factory)

**Step 5: Commit**
```bash
git add Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs
git commit -m "refactor: configure streams directly in Builder without Factory"
```

---

## Task 2: Add Entity Configuration to EnergyStreamFactory

**Files:**
- Modify: `Enerflow.Simulation/Flowsheet/Streams/IEnergyStreamFactory.cs`
- Modify: `Enerflow.Simulation/Flowsheet/Streams/EnergyStreamFactory.cs`

**Steps:**

**Step 1: Add interface method for Entity configuration**
- Add `Configure(EnergyStream dwsimStream, Domain.Entities.Streams.EnergyStream entity)` to interface

**Step 2: Implement Entity configuration in Factory**
- Copy logic from StreamPropertiesMapper.MapEnergyStream()
- Set EnergyFlow from entity property

**Step 3: Run tests**
- Command: `dotnet test --filter "FullyQualifiedName~EnergyStreamFactory"`

**Step 4: Commit**
```bash
git add Enerflow.Simulation/Flowsheet/Streams/
git commit -m "feat: add Entity configuration to EnergyStreamFactory"
```

---

## Task 3: Update Builder to Use Entity Configuration

**Files:**
- Modify: `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs:93-136`

**Steps:**

**Step 1: Replace DTO-based configuration with Entity-based**
- For Material Streams: Call `_materialStreamFactory.Configure(ms, stream)` instead of creating DTO
- For Energy Streams: Call `_energyStreamFactory.Configure(es, stream)` instead of creating DTO
- Remove DTO creation logic

**Step 2: Run Builder tests**
- Command: `dotnet test --filter "FullyQualifiedName~FlowsheetBuilder"`

**Step 3: Commit**
```bash
git add Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs
git commit -m "refactor: use Factory Entity configuration in Builder"
```

---

## Task 4: Remove Mapper from Solver and Delete Mapper Classes

**Files:**
- Modify: `Enerflow.Worker/Solvers/DWSIMSolver.cs`
- Delete: `Enerflow.Worker/Mappers/StreamPropertiesMapper.cs`
- Delete: `Enerflow.Worker/Mappers/IStreamPropertiesMapper.cs`

**Steps:**

**Step 1: Remove Mapper from Solver**
- Remove `IStreamPropertiesMapper` dependency from constructor
- Delete stream mapping loops (lines 50-67)

**Step 2: Delete Mapper files**
- Remove both Mapper interface and implementation

**Step 3: Run tests**
- Command: `dotnet test`

**Step 4: Commit**
```bash
git add -A
git commit -m "refactor: remove obsolete StreamPropertiesMapper"
```

---

## Task 5: Update DI Registrations and Final Verification

**Files:**
- Modify: `Enerflow.Worker/Program.cs`
- Modify: `Enerflow.Tests.Functional/IntegrationTestWebAppFactory.cs`

**Steps:**

**Step 1: Remove Mapper DI registrations**
- Delete `IStreamPropertiesMapper` registration from both files

**Step 2: Build and test**
- Command: `dotnet build && dotnet test`

**Step 3: Commit**
```bash
git add Enerflow.Worker/Program.csflow.Tests.Functional/IntegrationTestWebAppFactory.cs
git commit -m "refactor: remove StreamPropertiesMapper DI registrations"
```

---

## Verification Checklist

- [ ] Factory has Entity configuration methods
- [ ] Builder uses Factory Entity methods
- [ ] Solver no longer calls Mapper
- [ ] Mapper classes deleted
- [ ] DI registrations updated
- [ ] All tests pass
- [ ] Simulation workflow works end-to-end

---

## Success Criteria

1. **Single Source of Truth:** Factory handles all DWSIM stream configuration
2. **Layer Separation:** Worker doesn't manipulate DWSIs directly
3. **Code Reduction:** ~150 lines of redundant code removed
4. **Maintainability:** Clear responsibility boundaries between Factory and Builder
