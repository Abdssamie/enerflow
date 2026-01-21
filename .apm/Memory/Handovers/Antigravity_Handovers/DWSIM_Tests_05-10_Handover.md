# DWSIM API Tests 05-10 Implementation Handover

## Context
You are continuing the implementation of the DWSIM API Test Suite. Tests 01-04 have been completed and are passing. Your task is to implement Tests 05-10 following established patterns and incorporating critical API discoveries.

## Completed Work (Tests 01-04)

| Test | File | Description | Status |
|------|------|-------------|--------|
| 01 | `Test01_SimpleHeating.cs` | Methane stream through Heater (+50C) | PASSING |
| 02 | `Test02_CompressionCooling.cs` | Natural gas compression (5->20 bar) + cooling | PASSING |
| 03 | `Test03_PressureReduction.cs` | Propane/n-Butane valve flash + separator | PASSING |
| 04 | `Test04_EthanolWaterVLE.cs` | NRTL vs Peng-Robinson bubble point comparison | PASSING |

**Note**: Test04 was modified from the original plan (which specified hydrocarbon mixing) to test property package comparison for polar mixtures. This better demonstrates non-ideal VLE behavior.

---

## CRITICAL API DISCOVERIES

These patterns MUST be followed. They differ from the original plan and from typical DWSIM documentation.

### 1. Automation Class
```csharp
// WRONG - Legacy class
using DWSIM.Automation.Automation;

// CORRECT - Full-featured class
using DWSIMAutomation = DWSIM.Automation.Automation3;
```

### 2. CalculateFlowsheet2 Returns VOID
```csharp
// WRONG - Returns void, not List<Exception>
var errors = Automation.CalculateFlowsheet2(flowsheet);

// CORRECT - Call without assignment
Automation.CalculateFlowsheet2(flowsheet);

// Error checking AFTER calculation:
if (!flowsheet.Solved)
{
    Logger.Error("Failed: {Error}", flowsheet.ErrorMessage);
}
```

### 3. DO NOT Call AddCompoundsToMaterialStream
```csharp
// WRONG - Causes "duplicate key" exception
var stream = flowsheet.AddObject(ObjectType.MaterialStream, 100, 100, "Feed") as MaterialStream;
flowsheet.AddCompoundsToMaterialStream(stream);  // THROWS EXCEPTION!

// CORRECT - AddObject already calls AddCompoundsToMaterialStream internally
var stream = flowsheet.AddObject(ObjectType.MaterialStream, 100, 100, "Feed") as MaterialStream;
// Compounds are already added, just set properties:
stream.Phases[0].Compounds["Methane"].MoleFraction = 1.0;
```

### 4. Unit Operations Require CalcMode BEFORE Value
```csharp
// WRONG - Default CalcMode is DeltaP, so OutletPressure is ignored
valve.OutletPressure = 500000;

// CORRECT - Set CalcMode first
valve.CalcMode = Valve.CalculationMode.OutletPressure;
valve.OutletPressure = 500000;

// Similarly for Heater/Cooler:
heater.CalcMode = Heater.CalculationMode.OutletTemperature;
heater.OutletTemperature = 348.15;

// Similarly for Compressor:
compressor.CalcMode = Compressor.CalculationMode.OutletPressure;
compressor.POut = 2000000;
```

### 5. Heat Duty Sign Convention
```csharp
// WRONG assumption - Cooler returns negative duty
Assert.True(cooler.DeltaQ < 0, "Heat removed");  // FAILS!

// CORRECT - DWSIM reports POSITIVE magnitude for both
Assert.True(cooler.DeltaQ > 0, "Cooling duty magnitude");
// Validate by checking temperature decrease instead:
Assert.True(outletTemp < inletTemp, "Temperature decreased");
```

### 6. Thread Safety - Sequential Test Execution
The project includes `xunit.runner.json` with:
```json
{
  "parallelizeTestCollections": false,
  "maxParallelThreads": 1
}
```
**Do not remove or modify these settings.** DWSIM Automation is single-threaded.

### 7. Property Names (VB.NET Origin)
```csharp
// Vapor fraction property is lowercase
stream.Phases[0].Properties.molarfraction = 0.5;  // NOT MolarFraction

// StreamSpec enum uses underscore
stream.SpecType = StreamSpec.Pressure_and_VaporFraction;  // NOT PressureAndVaporFraction
```

---

## Tests 05-10 Specifications

Implement the following tests. Each should follow the patterns in `TestBase.cs` and use `TestHelpers.cs` for assertions.

### Test 05: Flash Algorithm Comparison
**File**: `Scenarios/Test05_FlashAlgorithmComparison.cs`

**Scenario**: Same separation problem solved with different flash algorithms

**Setup**:
- Compounds: Propane (50%), n-Butane (50%)
- Feed: 10 bar, 300 K, 1 kg/s
- Separator: Flash to 5 bar

