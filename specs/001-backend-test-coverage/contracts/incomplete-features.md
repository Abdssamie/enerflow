# Incomplete Features Implementation Contracts

**Feature**: Backend Test Coverage & MVP Readiness Assessment  
**Date**: 2025-01-30  
**Phase**: 1 - Design

## Overview

This document defines implementation contracts for the 4 incomplete features identified in the codebase analysis. Each feature has a TODO comment indicating partial implementation. Completing these features is required for MVP readiness.

## Feature 1: Mass Balance Validation

### Current State

**Location**: `Enerflow.Simulation/Services/SimulationService.cs:497`

**Current Implementation**:
```csharp
public bool ValidateMassBalance(Flowsheet flowsheet)
{
    // TODO: Implement rigorous mass balance check
    return true; // Placeholder
}
```

**Problem**: Method always returns `true`, providing no actual validation of mass conservation.

### Requirements

**Functional Requirement**: System MUST validate that total mass in equals total mass out (within tolerance) for all simulations.

**Acceptance Criteria**:
1. Calculate total mass flow rate of all inlet streams
2. Calculate total mass flow rate of all outlet streams
3. Compare inlet vs outlet with configurable tolerance (default: 1%)
4. Return `true` if balanced, `false` if imbalanced
5. Log warning with imbalance percentage if validation fails

### Implementation Contract

**Input**:
- `flowsheet` (DWSIM.Flowsheet): Completed simulation flowsheet
- `tolerance` (double, optional): Acceptable imbalance percentage (default: 0.01 = 1%)

**Output**:
- `bool`: `true` if mass balance is within tolerance, `false` otherwise

**Algorithm**:
```csharp
public bool ValidateMassBalance(Flowsheet flowsheet, double tolerance = 0.01)
{
    // 1. Get all material streams
    var materialStreams = flowsheet.SimulationObjects.Values
        .OfType<DWSIM.UnitOperations.Streams.MaterialStream>()
        .ToList();

    if (!materialStreams.Any())
    {
        _logger.LogWarning("No material streams found in flowsheet");
        return true; // No streams to validate
    }

    // 2. Identify inlet streams (no source unit operation)
    var inletStreams = materialStreams.Where(stream =>
    {
        var inputConnector = stream.GraphicObject?.InputConnectors?.FirstOrDefault();
        return inputConnector == null || 
               string.IsNullOrEmpty(inputConnector.AttachedConnector?.AttachedFrom?.Name);
    }).ToList();

    // 3. Identify outlet streams (no destination unit operation)
    var outletStreams = materialStreams.Where(stream =>
    {
        var outputConnector = stream.GraphicObject?.OutputConnectors?.FirstOrDefault();
        return outputConnector == null || 
               string.IsNullOrEmpty(outputConnector.AttachedConnector?.AttachedTo?.Name);
    }).ToList();

    // 4. Calculate total mass in
    double totalMassIn = inletStreams.Sum(stream =>
    {
        var massFlow = stream.Phases[0]?.Properties?.massflow?.GetValueOrDefault() ?? 0.0;
        return massFlow;
    });

    // 5. Calculate total mass out
    double totalMassOut = outletStreams.Sum(stream =>
    {
        var massFlow = stream.Phases[0]?.Properties?.massflow?.GetValueOrDefault() ?? 0.0;
        return massFlow;
    });

    // 6. Handle edge case: no flow
    if (totalMassIn == 0.0 && totalMassOut == 0.0)
    {
        return true; // No flow, balanced by definition
    }

    if (totalMassIn == 0.0)
    {
        _logger.LogWarning("Total mass in is zero, but mass out is {MassOut}", totalMassOut);
        return false;
    }

    // 7. Calculate imbalance percentage
    double imbalance = Math.Abs(totalMassIn - totalMassOut) / totalMassIn;

    // 8. Log and return result
    if (imbalance > tolerance)
    {
        _logger.LogWarning(
            "Mass balance validation failed: In={MassIn} kg/s, Out={MassOut} kg/s, Imbalance={Imbalance:P2} (tolerance={Tolerance:P2})",
            totalMassIn, totalMassOut, imbalance, tolerance);
        return false;
    }

    _logger.LogInformation(
        "Mass balance validated: In={MassIn} kg/s, Out={MassOut} kg/s, Imbalance={Imbalance:P2}",
        totalMassIn, totalMassOut, imbalance);
    return true;
}
```

### Test Requirements

