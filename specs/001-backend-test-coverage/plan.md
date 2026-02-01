# Implementation Plan: Backend Test Coverage & MVP Readiness Assessment

**Branch**: `001-backend-test-coverage` | **Date**: 2025-01-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-backend-test-coverage/spec.md`

## Summary

This plan addresses comprehensive backend testing and MVP readiness assessment for the Enerflow chemical process simulation platform. The backend currently has 0% test coverage on API, Worker, Service, and Infrastructure layers, with a critical blocker preventing functional tests from running. The plan establishes a 3-phase approach over 2-3 weeks to achieve 80% coverage on critical layers (API, Worker), 70% on supporting layers (Service, Infrastructure), complete 4 incomplete features, and deliver a production-ready backend MVP.

**Critical Path**: Fix Testcontainer blocker (1-2 days) → API tests (3-5 days) → Worker tests (3-5 days) → Complete features (5 days) → Cross-cutting validation (3-5 days)

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: 
- ASP.NET Core (Web API)
- Entity Framework Core 10.0.2 (ORM)
- MassTransit 9.0.0 (Message Queue)
- PostgreSQL (Database via EF Core)
- RabbitMQ (Message Broker via MassTransit)
- DWSIM 9.0.5 (Chemical Process Simulation Engine)
- xUnit (Test Framework - already in use)
- Testcontainers (Functional Testing Infrastructure)
- Docker (Container Runtime)

**Storage**: PostgreSQL with JSONB support for flexible data storage  
**Testing**: 
- xUnit for unit/integration/functional tests
- Testcontainers for isolated database/message queue testing
- Coverlet for code coverage analysis
- ReportGenerator for coverage reporting
- NBomber or k6 for performance/load testing

**Target Platform**: Linux server (Docker containers), cross-platform .NET  
**Project Type**: Web API + Background Worker (distributed system)  
**Performance Goals**: 
- API response time: <500ms p95 for simulation submission
- Worker throughput: Process simulations within acceptable time based on complexity
- Concurrent users: Support expected load (to be defined)
- Test suite execution: <10 minutes for rapid feedback

**Constraints**: 
- Must resolve Testcontainer/MassTransit/Postgres connection blocker before proceeding
- Test coverage targets: API 80%, Worker 80%, Service 70%, Infrastructure 70%
- Flaky test rate: <5%
- No breaking changes to existing DWSIM integration (10 scenario tests must continue passing)
- Timeline: 2-3 weeks to MVP readiness

**Scale/Scope**: 
- 119 C# files total
- 31 existing test files (26% of codebase)
- 817+ lines of untested API controller code
- 536+ lines of untested Worker/Consumer code
- 165+ lines of untested Service code
- 4 incomplete features requiring completion
- 5 TODO/FIXME comments identified

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Note**: The constitution template is not populated for this project. Assuming standard .NET best practices apply:

### Assumed Principles

✅ **Test-First Approach**: This entire feature is about establishing comprehensive testing, aligning with TDD principles

✅ **Separation of Concerns**: Existing architecture follows clean separatioI, Domain, Infrastructure, Worker, Simulation layers)

✅ **Integration Testing**: Plan includes functional tests with Testcontainers for end-to-end validation

✅ **Code Coverage Standards**: Explicit coverage targets defined (80% critical, 70% supporting)

✅ **CI/CD Integration**: Plan includes automated test execution in pipeline

### Potential Violations

⚠️ **Existing Code Without Tests**: Current 0% coverage on multiple layers violates test-first principle
- **Justification**: This plan specifically addresses this violation by implementing comprehensive test coverage
- **Remediation**: Retroactive test implementation with clear coverageargets

⚠️ **Incomplete Features in Production Code**: 4 TODO comments indicate incomplete implementations
- **Justification**: Features are partially implemented but functional for current use cases
- **Remediation**: Phase 2 completes all incomplete features with corresponding tests

**Gate Status**: ✅ **PASS** - Violations are being actively remediated by this plan

## Project Structure

### Documentation (this feature)

```text
specs/001-backend-test-coverage/
├── spec.md              # Feature specification (completed)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (to be generated)
├── data-model.md        # Phase 1 output (to be generated)
├── quickstart.md        # Phase 1 output (to be generated)
├── contracts/           # Phase 1 output (to be generated)
│   ├── test-patterns.md # Testing patterns and conventions
│   └── coverage-targets.md # Coverage requirements by layer
├── checklists/
│   └── requirements.md  # Quality validation checklist (completed)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
# Existing Enerflow Backend Structure (Web API + Worker)

