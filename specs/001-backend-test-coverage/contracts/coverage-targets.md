# Coverage Targets by Layer

**Feature**: Backend Test Coverage & MVP Readiness Assessment  
**Date**: 2025-01-30  
**Phase**: 1 - Design

## Overview

This document defines specific code coverage targets for each layer of the Enerflow backend. These targets are based on industry best practices, layer criticality, and MVP readiness requirements.

## Coverage Metrics

### Metric Definitions

- **Line Coverage**: Percentage of executable code lines covered by tests
- **Branch Coverage**: Percentage of decision branches (if/else, switch) covered by tests
- **Method Coverage**: Percentage of methods with at least one test
- **Primary Metric**: Line Coverage (used for target validation)

## Layer-Specific Targets

### API Layer (80% Line Coverage)

**Rationale**: API layer is the primary interface for frontend applications. High coverage ensures reliable request/response handling.

**Scope**:
- `Enerflow.API/Controllers/`
- All controller actions
- Request validation
- Response formatting
- Error handling

**Target Breakdown**:

| Component | Target | Priority | Lines |
|-----------|--------|----------|-------|
| SimulationsController | 80% | P1 | 629 |
| SimulationJobsController | 80% | P1 | 188 |
| CatalogsController | 80% | P1 | ~100 |
| Error Handling Middleware | 70% | P2 | ~50 |

**Critical Paths** (Must be 100% covered):
- All POST/PUT endpoints (data modification)
- Authentication/authorization checks (if implemented)
- Error response formatting
- Input validation

**Acceptable Lower Coverage**:
- Logging statements: 50%
- Diagnostic endpoints: 60%
- Swagger/OpenAPI configuration: 0% (excluded)

**Test Types**:
- Unit tests: Controller actions with mocked services
- Integration tests: Full request/response cycle with test database

**Example Coverage Report**:
```
Enerflow.API
├── Controllers
│   ├── SimulationsController.cs: 85% (536/629 lines)
│   ├── SimulationJobsController.cs: 82% (154/188 lines)
│   └── CatalogsController.cs: 81% (81/100 lines)
└── Middleware
    └── ErrorHandlingMiddleware.cs: 72% (36/50 lines)

Overall API Layer: 81% ✅ (Meets 80% target)
```

---

### Worker Layer (80% Line Coverage)

**Rationale**: Worker layer orchestrates simulation execution. High coverage ensures reliable job processing and result collection.

**Scope**:
- `Enerflow.Worker/Consumers/`
- `Enerflow.Worker/Solvers/`
- `Enerflow.Worker/Builders/`
- `Enerflow.Worker/Mappers/`

**Target Breakdown**:

| Component | Target | Priority | Lines |
|-----------|--------|----------|-------|
| SimulationJobConsumer | 80% | P1 | 266 |
| DWSIMSolver | 80% | P1 | 181 |
| ResultCollector | 80% | P1 | 89 |
| DWSIMFlowsheetBuilder | 75% | P1 | ~200 |
| StreamMapper | 75% | P2 | ~100 |
| UnitOperationMapper | 75% | P2 | ~150 |
| ConnectionMapper | 75% | P2 | ~80 |
| WegsteinAccelerator | 70% | P2 | ~100 |
| ErrorCalculator | 70% | P2 | ~50 |

**Critical Paths** (Must be 100% covered):
- Job consumption and acknowledgment
- Error handling and retry logic
- Result persistence
- DWSIM solver invocation

**Acceptable Lower Coverage**:
- Logging statements: 50%
- Diagnostic/debugging code: 60%
- Complex DWSIM integration edge cases: 70%

**Test Types**:
- Unit tests: Individual components with mocked dependencies
- Integration tests: Full job processing with test DWSIM simulations
- Functional tests: End-to-end with Testcontainers

