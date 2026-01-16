---
name: enerflow-architecture
description: Enforces Enerflow's specific Enterprise Worker architecture and DWSIM integration rules.
license: MIT
compatibility: opencode
metadata:
  project: enerflow
  type: architecture
---

## What I do

- **Enforce Worker Isolation**: Ensure DWSIM binaries are ONLY referenced in `Enerflow.Worker`. The API and Domain must remain pure.
- **DWSIM Safety Checks**: Verify that `DWSIM.GlobalSettings.Settings.AutomationMode = true` is set before any simulation logic.
- **Unit Consistency**: Enforce that all `StreamState` values (T, P, Flow) are handled in **SI Units** (Kelvin, Pascal, kg/s) within the Domain and Mapper layers.
- **Error Handling**: Ensure the Worker wraps `Solve()` calls in try-catch blocks and writes a `FailureResult` JSON instead of crashing silently.
- **DTO Usage**: Verify that data is passed between API and Worker via `Enerflow.Domain` DTOs, not DWSIM objects.
- **Block Non-Production Shortcuts**: If a DWSIM constraint or architectural blocker is hit, **DO NOT implement a "hacky" fix**. Stop and request User guidance. User feedback is required for all architectural hurdles.

## When to use me

- **Architectural Review**: When creating new projects or adding dependencies.
- **Worker Implementation**: When writing code in `Enerflow.Worker`.
- **API Design**: When designing endpoints that trigger simulations.

## Key Constraints

1. **Split Architecture**:
   - `Enerflow.API`: Orchestrator (DB, Queue, HTTP). NO DWSIM DLLs.
   - `Enerflow.Worker`: Executor (DWSIM DLLs). Transient or Hosted Service.
   - `Enerflow.Domain`: Shared Kernel (POCOs, Enums).

2. **DWSIM Integration**:
   - Always check `flowsheet.Solved` and `flowsheet.ErrorMessage`.
   - Never assume a `Calculate()` call succeeded without verification.

3. **Data Flow**:
   - API -> Redis (JSON) -> Worker -> DWSIM -> Worker -> Redis/DB (JSON) -> API.
