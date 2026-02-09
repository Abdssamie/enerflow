# Archived Plans

This directory contains completed implementation plans.

## Completed Plans

### 2026-02-09

1. **fix-unit-operation-mapper-bug.md** ✅
   - Fixed duplicate object creation bug
   - Changed mappers to use lookup instead of AddObject
   - Commits: `f40f156`, `9970bab`

2. **architecture-cleanup.md** ✅
   - Removed legacy SimulationService
   - Removed redundant connection logic from Builder
   - Commits: `8390f52`, `3ea805e`

3. **fix-functional-test-masstransit.md** ✅
   - Fixed MassTransit configuration in functional tests
   - Added MassTransitHostOptions to ensure bus starts
   - Commit: `a474425`

4. **Migration to CalculateFlowsheet4** ✅
   - Migrated from RequestCalculationAndWait to CalculateFlowsheet4
   - Simplified solver architecture
   - Commit: `14ec62a`

## Active Plans

See `docs/plans/` for current active plans.