# API Layer
Enerflow.API/
├── Controllers/
│   ├── SimulationsController.cs      # 629 lines - 0% coverage
│   ├── SimulationJobsController.cs   # 188 lines - 0% coverage
│   └── CatalogsController.cs         # 0% coverage
├── Program.cs
└── Enerflow.API.csproj

# Domain Layer
Enerflow.Domain/
├── Entities/                          # Minimal coverage
│   ├── Simulation.cs
│   ├── Compound.cs
│   ├── Stream.cs (Material, Energy)
│   └── UnitOperation.cs (+ subtypes)
├── DTOs/
│   ├── SimulationJob.cs
│   ├── ApiRequests.cs
│   └── Results.cs
├── Enums/                             # 11 types
└── Extensions/
    └── SimulationMappingExtensions.cs

# Infrastructure Layer
Enerflow.Infrastructure/
├── Persistence/
│   └── EnerflowDbContext.cs          # 0% coverage
└── Enerflow.Infrastructure.csproj

# Simulation Layer (DWSIM Wrapper)
Enerflow.Simulation/
├── Services/
│   └── SimulationService.cs          # Minimal coverage, has TODO
├── Factories/
│   ├── MaterialStreamFactory.cs
│   ├── EnergyStreamFactory.cs
│   └── UnitOperationFactory.cs
├── Managers/
│   ├── CompoundManager.cs
│   ├── PropertyPackageManager.cs
│   └── FlashAlgorithmManager.cs
└── Enerflow.Simulation.csproj

# Worker Layer (Background Job Processing)
Enerflow.Worker/
├── Consumers/
│   └── SimulationJobConsumer.cs      # 266 lines - 0% coverage
├── Solvers/
│   ├── DWSIMSolver.cs                # 181 lines - 0% coverage, has TODO
│   ├── ResultCollector.cs            # 89 lines - 0% coverage, has TODO
│   ├── WegsteinAccelerator.cs
│   └── ErrorCalculator.cs
├── Builders/
│   └── DWSIMFlowsheetBuilder.cs      # Has TODO
├── Mappers/
│   ├── StreamMapper.cs               # 0% coverage
│   ├── UnitOperationMapper.cs        # 0% coverage
│   ├── ConnectionMapper.cs           # 0% coverage
│   └── PostConnectionConfigurator.cs
└── Enerflow.Worker.csproj

# Test Projects
Enerflow.Tests.Unit/
├── IdGenerationTests.cs              # Only existing unit test
└── Enerflow.Tests.Unit.csproj

Enerflow.Tests.Integration/
├── FlowsheetBuilderTests.cs          # Basic integration tests
└── Enerflow.Tests.Integration.csproj

Enerflow.Tests.Functional/
├── Scenarios/
│   └── SimulationFlowTests.cs        # BLOCKED - Testcontainer issue
├── IntegrationTestWebAppFactory.cs
└── Enerflow.Tests.Functional.csproj

Enerflow.Tests.DWSIM/
├── Scenarios/                       # 10 comprehensive tests - ALL PASSING
│   ├── SimpleHeatingTests.cs
│   ├── CompressionTests.cs
│   ├── VLETests.cs
│   ├── FlashTests.cs
│   ├── HeatExchangerTests.cs
│   ├── DistillationTests.cs
│   ├── ReactorTests.cs
│   └── RecycleTests.cs
└── Enerflow.Tests.DWSIM.csproj

