# Enerflow Implementation Task: Strongly-Typed Domain & Advanced Solver

You are tasked with implementing the core domain architecture and solver logic for Enerflow, enabling it to run the DWSIM simulations verified in tests 01-10.

## Context
We have successfully verified the DWSIM API through `Enerflow.Tests.DWSIM`. We must now translate these findings into a robust, type-safe backend architecture. The user explicitly rejects generic JSON blobs; the system must be strongly typed and organized.

## Core Directives

### 1. Strongly-Typed Domain Modeling (`Enerflow.Domain`)
Refactor the domain to use explicit classes for supported Unit Operations. **Do not use generic JSON fields.**

- **Base Class**: `SimulationObject` (Id, Name, Position).
- **Derived Classes**: Create specific classes for every unit operation verified in tests:
  - `Heater` / `Cooler` (Properties: `Efficiency`, `OutletTemperature`, `PressureDrop`)
  - `Mixer` / `Splitter` (Properties: `SplitRatios`)
  - `Valve` (Properties: `OutletPressure`)
  - `FlashDrum` (Properties: `Temperature`, `Pressure`)
  - `ShortcutColumn` (Properties: `RefluxRatio`, `LightKey`, `HeavyKey`, `CondenserPressure`, etc.)
  - `Recycle` (Properties: `Tolerance`, `Acceleration`)
- **Enums**: Implement explicit enums for all modes (e.g., `FlashAlgorithm`, `CalculationMode`, `PropertyPackageType`).

### 2. The "Super-Solver" with Wegstein Acceleration (`Enerflow.Worker`)
Implement a custom convergence loop in the Worker's `SimulationService` to handle recycle loops robustly, as DWSIM's internal solver can be insufficient for complex topologies.

**Algorithm:**
1.  **Build Flowsheet**: Map the domain objects to DWSIM objects.
2.  **Iterate**: Run `Automation.CalculateFlowsheet2(flowsheet)`.
3.  **Check Convergence**: Verify mass/energy balance error is `< 1e-4`.
4.  **Tearing & Acceleration**: If not converged, update recycle streams using **Wegstein Acceleration**:
    $$X_{next} = \lambda \cdot X_{calc} + (1 - \lambda) \cdot X_{old}$$
    - $\lambda$ (Lambda): Damping factor (default 0.7, configurable).
    - $X_{calc}$: The value calculated in this iteration.
    - $X_{old}$: The value from the previous iteration.
5.  **Fail/Succeed**: Throw exception if `MaxIterations` is reached.

### 3. Incremental Integration Strategy
Only implement support for the Unit Operations and Property Packages that have **passed tests**.
- **Supported**: Material Stream, Energy Stream, Mixer, Splitter, Heater, Cooler, Valve, Flash Vessel, Shortcut Column, Recycle Block.
- **Deferred**: Reactors (pending further testing of reaction set configuration).

### 4. Code Standards
- **Validation**: Ensure domain entities validate their properties (e.g., `RefluxRatio > 0`).
- **Mapping**: Use specific "Mappers" or "Builders" in the Worker to translate strong domain types to DWSIM API calls.
- **Safety**: Maintain the `ConcurrentMessageLimit = 1` constraint for the Worker.

## Deliverables
1.  Update `Enerflow.Domain` with the class hierarchy.
2.  Implement the `DWSIMSolver` in `Enerflow.Worker` with the Wegstein loop.
3.  Create the Mappers/Builders to bridge Domain -> DWSIM.
