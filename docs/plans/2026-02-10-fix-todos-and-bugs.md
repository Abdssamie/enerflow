# Fix TODOs and Bugs Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task.

**Goal:** Resolve all remaining TODOs and critical bugs in the solver pipeline (stale TODO removal, basic result extraction, energy stream wiring, mass balance validation).

**Architecture:** Clean up technical debt, implement missing features in existing components without architectural changes.

**Tech Stack:** C# (.NET 10), DWSIM API, xUnit

---

## Task 1: Remove Stale TODO in DWSIMFlowsheetBuilder

**Files:**
- Modify: `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs:150-152`

**Context:** The TODO at line 150 says "Configure unit operation parameters based on unit.Type" but `UnitOperationMapper.cs` already handles this responsibility. The Builder's job is to CREATE objects, the Mapper's job is to CONFIGURE them. This TODO is stale and misleading.

**Step 1: Remove the TODO comment**

Delete lines 150-151:
```csharp
// TODO: Configure unit operation parameters based on unit.Type
// This would require extracting config from the entity or having a separate config step
```

**Step 2: Add clarifying comment**

Replace with:
```csharp
// Note: Unit operation parameters are configured by UnitOperationMapper after creation
```

**Step 3: Verify tests still pass**

Run: `dotnet test Enerflow.Tests.DWSIM && dotnet test Enerflow.Tests.Functional`
Expected: All tests PASS (no behavior change)

**Step 4: Commit**

```bash
git add Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs
git commit -m "refactor: remove stale TODO in DWSIMFlowsheetBuilder

The TODO suggested configuring unit parameters in the Builder, but this
is already handled by UnitOperationMapper. Clarified the separation of
concerns: Builder creates, Mapper configures."
```

---

## Task 2: Implement Basic Unit-Specific Result Extraction

**Files:**
- Modify: `Enerflow.Worker/Solvers/ResultCollector.cs:56-96`

**Context:** The TODO at line 82 says "Add more specific property extraction based on Unit Types if needed." Currently only generic `Calculated` and `ErrorMessage` are extracted. We need to extract key calculated properties for each unit type.

**Step 1: Add using statements**

Add after line 7:
```csharp
using DWSIM.UnitOperations.UnitOperations;
using DWSIM.UnitOperations.SpecialOps;
```

**Step 2: Replace generic extraction with type-specific logic**

Replace lines 63-82 with:
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
                        calculatedParams["DeltaQ"] = heater.DeltaQ.GetValueOrDefault();
                        calculatedParams["DeltaT"] = heater.DeltaT.GetValueOrDefault();
                        calculatedParams["OutletTemperature"] = heater.OutletTemperature.GetValueOrDefault();
                        calculatedParams["Efficiency"] = heater.Efficiency;
                        break;
                    
                     Cooler cooler:
                        calculatedParams["DeltaQ"] = cooler.DeltaQ.GetValueOrDefault();
                        calculatedParams["DeltaT"] = cooler.DeltaT.GetValueOrDefault();
                        calculatedParams["OutletTemperature"] = cooler.OutletTemperature.GetValueOrDefault();
                        calculatedParams["Efficiency"] = cooler.Efficiency;
                        break;
                    
                    case Valve valve:
                        calculatedParams["DeltaP"] = valve.DeltaP.GetValueOrDefault();
                        calculatedPautletPressure"] = valve.OutletPressure.GetValueOrDefault();
                        calculatedParams["DeltaT"] = valve.DeltaT.GetValueOrDefault();
                        break;
                    
                    case Splitter splitter:
                        var ratios = new List<double>();
                        foreach (double ratio in splitter.Ratios)
                        {
                            ratios.Add(ratio);
                        }
                        calculatedParams["Ratios"] = ratios;
                        break;
                    
                    case Vessel vessel:
                        calculatedParams["DeltaQ"] = vessel.DeltaQ.GetValurDefault();
                        break;
                    
                    case DWSIMEnergyStream es:
                        calculatedParams["EnergyFlow"] = es.EnergyFlow.GetValueOrDefault();
                        break;
                }
```

**Step 3: Run existing tests**

Run: `dotnet test Enerflow.Tests.DWSIM && dotnet test Enerflow.Tests.Functional`
Expected: All tests PASS (backward compatible - still extracts Calculated and Error)

**Step 4: Commit**

```bash
git add Enerflow.Worker/Solvers/ResultCollector.cs
git commit -m "feat: add type-specific result extraction for unit operations

Resolves TODO in ResultCollector by extracting calculated properties:
- Heater/Cooler: DeltaQ, DeltaT, OutletTemperature, Efficiency
- Valve: DeltaP, OutletPressure, DeltaT
- Splitter: Ratios array
- Vessel: DeltaQ
- EnergyStream: EnergyFlow

