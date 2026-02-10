# Move Mappers to Simulation Layer - Final Architecture Refactor

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Move all Worker mappers to Enerflow.Simulation as factories, establish clear model usage (Domain Entities only), and create a unified connection factory. This is the final refactor for the simulation building/configuration layer.

**Architecture:** Enerflow.Simulation becomes the single source of truth for all DWSIM object creation and configuration. Worker layer becomes pure orchestration with no DWSIM manipulation logic.

**Tech Stack:** C# .NET, DWSIM Automation API, Dependency Injection

---

## Current Problems

1. **Layer Violation**: Worker has DWSIM configuration logic (should be in Simulation layer)
2. **Mixed Models**: Factories accept both DTOs and Domain Entities (confusing responsibility)
3. **Scattered Logic**: Connection logic mixed with unit operation configuration
4. **Naming Inconsistency**: "Mappers" in Worker vs "Factories" in Simulation

---

## Target Architecture

### Model Usage Clarity

**Domain Entities** (`Enerflow.Domain.Entities.*`)
- Purpose: Internal business state, persistence, Worker orchestration
- Always in SI units (Kelvin, Pascal, kg/s)
- Used by: Worker, Simulation layer
- Examples: `MaterialStream`, `HeaterObject`, `Simulation`

**DTOs** (`Enerflow.Domain.DTOs.*`)
- Purpose: API transport, message queue payloads
- Units can vary (SI/CGS/English) based on user input
- Used by: API boundary only
- Examples: `MaterialStreamDto`, `SimulationDefinitionDto`
- **Conversion**: API layer converts DTO → Domain Entity before sending to Worker

**DWSIM Objects** (DWSIM library types)
- Purpose: DWSIM simulation engine objects
- Used by: Simulation layer only (never exposed to Worker)
- Examples: `DWSIM.Thermodynamics.Streams.MaterialStream`, `DWSIM.UnitOperations.UnitOperations.Heater`

### Layer Responsibilities

**Enerflow.Simulation** (Low-level DWSIM expertise)
- Creates DWSIM objects from Domain Entities
- Configures DWSIM properties
- Connects DWSIM objects
- Encapsulates all DWSIM API knowledge

**Enerflow.Worker** (High-level orchestration)
- Receives Domain Entities from queue
- Calls Simulation factories in correct order
- Handles errors and results
- No DWSIM manipulation

---

## Files Affected

### New Files (Create)
- `Enerflow.Simulation/Flowsheet/UnitOperations/IUnitOperationConfigurator.cs`
- `Enerflow.Simulation/Flowsheet/UnitOperations/UnitOperationConfigurator.cs`
- `Enerflow.Simulation/Flowsheet/Connections/IConnectionFactory.cs`
- `Enerflow.Simulation/Flowsheet/Connections/ConnectionFactory.cs`

### Modified Files
- `Enerflow.Simulation/Flowsheet/Streams/IMaterialStreamFactory.cs` (remove DTO overload)
- `Enerflow.Simulation/Flowsheet/Streams/MaterialStreamFactory.cs` (remove DTO overload)
- `Enerflow.Simulation/Flowsheet/Streams/IEnergyStreamFactory.cs` (remove DTO overload)
- `Enerflow.Simulation/Flowsheet/Streams/EnergyStreamFactory.cs` (remove DTO overload)
- `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs` (simplified, no DTO creation)
- `Enerflow.Worker/Solvers/DWSIMSolver.cs` (use new factories)
- `Enerflow.Worker/Program.cs` (update DI registrations)
- `Enerflow.Tests.Functional/IntegrationTestWebAppFactory.cs` (update DI registrations)

### Deleted Files
- `Enerflow.Worker/Mappers/IUnitOperationMapper.cs`
- `Enerflow.Worker/Mappers/UnitOperationMapper.cs`
- `Enerflow.Worker/Mappers/IConnectionMapper.cs`
- `Enerflow.Worker/Mappers/ConnectionMapper.cs`

---

## Task 1: Create UnitOperationConfigurator in Simulation Layer

**Files:**
- Create: `Enerflow.Simulation/Flowsheet/UnitOperations/IUnitOperationConfigurator.cs`
- Create: `Enerflow.Simulation/Flowsheet/UnitOperations/UnitOperationConfigurator.cs`

**Steps:**

**Step 1: Create interface**
- Define `IUnitOperationConfigurator` with single method: `Configure(UnitOperationObject domainEntity, IFlowsheet flowsheet, IReadOnlyDictionary<Guid, string> compoundNames)`
- Takes Domain Entity (not DTO)
- Returns void (configures in-place)

