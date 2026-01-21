---
agent_type: Implementation
agent_id: Agent_QA_1
handover_number: 1
last_completed_task: T4.3 (Attempted)
---

# Implementation Agent Handover File - Agent_QA

## Active Memory Context
**User Preferences:** 
- **Strict Adherence to Production Standards:** User explicitly forbade modifying production code (specifically `DbContext`) to accommodate test-specific constraints (like EF Core InMemory quirks).
- **Consultation over Hacks:** "It is better to stop and ask than to commit corrupt or hacky code."
- **Verification:** Ensure tests reflect reality, not a hacked environment.

**Working Insights:**
- **EF Core InMemory Limitations:** The project uses `jsonb` columns heavily (for `ResultJson`, `ConstantProperties`, `Composition`, etc.). The standard EF Core InMemory provider does NOT support these types or conversions natively without hacks.
- **Testing Strategy Shift:** Future testing needs to move away from `UseInMemoryDatabase` for components that rely on PostgreSQL-specific features (`jsonb`, `uuid[]`). Options include:
    - Testcontainers (Integration Tests).
    - A dedicated `TestDbContext` inheriting from `EnerflowDbContext` (if strictly necessary and isolated).
    - Mocking the Repository layer instead of the DbContext directly (Unit Tests).

## Task Execution Context
**Working Environment:**
- **Worker Project:** `Enerflow.Worker` is the focus.
- **Tests:** `Enerflow.Tests.Unit/WorkerTests/SimulationJobConsumerTests.cs` was created but failed due to `jsonb` mapping issues in the InMemory provider.
- **Rules Updated:** `AGENTS.md` and `.agent/rules/handling-constraints-and-scope.md` have been updated to strictly forbid the "hacky" approach I attempted.

**Issues Identified:**
- **Blocker:** `SimulationJobConsumerTests` are currently failing because `EnerflowDbContext` uses `jsonb`, which crashes on InMemory provider.
- **Resolution Path:** The incoming agent needs to refactor the tests to use a valid strategy (Mocking or Testcontainers) rather than hacking the DbContext.

## Current Context
**Recent User Directives:**
- "Remove the anti production and enterprise level of testing against a database that isn't the one used by the main app service."
- "Never do that again."
- "Update AGENTS.md and .agent/rules/handling-constraints-and-scope.md."

**Working State:**
- `EnerflowDbContext.cs` is clean (hacks reverted).
- `SimulationJobConsumerTests.cs` exists but fails.
- Rules are updated.

## Working Notes
**Development Patterns:**
- **Do not use `UseInMemoryDatabase`** for `EnerflowDbContext` if `jsonb` columns are involved.
- **Use `Mock<EnerflowDbContext>`** (difficult due to DbSet mocking) OR **Repository Pattern Mocks** (preferred if repository exists, otherwise Mock DbSet extensions might be needed, or just use Testcontainers).