**Compare**:
1. NestedLoops flash algorithm
2. InsideOut flash algorithm

**Assertions**:
- Both algorithms converge
- Results match within tolerance (temperature, vapor fraction, compositions)
- Log any performance differences

**Property Package Selection**:
```csharp
var pr = new PengRobinsonPropertyPackage();
// Set flash algorithm:
pr.FlashAlgorithm = new NestedLoops();  // or InsideOut
```

---

### Test 06: Three-Phase Flash
**File**: `Scenarios/Test06_ThreePhaseFlash.cs`

**Scenario**: Oil/Water/Gas mixture separation

**Setup**:
- Compounds: Methane, n-Hexane, Water
- Composition: 20% Methane, 40% n-Hexane, 40% Water (molar)
- Feed: 10 bar, 350 K
- Separator: Flash at feed conditions

**Assertions**:
- Three phases present (Vapor, Liquid1, Liquid2)
- Vapor phase enriched in Methane
- Liquid1 (organic) enriched in Hexane
- Liquid2 (aqueous) enriched in Water
- Mass balance across all phases

**Flash Algorithm**:
```csharp
// For three-phase, use:
pr.FlashAlgorithm = new GibbsMinimization3P();
// OR
pr.FlashAlgorithm = new NestedLoops3P();
```

---

### Test 07: Heat Exchanger
**File**: `Scenarios/Test07_HeatExchanger.cs`

**Scenario**: Counter-current heat exchange

**Setup**:
- Hot stream: Methane at 400 K, 10 bar, 1 kg/s
- Cold stream: Methane at 300 K, 10 bar, 1 kg/s
- Heat exchanger duty or approach temperature specification

**Unit Operation**:
```csharp
var hx = flowsheet.AddObject(ObjectType.HeatExchanger, x, y, "HX") as HeatExchanger;
hx.CalculationMode = HeatExchanger.HeatExchangerCalcMode.CalcBothTemp_UA;
// OR specify outlet temp for one side
```

**Assertions**:
- Hot stream outlet < Hot stream inlet
- Cold stream outlet > Cold stream inlet
- Energy balance: Q_hot = Q_cold (within tolerance)
- Approach temperature > 0 (no crossing)

---

### Test 08: Shortcut Distillation Column
**File**: `Scenarios/Test08_ShortcutDistillation.cs`

**Scenario**: Propane/n-Butane separation

**Setup**:
- Feed: 50% Propane, 50% n-Butane, 1 kg/s, 5 bar, saturated liquid
- Target: 95%+ Propane purity in distillate

**Unit Operation**:
```csharp
// Use ShortcutColumn for simpler convergence
var column = flowsheet.AddObject(ObjectType.ShortcutColumn, x, y, "Column") as ShortcutColumn;
column.LightKeyCompoundID = "Propane";
column.HeavyKeyCompoundID = "n-Butane";
column.LightKeyRecovery = 0.95;  // 95% of Propane to distillate
column.HeavyKeyRecovery = 0.95;  // 95% of n-Butane to bottoms
column.RefluxRatio = 1.5;
```

**Assertions**:
- Column converges
- Distillate enriched in Propane (>90%)
- Bottoms enriched in n-Butane (>90%)
- Mass balance: Feed = Distillate + Bottoms
- Condenser and Reboiler duties are reasonable

**Note**: If ShortcutColumn fails, fall back to simpler flash-based separation and document the limitation.

---

### Test 09: Conversion Reactor
**File**: `Scenarios/Test09_ConversionReactor.cs`

**Scenario**: Simple A -> B conversion

**Setup**:
- Compounds: Methane (reactant), Ethane (product) - or choose appropriate pair
- Feed: 100% Methane, 1 kg/s, 10 bar, 400 K
- Reactor: 50% conversion

**Unit Operation**:
```csharp
var reactor = flowsheet.AddObject(ObjectType.RCT_Conversion, x, y, "Reactor") as Reactor_Conversion;
// Define reaction
var reaction = new Reaction();
reaction.ReactionType = ReactionType.Conversion;
reaction.Components.Add("Methane", new ReactionStoichCoef { StoichCoeff = -1 });
reaction.Components.Add("Ethane", new ReactionStoichCoef { StoichCoeff = 0.5 });  // stoichiometry
reaction.Conversion = 0.5;  // 50% conversion
```

**Assertions**:
- Reactor converges
- Methane mole fraction reduced by ~50%
- Ethane present in outlet
- Mass balance (accounting for molar mass change)

**Alternative**: If reaction setup is complex, use a simpler approach with pre-defined reaction from DWSIM examples.

---

### Test 10: Recycle Loop
**File**: `Scenarios/Test10_RecycleLoop.cs`

**Scenario**: Simple process with recycle stream