Maintains backward compatibility with existing generic extraction."
```

---

## Task 3: Implement Energy Stream Connections

**Files:**
- Modify: `Enerflow.Worker/Mappers/ConnectionMapper.cs`

**Context:** Energy streams are created but never connected to unit operations. Heaters and Coolers need energy stream connections for proper energy balance trackintep 1: Read ConnectionMapper to understand current structure**

Run: `cat Enerflow.Worker/Mappers/ConnectionMapper.cs`

**Step 2: Add energy stream connection logic**

After the material stream connection loop (after `ConfigureSplitterRatios` call), add:

```csharp
        // Connect Energy Streams to Unit Operations
        _logger.LogDebug("Connecting energy streams to unit operations");
        
        foreach (var energyStream in simulation.EnergyStreams)
        {
            var connection = simulation.Connections.FirstOrDefault(c => 
                c.FromStreamId == energyStream.Id || c.ToStreamId == energyStream.Id);
            
            if (connection == null)
            {
                _logger.LogWarning("Energy stream {Name} (ID: {Id}) has no connections", 
                    energyStream.Name, energyStream.Id);
                continue;
            }
            
            var streamId = energyStream.Id.ToString();
            var streamObj = flowsheet.SimulationObjects[streamId];
            
            // Determine if this is an inlet or outlet energy stream
         ection.FromStreamId == energyStream.Id)
            {
                // Energy stream is an outlet (from unit operation)
                var unitId = connection.FromUnitId.ToString();
                var unitObj = flowsheet.SimulationObjects[unitId];
                
                // Energy connector is typically index 1 for heaters/coolers
                flowsheet.ConnectObjects(unitObj, streamObj, 0, 0);
                _logger.LogDebug("Connected energy stream {Stream} as outlet from unit {Unit}", 
                    energyStream.Name, connection.FromUnitId);
            }
            else
            {
                // Energy stream is an inlet (to unit operation)
                var unitId = connection.ToUnitId.ToString();
                var unitObj = flowsheet.SimulationObjects[unitId];
                
                // Energy connector is typically index 1 for heaters/coolers
                flowsheet.ConnectObjects(streamObj, unitObj, 0, 1);
                _logger.LogDebug("Connected energy stream {Stream} as inlet to unit {Unit}", 
                    energyStream.Name, connection.ToUnitId);
            }
        }
```

**Step 3: Run tests**

Run: `dotnet test EnerflowSIM && dotnet test Enerflow.Tests.Functional`
Expected: All tests PASS (no energy streams in current tests, so no behavior change)

**Step 4: Commit**

```bash
git add Enerflow.Worker/Mappers/ConnectionMapper.cs
git commit -m "feat: implement energy stream connections in ConnectionMapper

Energy streams are now properly connected to unit operations (heaters,
coolers) for energy balance tracking. Uses energy connector port (index 1)
for unit operations."
```

---

## Task 4: Add Post-Solve Mass Balance Validation

**Files:**
- Create: `Enerflow.Worker/Validation/MassBalanceValidator.cs`
- Create: `Enerflow.Worker/Validation/IMassBalanceValidator.cs`
- Modify: `Enerflow.Worker/Solvers/DWSIMSolver.cs`
- Modify: `Enerflow.Worker/Program.cs`

**Context:** Need rigorous mass balance check after solve to verify thermodynamic accuracy. This validates that mass in = mass out for the entire flowsheet.

**Step 1: Create IMassBalanceValidator interface**

Create `Enerflow.Worker/Validation/IMassBalanceValidator.cs`:
```csharp
namespace Enerflow.Worker.Validation;

public interface IMassBalanceValidator
{
    /// <summary>
    /// Validates mass balance for the flowsheet after solving.
    /// </summary>
    /// <param name="flowsheet">The solved DWSIM flowsheet</param>
    /// <param name="tolerance">Relative tolerance for mass balance (default 0.01 = 1%)</param>
    /// <returns>True if mass balance is satisfied within tolerance</returns>
    bool ValidateMassBalance(DWSIM.Interfaces.IFlowsheet flowsheet, double tolerance = 0.01);
    
    /// <summary>
    /// Gets the last validation error message if validation failed.
    /// </summary>
    string? LastErrorMessage { get; }
}
```tep 2: Implement MassBalanceValidator**

Create `Enerflow.Worker/Validation/MassBalanceValidator.cs`:
```csharp
using DWSIM.Interfaces;
using Microsoft.Extensions.Logging;

namespace Enerflow.Worker.Validation;

public class MassBalanceValidator : IMassBalanceValidator
{
    private readonly ILogger<MassBalanceValidator> _logger;
    
    public string? LastErrorMessage { get; private set; }
    
