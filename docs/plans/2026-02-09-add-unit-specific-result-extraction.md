# Add Unit-Specific Result Extraction (TODO #1)

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Extract unit-specific calculated properties (duty, efficiency, split ratios) instead of just generic status.

**Architecture:** Add type-specific extraction logic in `ResultCollector` using pattern matching on DWSIM unit operation types.

**Tech Stack:** C#, DWSIM API, xUnit

---

## Task 1: Document Available Properties

**Files:**
- Create: `docs/DWSIM/UNIT_OPERATION_RESULTS.md`

**Step 1: Research and document extractable properties**

Reference `docs/DWSIM/DWSIM_API_MAP.md` and DWSIM source to list:
- Heater: `DeltaQ`, `DeltaT`, `Efficiency`
- Cooler: `DeltaQ`, `DeltaT`, `Efficiency`
- Valve: `DeltaP`, `OutletPressure`
- Splitter: `Ratios` (array)
- Mixer: (no specific properties)
- Vessel: `VaporFraction`, `LiquidFraction`
- ShortcutColumn: `NumberOfStages`, `FeedStage`, `RefluxRatio`
- Recycle: `IterationCount`, `ConvergenceStatus`

**Step 2: Commit**

```bash
git add docs/DWSIM/UNIT_OPERATION_RESULTS.md
git commit -m "docs: document extractable unit operation result properties"
```

---

## Task 2: Write Failing Tests

**Files:**
- Create: `Enerflow.Tests.Unit/Solvers/ResultCollectorTests.cs`

**Step 1: Write test for heater result extraction**

```csharp
[Fact]
public void ExtractResults_Heater_ExtractsDutyAndEfficiency()
{
    // Arrange: Create flowsheet with calculated heater
    var automation = new Automation3();
    var flowsheet = automation.CreateFlowsheet();
    var heater = (Heater)flowsheet.AddObject(ObjectType.Heater, 0, 0, "H-101");
    heater.DeltaQ = 1000.0; // kW
    heater.Efficiency = 85.0;
    heater.Calculated = true;
    
    var simulation = CreateSimulationWithHeater("H-101");
    var result = new SimulationResult { UnitResults = new List<UnitResultDto>() };
    
    var collector = new ResultCollector(NullLogger<ResultCollector>.Instance);
    
    // Act
    collector.ExtractResults(flowsheet, simulation, result);
    
    // Assert
    var unitResult = result.UnitResults.First();
    var calculatedParams = JsonSerializer.Deserialize<Dictionary<string, object>>(unitResult.CalculatedParams);
    Assert.Equal(1000.0, calculatedParams["DeltaQ"]);
    Assert.Equal(85.0, calculatedParams["Efficiency"]);
}
```

**Step 2: Run test**

```bash
dotnet test Enerflow.Tests.Unit --filter "FullyQualifiedName~ResultCollectorTests" -v n
```

Expected: FAIL

**Step 3: Commit**

```bash
git add Enerflow.Tests.Unit/Solvers/ResultCollectorTests.cs
git commit -m "test: add failing tests for unit-specific result extraction"
```

---

## Task 3: Implement Type-Specific Extraction

**Files:**
- Modify: `Enerflow.Worker/Solvers/ResultCollector.cs:54-86`

**Step 1: Replace generic extraction with type-specific logic**

Replace lines 59-78 with:

```csharp
var calculatedParams = new Dictionary<string, object>
{
    ["Calculated"] = simObj.Calculated
};

if (!string.IsNullOrEmpty(simObj.ErrorMessage))
{
    calculatedParams["Error"] = simObj.ErrorMessage;
}

// Type-specific extraction
switch (simObj)
{
    case Heater heater:
        calculatedParams["DeltaQ"] = heater.DeltaQ ?? 0.0;
        calculatedParams["DeltaT"] = heater.DeltaT ?? 0.0;
        calculatedParams["Efficiency"] = heater.Efficiency;
        break;
    
    case Cooler cooler:
        calculatedParams["DeltaQ"] = cooler.DeltaQ ?? 0.0;
        calculatedParams["DeltaT"] = cooler.DeltaT ?? 0.0;
        calculatedParams["Efficiency"] = cooler.Efficiency;
        break;
    
    case Valve valve:
        calculatedParams["DeltaP"] = valve.DeltaP ?? 0.0;
        calculatedParams["OutletPressure"] = valve.OutletPressure ?? 0.0;
        break;
    
    case Splitter splitter:
        calculatedParams["Ratios"] = splitter.Ratios;
        break;
    
    case Vessel vessel:
        calculatedParams["VaporFraction"] = vessel.VaporFraction ?? 0.0;
        calculatedParams["LiquidFraction"] = vessel.LiquidFraction ?? 0.0;
        break;
    
    case ShortcutColumn column:
        calculatedParams["NumberOfStages"] = column.m_stages;
        calculatedParams["FeedStage"] = column.m_feedstage;
        break;
    
    case Recycle recycle:
        calculatedParams["IterationCount"] = recycle.IterationCount;
        calculatedParams["Converged"] = recycle.Converged;
        break;
    
    case DWSIMEnergyStream es:
        calculatedParams["EnergyFlow"] = es.EnergyFlow ?? 0.0;
        break;
}
```

**Step 2: Add using statements**

Add at top:
```csharp
using DWSIM.UnitOperations.UnitOperations;
using DWSIM.UnitOperations.SpecialOps;
```

**Step 3: Run tests**

```bash
dotnet test Enerflow.Tests.Unit --filter "FullyQualifiedName~ResultCollectorTests" -v n
```

Expected: PASS

**Step 4: Commit**

```bash
git add Enerflow.Worker/Solvers/ResultCollector.cs
git commit -m "feat: add type-specific result extraction for unit operations"
```

---

## Task 4: Add Tests for All Unit Types

**Files:**
- Modify: `Enerflow.Tests.Unit/Solvers/ResultCollectorTests.cs`

**Step 1: Add tests for Valve, Splitter, Vessel**

```csharp
[Fact]
public void ExtractResults_Splitter_ExtractsRatios() { /* ... */ }

[Fact]
public void ExtractResults_Vessel_ExtractsVaporFraction() { /* ... */ }
```

**Step 2: Run all tests**

```bash
dotnet test Enerflow.Tests.Unit --filter "FullyQualifiedName~ResultCollectorTests" -v n
```

Expected: PASS

**Step 3: Commit**

```bash
git add Enerflow.Tests.Unit/Solvers/ResultCollectorTests.cs
git commit -m "test: add comprehensive coverage for all unit operation result types"
```