**Setup**:
- Fresh feed: Methane, 1 kg/s
- Mixer: Combines fresh feed + recycle
- Heater: Heat to 400 K
- Splitter: 80% to product, 20% to recycle
- Recycle block: Connects splitter outlet back to mixer

**Unit Operations**:
```csharp
var mixer = flowsheet.AddObject(ObjectType.NodeIn, x, y, "Mixer") as Mixer;
var splitter = flowsheet.AddObject(ObjectType.NodeOut, x, y, "Splitter") as Splitter;
splitter.Ratios = new double[] { 0.8, 0.2 };  // 80% out, 20% recycle

var recycle = flowsheet.AddObject(ObjectType.OT_Recycle, x, y, "Recycle") as Recycle;
```

**Assertions**:
- Flowsheet converges (recycle loop closes)
- Recycle stream matches assumed values within tolerance
- Mass balance: Fresh feed = Product out
- Multiple iterations may be needed - log iteration count

---

## File Structure

```
Enerflow.Tests.DWSIM/
├── TestBase.cs              # Base class - DO NOT MODIFY
├── TestHelpers.cs           # Assertion helpers - extend if needed
├── xunit.runner.json        # Sequential execution - DO NOT MODIFY
├── Scenarios/
│   ├── Test01_SimpleHeating.cs          # COMPLETE
│   ├── Test02_CompressionCooling.cs     # COMPLETE
│   ├── Test03_PressureReduction.cs      # COMPLETE
│   ├── Test04_EthanolWaterVLE.cs        # COMPLETE
│   ├── Test05_FlashAlgorithmComparison.cs   # TODO
│   ├── Test06_ThreePhaseFlash.cs            # TODO
│   ├── Test07_HeatExchanger.cs              # TODO
│   ├── Test08_ShortcutDistillation.cs       # TODO
│   ├── Test09_ConversionReactor.cs          # TODO
│   └── Test10_RecycleLoop.cs                # TODO
```

---

## Execution Commands

```bash
# Build
dotnet build Enerflow.Tests.DWSIM/Enerflow.Tests.DWSIM.csproj

# Run all tests
dotnet test Enerflow.Tests.DWSIM/Enerflow.Tests.DWSIM.csproj

# Run specific test
dotnet test Enerflow.Tests.DWSIM/Enerflow.Tests.DWSIM.csproj --filter "FullyQualifiedName~Test05"

# Verbose output
dotnet test Enerflow.Tests.DWSIM/Enerflow.Tests.DWSIM.csproj --logger "console;verbosity=detailed"
```

---

## API Verification Protocol

Before using ANY DWSIM API call not already demonstrated in Tests 01-04:

1. **Check SKILL.md**: Read `.agent/skills/dwsim-api-verification/SKILL.md` for known pitfalls
2. **Verify in source**: `grep -r "MethodName" libs/dwsim_src --include="*.vb"`
3. **Check return type**: Many VB.NET methods return `void` despite their names suggesting otherwise
4. **Test incrementally**: Build and run after each major addition

---

## Success Criteria

1. All 10 tests pass (`dotnet test` shows 10 passed)
2. Each test produces comprehensive log output in `TestResults/`
3. Results are thermodynamically reasonable
4. No DWSIM exceptions or crashes
5. Update `SKILL.md` with any new API discoveries

---

## Reference Files

- **TestBase.cs**: See for logger setup, `AssertConverged()`, `LogFlowsheetSummary()`
- **TestHelpers.cs**: See for `LogStreamProperties()`, `AssertTemperatureInRange()`, `AssertPressureInRange()`
- **Existing Tests**: Study Test01-04 patterns before implementing new tests
- **DWSIM Source**: `libs/dwsim_src/` for API verification
- **SKILL.md**: `.agent/skills/dwsim-api-verification/SKILL.md` for pitfalls

---

## Estimated Complexity

| Test | Complexity | Notes |
|------|------------|-------|
| 05 | Low | Similar to Test03, just two runs |
| 06 | Medium | Three-phase flash may need specific algorithm |
| 07 | Medium | Heat exchanger has multiple calc modes |
| 08 | High | Distillation columns are sensitive to specifications |
| 09 | Medium-High | Reaction definition can be tricky |
| 10 | High | Recycle convergence requires careful setup |

**Recommendation**: Implement in order (05 -> 10). If a test proves too complex (especially 08-10), implement a simplified version and document limitations.

---

## Contact for Clarification

If you encounter:
- Persistent `TypeLoadException` or `MissingMethodException`: Check if using correct Automation class
- `ArgumentException` with duplicate keys: Remove `AddCompoundsToMaterialStream` calls
- Unit operation not calculating: Verify `CalcMode` is set correctly
- Convergence failures: Try different initial conditions or simpler specifications

Document all discoveries in the Memory Log and update `SKILL.md` for future agents.
