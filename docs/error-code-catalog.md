# Enerflow Error Code Catalog

**Version**: 1.0  
**Last Updated**: 2025-02-01  
**Purpose**: Comprehensive reference for all validation error codes in Enerflow

---

## Table of Contents

1. [Topology Errors](#topology-errors)
2. [Compound Errors](#compound-errors)
3. [Physical Property Errors](#physical-property-errors)
4. [Unit Operation Configuration Errors](#unit-operation-configuration-errors)
5. [Unit Operation Topology Errors](#unit-operation-topology-errors)
6. [Generic Errors](#generic-errors)
7. [Error Response Format](#error-response-format)
8. [Troubleshooting Guide](#troubleshooting-guide)

---

## Topology Errors

### DISCONNECTED_UNIT

**Code**: `DISCONNECTED_UNIT`  
**Severity**: Error  
**Entity Type**: UnitOperation

**Description**: A unit operation has no connected streams (neither input nor output).

**Message Format**:
```
Unit operation '{unitName}' has no connected streams
```

**Causes**:
- Unit operation was created but streams were not connected
- All connections were removed
- Stream IDs in InputStreamIds/OutputStreamIds don't match any streams

**Resolution**:
1. Ensure the unit has at least one input or output stream
2. Verify stream IDs are correct
3. Check that streams exist in the simulation

**Example**:
```json
{
  "code": "DISCONNECTED_UNIT",
  "message": "Unit operation 'Mixer1' has no connected streams",
  "entityType": "UnitOperation",
  "entityName": "Mixer1",
  "severity": "Error"
}
```

---

### ORPHANED_STREAM

**Code**: `ORPHANED_STREAM`  
**Severity**: Error  
**Entity Type**: MaterialStream or EnergyStream

**Description**: A stream is not connected to any unit operation.

**Message Format**:
```
Stream '{streamName}' is not connected to any unit operation
```

**Causes**:
- Stream was created but not connected to any unit
- Unit operation was deleted but stream remained
- Stream ID not referenced in any unit's InputStreamIds/OutputStreamIds

**Resolution**:
1. Connect the stream to at least one unit operation
2. Delete the stream if it's not needed
3. Verify unit operation connections

**Example**:
```json
{
  "code": "ORPHANED_STREAM",
  "message": "Stream 'Feed' is not connected to any unit operation",
  "entityType": "MaterialStream",
  "entityName": "Feed",
  "severity": "Error"
}
```

---

## Compound Errors

### NO_COMPOUNDS_DEFINED

**Code**: `NO_COMPOUNDS_DEFINED`  
**Severity**: Error  
**Entity Type**: Simulation

**Description**: The simulation has no compounds defined.

**Message Format**:
```
Simulation must have at least one compound defined
```

**Causes**:
- Simulation was created without adding compounds
- All compounds were deleted

**Resolution**:
1. Add at least one compound to the simulation
2. Compounds must exist in DWSIM's compound database

**Example**:
```json
{
  "code": "NO_COMPOUNDS_DEFINED",
  "message": "Simulation must have at least one compound defined",
  "entityType": "Simulation",
  "entityName": "MySimulation",
  "severity": "Error"
}
```

---

### UNDEFINED_COMPOUND_REFERENCE

**Code**: `UNDEFINED_COMPOUND_REFERENCE`  
**Severity**: Error  
**Entity Type**: MaterialStream

**Description**: A stream's composition references a compound that doesn't exist in the simulation.

**Message Format**:
```
Stream '{streamName}' references undefined compound '{compoundName}'
```

**Causes**:
- Compound name in composition doesn't match any defined compound
- Typo in compound name
- Compound was deleted after stream was created

**Resolution**:
1. Add the missing compound to the simulation
2. Fix the compound name in the stream composition
3. R the compound from the composition if not needed

**Note**: Compound matching is case-insensitive ("Water" matches "water")

**Example**:
```json
{
  "code": "UNDEFINED_COMPOUND_REFERENCE",
  "message": "Stream 'Feed' references undefined compound 'Methanol'",
  "entityType": "MaterialStream",
  "entityName": "Feed",
  "severity": "Error"
}
```

---

### INVALID_LIGHT_KEY_REFERENCE

**Code**: `INVALID_LIGHT_KEY_REFERENCE`  
**Severity**: Error  
**Entity Type**: ShortcutColumn

**Description**: The ShortcutColumn's LightKey references an invalid compound ID.

**Message Format**:
```
Shor'{columnName}' has invalid LightKey reference
```

**Causes**:
- LightKey GUID doesn't match any compound ID
- Compound was deleted after column was configured

**Resolution**:
1. Set LightKey to a valid compound ID from the simulation
2. Ensure the compound exists before configuring the column

**Example**:
```json
{
  "code": "INVALID_LIGHT_KEY_REFERENCE",
  "message": "ShortcutColumn 'Column1' has invalid LightKey reference",
  "entityType": "ShortcutColumn",
  "entityName": "Column1",
  "severity": "Error"
}
```

---

### INVALID_HEAVY_KEY_REFERENCE

**Code**: `INVALID_HEAVY_KEY_REFERENCE`  
verity**: Error  
**Entity Type**: ShortcutColumn

**Description**: The ShortcutColumn's HeavyKey references an invalid compound ID.

**Message Format**:
```
ShortcutColumn '{columnName}' has invalid HeavyKey reference
```

**Causes**:
- HeavyKey GUID doesn't match any compound ID
- Compound was deleted after column was configured

**Resolution**:
1. Set HeavyKey to a valid compound ID from the simulation
2. Ensure the compound exists before configuring the column

**Example**:
```json
{
  "code": "INVALID_HEAVY_KEY_REFERENCE",
  "message": "ShortcutColumn 'Column1' has invalid HeavyKey reference",
  "entityType": "ShortcutColumn",
  "entityName": "Column1",
  "severity": "Error"
}
```

---

## Physical Property Errors

### INVALID_TEMPERATURE

**Code**: `INVALID_TEMPERATURE`  
**Severity**: Error  
**Entity Type**: MaterialStream

**Description**: Temperature is zero or negative (must be > 0 K).

**Message Format**:
```
Temperature must be greater than 0 K. (Parameter 'Temperature')
```

**Causes**:
- Temperature set to 0 or negative value
- Temperature not initialized

**Resolution**:
1. Set temperature to a positive value in Kelvin
2. Typical range: 200-1500 K for most processes

**Example**:
```json
{
  "code": "INVALID_TEMPERATURE",
  "message": "Temperature must be greater than 0 K. (Parameter 'Temperature')",
  "entityType": "MaterialStream",
  "entityName": "Feed",
  "severity": "Error"
}
```

---

### INVALID_PRESSURE

**Code**: `INVALID_PRESSURE`  
**Severity**: Error  
**Entity Type**: MaterialStream

**Description**: Pressure is zero or negative (must be > 0 Pa).

**Message Format**:
```
Pressure must be greater than 0 Pa. (Parameter 'Pressure')
```

**Causes**:
- Pressure set to 0 or negative value
- Pressure not initialized

**Resolution**:
1. Set pressure to a positive value in Pascals
2. Typical range: 1000-10,000,000 Pa (0.01-100 bar)

**Example**:
```json
{
  "code": "INVALID_PRESSURE",
  "message": "Pressure must be greater than 0 Pa. (Parameter 'Pressure')",
  "entityType": "MaterialStream",
  "entityName": "Feed",
  "severity": "Error"
}
```

---

### INVALID_MASS_FLOW

**Code**: `INVALID_MASS_FLOW`  
**Severity**: Error  
**Entity Type**: MaterialStream

**Description**: Mass flow is negative (must be ≥ 0 kg/s).

**Message FormaassFlow must be non-negative. (Parameter 'MassFlow')
```

**Causes**:
- Mass flow set to negative value

**Resolution**:
1. Set mass flow to zero or positive value
2. Zero mass flow is allowed (for inactive streams)

**Example**:
```json
{
  "code": "INVALID_MASS_FLOW",
  "message": "MassFlow must be non-negative. (Parameter 'MassFlow')",
  "entityType": "MaterialStream",
  "entityName": "Feed",
  "severity": "Error"
}
```

---

### INVALID_ENERGY_FLOW

**Code**: `INVALID_ENERGY_FLOW`  
**Severity**: Error  
**Entity Type**: EnergyStream

**Description**: Energy flow is negative (must be ≥ 0 W).**Message Format**:
```
EnergyFlow must be non-negative. (Parameter 'EnergyFlow')
```

**Causes**:
- Energy flow set to negative value

**Resolution**:
1. Set energy flow to zero or positive value
2. Use positive values for heating, negative for cooling (if supported)

**Example**:
```json
{
  "code": "INVALID_ENERGY_FLOW",
  "message": "EnergyFlow must be non-negative. (Parameter 'EnergyFlow')",
  "entityType": "EnergyStream",
  "entityName": "HeatDuty",
  "severity": "Error"
}
```

---

### INVALID_COMPOSITION_SUM

**Code**: `INVALID_COMPOSITION_SUM`  
**Severity**: Error  
**Entity Type**: MaterialStream

**Description**: Mole fractions don't sum to 1.0 (tolerance: ±0.01).

**Message Format**:
```
Stream '{streamName}' composition sums to {sum:F4} (must be 1 ± 0.01). Please adjust mole fractions to sum to 1.
```

**Causes**:
- Mole fractions sum to value outside 0.99-1.01 range
- Composition not normalized

**Resolution**:
1. Adjust mole fractions to sum to exactly 1.0
2. Normalize: divide each fraction by the sum

**Example**:
```json
{
  "code": "INVALID_COMPOSITION_SUM",
  "message": "Stream 'Feed' composition sums to 0.8000 (must be 1 ± 0.01). Please adjust mole fractions to sum to 1.",
  ": "MaterialStream",
  "entityName": "Feed",
  "severity": "Error"
}
```

---

### NEGATIVE_COMPOSITION

**Code**: `NEGATIVE_COMPOSITION`  
**Severity**: Error  
**Entity Type**: MaterialStream

**Description**: One or more mole fractions are negative.

**Message Format**:
```
Stream '{streamName}' has negative composition for '{compoundName}': {value}
```

**Causes**:
- Mole fraction set to negative value

**Resolution**:
1. Set all mole fractions to non-negative values
2. Remove compounds with zero mole fraction if not needed

**Example**:
```json
{
  "code": "NEGATIVE_COMPOSITION",
  "message": "Stream 'Feed' has negative composition for 'Ethanol': -0.2",
  "entityType": "MaterialStream",
  "entityName": "Feed",
  "severity": "Error"
}
```

---

## Unit Operation Configuration Errors

### INVALID_EFFICIENCY

**Code**: `INVALID_EFFICIENCY`  
**Severity**: Error  
**Entity Type**: Heater or Pump

**Description**: Efficiency is outside valid range (must be 0 < efficiency ≤ 1.0).

**Message Format**:
```
Efficiency must be between 0 and 1. (Parameter 'Efficiency')
```

**Causes**:
- Efficiency set to 0, negative, or > 1.0

**Resolution**:
1. Set efficiency to value between 0.01 and 1.0
2. Typical values: 0.7-0.95 for most equipment

**Example**:
```json
{
  "code": "INVALID_EFFICIENCY",
  "message": "Efficiency must be between 0 and 1. (Parameter 'Efficiency')",
  "entityType": "Heater",
  "entityName": "Heater1",
  "severity": "Error"
}
```

---

### INVALID_OUTLET_PRESSURE

**Code**: `INVALID_OUTLET_PRESSURE`  
**Severity**: Error  
**Entity Type**: Valve or Pump

**Description**: Outlet pressure is negative (must be ≥ 0 Pa).

**Message Format**:
```
OutletPressure must be non-negative. (Parameter 'OutletPressure')
```

**Causes**:
- Outlet pressure set to negative value

**Resolution**:
1. Set outlet pressure to positive value in Pascals
2. For valves: outlet pressure < inlet pressure
3. For pumps: outlet pressure > inlet pressure

**Example**:
```json
{
  "code": "INVALID_OUTLET_PRESSURE",
  "message": "OutletPressure must be non-negative. (Parameter 'OutletPressure')",
  "entityType": "Valve",
  "entityName": "Valve1",
  "severity": "Error"
}
```

---

### INVALID_REFLUX_RATIO

**Code**: `INVALID_REFLUX_RATIO`  
**Severity**: Error  
**Entity Type**: ShortcutColumn

**Description**: Reflux ratio is negative (must be ≥ 0).

**Message Format**:
```
RefluxRatio must be non-negative. (Parameter 'RefluxRatio')
```

**Causes**:
- Reflux ratio set to negative value

**Resolution**:
1. Set reflux ratio to non-negative value
2. Typical range: 1.2 × Rmin to 2.0 × Rmin

**Example**:
```json
{
  "code": "INVALID_REFLUX_RATIO",
  "message": "RefluxRatio must be non-negative. (Parameter 'RefluxRatio')",
  "entityType": "ShortcutColumn",
  "entityName": "Column1",
  "severity": "Error"
}
```

---

### INVALID_STAGES_COUNT

**Code**: `INVALID_STAGES_COUNT`  
**Severity**: Error  
**Entity Type**: ShortcutColumn

**Description**: Number of stages is zero or negative (must be > 0).

**Message Format**:
```
Stages must be greater than 0. (Parameter 'Stages')
```

**Causes**:
- Stages set to 0 or negative value

**Resolution**:
1. Set stages to positive integer
2. Typical range: 10-100 stages for most columns

**Example**:
```json
{
  "code": "INVALID_STAGES_COUNT",
  "message": "Stages must be greater than 0. (Parameter 'Stages')",
  "entityType": "ShortcutColumn",
  "entityName": "Column1",
  "severity": "Error"
}
```

---

### INVALID_TOLERANCE

**Code**: `INVALID_TOLERANCE`  
**Severity**: Error  
**Entity Type**: Recycle

**Description**: Convergence tolerance is zero or negative (must be > 0).

**Message Format**:
```
Tolerance must be positive. (Parameter 'Tolerance')
```

**Causes**:
- Tolerance set to 0 or negative value

**Resolution**:
1. Set tolerance to small positive value
2. Typical range: 1e-6 to 1e-3

**Example**:
```json
{
  "code": "INVALID_TOLERANCE",
  "message": "Tolerance must be positive. (Parameter 'Tolerance')",
  "entityType": "Recycle",
  "entityName": "Recycle1",
  "severity": "Error"


---

### INVALID_MAX_ITERATIONS

**Code**: `INVALID_MAX_ITERATIONS`  
**Severity**: Error  
**Entity Type**: Recycle

**Description**: Maximum iterations is zero or negative (must be > 0).

**Message Format**:
```
MaxIterations must be greater than 0. (Parameter 'MaxIterations')
```

**Causes**:
- MaxIterations set to 0 or negative value

**Resolution**:
1. Set max iterations to positive integer
2. Typical range: 50-200 iterations

**Example**:
```json
{
  "code": "INVALID_MAX_ITERATIONS",
  "message": "MaxIterations must be greater than 0. (Parameter 'MaxIterations')",
  "entityType": "Recycle",
  "entityName": "Recycle1",
  "severity": "Error"
}
```

---

### SPLITTER_INVALID_RATIOS

**Code**: `SPLITTER_INVALID_RATIOS`  
**Severity**: Error  
**Entity Type**: Splitter

**Description**: Split ratios don't sum to 1.0 (tolerance: ±0.01).

**Message Format**:
```
Splitter split ratios sum to {sum:F4} (must be 1 ± 0.01)
```

**Causes**:
- Split ratios sum to value outside 0.99-1.01 range

**Resolution**:
1. Adjust split ratios to sum to exactly 1.0
2. Normalize: divide each ratio by the sum

**Example**:
```json
{
  "code": "SPLITTER_INVALID_RATIOS",
  "message": "Splitter split ratios sum to 0.800be 1 ± 0.01)",
  "entityType": "Splitter",
  "entityName": "Splitter1",
  "severity": "Error"
}
```

---

## Unit Operation Topology Errors

### UNIT_REQUIRES_SINGLE_INPUT

**Code**: `UNIT_REQUIRES_SINGLE_INPUT`  
**Severity**: Error  
**Entity Type**: Heater, Cooler, Pump, Compressor, Valve

**Description**: Unit operation requires exactly one input stream.

**Message Format**:
```
{UnitType} must have exactly one input stream.
```

**Causes**:
- Unit has 0 or multiple input streams

**Resolution**:
1. Connect exactly one input stream to the unit

**Example**:
```json
{
  "code": "UNIT_REQUIRES_SINGLE_INPUT",
  "message": "Heater must have exactly one input stream.",
  "entityType": "Heater",
  "entityName": "Heater1",
  "severity": "Error"
}
```

---

### UNIT_REQUIRES_SINGLE_OUTPUT

**Code**: `UNIT_REQUIRES_SINGLE_OUTPUT`  
**Severity**: Error  
**Entity Type**: Heater, Cooler, Pump, Compressor, Valve, Mixer

**Description**: Unit operation requires exactly one output stream.

**Message Format**:
```
{UnitType} must have exactly one output stream.
```

**Causes**:
- Unit has 0 or multiple output streams

**Resolution**:
1. Connect exactly one output stream to the unit

**Example**:
```json
{
  "code": "UNIT_REQUIRES_SINGLE_OUTPUT",
  "message": "Mixer must have exactly one output stream.",
  "entityType": "Mixer",
  "entityName": "Mixer1",
  "severity": "Error"
}
```

---

### UNIT_REQUIRES_MULTIPLE_INPUTS

**Code**: `UNIT_REQUIRES_MULTIPLE_INPUTS`  
**Severity**: Error  
**Entity Type**: Mixer

**Description**: Mixer requires at least 2 input streams.

**Message Format**:
```
Mixer must have at least 2 input streams.
```

**Causes**:
- Mixer has 0 or 1 input stream

**Resolution**:
1. Connect at least 2 input streams to the mixer

**Example**:
```json
{
  "code": "UNIT_REQUIRES_MULTIPLE_INPUTS",
  "message": "Mixer must have at least 2 input streams.",
  "entityType": "Mixer",
  "entityName": "Mixer1",
  "severity": "Error"
}
```

---

### UNIT_REQUIRES_MULTIPLE_OUTPUTS

**Code**: `UNIT_REQUIRES_MULTIPLE_OUTPUTS`  
**Severity**: Error  
**Entity Type**: Splitter

**Description**: Splitter requires at least 2 output streams.

**Message Format**:
```
Splitter must have at least 2 output streams.
```

**:
- Splitter has 0 or 1 output stream

**Resolution**:
1. Connect at least 2 output streams to the splitter

**Example**:
```json
{
  "code": "UNIT_REQUIRES_MULTIPLE_OUTPUTS",
  "message": "Splitter must have at least 2 output streams.",
  "entityType": "Splitter",
  "entityName": "Splitter1",
  "severity": "Error"
}
```

---

### UNIT_REQUIRES_INPUT

**Code**: `UNIT_REQUIRES_INPUT`  
**Severity**: Error  
**Entity Type**: FlashDrum

**Description**: Unit operation requires at least one input stream.

**Message Format**:
```
{UnitType} must have at least one input stream.
```

**Causes**:
- Unit has no input streams

**Resolution**:
1. Connect at least one input stream to the unit

**Example**:
```json
{
  "code": "UNIT_REQUIRES_INPUT",
  "message": "FlashDrum must have at least one input stream.",
  "entityType": "FlashDrum",
  "entityName": "Flash1",
  "severity": "Error"
}
```

---

### UNIT_REQUIRES_TWO_OUTPUTS

**Code**: `UNIT_REQUIRES_TWO_OUTPUTS`  
**Severity**: Error  
**Entity Type**: FlashDrum

**Description**: Flash drum requires exactly 2 output streams (vapor + liquid).

**Message Format**:
```
FlashDrum must have exactly two output streams.
```

**Causes**:
- Flash drum has fewer or more than 2 output streams

**Resolution**:
1. Connect exactly 2 output streams (one for vapor, one for liquid)

**Example**:
```json
{
  "code": "UNIT_REQUIRES_TWO_OUTPUTS",
  "message": "FlashDrum must have exactly two output streams.",
  "entityType": "FlashDrum",
  "entityName": "Flash1",
  "severity": "Error"
}
```

---

## Generic Errors

### VALIDATION_ERROR

**Code**: `VALIDATION_ERROR`  
**Severity**: Error  
**Entity Type**: Various

**Description**: Generic validation error (used when specific error code not available).

**Message Format**:
```
{Specific error message from validator}
```

**Causes**:
- Various validation failures
- Fallback for unexpected errors

**Resolution**:
1. Read the specific error message
2. Fix the issue described

**Example**:
```json
{
  "code": "VALIDATION_ERROR",
  "message": "Pressure must be greater than 0 Pa. (Parameter 'Pressure')",
  "entityType": "MaterialStream",
  "entityName": "Feed",
  "severity": "Error"
}
```

---

### NULL_REFERENCE_ERROR

**Code**: `NULL_REFERENCE_ERROR`  
**Severity**: Error  
**Entity Type**: Various

**Description**: Required property is null.

**Message Format**:
```
{Property} cannot be null. (Parameter '{PropertyName}')
```

**Causes**:
- Required property not initialized
- Null value assigned to non-nullable property

**Resolution**:
1. Initialize the required property
2. Provide a valid value

**Example**:
```json
{
  "code": "NULL_REFERENCE_ERROR",
  "message": "Composition cannot be null. (Parameter 'Composition')",
  "entityType": "MaterialStream",
  "entityName": "Feed",
  "severity": "Error"
}
```

---

## Error Response Format

### API Response Structure

```json
{
  "simulationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "ValidationFailed",
  "errors": [
    {
      "code": "INVALID_TEMPERATURE",
      "message": "Temperature must be greater than 0 K. (Parameter 'Temperature')",
      "entityType": "MaterialStream",
      "entityName": "Feed",
      "severity": "Error"
    },
    {
      "code": "DISCONNECTED_UNIT",
      "message": "Unit operation 'Mixer1' has no connected streams",
      "entityType": "UnitOperation",
      "entityName": "Mixer1",
      "severity": "Error"
    }
  ],
  "warnings": []
}
```

### Error Object Schema

```typescript
interface ValidationError {
  code: string;           // Error code from this catalog
  message: string;        // Human-readable error message
  entityType: string;     // Type of entity (MaterialStream, UnitOperation, etc.)
  entityName: string;     // Name of the specific entity
  severity: "Error" | "Warning";  // Severity level
}
```

---

## Troubleshooting Guide

### Common Error Combinations

#### "Simulation won't run - multiple errors"

**Symptoms**: Multiple validation errors returned

**Solution**:
1. Fix errors in order: Topology → Compounds → Properties → Unit Configs
2. Topology errors often cause cascading issues
3. Fix disconnected units first

#### "Stream composition issues"

**Symptoms**: `INVALID_COMPOSITION_SUM` or `NEGATIVE_COMPOSITION`

**Solution**:
```python
# Normalize composition
total = sum(composition.values())
normalized = {k: v/total for k, v in composition.items()}
```

#### "Unit operation not working"

**Symptoms**: `UNIT_REQUIRES_*` errors

**Solution**:
1. Check unit operation type requirements
2. Verify correct number of inputs/outputs
3. Ensure streams are properly connected

### Debugging Tips

1. **Enable Detailed Logging**: Set log level to Debug for validation details
2. **Check Entity Names**: Ensure entity names match between streams and units
3. **Verify GUIDs**: Check that stream/compound IDs are correct
4. **Test Incrementally**: Add one unit at a time and validate
5. **Use Validation Endpoint**: Call `/api/simulations/{id}/validate` before running

### Support Resources

- **Documentation**: `/docs/ENERFLOW_SIMULATION_GUIDE.md`
- **API Reference**: `/swagger`
- **Test Examples**: `/Enerflow.Tests.Unit/Worker/Validation/`
- **GitHub Issues**: Report bugs with error codes

---

## Appendix: Error Code Quick Reference

| Code | Category | Severity | Entity Type |
|------|----------|----------|-------------|
| DISCONNECTED_UNIT | Topology | Error | UnitOperation |
| ORPHANED_STREAM | Topology | Error | Stream |
| NO_COMPOUNDS_DEFINED | Compound | Error | Simulation |
| UNDEFINED_COMPOUND_REFERENCE | Compound | Error | MaterialStream |
| INVALID_LIGHT_KEY_REFERENCE | Compound | Error | ShortcutColumn |
| INVALID_HEAVY_KEY_REFERENCE | Compound | Error | ShortcutColumn |
| INVALID_TEMPERATURE | Property | Error | MaterialStream |
| INVALID_PRESSURE | Property | Error | MaterialStream |
| INVALID_MASS_FLOW | Property | Error | MaterialStream |
| INVALID_ENERGY_FLOW | Property | Error | EnergyStream |
| INVALID_COMPOSITION_SUM | Property | Error | MaterialStream |
| NEGATIVE_COMPOSITION | Property | Error | MaterialStream |
| INVALID_EFFICIENCY | Config | Error | Heater/Pump |
| INVALID_OUTLET_PRESSURE | Config | Error | Valve/Pump |
| INVALID_REFLUX_RATIO | Config | Error | ShortcutColumn |
| INVALID_STAGES_COUNT | Config | Error | ShortcutColumn |
| INVALID_TOLERANCE | Config | Error | Recycle |
| INVALID_MAX_ITERATIONS | Config | Error | Recycle |
| SPLITTER_INVALID_RATIOS | Config | Error | Splitter |
| UNIT_REQUIRES_SINGLE_INPUT | Topology | Error | Various |
| UNIT_REQUIRES_SINGLE_OUTPUT | Topology | Error | Various |
| UNIT_REQUIRES_MULTIPLE_INPUTS | Topology | Error | Mixer |
| UNIT_REQUIRES_MULTIPLE_OUTPUTS | Topology | Error | Splitter |
| UNIT_REQUIRES_INPUT | Topology | Error | FlashDrum |
| UNIT_REQUIRES_TWO_OUTPUTS | Topology | Error | FlashDrum |
| VALIDATION_ERROR | Generic | Error | Various |
| NULL_REFERENCE_ERROR | Generic | Error | Various |

---

**Document Version**: 1.0  
**Total Error Codes**: 26  
**Last Updated**: 2025-02-01  
**Maintained By**: Enerflow Development Team
