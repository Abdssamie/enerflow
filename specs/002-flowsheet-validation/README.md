# Flowsheet Validation Feature

**Status**: Ready for Parallel Implementation  
**Priority**: P0 (Critical Bug Fix)  
**Execution Strategy**: Option C - Parallel Development

---

## Quick Links

- [Feature Specification](./spec.md) - Complete feature requirements and design
- [Track 1: Bug Fix](./track1-bug-fix.md) - P0 topology validation (Agent A, 1 day)
- [Track 2: Comprehensive Validation](./track2-comprehensive-validation.md) - P1-P2 full validation (Agent B, 2 days)

---

## Problem

**Critical P1 Bug**: Simulations incorrectly report "Converged" for invalid flowsheets with disconnected streams.

**Test Failing**: `Should_Fail_On_Disconnected_Stream`

---

## Solution

Implement `FlowsheetValidator` component with:
1. **Topology validation** (Track 1, P0) - Fixes the bug
2. **Physical property validation** (Track 2, P1) - Production readiness
3. **Compound validation** (Track 2, P1) - Production readiness
4. **Unit operation validation** (Track 2, P2) - Enhanced validation

---

## Parallel Development Plan

### Track 1: Bug Fix (Agent A) - 1 day
**Goal**: Fix P1 bug immediately

**Tasks**: 10 tasks (T001-T010)
- Create validation infrastructure
- Implement topology validation
- Integrate into DWSIMFlowsheetBuilder
- Fix failing functional test

**Deliverable**: `Should_Fail_On_Disconnected_Stream` test PASSES

### Track 2: Comprehensive Validation (Agent B) - 2 days
**Goal**: Production-ready validation layer

**Tasks**: 14 tasks (T011-T024)
- Physical property validation
- Compound validation
- Unit operation validation
- Comprehensive unit tests
- Integration tests
- Documentation

**Deliverable**: 100% test coverage, production-ready

### Merge & Integration - 0.5 days
- Merge both tracks
- Run full test suite
- Verify no conflicts
- Final documentation

**Total Time**: 2.5 days (29% faster than sequential)

---

## Architecture

```
DWSIMFlowsheetBuilder
    ↓ (injects)
IFlowsheetValidator
    ↓ (implements)
FlowsheetValidator
    ├── ValidateTopology() ← Track 1
    ├── ValidatePhysicalProperties() ← Track 2
    ├── ValidateCompounds() ← Track 2
    └── ValidateUnitOperations() ← Track 2
```

**Key Design Decisions**:
- Separate validator component (not inline in builder)
- Post-build, pre-solve validation timing
- Exception-based error handling
- Dependency injection for testability

---

## Success Criteria

### Track 1 (P0)
- ✅ Functional test passes
- ✅ Disconnected units detected
- ✅ Orphaned streams detected
- ✅ Error messages include entity names

### Track 2 (P1-P2)
- ✅ 100% test coverage
- ✅ All validation rules tested
- ✅ Integration tests pass
- ✅ Documentation complete

### Combined
- ✅ No breaking changes
- ✅ Performance < 100ms
- ✅ Production-ready

---

## Getting Started

### For Agent A (Track 1)
1. Read [track1-bug-fix.md](./track1-bug-fix.md)
2. Create branch: `git checkout -b feature/flowsheet-validation-track1`
3. Implement tasks T001-T010
4. Run functional test to verify fix
5. Create PR when complete

### For Agent B (Track 2)
1. Read [track2-comprehensive-validation.md](./track2-comprehensive-validation.md)
2. Wait for Track 1 interface contract (or work in parallel with agreed interface)
3. Create branch: `git checkout -b feature/flowsheet-validation-track2`
4. Implement t1-T024
5. Create PR when complete

### Merge Coordinator
1. Review both PRs
2. Merge Track 1 first
3. Rebase Track 2 on Track 1
4. Resolve any conflicts (expected in FlowsheetValidator.Validate())
5. Run full test suite
6. Merge Track 2

---

## Files Created

### Track 1 (Agent A)
- `Enerflow.Worker/Validation/IFlowsheetValidator.cs`
- `Enerflow.Worker/Validation/ValidationResult.cs`
- `Enerflow.Worker/Validation/ValidationError.cs`
- `Enerflow.Worker/Validation/FlowsheetValidationException.cs`
- `Enerflow.Worker/Validation/FlowsheetValidator.cs` (topology only)
- `Enerflow.Tests.Unit/Worker/Validation/FlowsheetValidatorTe.cs`
- Modified: `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs`
- Modified: `Enerflow.Worker/Consumers/SimulationJobConsumer.cs`
- Modified: `Enerflow.Worker/Program.cs` (DI registration)

### Track 2 (Agent B)
- Extended: `Enerflow.Worker/Validation/FlowsheetValidator.cs` (add methods)
- `Enerflow.Tests.Unit/Worker/Validation/PhysicalPropertyValidationTests.cs`
- `Enerflow.Tests.Unit/Worker/Validation/CompoundValidationTests.cs`
- `Enerflow.Tests.Unit/Worker/Validation/UnitOperationValidationTests.cs`
- `Enerflow.Tests.Integration/Worker/Validation/FlowsheetValidationIntegrationTests.cs`
- Modified: `ENERFLOW_SIMULATION_GUIDE.md`
- `specs/002-flowsheet-validation/adr-001-validation-architecture.md`

---

## Testing

### Unit Tests
```bash
dotnet test Enerflow.Tests.Unit --filter "FullyQualifiedName~Validation"
```

### Integration Tests
```bash
dotnet test Enerflow.Tests.Integration --filter "FullyQualifiedName~Validation"
```

### Functional Test (Bug Verification)
```bash
dotnet test Enerflow.Tests.Functional --filter "Should_Fail_On_Disconnected_Stream"
```

### Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Include="[Enerflow.Worker]*Validation*"
```

---

## References

- [ENERFLOW_SIMULATION_GUIDE.md](../../ENERFLOW_SIMULATION_GUIDE.md) - Priority 1: Error Handling & Validation
- [incomplete-features.md](../001-backend-test-coverage/contracts/incomplete-features.md) - Feature requirements
- [SimulationFlowTests.cs](../../Enerflow.Tests.Functional/Scenarios/SimulationFlowTests.cs) - Failing test
- [DWSIMFlowsheetBuilder.crflow.Worker/Builders/DWSIMFlowsheetBuilder.cs) - Integration point

---

**Created**: 2025-01-31  
**Last Updated**: 2025-01-31  
**Status**: ✅ Ready for Implementation
