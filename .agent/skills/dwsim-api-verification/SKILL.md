---
name: dwsim-api-verification
description: Verifies DWSIM API usage by cross-referencing against the local source code in libs/dwsim_src.
license: MIT
compatibility: opencode
metadata:
  project: enerflow
  type: verification
---

## What I do

- **Source Code Verification**: I verify that classes, methods, and properties used in `Enerflow.Worker` actually exist in the local DWSIM source code (`libs/dwsim_src`).
- **Signature Checking**: I ensure that the arguments passed to DWSIM methods match the function signatures found in the source.
- **Deprecation Guard**: I actively check for deprecated or void methods (e.g., `CalculateFlowsheet2`) and suggest the correct alternatives (e.g., `RequestCalculation`).

## When to use me

- **Coding**: IMMEDIATELY BEFORE writing any code that calls into `DWSIM.*` namespaces.
- **Debugging**: When a `MethodNotFoundException` or `MissingMemberException` occurs related to DWSIM.
- **Refactoring**: When upgrading DWSIM versions or changing simulation logic.

## How to Verify (The "Grep Check")

**FIRST:** Check the `DWSIM_API_MAP.md` file in the project root. It contains the pre-scanned, authoritative API context.

If the information is not in the map, use `grep` to find it in `libs/dwsim_src`.

### 1. Find the Class Definition
Do not guess where a class is. Find it.

```bash
# Example: Finding the Flowsheet class
grep -r "class Flowsheet" libs/dwsim_src/DWSIM.FlowsheetBase
```

### 2. Verify the Method Signature
Once you know the file, read it to check the method arguments.

```bash
# Example: Checking RequestCalculation arguments
grep -A 5 "public void RequestCalculation" libs/dwsim_src/path/to/Flowsheet.vb
```

### 3. Check for Enum Values
DWSIM uses many Enums (e.g., `PropertyPackageType`). Verify the exact spelling.

```bash
grep -r "Enum PropertyPackageType" libs/dwsim_src
```

## Common DWSIM Pitfalls

### 1. Automation Class Selection
- **WRONG**: `DWSIM.Automation.Automation` (Legacy, limited features)
- **CORRECT**: `DWSIM.Automation.Automation3` (Full-featured, use for tests and advanced automation)
- **Alias**: `using DWSIMAutomation = DWSIM.Automation.Automation3;`

### 2. Calculation Methods
- **WRONG**: `var errors = Automation.CalculateFlowsheet2(flowsheet)` (Automation3 returns **void**, not List<Exception>)
- **CORRECT**: `Automation.CalculateFlowsheet2(flowsheet);` (Call without assignment)
- **Error Checking**: Use `flowsheet.Solved` and `flowsheet.ErrorMessage` properties after calculation
- **Alternative**: `flowsheet.RequestCalculation(...)` (For async/internal calculation)

### 3. MaterialStream Compound Handling
- **WRONG**: Calling `flowsheet.AddCompoundsToMaterialStream(stream)` after `AddObject(ObjectType.MaterialStream, ...)`
- **CORRECT**: Just use `AddObject()` - it **already calls** `AddCompoundsToMaterialStream` internally
- **Error**: Double-calling causes `ArgumentException: An item with the same key has already been added`
- **Source**: `FlowsheetBase.vb:1297` shows `AddCompoundsToMaterialStream(myCOMS)` is called during `AddObjectToSurface`

### 4. Unit Operation Calculation Modes
- **Valve**: Default `CalcMode` is `DeltaP`. To specify outlet pressure, set `CalcMode = Valve.CalculationMode.OutletPressure` **before** setting `OutletPressure`
- **Heater/Cooler**: Set `CalcMode` to `OutletTemperature`, `HeatAdded`, etc. based on known specification
- **Compressor**: Use `CalculationMode.OutletPressure` when specifying target pressure

### 5. Heat Duty Sign Convention
- **DWSIM Convention**: Both Heater and Cooler report `DeltaQ` as **positive magnitude**
- Cooler does NOT return negative values for heat removed
- **Validation**: Check that cooler output temperature is lower than input, not the duty sign

### 6. Property Names Case Sensitivity
- **Observation**: VB.NET origin means some properties are lowercase or inconsistent.
- **Example**: `IPhaseProperties.molarfraction` (lowercase) instead of `VaporFraction` or `MolarFraction`.
- **Reference**: Check `docs/DWSIM_API/IPhaseProperties.cs` for exact property names.

### 7. Thread Safety
- **Constraint**: DWSIM Automation is **single-threaded**.
- **xUnit Config**: Use `xunit.runner.json` with `"parallelizeTestCollections": false` and `"maxParallelThreads": 1`
- **Worker**: Architecture handles this via `ConcurrentMessageLimit = 1`.
