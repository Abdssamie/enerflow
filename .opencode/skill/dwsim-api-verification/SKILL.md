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

Since `libs/dwsim_src` contains the authoritative source code, use `grep` and `read` to validate your assumptions.

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

### 1. Calculation Methods
- **WRONG**: `flowsheet.CalculateFlowsheet2(...)` (Often void or deprecated in patched binaries)
- **CORRECT**: `flowsheet.RequestCalculation(...)`

### 2. Automation Mode
- **Requirement**: `DWSIM.GlobalSettings.Settings.AutomationMode = true` must be set.
- **Verification**: Check `DWSIM.GlobalSettings/Settings.vb` to confirm the property exists if you are unsure.

### 3. Thread Safety
- **Constraint**: DWSIM Automation is **single-threaded**.
- **Verification**: Ensure no `Task.Run` wraps direct DWSIM calls without a lock, although the Worker architecture handles this via `ConcurrentMessageLimit = 1`.
