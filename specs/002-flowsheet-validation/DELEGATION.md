# Delegation Instructions for Parallel Development

**Feature**: Flowsheet Validation  
**Strategy**: Option C - Parallel Development (2 tracks)  
**Total Time**: 2.5 days

---

## 🎯 Quick Start

### Agent A - Track 1 (Bug Fix)
```bash
# Read your assignment
cat specs/002-flowsheet-validation/track1-bug-fix.md

# Create your branch
git checkout -b feature/flowsheet-validation-track1

# Start with task T001
# Goal: Fix P1 bug in 1 day
```

### Agent B - Track 2 (Comprehensive Validation)
```bash
# Read your assignment
cat specs/002-flowsheet-validation/track2-comprehensive-validation.md

# Create your branch
git checkout -b feature/flowsheet-validation-track2

# Start with task T011 (can work in parallel with Track 1)
# Goal: Production-ready validation in 2 days
```

---

## 📋 Task Distribution

### Track 1: Bug Fix (Agent A) - 10 Tasks
**Priority**: P0 (Critical)  
**Time**: 1 day  
**Tasks**: T001-T010

**Deliverables**:
- ✅ `IFlowsheetValidator` interface
- ✅ `ValidationResult`, `ValidationError` value objects
- ✅ `FlowsheetValidationException`
- ✅ `FlowsheetValidator` with topology validation
- ✅ Integration with `DWSIMFlowsheetBuilder`
- ✅ Error handling in `SimulationJobConsumer`
- ✅ Unit tests for topology validation
- ✅ **Functional test `Should_Fail_On_Disconnected_Stream` PASSES**

### Track 2: Comprehensive Validation (Agent B) - 14 Tasks
**Priority**: P1-P2  
**Time**: 2 days  
**Tasks**: T011-T024

**Deliverables**:
- ✅ Physical property validation (temperature, pressure, mass flow, composition)
- ✅ Compound validation
- ✅ Unit operation validation (heater, splitter)
- ✅ Comprehensive unit tests (100% coverage)
- ✅ Integration tests
- ✅ Documentation updates
- ✅ Architecture Decision Record (ADR)

---

## 🔄 Coordination Points

### Interface Contract (Agreed Between Both Tracks)

**Track 1 creates these interfaces** (Track 2 depends on them):
```csharp
// IFlowsheetValidator.cs
public interface IFlowsheetValidator
{
    ValidationResult Validate(Simulation simulation, IFlowsheet flowsheet);
}

// ValidationResult.cs
public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<ValidationError> Errors { get; }
    public List<ValidationWarning> Warnings { get; }
}

// ValidationError.cs
public class ValidationError
{
    public string Code { get; init; }
    public string Message { get; init; }
    public string EntityType { get; init; }
    public string EntityName { get; init; }
    pubrSeverity Severity { get; init; }
}
```