**Unit Tests**:
1. Test with balanced flowsheet (inlet = outlet) → returns `true`
2. Test with imbalanced flowsheet (inlet ≠ outlet) → returns `false`
3. Test with no streams → returns `true`
4. Test with zero flow → returns `true`
5. Test with custom tolerance → respects tolerance parameter
6. Test edge case: inlet = 0, outlet > 0 → returns `false`

**Integration Tests**:
1. Run actual DWSIM simulation, validate mass balance
2. Test with various unit operations (heater, mixer, splitter)
3. Test with recycle loops

**Test Data**:
- Simple heating: 100 kg/s in, 100 kg/s out (balanced)
- Mixer: 50 kg/s + 50 kg/s in, 100 kg/s out (balanced)
- Imbalanced: 100 kg/s in, 90 kg/s out (10% imbalance, should fail with 1% tolerance)

### Estimated Effort

- Implementation: 2 hours
- Unit tests: 2 hours
- Integration tests: 2 hours
- **Total**: 6 hours (0.75 days)

---

## Feature 2: Unit Operation Parameter Configuration

### Current State

**Location**: `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs:146`

**Current Implementation**:
```csharp
private void CreateUnitOperation(UnitOperation entity)
{
    var dwsimUnit = UnitOperationFactory.Create(entity.Type);
    _floheet.AddObject(dwsimUnit);
    
    // TODO: Configure unit operation parameters based on unit.Type
    // Currently using default parameters
}
```

**Problem**: Unit operations are created but not configured with user-specified parameters from the entity.

### Requirements

**Functional Requirement**: System MUST configure DWSIM unit operation parameters from entity data based on unit type.

**Acceptance Criteria**:
1. Map entity parameters to DWSIM unit operation properties
2. Support all unit operation types (Heater, Cooler, Compressor, Expander, Mixer, Splitter, Separator, HeatExchanger, Reactor, DistillationColumn, Valve)
3. Validate parameter values before setting
4. Log parameter configuration for debugging
5. Handle missing or invalid parameters gracefully

### Implementation Contract

**Input**:
- `entity` (UnitOperation): Domain entity with parameters
- `dwsimUnit` (DWSIM.SharedClasses.UnitOperations.BaseClass): DWSIM unit operation instance

**Output**:
- Configured DWSIM unit operation (void method, modifies dwsimUnit in place)