**Step 2: Implement configurator**
- Copy logic from `Enerflow.Worker/Mappers/UnitOperationMapper.cs`
- Rename class to `UnitOperationConfigurator`
- Keep all private methods (MapHeater, MapCooler, etc.)
- Update namespace to `Enerflow.Simulation.Flowsheet.UnitOperations`
- Ensure it only uses Domain Entities

**Step 3: Build and verify**
- Command: `dotnet build Enerflow.Simulation`
- Expected: Clean build

**Step 4: Commit**
```bash
git add Enerflow.Simulation/Flowsheet/UnitOperations/
git commit -m "feat: add UnitOperationConfigurator to Simulation layer"
```

---

## Task 2: Create ConnectionFactory in Simulation Layer

**Files:**
- Create: `Enerflow.Simulation/Flowsheet/Connections/IConnectionFactory.cs`
- Create: `Enerflow.Simulation/Flowsheet/Connections/ConnectionFactory.cs`

**Steps:**

**Step 1: Create interface**
- Define `IConnectionFactory` with method: `ConnectFlowsheet(Simulation domainSimulation, IFlowsheet flowsheet)`
- Takes full Simulation entity (has all streams and units)
- Handles all connection logic

**Step 2: Implement factory**
- Copy logic from `Enerflow.Worker/Mappers/ConnectionMapper.cs`
- Rename class to `ConnectionFactory`
- Keep all private methods (ConnectStreamToUnit, ConfigureSplitterRatios, etc.)
- Update namespace to `Enerflow.Simulation.Flowsheet.Connections`
- Ensure it only uses Domain Entities

**Step 3: Build and verify**
- Command: `dotnet build Enerflow.Simulation`
- Expected: Clean build

**Step 4: Commit**
```bash
git add Enerflow.Simulation/Flowsheet/Connections/
git commit -m "feat: add ConnectionFactory to Simulation layer"
```

---

## Task 3: Remove DTO Overloads from Stream Factories

**Files:**
- Modify: `Enerflow.Simulation/Flowsheet/Streams/IMaterialStreamFactory.cs`
- Modify: `Enerflow.Simulation/Flowsheet/Streams/MaterialStreamFactory.cs`
- Modify: `Enerflow.Simulation/Flowsheet/Streams/IEnergyStreamFactory.cs`
- Modify: `Enerflow.Simulation/Flowsheet/Streams/EnergyStreamFactory.cs`

**Steps:**

**Step 1: Remove DTO methods from MaterialStreamFactory**
- Delete `CreateMaterialStream(MaterialStreamDto, SystemOfUnits)` method
- Delete `Configure(DwsimMaterialStream, MaterialStreamDto, SystemOfUnits)` method
- Delete unit conversion methods (ConvertTemperatureToSI, ConvertPressureToSI, ConvertMassFlowToSI)
- Keep only `Configure(DwsimMaterialStream, DomainMaterialStream, SystemOfUnits)` method
- Update interface to match

**Step 2: Remove DTO methods from EnergyStreamFactory**
- Delete `CreateEnergyStream(EnergyStreamDto)` method
- Delete `Configure(DwsimEnergyStream, EnergyStreamDto)` method
- Keep only `Configure(DwsimEnergyStream, DomainEnergyStream)` method
- Update interface to match

**Step 3: Build and verify**
- Command: `dotnet build Enerflow.Simulation`
- Expected: Clean build (Worker will fail, fixed in next task)

**Step 4: Commit**
```bash
git add Enerflow.Simulation/Flowsheet/Streams/
git commit -m "refactor: remove DTO overloads from stream factories"
```

---

## Task 4: Update Worker to Use New Factories

**Files:**
- Modify: `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs`
- Modify: `Enerflow.Worker/Solvers/DWSIMSolver.cs`
- Modify: `Enerflow.Worker/Program.cs`
- Modify: `Enerflow.Tests.Functional/IntegrationTestWebAppFactory.cs`

**Steps:**

**Step 1: Update DWSIMFlowsheetBuilder**
- Builder already uses Entity-based Configure (no changes needed to stream creation)
- Verify it calls `_materialStreamFactory.Configure(ms, stream, simulation.SystemOfUnits)` with Domain Entity
- Verify it calls `_energyStreamFactory.Configure(es, stream)` with Domain Entity

**Step 2: Update DWSIMSolver**
- Replace `IUnitOperationMapper` with `IUnitOperationConfigurator`
- Replace `IConnectionMapper` with `IConnectionFactory`
- Update constructor parameters
- Update method calls: `_unitOpConfigurator.Configure(...)` and `_connectionFactory.ConnectFlowsheet(...)`

