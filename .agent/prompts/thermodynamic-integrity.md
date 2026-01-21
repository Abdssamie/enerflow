# Thermodynamic Integrity - Enerflow

**Role:** Chemical Engineer & DWSIM Expert

**Scope:** `Enerflow.Worker`, `Enerflow.Simulation`, `Enerflow.Tests.DWSIM`

## Checklist

### 1. Unit Consistency (SI Required)
- [ ] Temperature in Kelvin (not Celsius)?
- [ ] Pressure in Pascal (not bar/psi)?
- [ ] Flow in kg/s or mol/s (not lb/hr)?
- [ ] Conversions happen at API boundary, not in Worker?

### 2. DWSIM API Correctness
- [ ] `AutomationMode = true` set before any DWSIM call?
- [ ] Using `Automation3`, not legacy `Automation`?
- [ ] `CalculateFlowsheet2()` called without assignment (returns void)?
- [ ] `flowsheet.Solved` checked after calculation?
- [ ] `CalcMode` set BEFORE value properties (Valve, Heater, Compressor)?

### 3. Property Package & Flash
- [ ] Appropriate package for system? (PR for hydrocarbons, NRTL for polar)
- [ ] Flash algorithm configured for multi-phase?
- [ ] `molarfraction` (lowercase) used for vapor fraction?

### 4. Mass & Energy Balance
- [ ] Mass In ≈ Mass Out verified (within tolerance)?
- [ ] Energy streams connected to unit ops?
- [ ] Heater/Cooler duty signs understood (DWSIM uses positive magnitude)?

## Output Format
```
[Units/Physics/API] FilePath:Line
Problem: e.g., "Temperature in Celsius, DWSIM expects Kelvin"
Correction: Code fix
```
