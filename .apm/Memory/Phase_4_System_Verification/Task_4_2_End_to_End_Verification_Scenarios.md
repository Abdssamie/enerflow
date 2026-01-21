# Task 4.2 - End-to-End Verification Scenarios

## Status
- **Date**: 2026-01-17
- **Status**: Blocked
- **Agent**: Agent_QA
- **Blocker**: MassTransit Connection Refused (Postgres Testcontainer).

## Objectives
- [x] Create `SimulationFlowTests.cs`.
- [x] Implement "Happy Path" (Mixer) scenario.
- [x] Implement "Error Path" (Disconnected Stream) scenario.
- [ ] Execute tests successfully (Currently failing on DB Connection).

## Implementation Details
- **Test Class**: `SimulationFlowTests` inherits `BaseIntegrationTest`.
- **Scenarios**:
  - `Can_Run_Simple_Mixer_Simulation`: Creates Sim -> Adds Water -> Adds Streams -> Adds Mixer -> Connects -> Submits -> Polls -> Verifies Mass Balance.
  - `Should_Fail_On_Disconnected_Stream`: Submits invalid topology -> Polls -> Verifies Error Code.
- **Fixes Applied**:
  - Fixed `JsonElement` handling in tests.
  - Fixed `System.Drawing.Common` / `libgdiplus` crash by using v6.0.0 and `runtimeconfig.template.json`.
  - Fixed `System.Configuration.ConfigurationManager` missing dependency.

## Issues & Blockers
1.  **MassTransit Connection Refused**:
    - **Error**: `Npgsql.NpgsqlException: Failed to connect to 127.0.0.1:xxxxx ... Connection refused`.
    - **Context**: The Worker (running in-process via `IntegrationTestWebAppFactory`) fails to connect to the PostgreSQL Testcontainer for the MassTransit transport.
    - **Status**: Active. Needs investigation into connection string propagation and Testcontainer port mapping visibility.

2.  **PREVIOUSLY RESOLVED**: `System.Drawing.Common` crash on Linux.
    - **Fix**: Downgraded to `System.Drawing.Common` 6.0.0 and enabled `System.Drawing.EnableUnixSupport` in runtime config. Added `System.Configuration.ConfigurationManager`.

## Next Steps for Resolution
1.  **Debug Connection String**: Verify exactly what connection string the `SimulationJobConsumer` is receiving.
2.  **Check Service Configuration**: Ensure `IntegrationTestWebAppFactory` properly overrides the MassTransit configuration for the Worker services.
3.  **Inspect Docker Networking**: Verify if the random port assigned by Testcontainers is accessible to the test runner process (it should be, as it's localhost).