**Example Coverage Report**:
```
Enerflow.Worker
├── Consumers
│   └── SimulationJobConsumer.cs: 83% (221/266 lines)
├── Solvers
│   ├── DWSIMSolver.cs: 82% (148/181 lines)
│   ├── ResultCollector.cs: 85% (76/89 lines)
│   ├── WegsteinAccelerator.cs: 72% (72/100 lines)
│   └── ErrorCalculator.cs: 74% (37/50 lines)
├── Builders
│   └── DWSIMFlowsheetBuilder.cs: 77% (154/200 lines)
└── Mappers
    ├── StreamMapper.cs: 78% (78/100 lines)
    ├── UnitOperationMapper.cs: 76% (114/150 lines)
    └── ConnectionMapper.cs: 79% (63/80 lines)

Overall Worker Layer: 80% ✅ (Meets 80% target)
```

---

### Service Layer (70% Line Coverage)

**Rationale**: Service layer contains business logic. Good coverage ensures domain operations are validated, but some complex scenarios may be deferred.

**Scope**:
- `Enerflow.API/Services/` (if exists)
- `Enerflow.Simulation/Services/`
- Business logic methods

**Target Breakdown**:

| Component | Target | Priority | Lines |
|-----------|--------|----------|-------|
| CatalogService | 70% | P2 | 165 |
| JobProducer | 70% | P2 | ~80 |
| SimulationService (business logic) | 70% | P2 | ~300 |

**Critical Paths** (Must be 100% covered):
- Data validation logic
- Business rule enforcement
- State transitions

**Acceptable Lower Coverage**:
- Complex DWSIM wrapper methods: 60%
- Caching logic: 60%
- Logging: 50%

**Test Types**:
- Unit tests: Service methods with mocked repositories
- Integration tests: Service methods with real database

**Example Coverage Report**:
```
Enerflow.Simulation/Services
├── CatalogService.cs: 72% (119/165 lines)
├── JobProducer.cs: 75% (60/80 lines)
└── SimulationService.cs: 68% (204/300 lines)

Overall Service Layer: 71% ✅ (Meets 70% target)
```

---

### Infrastructure Layer (70% Line Coverage)

**Rationale**: Infrastructure layer handles data persistence. Good coverage ensures CRUD operations and transactions work correctly.

**Scope**:
- `Enerflow.Infrastructure/Persistence/`
- Database context
- Repository implementations (if exists)

**Target Breakdown**:

| Component | Target | Priority | Lines |
|-----------|--------|----------|-------|
| EnerflowDbContext | 70% | P1 | ~200 |
| Migrations | 0% | N/A | Excluded |
| Configuration classes | 50% | P3 | ~100 |

**Critical Paths** (Must be 100% covered):
- CRUD operations (Create, Read, Update, Delete)
- Transaction handling
- Concurrent access scenarios

**Acceptable Lower Coverage**:
- EF Core configuration: 50%
- Migration files: 0% (excluded)
- Connection string handling: 60%

**Test Types**:
- Integration tests: Database operations with Testcontainers
- Concurrent access tests: Multiple threads/tasks

**Example Coverage Report**:
```
Enerflow.Infrastructure
└── Persistence
    ├── ntext.cs: 72% (144/200 lines)
    └── Configurations/: 52% (52/100 lines)

Overall Infrastructure Layer: 71% ✅ (Meets 70% target)
```

---

### Domain Layer (70% Line Coverage)

**Rationale**: Domain layer contains entities and domain logic. Good coverage ensures business rules are validated.

**Scope**:
- `Enerflow.Domain/Entities/`
- `Enerflow.Domain/Extensions/`
- Domain validation logic

**Target Breakdown**:

| Component | Target | Priority | Lines |
|-----------|--------|----------|-------|
| Entity validation | 70% | P2 | ~200 |
| Domain extensions | 70% | P2 | ~150 |
| Enums | 0% | N/A | Excluded |
| DTOs | 50% | P3 | ~300 |

**Critical Paths** (Must be 100% covered):
- Entity validation rules
- Domain invariants
- State transitions

**Acceptable Lower Coverage**:
- Simple DTOs: 50%
- Enums: 0% (excluded)
- Auto-generated properties: 0% (excluded)

**Test Types**:
- Unit tests: Entity validation, domain logic

**Example Coverage Report**:
```
Enerflow.Domain
├── Entities/: 72% (144/200 lines)
├── Extensions/: 75% (113/150 lines)
└── DTOs/: 52% (156/300 lines)

Overall Domain Layer: 71% ✅ (Meets 70% target)
```

---

## Exclus