**Algorithm**:
```csharp
private void ConfigureUnitOperationParameters(UnitOperation entity, DWSIM.SharedClasses.UnitOperations.BaseClass dwsimUnit)
{
    if (entity.Parameters == null || !entity.Parameters.Any())
    {
        _logger.LogInformation("No parameters to configure for unit operation {Name}", entity.Name);
        return;
    }

    _logger.LogInformation("Configuring {Count} parameters for {Type} unit operation {Name}",
        entity.Parameters.Count, entity.Type, entity.Name);

    switch (entity.Type)
    {
        case UnitOperationType.Heater:
            ConfigureHeater(entity, (DWSIM.UnitOperations.UnitOperations.Heater)dwsimUnit);
            break;

        case UnitOperationType.Cooler:
            ConfigureCooler(entity, (DWSIM.UnitOperations.UnitOperations.Cooler)dwsimUnit);
            break;

        case UnitOperationType.Compressor:
            ConfigureCompressor(entity, (DWSIM.UnitOperations.UnitOperations.Compressor)dwsimUnit);
            break;

        case UnitOperationType.Expander:
            ConfigureExpander(entity, (DWSIM.UnitOperations.UnitOperations.Expander)dwsimUnit);
            break;

        case UnitOperationType.Mixer:
            ConfigureMixer(entity, (DWSIM.UnitOperations.UnitOperations.Mixer)dwsimUnit);
            break;

        case UnitOperationType.Splitter:
            ConfigureSplitter(entity, (DWSIM.UnitOperations.UnitOperations.Splitter)dwsimUnit);
            break;

        case UnitOperationType.Separator:
            ConfigureSeparator(entity, (DWSIM.UnitOperations.UnitOperations.ComponentSeparator)dwsimUnit);
            break;

        case UnitOperationType.HeatExchanger:
            ConfigureHeatExchanger(entity, (DWSIM.UnitOperations.UnitOperations.HeatExchanger)dwsimUnit);
            break;

        case UnitOperationType.Valve:
            ConfigureValve(entity, (DWSIM.UnitOperations.UnitOperations.Valve)dwsimUnit);
            break;

        // Add other unit types as needed

        default:
            _logger.LogWarning("No parameter configuration implemented for unit type {Type}", entity.Type);
            break;
    }
}

private void ConfigureHeater(UnitOperation entity, DWSIM.UnitOperations.UnitOperations.Heater heater)
{
    if (entity.Parameters.TryGetValue("OutletTemperature", out var tempObj))
    {
        var temperature = Convert.ToDouble(tempObj);
        heater.CalcMode = DWSIM.UnitOperations.UnitOperations.Heater.CalculationMode.OutletTemperature;
        heater.OutletTemperature = temperature;
        _loLogDebug("Set heater outlet temperature: {Temp} K", temperature);
    }

    if (entity.Parameters.TryGetValue("PressureDrop", out var pdropObj))
    {
        var pressureDrop = Convert.ToDouble(pdropObj);
        heater.DeltaP = pressureDrop;
        _logger.LogDebug("Set heater pressure drop: {PDrop} Pa", pressureDrop);
    }

    if (entity.Parameters.TryGetValue("Efficiency", out var effObj))
    {
        var efficiency = Convert.ToDouble(effObj);
        heater.Eficiencia = efficiency;
        _logger.LogDebug("Set heater efficiency: {Eff}", efficiency);
    }
}

private void ConfigureCompressor(UnitOperation entity, DWSIM.UnitOperations.UnitOperations.Compressor compressor)
{
    if (entity.Parameters.TryGetValue("OutletPressure", out var pressObj))
    {
        var pressure = Convert.ToDouble(pressObj);
        compressor.CalcMode = DWSIM.UnitOperations.UnitOperations.Compressor.CalculationMode.OutletPressure;
        compressor.POut = pressure;
        _logger.LogDebug("Set compressor outlet pressure: {Pressure} Pa", pressure);
    }

    if (entity.Parameters.TryGetValue("Efficiency", out var effObj))
    {
        var efficiency = Convert.ToDouble(effObj);
        compressor.Eficiencia = efficienc        _logger.LogDebug("Set compressor efficiency: {Eff}", efficiency);
    }

    if (entity.Parameters.TryGetValue("AdiabaticEfficiency", out var adEffObj))
    {
        var adiabaticEff = Convert.ToDouble(adEffObj);
        compressor.AdiabaticEfficiency = adiabaticEff;
        _logger.LogDebug("Set compressor adiabatic efficiency: {AdEff}", adiabaticEff);
    }
}

// Implement similar methods for other unit types...
```

### Test Requirements

**Unit Tests**:
1. Test heater configuration with outlet temperature
2. Test compressor configuration with outlet pressure and efficiency
3. Test mixer configuration with outlet pressure
4. Test with missing parameters → uses defaults
5. Test with invalid parameter values → logs warning, uses defaults
6. Test with unknown unit type → logs warning, no configuration

**Integration Tests**:
1. Build flowsheet with configured unit operations
2. Run simulation, verify parameters were applied correctly
3. Compare results with manually configured DWSIM simulation

**Test Data**:
- Heater: OutletTemperature = 400 K, Efficiency = 0.85
- Compressor: OutletPressure = 500000 Pa, Efficiency = 0.75
- Mixer: OutletPressure = 101325 Pa

### Estimated Effort

- Implementation: 6 hours (all unit types)
- Unit tests: 3 hours
- Integration tests: 3 hours
- **Total**: 12 hours (1.5 days)

---

## Feature 3: Result Extraction Enhancement

### Current State

**Location**: `Enerflow.Worker/Solvers/ResultCollector.cs:78`

**Current Implementation**:
```csharp
private Dictionary<string, object> ExtractUnitOperationResults(BaseClass dwsimUnit)
{
    var results = new Dictionary<string, object>
    {
        ["Name"] = dwsimUnit.GraphicObject.Tag,
        ["Type"] = dwsimUnit.GraphicObject.ObjectType
    };
    
    // TODO: Add more specific property extraction based on Unit Types
    // Crently only extracting generic properties
    
    return results;
}
```

**Problem**: Only generic properties are extracted. Unit-specific properties (heat duty, power, efficiency, etc.) are not captured.

### Requirements

**Functional Requirement**: System MUST extract unit-specific result properties based on unit type.

**Acceptance Criteria**:
1. Extract generic properties for all unit types (name, type, status)
2. Extract unit-specific properties based on type
3. Handle null/missing properties gracefully
4. Return structured dictionary with all available properties
5. Log extraction for debugging