**Track 2 extends FlowsheetValidator** (adds methods to Track 1's class):
```csharp
// FlowsheetValidator.cs - Track 1 creates, Track 2 extends
public class FlowsheetValidator : IFlowsheetValidator
{
    // Track 1 implements:
    public ValidationResult Validate(Simulation simulation, IFlowsheet flowsheet)
    {
        var errors = new List<ValidationError>();
        errors.AddRange(ValidateTopology(simulation));
        // Track 2 adds these lines:
        errors.AddRange(ValidatePhysicalProperties(simulation));
        errors.AddRange(ValidateCompounds(simulation));
        errors.AddRange(ValidateUnitOperations(simulation));
        return new ValidationResult(errors);
    }
    
    private List<ValidationError> ValidateTopology(...) { } // Track 1
    private List<ValidationError> ValidatePhysicalProperties(...) { } // Track 2
    private List<ValidationError> ValidateCompounds(...) { } // Track 2
    private List<ValidationError> ValidateUnitOperations(...) { } // Track 2
}
```

### Parallel Work Strategy

**Option 1: True Parallel (Recommended)**
- Track 1 and Track 2 work simultaneously
- Track 2 creates stub interfaces locally if needed
- Merge Track 1 first, then rebase Track 2

**Option 2: Sequential Start**
- Track 1 completes T001-T004 (interfaces) first
- Track 2 starts after interfaces are committed
- Both tracks work in parallel after that

---

## 🔀 Merge Strategy

### Step 1: Track 1 Completes (Day 1)
```bash
# Agent A
git add .
git commit -m "feat: implement flowsheet topology validation (Track 1)

- Add IFlowsheetValidator interface and value objects
- Implement topology validation (disconnected units, orphaned streams)
- Integrate with DWSIMFlowsheetBuilder
- Add error h in SimulationJobConsumer
- Fix P1 bug: Should_Fail_On_Disconnected_Stream test now passes

Fixes: P1 bug where simulations converge with disconnected streams"

git push origin feature/flowsheet-validation-track1

# Create PR for Track 1
```

### Step 2: Track 1 Merged (Day 1 EOD)
```bash
# Merge coordinator reviews and merges Track 1 PR
# This unblocks functional testing
```

### Step 3: Track 2 Rebases (Day 2)
```bash
# Agent B
git fetch origin
git rebase origin/001-backend-test-coverage  # or main branch

# Resolve conflicts in FlowsheetValidator.Validate() method
# Keep all validation methalls from both tracks
```

### Step 4: Track 2 Completes (Day 2-3)
```bash
# Agent B
git add .
git commit -m "feat: add comprehensive flowsheet validation (Track 2)

- Add physical property validation (temp, pressure, mass flow, composition)
- Add compound validation
- Add unit operation validation (heater, splitter)
- Achieve 100% test coverage for FlowsheetValidator
- Add integration tests
- Update documentation and create ADR

Extends Track 1 topology validation with production-ready rules"

git push origin feature/flowsheet-validation-track2

# Create PR for Track 2
```

### Step 5: Final Merge (D```bash
# Merge coordinator
# 1. Review Track 2 PR
# 2. Run full test suite
# 3. Verify functional test still passes
# 4. Merge Track 2
```

---

## ✅ Success Criteria

### Track 1 (Agent A)
- [ ] All 10 tasks (T001-T010) completed
- [ ] Functional test `Should_Fail_On_Disconnected_Stream` **PASSES**
- [ ] Unit tests for topology validation pass
- [ ] Code compiles without warnings
- [ ] PR created and ready for review

### Track 2 (Agent B)
- [ ] All 14 tasks (T011-T024) completed
- [ ] FlowsheetValidator has 100% line coverage
- [ ] All unit tests pass (physical properties, compounds, unit operations)
- [ ] Integration tests pass
- [ ] Documentation updated
- [ ] ADR created
- [ ] PR created and ready for review

### Combined (After Merge)
- [ ] Functional test still passes
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No breaking changes
- [ ] Performance < 100ms for validation
- [ ] Production-ready

---

## 🚨 Risk Mitigation

### Risk: Merge Conflicts
**Probability**: Medium  
**Impact**: Low  
**Mitigation**: 
- Clear interface contract defined upfront
- Track 2 extends Track 1 (doesn't replace)
- Expected conflict in `Validate()` method is trivial to resolve

### Risk: Track 1 Delays Track 2
**Probability**: Low  
**Impact**: Medium  
**Mitigation**:
- Track 2 can work with stub interfaces
- Track 2 focuses on validation logic (doesn't need DWSIM integration)
- Parallel work is still possible

### Risk: Breaking Changes
**Probability**: Low  
**Impact**: High  
**Mitigan- Comprehensive testing at each stage
- Functional test verifies bug fix
- Integration tests verify end-to-end flow

---

## 📞 Communication

### Daily Sync (Optional but Recommended)
- **Time**: End of day
- **Duration**: 5 minutes
- **Topics**:
  - Track 1: Interface changes, blockers
  - Track 2: Dependencies on Track 1, progress
  - Both: Merge strategy adjustments

### Handoff Points
1. **Day 1 EOD**: Track 1 completes, interfaces are stable
2. **Day 2**: Track 2 rebases on Track 1
3. **Day 3**: Final merge and verification

---

## 📚 Resources

- [Feature Specification](./spec.md)
- [Tra 1 Tasks](./track1-bug-fix.md)
- [Track 2 Tasks](./track2-comprehensive-validation.md)
- [ENERFLOW_SIMULATION_GUIDE.md](../../ENERFLOW_SIMULATION_GUIDE.md)
- [Failing Test](../../Enerflow.Tests.Functional/Scenarios/SimulationFlowTests.cs)

---

## 🎯 Final Goal

**By Day 3**:
- ✅ P1 bug fixed (simulations no longer converge with disconnected streams)
- ✅ Production-ready validation layer with 100% coverage
- ✅ Comprehensive validation rules (topology, physical properties, compounds, unit operations)
- ✅ All tests passing
- ✅ Documentation complete
- ✅ 29% faster than sequential development (2.5 days vs 3.5 days)

---

**Status**: ✅ Ready for Delegation  
**Created**: 2025-01-31  
**Execution**: Start immediately