**Step 3: Update DI registrations in Program.cs**
- Remove: `builder.Services.AddScoped<IUnitOperationMapper, UnitOperationMapper>()`
- Remove: `builder.Services.AddScoped<IConnectionMapper, ConnectionMapper>()`
- Add: `builder.Services.AddScoped<IUnitOperationConfigurator, UnitOperationConfigurator>()`
- Add: `builder.Services.AddScoped<IConnectionFactory, ConnectionFactory>()`

**Step 4: Update DI registrations in IntegrationTestWebAppFactory.cs**
- Remove: `services.TryAddScoped<IUnitOperationMapper, UnitOperationMapper>()`
- Remove: `services.TryAddScoped<IConnectionMapper, ConnectionMapper>()`
- Add: `services.TryAddScoped<IUnitOperationConfigurator, UnitOperationConfigurator>()`
- Add: `services.TryAddScoped<IConnectionFactory, ConnectionFactory>()`

**Step 5: Build and test**
- Command: `dotnet build && dotnet test`
- Expected: All tests pass

**Step 6: Commit**
```bash
git add Enerflow.Worker/ Enerflow.Tests.Functional/
git commit -m "refactor: update Worker to use Simulation layer factories"
```

---

## Task 5: Delete Old Worker Mappers

**Files:**
- Delete: `Enerflow.Worker/Mappers/IUnitOperationMapper.cs`
- Delete: `Enerflow.Worker/Mappers/UnitOperationMapper.cs`
- Delete: `Enerflow.Worker/Mappers/IConnectionMapper.cs`
- Delete: `Enerflow.Worker/Mappers/ConnectionMapper.cs`

**Steps:**

**Step 1: Verify no remaining references**
- Command: `rg "IUnitOperationMapper|UnitOperationMapper|IConnectionMapper|ConnectionMapper" --type cs`
- Expected: No matches (except in git history)

**Step 2: Delete mapper files**
- Remove all four files

**Step 3: Build and test**
- Command: `dotnet build && dotnet test`
- Expected: All tests pass

**Step 4: Commit**
```bash
git add -A
git commit -m "refactor: remove obsolete Worker mappers"
```

---

## Task 6: Final Verification and Documentation

**Files:**
- Test: All test suites
- Document: Architecture decision

**Steps:**

**Step 1: Run full test suite**
- Command: `dotnet test`
- Expected: All tests pass

**Step 2: Verify layer boundaries**
- Simulation layer: Only references Domain (no Worker references)
- Worker layer: References Simulation and Domain (no DWSIM manipulation)
- Domain layer: No dependencies on Simulation or Worker

**Step 3: Verify model usage**
- Factories only accept Domain Entities (no DTOs)
- Worker only passes Domain Entities to factories
- DTOs only used at API boundary (not in this refactor scope)

**Step 4: Commit**
```bash
git add -A
git commit -m "docs: verify final architecture boundaries"
```

---

## Verification Checklist

- [ ] UnitOperationConfigurator created in Simulation layer
- [ ] ConnectionFactory created in Simulation layer
- [ ] Stream factories only accept Domain Entities (no DTOs)
- [ ] Worker uses new factories from Simulation layer
- [ ] Old Worker mappers deleted
- [ ] DI registrations updated
- [ ] All tests pass
- [ ] Layer boundaries respected (Simulation → Domain only)
- [ ] No DWSIM manipulation in Worker layer

---

## Success Criteria

1. **Clear Layer Separation**: Simulation layer encapsulates all DWSIM logic
2. **Single Model Type**: Factories only accept Domain Entities (no DTO confusion)
3. **Consistent Naming**: "Factories" and "Configurators" (not "Mappers")
4. **Code Reduction**: ~540 lines moved from Worker to Simulation
5. **Maintainability**: Future DWSIM changes only affect Simulation layer
6. **Tests Pass**: All existing tests pass without modification

---

## Architecture Benefits

### Before (Current)
```
API (DTOs) → Worker (Mappers + DTOs + Entities) → DWSIM
                ↓
           Simulation (Factories + DTOs + Entities)
```
**Problems**: Mixed models, scattered DWSIM logic, layer violations

### After (Target)
```
API (DTOs) → Worker (Entities only) → Simulation (Entities → DWSIM)
```
**Benefits**: Clear boundaries, single model type per layer, encapsulated DWSIM logic

---

## Future Considerations

**API Layer DTO Conversion** (Not in this refactor)
- API should convert DTOs → Domain Entities before sending to Worker
- Worker should never see DTOs
- Requires API layer changes (separate task)

**Unit Conversion** (Already handled)
- Domain Entities are always SI units
- Factories assume SI input (no conversion needed)
- DTO → Entity conversion (in API) handles unit conversion

**Energy Stream Connections** (Future enhancement)
- Current implementation only connects material streams
- Energy stream connections (for heaters with energy stream mode) need separate logic
- Domain model may need `EnergyInputId`/`EnergyOutputId` properties
