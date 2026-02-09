# Enerflow Development Roadmap - Next Steps

## 🎯 Current Status (2026-02-09)

### ✅ Recently Completed
1. **Fixed DWSIM Mixer Bug** - Outlet streams now calculate correctly (3.0 kg/s = 1.0 + 2.0)
2. **Migrated to CalculateFlowsheet4()** - Using modern DWSIM API
3. **Fixed Mapper Architecture** - No more duplicate object creation
4. **Cleaned Up Legacy Code** - Removed SimulationService, redundant connection logic
5. **All Tests Passing** - 62/62 tests pass (11 DWSIM + 51 functional)

---

## 📋 Active Plans (Priority Order)

### 1. **Cleanup Unnecessary Code** 🔥 HIGH PRIORITY
**Plan:** `docs/plans/2026-02-09-cleanup-unnecessary-code.md`

**Why Now:** After migrating to CalculateFlowsheet4(), we have dead code adding maintenance overhead.

**What to Remove:**
- ❌ `ErrorCalculator` - Not used by CalculateFlowsheet4
- ❌ `ConvergenceConfig` - DWSIM handles convergence internally
- ❌ `Convergence/` folder - Custom convergence logic obsolete
- ❌ `PostConnectionConfigurator` - Can be merged into UnitOperationMapper
- ❌ `ConvergenceException` - Never thrown

**Expected Impact:**
- Remove ~500+ lines of code
- Simplify DWSIMSolver
- Reduce DI complexity
- Clearer architecture

**Estimated Time:** 2-3 hours

---

### 2. **Add Unit-Specific Result Extraction** 📊 MEDIUM PRIORITY
**Plan:** `docs/plans/2026-02-09-add-unit-specific-result-extraction.md`

**Why:** Currently we only extract generic status. Users need calculated properties like:
- Heater: `DeltaQ`, `DeltaT`, `Efficiency`
- Cooler: `DeltaQ`, `DeltaT`, `Efficiency`
- Valve: `DeltaP`, `OutletPressure`
- Splitter: `Ratios`
- Vessel: `VaporFraction`, `LiquidFraction`

**Approach:**
1. Document available properties from DWSIM API
2. Write failing tests for each unit type
3. Implement type-specific extraction in `ResultCollector`
4. Verify with integration tests

**Estimated Time:** 4-6 hours

---

### 3. **Fix Energy Stream Wiring** ⚡ LOW PRIORITY
**Plan:** `docs/plans/2026-02-09-fix-topology-wiring.md`

**Why:** Energy streams (for heaters/coolers) are not connected properly.

**Status:** 
- Material streams work ✅
- Energy streams need implementation ⚠️

**Impact:** 
- Currently: Heaters/Coolers work but energy streams aren't tracked
- After fix: Full energy balance tracking

**Estimated Time:** 2-3 hours

---

## 🚀 Recommended Execution Order

### Phase 1: Cleanup ✅ COMPLETED
- Removed ~270 lines of unused code
- Simplified solver architecture (DWSIMSolver: 221 → 180 lines)
- All 62 tests passing
- See: `docs/ARCHITECTURE/SOLVER_PIPELINE.md`
- Archived: `docs/plans/archive/2026-02-09-cleanup-unnecessary-code.md`

**Why First:** Clean foundation before adding new features. Reduces cognitive load.

---

### Phase 2: Result Extraction (Next Session)
```bash
# Execute result extraction plan
1. Document DWSIM unit operation properties
2. Write failing tests for each unit type
3. Implement type-specific extraction
4. Verify with integration tests
5. Commit changes
```

**Why Second:** Adds immediate user value. Tests ensure correctness.

---

### Phase 3: Energy Streams (Future)
```bash
# Execute energy stream wiring plan
1. Implement energy stream connections in ConnectionMapper
2. Write tests for heater with energy stream
3. Verify energy balance tracking
4. Commit changes
```

**Why Last:** Nice-to-have feature. System works without it.

---

## 📊 Success Metrics

### After Phase 1 (Cleanup):
- ✅ ~500 lines of code removed
- ✅ Simpler DWSIMSolver (< 200 lines)
- ✅ Fewer DI registrations (< 10 services)
- ✅ All tests still pass

### After Phase 2 (Result Extraction):
- ✅ Unit-specific properties in API responses
- ✅ Heater returns `DeltaQ`, `Efficiency`
- ✅ Valve returns `DeltaP`, `OutletPressure`
- ✅ Test coverage for all unit types

### After Phase 3 (Energy Streams):
- ✅ Energy streams connected to heaters/coolers
- ✅ Energy balance tracking works
- ✅ Integration tests verify energy flows

---

## 🎯 Next Immediate Action

**Start with:** `docs/plans/2026-02-09-cleanup-unnecessary-code.md`

**Command:**
```bash
# Load the cleanup plan and execute task-by-task
cat docs/plans/2026-02-09-cleanup-unnecessary-code.md
```

**Expected Duration:** 2-3 hours

**Expected Outcome:** Cleaner, simpler codebase with all tests passing.

---

## 📝 Notes

- All plans use TDD approach (wriirst)
- Each task commits independently for easy rollback
- Plans are designed to be executed by AI agents using superpowers:executing-plans
- Priority can be adjusted based on user needs

---

## 🔄 Plan Updates

This roadmap will be updated as plans are completed and new priorities emerge.

**Last Updated:** 2026-02-09 23:20 UTC