# External Dependencies
libs/dwsim_9.0.5/dwsim/               # DWSIM binaries (referenced, not modified)
```

**Structure Decision**: Existing multi-project solution follows clean architecture with clear separation of concerns. Test projects are organized by test type (Unit, Integration, Functional, DWSIM). This plan adds tests to existing test projects rather than creating new structure.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Retroactive Testing | Existing codebase has 0% coverage on critical layers | Writing tests before code (TDD) not possible for existing code; must test retroactively to achieve MVP readiness |
| 4 Test Projects | Different test types require diinfrastructure | Single test project ient: Unit tests need no infrastructure, Integration tests need partial infrastructure, Functional tests need full Testcontainers, DWSIM tests need DWSIM binaries |

## Phase 0: Research & Technical Decisions

### Research Tasks

The following unknowns from Technical Context require research:

1. **Testcontainer/MassTransit/Postgres Connection Issue** (CRITICAL)
   - **Unknown**: Root cause of "Connection refused" error when Worker connects to Testcontainer Postgres
   - **Research**: Debug connection string propagation, MassTransit configuration overrides, Docker networking
   - **Decision Needed**: Fix approach (connection string fix, configuration override, networking adjustment, or alternative testing strategy)

2. **Performance Benchmarks** (HIGH PRIORITY)
   - **Unknown**: Specific performance targets for MVP readiness (response times, throughput, concurrent users)
   - **Research**: Industry standards for chemical simulation APIs, expected user load, acceptable latency
   - **Decision Needed**: Define concrete performance targets for load testing

3. **Authentication/Authorization Requirements** (HIGH PRIORITY)
   - **Unknown**: Whether auth/authz is required for backend MVP
   - **Research**: Security requirements, user access patterns, deployment environment
   - **Decision Needed**: Implement now (adds 1-2 weeks) or defer to post-MVP

4. **Test Coverage Tools** (MEDIUM PRIORITY)
   - **Unknown**: Best tools for .NET 10 code coverage analysis and reporting
   - **Research**: Coverlet vs alternatives, ReportGenerator configuration, CI integration
   - **Decision Needed**: Tool selection and configuration approach

5. **Performance Testing Tools** (MEDIUM PRIORITY)
   - **Unknown**: Best tool for load testing .NET APIs (NBomber vs k6 vs JMeter)
   - **Research**: .NET-native options, ease of CI integration, reporting capabilities
   - **Decision Needed**: Tool selection for Phase 3 load testing

6. **Incomplete Feature Implementation Approach** (MEDIUM PRIORITY)
   - **Unknown**: Best approach for completing 4 incomplete features (mass balance, unit op config, result extraction, Wegstein acceleration)
   - **Research**: DWSIM API documentation, chemical engineering domain knowledge, existing implementation patterns
   - **Decision Needed**: Implementation strategy for each incomplete feature

### Research Agent Dispatch

**Agent 1: Critical Blocker Resolution**
- Task: "Research Testcontainer/MassTransit/Postgres connection issue in .NET functional tests"
- Focus: Connection string propagation, MassTransit configuration in test environments, Docker networking for Testcontainers
- Deliverable: Root cause analysis and fix recommendations

**Agent 2: Performance Standards**
- Task: "Research performance benchmarks for chemical simulation REST APIs"
- Focus: Industry standards, acceptable latency for simulation submission/retrieval, concurrent user expectations
- Deliverable: Recommended performance targets for MVP

**Agent 3: Security Requirements**
- Task: "Research authentication/authorization requirements for backend MVP APIs"
- Focus: Security best practices, minimal viable security, deferral strategies
- Deliverable: Auth/authz recommendation (implement now vs defer)

**Agent 4: Testing Tools**
- Task: "Research .NET 10 code coverage and performance testing tools"
- Focus: Coverlet configuration, ReportGenerator setup, NBomber vs k6 for load testing
- Deliverable: Tool recommendations with configuration examples

**Agent 5: Domain-Specific Implementation**
- Task: "Research DWSIM API patterns for incomplete features (mass balance, unit op config, result extraction, Wegstein acceleration)"
- Focus: DWSIM documentation, chemical engineering domain knowledge, existing codebase patterns
- Deliverable: Implementation guidance for 4 incomplete features

**Output**: `research.md` consolidating all findings with decisions, rationale, and alternatives considered

## Phase 1: Design & Contracts

**Prerequisites**: `research.md` complete, critical blocker resolved

### Data Model

**Note**: This feature focuses on testing existing entities rather than creating new ones. The data model documentation will capture test-specific entities:

**Test-Specific Entities** (to be documented in `data-model.md`):

1. **Test Suite**
   - Purpose: Organize tests by type and layer
   - Attributes: Name, Type (Unit/Integration/Functional), Layer (API/Worker/Service/Infrastructure), Test Count, Pass Rate
   - Relationships: Contains multiple Test Cases

2. **Test Coverage Report**
   - Purpose: Track coverage metrics by layer
   - Attributes: Layer, Line Coverage %, Branch Coverage %, Target Coverage %, Gap
   - Relationships: Aggregates coverage from multiple Test Suites

3. **Test Fixture**
   - Purpose: Reusable test data and setup
   - Attributes: Name, Setup Logic, Teardown Logic, Shared State
   - Relationships: Used by multiple Test Cases

4. **Test Container Configuration**
   - Purpose: Define containerized dependencies for functional tests
   - Attributes: Container Type (Postgres/RabbitMQ), Image, Port Mappings, Environment Variables
   - Relationships: Used by Functional Test Suites

5. **Performance Test Scenario**
   - Purpose: Define load testing scenarios
   - Attributes: Name, Concurrent Users, Duration, Target Endpoint, Expected Throughput
   - Relationships: Produces Performance Test Results

6. **MVP Readiness Checklist**
   - Purpose: Track MVP readiness criteria
   - Attributes: Criterion, Status (Pass/Fail), Evidence, Blocker (if failed)
   - Relationships: References Test Coverage Reports, Feature Completion StatusPI Contracts

**Note**: This feature tests existing APIs rather than creating new ones. The contracts documentation will define testing patterns:

**Testing Contracts** (to be documented in `/contracts/`):

1. **`test-patterns.md`**: Testing conventions and patterns
   - Unit test structure (Arrange-Act-Assert)
   - Integration test patterns (test fixtures, shared context)
   - Functional test patterns (Testcontainers setup, end-to-end flows)
   - Mocking strategies (when to mock vs use real dependencies)
   - Test naming conventions
   - Test organization (one test class per production class)

2. **`coverage-targets.md`**: Coverage requirements by layer
   - API Layer: 80% line coverage
     - All controller actions tested
     - Valid and invalid request scenarios
     - Error handling paths
   - Worker Layer: 80% line coverage
     - Job consumption and processing
     - Solver execution paths
     - Result collection logic
   - Service Layer: 70% line coverage
     - Business logic methods
     - Integration with domain entities
   - Infrastructure Layer: 70% line coverage
     - CRUD operations
     - Transaction handling
     - Concurrent access scenarios
   - Domain Layer: 70% line co   - Entity validation
     - Domain logic
     - Mapping extensions

3. **`incomplete-features.md`**: Implementation contracts for 4 incomplete features
   - Mass Balance Validation: Input/output requirements, validation logic, error reporting
   - Unit Operation Configuration: Parameter mapping, validation, DWSIM API integration
   - Result Extraction: Unit-specific property extraction, type-based logic
   - Wegstein Acceleration: Tear stream identification, convergence acceleration logic

### Quickstart Guide

**Output**: `quickstart.md` with:
- How to run the full test suite locally
- How to run tests by layer (API, Worker, Service, Infrastructure)
- How to generate coverage reports
- How to run functional tests with Testcontainers
- How to run performance tests
- How to interpret coverage reports and identify gaps
- Troubleshooting common test failures
- CI/CD integration instructions

### Agent Context Update

After Phase 1 design completion:

```bash
.specify/scripts/bash/update-agent-context.sh opencode
```

This will update `.agent/` or `.opencode/` context files with:
- Testing framework: xUnit
- Coverage tools: Coverlet, ReportGenerator
- Functional testing: Testcontainers
- Performance testing: [Tool selected in research]
- Test organization patterns
- Coverage targets by layer

## Phase 2: Implementation Planning (Handled by `/speckit.tasks`)

**Note**: This phase is executed by the `/speckit.tasks` command, NOT by `/speckit.plan`. The plan stops here.

The tasks command will generate `tasks.md` with:

### Phase 0 Tasks: Infrastructure (Week 1, Days 1-2)
- **Task 0.1**: Debug and fix Testcontainer/MassTransit/Postgres connection issue
- **Task 0.2**: Validate functional test infrastructure with one passing end-to-end test
- **Task 0.3**: Document fix and update functional test setup guide

### Phase 1 Tasks: Critical Test Coverage (Weeks 1-2, Days 3-12)

**API Layer (Days 3-5)**
- **Task 1.1**: Create test fixtures for API controller testing
- **Task 1.2**: Implement SimulationsController tests (629 lines, target 80% coverage)
- **Task 1.3**: Implement SimulationJobsController tests (188 lines, target 80% coverage)
- **Task 1.4**: Implement CatalogsController tests (target 80% coverage)
- **Task 1.5**: Validate API layer achieves 80% coverage target

**Worker Layer (Days 6-8)**
- **Task 1.6**: Create test fixtures for Worker/Consumer testing
- **Task 1.7**: Implement SimulationJobConsumer tests (266 lines, target 80% coverage)
- **Task 1.8**: Implement DWSIMSolver tests (181 lines, target 80% coverage)
- **Task 1.9**: Implement ResultCollector tests (89 lines, target 80% coverage)
- **Task 1.10**: Implement Mapper tests (StreamMapper, UnitOperationMapper, ConnectionMapper)
- **Task 1.11**: Validate Worker layer achieves 80e target

**Infrastructure Layer (Days 9-10)**
- **Task 1.12**: Create test fixtures for database testing with Testcontainers
- **Task 1.13**: Implement EnerflowDbContext CRUD operation tests
- **Task 1.14**: Implement transaction handling tests
- **Task 1.15**: Implement concurrent access tests
- **Task 1.16**: Validate Infrastructure layer achieves 70% coverage target

**Service Layer (Days 11-12)**
- **Task 1.17**: Implement CatalogService tests (165 lines, target 70% coverage)
- **Task 1.18**: Implement JobProducer tests
- **Task 1.19**: Implement SimulationService business logic tests
- **Task 1.20**: Validate Service layer achieves 70% coverage target

### Phase 2 Tasks: Feature Completion (Week 2-3, Days 13-17)
- **Task 2.1**: Implement mass balance validation (SimulationService.cs:497)
- **Task 2.2**: Write tests for mass balance validation
- **Task 2.3**: Implement unit operation parameter configuration (DWSIMFlowsheetBuilder.cs:146)
- **Task 2.4**: Write tests for unit operation configuration
- **Task 2.5**: Implement result extraction enhancement (ResultCollector.cs:78)
- **Task 2.6**: Write tests for result extraction
- **Task 2.7**: Complete Wegstein acceleration (DWSIMSolver.cs:138)
- **Task 2.8**: Write testsWegstein acceleration
- **Task 2.9**: Validate all 4 features complete with 100% test pass rate

### Phase 3 Tasks: Cross-Cutting Validation (Week 3, Days 18-21)
- **Task 3.1**: Set up code coverage reporting (Coverlet + ReportGenerator)
- **Task 3.2**: Configure CI/CD pipeline for automated test execution
- **Task 3.3**: Implement performance/load tests for API endpoints
- **Task 3.4**: Run performance tests and validate against targets
- **Task 3.5**: Identify and remediate flaky tests (target <5% flaky rate)
- **Task 3.6**: Optimize test suite execution time (target <10 minutes)
- **Task 3.7**: Generate MVP readiness assessment report
- **Task 3.8**: Validate all success criteria met (SC-000 through SC-015)

## Timeline & Milestones

**Total Duration**: 2-3 weeks (15-21 working days)

### Week 1: Infrastructure + API/Worker Tests
- **Days 1-2**: Fix critical blocker, validate functional tests work
- **Days 3-5**: API layer test coverage (80% target)
- **Days 6-8**: Worker layer test coverage (80% target)
- **Milestone**: Functional tests unblocked, API and Worker layers at 80% coverage

### Week 2: Infrastructure/Service Tests + Feature Completion
- **Days 9-10**: Infrastructure layer test coverage (70% target)
- **Days 11-12**: Service layer test coverage (70% target)
- **Days 13-17**: Complete 4 incomplete features with tests
- **Milestone**: All layers meet coverage targets, all features complete

### Week 3: Cross-Cutting Validation + MVP Assessment
- **Days 18-21**: Performance testing, CI/CD integration, flaky test remediation, MVP assessment
- **Milestone**: MVP READY status achieved, backend ready for frontend development

## Success Criteria Mapping

| Success Criterion | Phase | Validation Method |
|-------------------|-------|-------------------|
| SC-000: Functional tests unblocked | Phase 0 | Atst one end-to-end test passes |
| SC-001: API 80% coverage | Phase 1 | Coverlet report shows ≥80% line coverage for API layer |
| SC-002: Worker 80% coverage | Phase 1 | Coverlet report shows ≥80% line coverage for Worker layer |
| SC-003: Infrastructure 70% coverage | Phase 1 | Coverlet report shows ≥70% line coverage for Infrastructure layer |
| SC-004: Service 70% coverage | Phase 1 | Coverlet report shows ≥70% line coverage for Service layer |
| SC-005: All functional tests pass | Phase 1 | xUnit reports 100% pass rate for functional tests |
| SC-006: Zero critical bugs | Phase 1 | Bug tracker shows 0 critical/high-severity bugs |
| SC-007: 4 features complete | Phase 2 | All TODO comments resolved, features tested |
| SC-008: Feature tests pass | Phase 2 | xUnit reports 100% pass rate for new feature tests |
| SC-009: Test suite <10min | Phase 3 | CI/CD pipeline execution time measurement |
| SC-010: Flaky tests <5% | Phase 3 | Flaky test tracking over 10 runs |
| SC-011: Performance targets met | Phase 3 | Load test results meet defined benchmarks |
| SC-012: CI/CD automated | Phase 3 | Pipeline runs all tests on every commit |
| SC-013: Local test execution | Phase 3 | All team men run tests locally |
| SC-014: MVP assessment complete | Phase 3 | Assessment document generated |
| SC-015: MVP READY status | Phase 3 | All SC-000 through SC-014 achieved |

## Risk Mitigation Strategies

| Risk | Mitigation Strategy | Contingency Plan |
|------|---------------------|------------------|
| Testcontainer blocker takes >2 days | Allocate senior developer immediately | Switch to in-memory database for unit tests, separate test database for integration tests |
| Coverage goals unmet due to untestable code | Focus on critical paths (80/20 rule) | Accept lower coverage for legacy code, document untestable areas |
| Incomplete features reveal architectural issues | Early architectural review | Defer non-critical features to post-MVP |
| Performance bottlenecks discovered | Allocate time for optimization | Phased performance improvements post-MVP |
| Flaky tests undermine confidence | Strict identification and remediation process | Isolate test data, use proper async/await patterns |
| CI/CD integration issues | Validate CI configuration early | Use Docker for consistent environments |
| Auth/authz requirement emerges | Clarify MVP security requirements immediately | Defer to post-MVP if not critical |
| Team capacity constraints | Prioritiuthlessly (P0 first) | Bring in additional resources, extend timeline |

## Open Questions for Stakeholders

**Critical (Need Immediate Answers)**:
1. Is authentication/authorization required for backend MVP? (Not currently implemented, would add 1-2 weeks)
2. What are the specific performance benchmarks that must be met for MVP readiness? (Response times, throughput, concurrent users)
3. Is the 2-3 week timeline acceptable for MVP readiness?

**Important (Need Answers Before Phase 2)**:
4. What is the minimum acceptable test coverage percentage for non-critical code paths? (Proposed: 50-60%)
5. Should load testing include stress testing beyond expected capacity? (Recommended: Yes, at 150% expected load)
6. What is the process for prioritizing and addressing bugs discovered during testing?

**Nice to Have (Can Be Decided During Implementation)**:
7. Should we implement mutation testing to validate test quality?
8. What level of integration testing is needed for DWSIM beyond existing 10 scenario tests?
9. Should we set up automated test reporting dashboards?

## Next Steps

1. **Immediate**: Run `/speckit.plan` research phase to generate `research.md` (resolves all NEEDS CLARIFICATION. **Phase 0**: Execute research tasks, make technical decisions, document in `research.md`
3. **Phase 1**: Generate `data-model.md`, `contracts/`, and `quickstart.md` based on research findings
4. **Phase 1**: Run agent context update script to add testing tools and patterns
5. **Phase 2**: Run `/speckit.tasks` to generate detailed task breakdown in `tasks.md`
6. **Implementation**: Execute tasks in priority order (P0 blocker → P1 critical coverage → P2 features → P3 validation)
7. **Validation**: Verify all success criteria met, generate MVP readiness assessment
8. **Handoff**: Deliver MVP-ready backend to frontend development team

---

**Plan Status**: ✅ Complete - Ready for Phase 0 Research

**Command Completion**: This plan was generated by `/speckit.plan`. Next command: `/speckit.tasks` (after research and design phases complete)