### Implementation Contract

**Input**:
- `dwsimUnit` (DWSIM.SharedClasses.UnitOperations.BaseClass): DWSIM unit operation after simulation

**Output**:
- `Dictionary<string, object>`: All extracted properties (generic + unit-specific)

**Algorithm**:
```csharp
private Dictionary<string, object> ExtractUnitOperationResults(DWSIM.SharedClasses.UnitOperations.BaseClass dwsimUnit)
{
    var results = new Dictionary<string, object>
    {
        ["Name"] = dwsimUnit.GraphicObject.Tag,
        ["Type"] = dwsimUnit.GraphictType.ToString(),
        ["Status"] = dwsimUnit.GraphicObject.Active ? "Active" : "Inactive"
    };

    // Extract unit-specific properties
    switch (dwsimUnit)
    {
        case DWSIM.UnitOperations.UnitOperations.Heater heater:
            ExtractHeaterProperties(heater, results);
            break;

        case DWSIM.UnitOperations.UnitOperations.Cooler cooler:
            ExtractCoolerProperties(cooler, results);
            break;

        case DWSIM.UnitOperations.UnitOperations.Compressor compressor:
            ExtractCompressorProperties(compressor, results);
            break;

        case DWSIM.UnitOperations.UnitOperations.Expander expander:
            ExtractExpanderProperties(expander, results);
            break;

        case DWSIM.UnitOperations.UnitOperations.Mixer mixer:
            ExtractMixerProperties(mixer, results);
            break;

        case DWSIM.UnitOperations.UnitOperations.Splitter splitter:
            ExtractSplitterProperties(splitter, results);
            break;

        case DWSIM.UnitOperations.UnitOperations.HeatExchanger heatExchanger:
            ExtractHeatExchangerProperties(heatExchanger, results);
            brea    case DWSIM.UnitOperations.UnitOperations.DistillationColumn column:
            ExtractDistillationColumnProperties(column, results);
            break;

        case DWSIM.UnitOperations.UnitOperations.Valve valve:
            ExtractValveProperties(valve, results);
            break;

        case DWSIM.UnitOperations.UnitOperations.Reactor reactor:
            ExtractReactorProperties(reactor, results);
            break;

        default:
            _logger.LogDebug("No specific property extraction for unit type {Type}", dwsimUnit.GetType().Name);
            break;
    }

    _logger.LogDebug("Extracted {Count} properties for unit {Name}", results.Count, dwsimUnit.GraphicObject.Tag);
    return results;
}

private void ExtractHeaterProperties(DWSIM.UnitOperations.UnitOperations.Heater heater, Dictionary<string, object> results)
{
    results["HeatDuty_W"] = heater.DeltaQ.GetValueOrDefault();
    results["OutletTemperature_K"] = heater.OutletTemperature;
    results["PressureDrop_Pa"] = heater.DeltaP.GetValueOrDefault();
    results["Efficiency"] = heater.Eficiencia;
}

private void ExtractCompressorProperties(DWSIM.UnitOperations.UnitOperations.Compressor compressor, Dictionary<string, object> results)esults["Power_W"] = compressor.DeltaQ.GetValueOrDefault();
    results["OutletPressure_Pa"] = compressor.POut;
    results["OutletTemperature_K"] = compressor.TOut;
    results["Efficiency"] = compressor.Eficiencia;
    results["AdiabaticEfficiency"] = compressor.AdiabaticEfficiency;
    results["PolytropicEfficiency"] = compressor.PolytropicEfficiency;
}

private void ExtractHeatExchangerProperties(DWSIM.UnitOperations.UnitOperations.HeatExchanger heatExchanger, Dictionary<string, object> results)
{
    results["HeatTransferred_W"] = heatExchanger.Q.GetValueOrDefault();
    results["LMTD_K"] = heatExchanger.LMTD.GetValueOrDefault();
    results["OverallHeatTransferCoefficient_W_m2_K"] = heatExchanger.OverallCoefficient.GetValueOrDefault();
    results["HeatExchangerArea_m2"] = heatExchanger.Area.GetValueOrDefault();
    results["HotSideOutletTemperature_K"] = heatExchanger.HotSideOutletTemperature;
    results["ColdSideOutletTemperature_K"] = heatExchanger.ColdSideOutletTemperature;
}

private void ExtractDistillationColumnProperties(DWSIM.UnitOperations.UnitOperations.DistillationColumn column, Dictionary<string, object> results)
{
    results["RefluxRatio"] = column.RefluxRatio;
    results["NumberOfStages"] = column.NumberOfStages;
    results["CondenserDuty_W"] = column.CondenserDuty.GetValueOrDefault();
    results["ReboilerDuty_W"] = column.ReboilerDuty.GetValueOrDefault();
    results["CondenserPressure_Pa"] = column.CondenserPressure;
    results["ReboilerPressure_Pa"] = column.ReboilerPressure;
}

// Implement similar methods for other unit types...
```