### Automatically Excluded from Coverage

- **Generated Code**: Auto-generated files (migrations, scaffolding)
- **Program.cs/Startup.cs**: Application entry points
- **Configuration Files**: appsettings.json, etc.
- **Test Projects**: All `*.Tests.*` projects
- **Third-Party Libraries**: DWSIM binaries, NuGet packages

### Manually Excluded (via attributes)

```csharp
[ExcludeFromCodeCoverage]
public class DiagnosticController : ControllerBase
{
    // Diagnostic endpoints excluded from coverage
}

[ExcludeFromCodeCoverage]
public void LogDiagnostics()
{
    // Logging-only method excluded
}
```

## Coverage Validation

### CI/CD Pipeline Checks

```yaml
# GitHub Actions example
- name: Check coverage thresholds
  run: |
    # Parse coverage report
    API_COVERAGE=$(grep "Enerflow.API" coverage.xml | extract_percentage)
    WORKER_COVERAGE=$(grep "Enerflow.Worker" coverage.xml | extract_percentage)
    SERVICE_COVERAGE=$(grep "Enerflow.Simulation" coverage.xml | extract_percentage)
    INFRA_COVERAGE=$(grep "Enerflow.Infrastructure" coverage.xml | extract_percentage)
    DOMAIN_COVERAGE=$(grep "Enerflow.Domain" coverage.xml | extract_percentage)
    
    # Validate targets
    if [ "$API_COVERAGE" -lt 80 ]; then
      echo "❌ API coverage $API_COVERAGE% below 80% target"
      exit 1
    fi
    
    if [ "$WORKER_COVERAGE" -lt 80 ]; then
      echo "❌ Worker coverage $WORKER_COVERAGE% below 80% target"
      exit 1
    fi
    
    if [ "$SERVICE_COVERAGE" -lt 70 ]; then
      echo "❌ Service coverage $SERVICE_COVERAGE% below 70% target"
      exit 1
    fi
    
    if [ "$INFRA_COVERAGE" -lt 70 ]; then
      echo "❌ Infrastructure coverage $INFRA_COVERAGE% below 70% target"
      exit 1
    fi
    
    if [ "$DOMAIN_COVERAGE" -lt 70 ]; then
      echo "❌ Domain covera_COVERAGE% below 70% target"
      exit 1
    fi
    
    echo "✅ All coverage targets met"
```

### Local Coverage Validation

```bash
# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total

# Generate detailed report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"Html;Badges"

# Open report
open CoverageReport/index.html
```

## Coverage Improvement Strategy

### Phase 1: Critical Paths (Week 1)
1. Implement tests for API controllers (80% target)
2. Implement tests for Worker consumers/solvers (80% target)
3. Focus on happy paths and critical error scenarios

### Phase 2: Supporting Layers (Week 2)
4. Implement tests for Service layer (70% target)
5. Implement tests for Infrastructure layer (70% target)
6. Implement tests for Domain layer (70% target)

### Phase 3: Edge Cases & Optimization (Week 3)
7. Add tests for edge cases to reach targets
8. Optimize test execution time
9. Remediate flaky tests

## Coverage Reporting

### Report Formats

1. **HTML Report**: Detailed drill-down by file/method
2. **Cobertura XML**: Machine-readable for CI/CD
3. **Badges**: Visual indicators for README

### Report Locations

- **Local**: `TestResults/CoverageReport/index.html`
- **CI/CD**: Uploaded as build artifacts
- **Dashboard** (optional): Coveralls, Codecov, or SonarQube

## Success Criteria

**MVP Readiness Coverage Targets**:

| Layer | Target | Status |
|-------|--------|--------|
| API | ≥ 80% | ⬜ Not Met |
| Worker | ≥ 80% | ⬜ Not Met |
| Service | ≥ 70% | ⬜ Not Met |
| Infrastructure | ≥ 70% | ⬜ Not Met |
| Domain | ≥ 70% | ⬜ Not Met |

**Overall Target**: All layers meet or exceed their targets

**Validation**: Run `dotnet test` with coverage, verify all targets met, generate report

---

**Status**: ✅ Complete  
**Next**: Create incomplete-features.md for feature implementation contracts
