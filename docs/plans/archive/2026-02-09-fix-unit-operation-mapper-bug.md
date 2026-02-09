# Fix UnitOperationMapper Duplicate Object Bug (TODO #2)

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix critical bug where unit operations are created twice but only the unconfigured duplicates are used.

**Architecture:** Change `UnitOperationMapper` to use `flowsheet.GetObject()` instead of `flowsheet.AddObject()` to retrieve and configure the objects already created by `DWSIMFlowsheetBuilder`.

**Tech Stack:** C#, DWSIM API, xUnit

---

## Task 1: Write Failing Test

**Files:**
- Create: `Enerflow.Tests.Unit/Mappers/UnitOperationMapperTests.cs`

**Step 1: Write test that verifies configuration is applied**

```csharp
using DWSIM.Automation;
using DWSIM.UnitOperations.UnitOperations;
using Enerflow.Domain.Entities.UnitOperations;
using Enerflow.Domain.Enums;
using Enerflow.Worker.Mappers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enerflow.Tests.Unit.Mappers;

public class UnitOperationMapperTests
{
    [Fact]
    public void Map_HeaterWithOutletTemp_ConfiguresExistingObject()
    {
        // Arrange
        var automation = new Automation3();
        var flowsheet = automation.CreateFlowsheet();
        
        // Simulate what DWSIMFlowsheetBuilder does: create object first
        var createdObj = flowsheet.AddObject(DWSIM.Interfaces.Enums.GraphicObjects.ObjectType.Heater, 0, 0, "H-101");
        
        var domainHeater = new HeaterObject
        {
            Name = "H-101",
            CalcMode = HeaterCalculationMode.OutletTemperature,
            OutletTemperature = 400.0,
            Efficiency = 0.85,
            PressureDrop = 5000.0
        };
        
        var mapper = new UnitOperationMapper(NullLogger<UnitOperationMapper>.Instance);
        
        // Act
        mapper.Map(domainHeater, flowsheet, new Dictionary<Guid, string>());
        
        // Assert
        var heater = (Heater)flowsheet.GetObject("H-101");
        Assert.Equal(Heater.CalculationMode.OutletTemperature, heater.CalcMode);
        Assert.Equal(400.0, heater.OutletTemperature);
        Assert.Equal(85.0, heater.Efficiency); // Converted to %
        Assert.Equal(5000.0, heater.PressureDrop);
        
        // Verify no duplicate objects created
        Assert.Single(flowsheet.SimulationObjects.Values.Where(o => o.GraphicObject.Tag == "H-101"));
    }
}
```

**Step 2: Run test**

```bash
dotnet test Enerflow.Tests.Unit/Enerflow.Tests.Unit.csproj --filter "FullyQualifiedName~UnitOperationMapperTests.Map_HeaterWithOutletTemp_ConfiguresExistingObject" -v n
```

Expected: FAIL - Test should fail because mapper creates duplicate object

**Step 3: Commit**

```bash
git add Enerflow.Tests.Unit/Mappers/UnitOperationMapperTests.cs
git commit -m "test: add failing test for unit operation mapper duplicate bug"
```

---

## Task 2: Fix UnitOperationMapper

**Files:**
- Modify: `Enerflow.Worker/Mappers/UnitOperationMapper.cs:55-262`

**Step 1: Replace AddObject with GetObject in all Map methods**

Change line 57 from:
```csharp
var obj = flowsheet.AddObject(ObjectType.Heater, 0, 0, domainHeater.Name);
```

To:
```csharp
var obj = flowsheet.GetObject(domainHeater.Name);
if (obj == null)
{
    _logger.LogError("Heater {Name} not found in flowsheet. Object must be created by builder first.", domainHeater.Name);
    return;
}
```

Apply same pattern to:
- Line 85: `MapCooler`
- Line 113: `MapValve`
- Line 137: `MapMixer` (remove AddObject call entirely, just GetObject)
- Line 142: `MapSplitter`
- Line 157: `MapFlashDrum`
- Line 185: `MapShortcutColumn`
- Line 220: `MapRecycle`

**Step 2: Run test**

```bash
dotnet test Enerflow.Tests.Unit/Enerflow.Tests.Unit.csproj --filter "FullyQualifiedName~UnitOperationMapperTests" -v n
```

Expected: PASS

**Step 3: Commit**

```bash
git add Enerflow.Worker/Mappers/UnitOperationMapper.cs
git commit -m "fix: use GetObject instead of AddObject to configure existing unit operations"
```

---

## Task 3: Add Tests for Other Unit Types

**Files:**
- Modify: `Enerflow.Tests.Unit/Mappers/UnitOperationMapperTests.cs`

**Step 1: Add tests for Cooler, Valve, Splitter**

```csharp
[Fact]
public void Map_Valve_ConfiguresExistingObject()
{
    var automation = new Automation3();
    var flowsheet = automation.CreateFlowsheet();
    flowsheet.AddObject(DWSIM.Interfaces.Enums.GraphicObjects.ObjectType.Valve, 0, 0, "V-101");
    
    var domainValve = new ValveObject
    {
        Name = "V-101",
        CalcMode = ValveCalculationMode.OutletPressure,
        OutletPressure = 101325.0
    };
    
    var mapper = new UnitOperationMapper(NullLogger<UnitOperationMapper>.Instance);
    mapper.Map(domainValve, flowsheet, new Dictionary<Guid, string>());
    
    var valve = (DWSIM.UnitOperations.UnitOperations.Valve)flowsheet.GetObject("V-101");
    Assert.Equal(DWSIM.UnitOperations.UnitOperations.Valve.CalculationMode.OutletPressure, valve.CalcMode);
    Assert.Equal(101325.0, valve.OutletPressure);
}
```

**Step 2: Run all tests**

```bash
dotnet test Enerflow.Tests.Unit/Enerflow.Tests.Unit.csproj --filter "FullyQualifiedName~UnitOperationMapperTests" -v n
```

Expected: PASS

**Step 3: Commit**

```bash
git add Enerflow.Tests.Unit/Mappers/UnitOperationMapperTests.cs
git commit -m "test: add coverage for valve and other unit operation types"
```

---

## Task 4: Integration Test

**Files:**
- Create: `Enerflow.Tests.Integration/Worker/UnitOperationConfigurationTests.cs`

**Step 1: Write end-to-end test**

```csharp
[Fact]
public async Task BuildAndSolve_HeaterWithConfig_AppliesConfiguration()
{
    // Create simulation with heater
    // Build flowsheet
    // Verify heater configuration is applied
    // Run solver
    // Verify results reflect configured parameters
}
```

**Step 2: Run integration test**

```bash
dotnet test Enerflow.Tests.Integration --filter "FullyQualifiedName~UnitOperationConfigurationTests" -v n
```

Expected: PASS

**Step 3: Commit**

```bash
git add Enerflow.Tests.Integration/Worker/UnitOperationConfigurationTests.cs
git commit -m "test: add integration test for unit operation configuration"
```