### Test Requirements

**Unit Tests**:
1. Test heater result extraction → includes HeatDuty, OutletTemperature
2. Test compressor result extraction → includes Power, Efficiency
3. Test heat exchanger result extraction → includes LMTD, HeatTransferred
4. Test distillation column result extraction → includes RefluxRatio, CondenserDuty
5. Test with null properties → handles gracefully
6. Test with unknown unit type → returns only generic properties

**Integration Tests**:
1. Run simulation, extract results, verify all expected properties present
2. Compare extracted properties with DWSIM UI values

**Test Data**:
- Heater: Verify HeatDuty matches expected value
- Compressor: Verify Power and Efficiency extracted
- Heat Exchanger: Verify LMTD and heat transferred

### Estimated Effort

- Implementation: 4 hours (all unit types)
- Unit tests: 2 hours
- Integration tests: 2 hours
- **Total**: 8 hours (1 day)

---

## Feature 4: Wegstein Acceleration Completion

### Current State

**Location**: `Enerflow.Worker/Solvers/DWSIMSolver.cs:138`

**Current Implementation**:
```csharp
private void ApplyConvergenceAcceleration()
{
    // NOTE: Actual acceleration wiring requires identifying Recycles 
    // and their specific Inlet/Outlet streams
    
    // Placeholder: No acceleration currently applied
}
```

**Problem**: Recycle loops converge slowly without Wegstein acceleration. Tear stream identification is not implemented.

### Requirements

**Functional Requirement**: System MUST identify tear streams in recycle loops and apply Wegstein acceleration for faster convergence.

**Acceptance Criteria**:
1. Build dependency graph of unit operations
2. Detect cycles (recycle loops) using graph traversal
3. Identify tear streams (streams to "break" cycles)
4. Apply Wegstein acceleration to tear stream values
5. Monitor convergence improvement (fewer iterations)

### Implementation Contract

**Input**:
- `flowsheet` (DWSIM.Flowsheet): Flowsheet with recycle loops
- `maxIterations` (int): Maximum convergence iterations
- `tolerance` (double): Convergence tolerance

**Output**:
- Converged flowsheet (void method, modifies flowsheet in place)
- Convergence metrics (iterations, time)