    public MassBalanceValidator(ILogger<MassBalanceValidator> logger)
    {
        _logger = logger;
    }
    
    public bool ValidateMassBalance(IFlowsheet flowsheet, double tolerance = 0.01)
    {
        LastErrorMessage = null;     
        double totalInletMass = 0.0;
        double totalOutletMass = 0.0;
        
        // Sum all inlet material streams (streams with no upstream unit)
        foreach (var obj in flowsheet.SimulationObjects.Values)
        {
            if (obj is DWSIM.Thermodynamics.Streams.MaterialStream ms)
            {
                var graphicObj = ms.GraphicObject;
                
                // Check if this is an inlet stream (no upstream connection)
                if (graphicObj.InputConnectors[0].IsAttached == false)
          {
                    var massFlow = ms.GetMassFlow();
                    totalInletMass += massFlow;
                    _logger.LogDebug("Inlet stream {Name}: {MassFlow} kg/s", ms.GraphicObject.Tag, massFlow);
                }
                
                // Check if this is an outlet stream (no downstream connection)
                if (graphicObj.OutputConnectors[0].IsAttached == false)
                {
                    var massFlow = ms.GetMassFlow();
                    totalOutletMass += massFlow;
                    _logger.LogDebug("Outlet stream {Name}: {MassFlow} kg/s", ms.GraphicObject.Tag, massFlow);
                      }
        }
        
        // Calculate relative error
        double averageMass = (totalInletMass + totalOutletMass) / 2.0;
        double relativeError = averageMass > 0 
            ? Math.Abs(totalInletMass - totalOutletMass) / averageMass 
            : 0.0;
        
        _logger.LogInformation(
            "Mass balance: Inlet = {Inlet} kg/s, Outlet = {Outlet} kg/s, Relative Error = {Error:P2}",
            totalInletMass, totalOutletMass, relativeError);
        
        if (relativeError > tolerance)
        {
            LastErrorMessage = $"Mass balance violation: Inlet = {totalInletMass:F4} kg/s, " +
                             $"Outlet = {totalOutletMass:F4} kg/s, " +
                             $"Relative Error = {relativeError:P2} (tolerance = {tolerance:P2})";
            _logger.LogWarning(LastErrorMessage);
            return false;
        }
        
        return true;
    }
}
```

**Step 3: Register validator in DI**

Modify `Enerflow.Worker/Program.cs`, add after other service registrations:
```csharp
builder.Services.AddSingleton<IMassBalanceValidator, MassBalanceValidator>();
```

**Step 4: Use validator in DWSIMSolver**

Modify `Enerflow.Worker/Solvers/DWSIMSolver.cs`:

Add field:
```csharp
private readonly IMassBalanceValidator _massBalanceValidator;
```

Update constructor:
```csharp
public DWSIMSolver(
    IFlowsheetBuilder builder,
    IStreamMapper streamMapper,
    IUnitOperationMapper unitOperationMapper,
    IConnectionMapper connectionMapper,
    IResultCollector resultCollector,
    IMassBalanceValidator massBalanceValidator,
    ILogger<DWSIMSolver> logger)
{
    _builder = builder;
    _streamMapper = streamMapper;
    _unitOperationMapper = unitOperationMapper;
    _connectionMapper = connectionMapper;
    _resultCollector = resultCollector;
    _massBalanceValidator = massBalanceValidator;
    _logogger;
}
```

Add validation after solve (after line with `_resultCollector.ExtractResults`):
```csharp
        // Validate mass balance
        if (!_massBalanceValidator.ValidateMassBalance(flowsheet))
        {
            _logger.LogWarning("Mass balance validation failed: {Error}", 
                _massBalanceValidator.LastErrorMessage);
            // Note: Not throwing exception to allow inspection of results
            // In production, you might want to add this to the result object
        }
```

**Step 5: Run tests**

Run: `dotnet test Enerflow.Tests.DWSIM && dotnet test Enerflow.Tests.Functional`
Expected: All tests PASS (mass balance should be satisfied for existing tests)

**Step 6: Commit**

```bash
git add Enerflow.Worker/Validation/IMassBalanceValidator.cs \
        Enerflow.Worker/Validation/MassBalanceValidator.cs \
        Enerflow.Worker/Solvers/DWSIMSolver.cs \
        Enerflow.Worker/Program.cs
git commit -m "feat: add post-solve mass balance validation

Implements rigorous mass balance check after flowsheet solving:
- Validates total inlet mass = total outlet mass
- Configurable tnce (default 1%)
- Logs warning if validation fails
- Does not throw exception to allow result inspection"
```


## Completion

After all tasks are complete:
1. Run full test suite: `dotnet test`
2. Verify all tests pass
3. Review git log to confirm all commits
4. Update ROADMAP.md to mark these items complete