**Algorithm**:
```csharp
private List<string> IdentifyTearStreams(Flowsheet flowsheet)
{
    var tearStreams = new List<string>();

    // 1. Build dependency graph
    var graph = BuildDependencyGraph(flowsheet);

    // 2. Detect cycles using DFS
    var cycles = DetectCycles(graph);

    if (!cycles.Any())
    {
        _logger.LogInformation("No recycle loops detected");
        return tearStreams;
    }

    _logger.LogInformation("Detected {Count} recycle loops", cycles.Count);

    // 3. For each cycle, identify tear stream
    foreach (var cycle in cycles)
    {
        // Heuristic: Choose stream with lowest mass flow rate
        var cycleStreams = GetStreamsInCycle(cycle, flowsheet);
        var tearStream = cycleStreams
            .OrderBy(s => GetMassFlowRate(s, flowsheet))
            .FirstOrDefault();

        if (tearStream != null)
        {
            tearStreams.Add(tearStream);
            _logger.LogInformation("Identified tear stream: {Stream} in cycle {Cycle}", 
                tearStream, string.Join(" -> ", cycle));
        }
    }

    return tearStreams;
}

private Dictionary<string, List<string>> BuildDependencyGraph(Flowsheet flowsheet)
{
    var graph = new Dictionary<string, List<string>>();

    foreach (var unitOp in flowsheet.SimulationObjects.Values.OfType<DWSIM.SharedClasses.UnitOperations.BaseClass>())
    {
        var unitName = unitOp.GraphicObject.Tag;
        graph[unitName] = new List<string>();

        // Find downstream units (units that receive output from this unit)
        var outputStreams = GetOutputStreams(unitOp, flowsheet);
        foreach (var stream in outputStreams)
        {
            var downstreamUnit = GetDownstreamUnit(stream, flowsheet);
            if (downstreamUnit != null)
            {
                graph[unitName].Add(downstreamUnit);
            }
        }
    }

    return graph;
}

private List<List<string>> DetectCycles(Dictionary<string, List<string>> graph)
{
    var cycles = new List<List<string>>();
    var visited = new HashSet<string>();
    var recursionStack = new HashSet<string>();
    var currentPath = new List<string>();

    foreach (var node in graph.Keys)
    {
        if (!visited.Contains(node))
        {
            DetectCyclesDFS(node, graph, visited, recursionStack, currentPath, cycles);
        }
    }

    return cycles;
}

private bool DetectCyclesDFS(string node, Dictionary<string, List<string>> graph, 
    HashSet<string> visited, HashSet<string> recursionStack, List<string> currentPath, List<List<string>> cycles)
{
    visited.Add(node);
    recursionStack.Add(node);
    currentPath.Add(node);

    foreach (var neighbor in graph[node])
    {
        if (!visited.Contains(neighbor))
        {
            if (DetectCyclesDFS(neighbor, graph, visited, recursionStack, currentPath, cycles))
            {
                return true;
            }
        }
        else if (recursionStack.Contains(neighbor))
        {
            // Cycle detected
            var cycleStart = currentPath.IndexOf(neighbor);
            var cycle = currentPath.Skip(cycleStart).ToList();
            cycles.Add(cycle);
        }
    }

    recursionStack.Remove(node);
    currentPath.RemoveAt(currentPath.Count - 1);
    return false;
}

private void ApplyWegsteinAcceleration(string streamId, double[] previousValues, double[] currentValues, double accelerationFactor = 0.5)
{
    // Wegstein acceleration formula:
    // x_new = x_current + q * (x_current - x_previous)
    // where q is the acceleration factor (typically 0.5 to 1.0)

    for (int i = 0; i < currentValues.Length; i++)
    {
        if (previousValues[i] != 0.0)
        {
            double delta = currentValues[i] - previousValues[i];
            currentValues[i] = currentValues[i] + accelerationFactor * delta;
        }
    }

    _logger.LogDebug("Applied Wegstein acceleration to stream {Stream} with factor {Factor}", 
        streamId, accelerationFactor);
}
```

### Test Requirements

**Unit Tests**:
1. Test dependency graph building → correct graph structure
2. Test cycle detection with simple recycle → detects cycle
3. Test cycle detection without recycle → no cycles
4. Test tear stream identification → selects lowest flow stream
5. Test Wegstein acceleration formula → correct calculation

**Integration Tests**:
1. Run simulation with recycle loop, no acceleration → measure iterations
2. Run simulation with recycle loop, with acceleration → fewer iterations
3. Verify convergence improvement (at least 20% fewer iterations)

**Test Data**:
- Simple recycle: Mixer → Heater → Splitter → (recycle to Mixer)
- Expected: Tear stream identified, convergence faster with acceleration

### Estimated Effort

- Implementation: 8 hours (graph algorithms, acceleration logic)
- Unit tests: 3 hours
- Integration tests: 3 hours
- **Total**: 14 hours (1.75 days)

---

## Summary

| Feature | Location | Effort | Priority | Dependencies |
|---------|----------|--------|----------|--------------|
| Mass Balance Validation | SimulationService.cs:497 | 0.75 days | P2 | None |
| Unit Op Configuration | DWSIMFlowsheetBuilder.cs:146 | 1.5 days | P2 | None |
| Result Extraction | ResultCollector.cs:78 | 1 day | P2 | None |
| Wegstein Acceleration | DWSIMSolver.cs:138 | 1.75 days | P2 | None |
| **Total** | | **5 days** | | |

## Implementation Order

**Recommended Sequence**:
1. **Unit Op Configuration** (1.5 days) - Enables proper simulation setup
2. **Result Extraction** (1 day) - Captures simulation outputs
3. **Mass Balance Validation** (0.75 days) - Validates simulation correctness
4. **Wegstein Acceleration** (1.75 days) - Optimizes convergence (optional for MVP)

**Rationale**: Configuration and extraction are foundational. Validation ensures correctness. Acceleration is an optimization that can be deferred if timeline is tight.

---

**Status**: ✅ Complete  
**Next**: Create quickstart.md for running tests and generating coverage reports
