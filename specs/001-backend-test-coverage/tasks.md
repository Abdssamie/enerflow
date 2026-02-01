# Tasks: Backend Test Coverage & MVP Readiness Assessment

**Input**: Design documents from `/specs/001-backend-test-coverage/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US0, US1, US2...)
- Include exact file paths in descriptions

## Path Conventions

Existing Enerflow structure:
- Test projects: `Enerflow.Tests.Unit/`, `Enerflow.Tests.Integration/`, `Enerflow.Tests.Functional/`
- Source projects: `Enerflow.API/`, `Enerflow.Worker/`, `Enerflow.Simulation/`, `Enerflow.Infrastructure/`

---

## Phase 1: Setup (Test Infrastructure)

**Purpose**: Configure test tooling and coverage infrastructure

- [ ] T001 [P] Install Coverlet packages in all test projects (Enerflow.Tests.Unit, Enerflow.Tests.Integration, Enerflow.Tests.Functional)
- [ ] T002 [P] Install ReportGenerator global tool for coverage report generation
- [ ] T003 [P] Install NBomber package in Enerflow.Tests.Performance project (create if needed)
- [ ] T004 [P] Create test helper utilities in Enerflow.Tests.Unit/Helpers/ for test data builders
- [ ] T005 [P] Create test fixtures base classes in Enerflow.Tests.Integration/Fixtures/

---

## Phase 2: Foundational (CRITICAL BLOCKER - US0)

**Purpose**: Unblock functional test infrastructure - MUST complete before ANY other user story

**⚠️ CRITICAL**: No other testing work can proceed until this phase is complete

### User Story 0 - Unblock Functional Test Infrastructure (Priority: P0) 🚨

**Goal**: Fix Testcontainer/MassTransit/Postgres connection issue to enable end-to-end testing

**Independent Test**: Run `dotnet test Enerflow.Tests.Functional` and verify at least one test passes

- [ ] T006 [US0] Debug and fix IntegrationTestWebAppFactory DbContext configuration in Enerflow.Tests.Functional/IntegrationTestWebAppFactory.cs
- [ ] T007 [US0] Override Worker DbContext registration to use Testcontainer connection string in Enerflow.Tests.Functional/IntegrationTestWebAppFactory.cs
- [ ] T008 [US0] Configure MassTransit to use Testcontainer RabbitMQ in Enerflow.Tests.Functional/IntegrationTestWebAppFactory.cs
- [ ] T009 [US0] Update SimulationFlowTests to validate end-to-end workflow in Enerflow.Tests.Functional/Scenarios/SimulationFlowTests.cs
- [ ] T010 [US0] Run functional tests and verify "Connection refused" error is resolved
- [ ] T011 [US0] Document fix in specs/001-backend-test-coverage/quickstart.md

**Checkpoint**: ✅ Functional test infrastructure unblocked - User story testing can now begin

---

## Phase 3: User Story 1 - API Layer Test Coverage (Priority: P1) 🎯 MVP

**Goal**: Achieve 80% test coverage on API controllers (SimulationsController, SimulationJobsController, CatalogsController)

**Independent Test**: Run `dotnet test Enerflow.Tests.Unit --filter "Layer=APverify 80% coverage with `dotnet test /p:CollectCoverage=true /p:Include="[Enerflow.API]*"`

### Implementation for User Story 1

- [ ] T012 [P] [US1] Create SimulationsControllerTests class in Enerflow.Tests.Unit/API/Controllers/SimulationsControllerTests.cs
- [ ] T013 [P] [US1] Create SimulationJobsControllerTests class in Enerflow.Tests.Unit/API/Controllers/SimulationJobsControllerTests.cs
- [ ] T014 [P] [US1] Create CatalogsControllerTests class in Enerflow.Tests.Unit/API/Controllers/CatalogsControllerTests.cs
- [ ] T015 [P] [US1] Implement tests for SimulationsController GET endpoints (GetAll, GetById) in SimulationsControllerTests.cs
- [ ] T016 [P] [US1] Implement tests for SimulationsController POST endpoint (Create) with valid/invalid data in SimulationsControllerTests.cs
- [ ] T017 [P] [US1] Implement tests for SimulationsController PUT endpoint (Update) in SimulationsControllerTests.cs
- [ ] T018 [P] [US1] Implement tests for SimulationsController DELETE endpoint in SimulationsControllerTests.cs
- [ ] T019 [P] [US1] Implement tests for SimulationJobsController endpoints (Submit, GetStatus, GetResults) in SimulationJobsControllerTests.cs
- [ ] T020 [P] [US1] Its for CatalogsController endpoints (GetCompounds, GetPropertyPackages, GetFlashAlgorithms) in CatalogsControllerTests.cs
- [ ] T021 [US1] Add error handling tests for all API controllers (400, 404, 500 scenarios)
- [ ] T022 [US1] Add concurrent request tests for API endpoints in Enerflow.Tests.Integration/API/ConcurrentRequestTests.cs
- [ ] T023 [US1] Run coverage report and verify API layer achieves 80% target

**Checkpoint**: ✅ API layer fully tested and validated - Frontend can rely on consistent API behavior

---

## Phase 4: User Story 2 - Worker & Consumer Layer Test Coverage (Priority: P1)

**Goal**: Achieve 80% test coverage on Worker layer (SimulationJobConsumer, DWSIMSolver, ResultCollector, Mappers)

**Independent Test**: Run `dotnet test Enerflow.Tests.Integration --filter "Layer=Worker"` and verify 80% coverage with `dotnet test /p:CollectCoverage=true /p:Include="[Enerflow.Worker]*"`

### Implementation for User Story 2

- [ ] T024 [P] [US2] Create SimulationJobConsumerTests class in Enerflow.Tests.Integration/Worker/Consumers/SimulationJobConsumerTests.cs
- [ ] T025 [P] [US2] Create DWSIMSolverTests class in Enerflow.Tests.Integration/Worker/Solvers/DWSIMSolverTests.cs
- [ ] T026 [P] [US2] Create ResultCollectorTests class in Enerflow.Tests.Integration/Worker/Solvers/ResultCollectorTests.cs
- [ ] T027 [P] [US2] Create StreamMapperTests class in Enerflow.Tests.Unit/Worker/Mappers/StreamMapperTests.cs
- [ ] T028 [P] [US2] Create UnitOperationMapperTests class in Enerflow.Tests.Unit/Worker/Mappers/UnitOperationMapperTests.cs
- [ ] T029 [P] [US2] Create ConnectionMapperTests class in Enerflow.Tests.Unit/Worker/Mappers/ConnectionMapperTests.cs
- [ ] T030 [P] [US2] Implement tests for SimulationJobConsumer job consumption in SimulationJobConsumerTests.cs
- [ ] T031 [P] [US2] Implement tests forulationJobConsumer error handling and retry logic in SimulationJobConsumerTests.cs
- [ ] T032 [P] [US2] Implement tests for DWSIMSolver execution (success and failure scenarios) in DWSIMSolverTests.cs
- [ ] T033 [P] [US2] Implement tests for ResultCollector property extraction in ResultCollectorTests.cs
- [ ] T034 [P] [US2] Implement tests for StreamMapper (MaterialStream, EnergyStream) in StreamMapperTests.cs
- [ ] T035 [P] [US2] Implement tests for UnitOperationMapper (all unit types) in UnitOperationMapperTests.cs
- [ ] T036 [P] [US2] Implement tests for ConnectionMapper in ConnectionMapperTests.cs
- [ ] T037 [US2] Add integration tests for full job processing workflow in Enerflow.Tests.Integration/Worker/JobProcessingIntegrationTests.cs
- [ ] T038 [US2] Run coverage report and verify Worker layer achieves 80% target

**Checkpoint**: ✅ Worker layer fully tested - Job processing is reliable and validated

---

## Phase 5: User Story 3 - Database Operations Test Coverage (Priority: P1)

**Goal**: Achieve 70% test coverage on Infrastructure layer (EnerflowDbContext, CRUD operations)

**Independent Test**: Run `dotnet test Enerflow.Tests.Integration --filter "Layer=Infrastructure"` and verify age with `dotnet test /p:CollectCoverage=true /p:Include="[Enerflow.Infrastructure]*"`

### Implementation for User Story 3

- [ ] T039 [P] [US3] Create DatabaseFixture for Testcontainer Postgres in Enerflow.Tests.Integration/Fixtures/DatabaseFixture.cs
- [ ] T040 [P] [US3] Create EnerflowDbContextTests class in Enerflow.Tests.Integration/Infrastructure/Persistence/EnerflowDbContextTests.cs
- [ ] T041 [P] [US3] Implement tests for Simulation entity CRUD operations in EnerflowDbContextTests.cs
- [ ] T042 [P] [US3] Implement tests for Compound entity CRUD operations in EnerflowDbContextTests.cs
- [ ] T043 [P] [US3] Implement tests for Stream entity CRUD operations in EnerflowDbContextTests.cs
- [ ] T044 [P] [US3] Implement tests for UnitOperation entity CRUD operations in EnerflowDbContextTests.cs
- [ ] T045 [US3] Implement tests for database transaction handling (commit/rollback) in Enerflow.Tests.Integration/Infrastructure/TransactionTests.cs
- [ ] T046 [US3] Implement tests for concurrent database access in Enerflow.Tests.Integration/Infrastructure/ConcurrencyTests.cs
- [ ] T047 [US3] Implement tests for database relationships and cascade deletes in EnerflowDbContextTests.cs
- [ ] T048 [US3] Run coverage report and verify Infrastructure layer achieves 70% target

**Checkpoint**: ✅ Database operations fully tested - Data integrity is guaranteed

---

## Phase 6: User Story 4 - Complete Incomplete Features (Priority: P2)

**Goal**: Implement and test 4 incomplete features (mass balance, unit op config, result extraction, Wegstein acceleration)

**Independent Test**: Run tests for each completed feature and verify 100% pass rate

### Implementation for User Story 4

**Feature 1: Mass Balance Validation**

- [ ] T049 [P] [US4] Implement ValidateMassBalance method in Enerflow.Simulation/Services/SimulationService.cs (replace TODO at line 497)
- [ ] T050 [P] [US4] Create MassBalanceValidationTests in Enerflow.Tests.Unit/Simulation/Services/MassBalanceValidationTests.cs
- [ ] T051 [US4] Test mass balance validation with balanced flowsheet in MassBalanceValidationTests.cs
- [ ] T052 [US4] Test mass balance validation with imbalanced flowsheet in MassBalanceValidationTests.cs

**Feature 2: Unit Operation Parameter Configuration**

- [ ] T053 [P] [US4] Implement ConfigureUnitOperationParameters method in Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs (replace TODO at line 146)
- [ ] T054 [P] [US4] Implement ConfigureHeater helper method in DWSIMFlowsheetBuilder.cs
- [ ] T055 [P] [US4] Implement ConfigureCompressor helper method in DWSIMFlowsheetBuilder.cs
- [ ] T056 [P] [US4] Implement ConfigureMixer helper method in DWSIMFlowsheetBuilder.cs
- [ ] T057 [P] [US4] Create UnitOperationConfigurationTests in Enerflow.Tests.Integration/Worker/Builders/UnitOperationConfigurationTests.cs
- [ ] T058 [US4] Test unit operation configuration for all unit types in UnitOperationConfigurationTests.cs

**Feature 3: ResultEnhancement**

- [ ] T059 [P] [US4] Implement ExtractUnitOperationResults method in Enerflow.Worker/Solvers/ResultCollector.cs (replace TODO at line 78)
- [ ] T060 [P] [US4] Implement ExtractHeaterProperties helper method in ResultCollector.cs
- [ ] T061 [P] [US4] Implement ExtractCompressorProperties helper method in ResultCollector.cs
- [ ] T062 [P] [US4] Implement ExtractHeatExchangerProperties helper method in ResultCollector.cs
- [ ] T063 [P] [US4] Create ResultExtractionTests in Enerflow.Tests.Unit/Worker/Solvers/ResultExtractionTests.cs
- [ ] T064 [US4] Test result extraction for all unit types in ResultExtractionTests.cs

**Feature 4: Wegstein Acceleration**

- [ ] T065 [P] [US4] Implement IdentifyTearStreams method in Enerflow.Worker/Solvers/DWSIMSolver.cs (replace NOTE at line 138)
- [ ] T066 [P] [US4] Implement BuildDependencyGraph helper method in DWSIMSolver.cs
- [ ] T067 [P] [US4] Implement DetectCycles helper method in DWSIMSolver.cs
- [ ] T068 [P] [US4] Implement ApplyWegsteinAcceleration method in DWSIMSolver.cs
- [ ] T069 [P] [US4] Create WegsteinAccelerationTests in Enerflow.Tests.Integration/Worker/Solvers/WegsteinAccelerationTests.cs
- [ ] T070 [US4] Test Wegstein acceleration with recycle loop in WegsteinAccelerationTests.cs

**Feature Validation**

- [ ] T071 [US4] Run all feature tests and verify 100% pass rate
- [ ] T072 [US4] Update incomplete-features.md with completion status

**Checkpoint**: ✅ All incomplete features implemented and tested - Technical debt eliminated

---

## Phase 7: User Story 5 - Service Layer Test Coverage (Priority: P2)

**Goal**: Achieve 70% test coverage on Service layer (CatalogService, JobProducer, SimulationService)

**Independent Test**: Run `dotnet test Enerflow.Tests.Unit --filter "Layer=Service"` and verify 70% coverage with `dotnet test /p:CollectCoverage=true /p:Include="[Enerflow.Simulation]*"`

### Implementation for User Story 5

- [ ] T073 [P] [US5] Create CatalogServiceTests class in Enerflow.Tests.Unit/Simulation/Services/CatalogServiceTests.cs
- [ ] T074 [P] [US5] Create JobProducerTests class in Enerflow.Tests.Unit/API/Services/JobProducerTests.cs
- [ ] T075 [P] [US5] Create SimulationServiceTests class in Enerflow.Tests.Unit/Simulation/Services/SimulationServiceTests.cs
- [ ] T076 [P] [US5] Implement tests for CatalogService GetCompounds method in CatalogServiceTests.cs
- [ ] T077 [P] [US5] Implement tests for CatalogService GetPropertyPackages method in CatalogServiceTests.cs
- [ ] T078 [P] [US5] Implement tests for CatalogService GetFlashAlgorithms method in CatalogServiceTests.cs
- [ ] T079 [P] [US5] Implement tests for JobProducer message publishing in JobProducerTests.cs
- [ ] T080 [P] [US5] Implement tests for SimulationService business logic methods in SimulationServiceTests.cs
- [ ] T081 [US5] Add error handling tests for all service methods
- [ ] T082 [US5] Run coverage report and verify Service layer achieves 70% target

**Checkpoint**: ✅ Service layer fully tested - Business logic is validated

---

## Phase 8: User Story 6 - Performance & Loidation (Priority: P3)

**Goal**: Validate backend performance under load (p95 < 500ms for API, 10 sim/min for Worker)

**Independent Test**: Run `dotnet test Enerflow.Tests.Performance` and verify all performance targets met

### Implementation for User Story 6

- [ ] T083 [P] [US6] Create Enerflow.Tests.Performance project if not exists
- [ ] T084 [P] [US6] Create SimulationSubmissionLoadTests class in Enerflow.Tests.Performance/Scenarios/SimulationSubmissionLoadTests.cs
- [ ] T085 [P] [US6] Create SimulationStatusLoadTests class in Enerflow.Tests.Performance/Scenarios/SimulationStatusLoadTests.cs
- [ ] T086 [P] [US6] Create ConcurrentUserTests class in Enerflow.Tests.Performance/Scenarios/ConcurrentUserTests.cs
- [ ] T087 [P] [US6] Implement load test for simulation submission (10 req/s, 2 min duration) in SimulationSubmissionLoadTests.cs
- [ ] T088 [P] [US6] Implement load test for simulation status retrieval in SimulationStatusLoadTests.cs
- [ ] T089 [P] [US6] Implement concurrent user test (50 concurrent users) in ConcurrentUserTests.cs
- [ ] T090 [US6] Run performance tests and verify p95 < 500ms target
- [ ] T091 [US6] Generate performance report and document results in specs/001-backend-test-coverage/performance-results.md

**Checkpoint**: ✅ Performance validated - Backend meets performance targets

---

## Phase 9: User Story 7 - MVP Readiness Assessment (Priority: P1)

**Goal**: Generate comprehensive MVP readiness report with go/no-go decision

**Independent Test**: Review generated report and verify all success criteria (SC-000 through SC-015) are met

### Implementation for User Story 7

- [ ] T092 [P] [US7] Run full test suite with coverage: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura`
- [ ] T093 [P] [US7] Generate HTML coverage report with ReportGenerator
- [ ] T094 [P] [US7] Validate API layer coverage ≥80% in coverage report
- [ ] T095 [P] [US7] Validate Wolayer coverage ≥80% in coverage report
- [ ] T096 [P] [US7] Validate Service layer coverage ≥70% in coverage report
- [ ] T097 [P] [US7] Validate Infrastructure layer coverage ≥70% in coverage report
- [ ] T098 [P] [US7] Validate Domain layer coverage ≥70% in coverage report
- [ ] T099 [US7] Verify all functional tests pass (100% pass rate)
- [ ] T100 [US7] Verify test suite execution time <10 minutes
- [ ] T101 [US7] Measure flaky test rate over 10 runs and verify <5%
- [ ] T102 [US7] Verify all 4 incomplete features are completed and tested
- [ ] T103 [US7] Create MVP readiness assessment report in specs/001-backend-test-coverage/mvp-readiness-report.md
- [ ] T104 [US7] Document all success criteria (SC-000 through SC-015) with evidence in mvp-readiness-report.md
- [ ] T105 [US7] Provide go/no-go decision for MVP readiness in mvp-readiness-report.md

**Checkpoint**: ✅ MVP readiness assessed - Clear decision on backend readiness for frontend development

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final improvements and CI/CD integration

- [ ] T106 [P] Create GitHub Actions workflow for automated test execution in .github/workflows/test-coverage.yml
- [ ] T107 [P] Configure coverage threshold validation in CI pipeline
- [ ] T108 [P] Add coverage badges to README.md
- [ ] T109 [P] Create test execution script in scripts/run-tests.sh
- [ ] T110 [P] Create coverage validation script in scripts/validate-coverage-targets.sh
- [ ] T111 [P] Update quickstart.md with final test execution instructions
- [ ] T112 Code cleanup: Remove any test scaffolding or temporary code
- [ ] T113 Documentation: Update AGENTS.md with testing guidelines
- [ ] T114 Run full validation per quickstart.md inn- [ ] T115 Final commit and push all changes

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2 - US0)**: Depends on Setup - **BLOCKS all other user stories**
- **User Stories 1-7 (Phase 3-9)**: All depend on US0 completion
  - US1 (API Tests): Can start after US0 - No dependencies on other stories
  - US2 (Worker Tests): Can start after US0 - No dependencies on other stories
  - US3 (Database Tests): Can start after US0 - No dependencies on other stories
  - US4 (Complete Features): Can start after US0 - No dependencies on other stories
  - US5 (Service Tests): Can start after US0 - No dependencies on other stories
  - US6 (Performance): Should start after US1, US2, US3 complete (needs working system)
  - US7 (MVP Assessment): Must start after US1, US2, US3, US4, US5 complete
- **Polish (Phase 10)**: Depends on all user stories being complete

### User Story Dependencies

```
US0 (Foundational) ──┬──> US1 (API Tests)
                     ├──> US2 (Worker Tests)
                     ├──> US3 (Database Tests)
                     ├──> US4 (Complete Features)
                     └──> US5 (Service Tests)

US1, US2, US3 ────────> US6 (Performance)

US1, US2, US3, US4, US5 ──> US7 (MVP Assessment)

US7 ──────────────────> Phase 10 (Polish)
```

### Critical Path

1. Setup (Phase 1) → 2-3 hours
2. **US0 - Unblock Infrastructure (Phase 2)** → 1-2 days **[BLOCKER]**
3. US1 - API Tests (Phase 3) → 3-5 days
4. US2 - Worker Tests (Phase 4) → 3-5 days
5. US3 - Database Tests (Phase 5) → 2-3 days
6. US4 - Complete Features (Phase 6) → 5 days
7. US5 - Service Tests (Phase 7) → 2-3 days
8. US6 - Performance (Phase 8) → 2-3 days
9. US7 - MVP Assessment (Phase 9) → 1 day
10. Polish (Phase 10) → 1-2 days

**Total Estimated Time**: 20-28 days (3-4 weeks)

### Parallel Opportunities

**After US0 completes, these can run in parallel:**

- US1 (API Tests) + US2 (Worker Tests) + US3 (Database Tests) + US4 (Complete Features) + US5 (Service Tests)
- If team has 5 developers, all 5 stories can proceed simultaneously
- This reduces timeline from 15-19 days sequential to 5 days parallel (longest story)

**Within each user story:**

- All tasks marked [P] can run in parallel
- Example US1: T012, T013, T014 can all run together (different test files)
- Example US2: T024-T029 can all run together (different test files)

---

## Parallel: After US0 Completes

```bash
# With 5 developers, launch all stories in parallel:

Developer A: "Implement API Layer Tests (US1)"
  - Tasks T012-T023 (API controller tests)

Developer B: "Implement Worker Layer Tests (US2)"
  - Tasks T024-T038 (Worker/Consumer tests)

Developer C: "Implement Database Tests (US3)"
  - Tasks T039-T048 (Infrastructure tests)

Developer D: "Complete Incomplete Features (US4)"
  - Tasks T049-T072 (4 features + tests)

Developer E: "Implement Service Layer Tests (US5)"
  - Tasks T073-T082 (Service tests)

# All complete in ~5 days instead of ~19 days sequential
```

---

## Implementation Strategy

### MVP First (Minimum Viable Testing)

**Goal**: Unblock functional tests + API tests only

1. Complete Phase 1: Setup (2-3 hours)
2. Complete Phase 2: US0 - Unblock Infrastructure (1-2 days) **[CRITICAL]**
3. Complete Phase 3: US1 - API Tests (3-5 days)
4. **STOP and VALIDATE**: Run API tests, verify 80% coverage
5. Generate coverage report, assess readiness

**Timeline**: 5-8 days to validate API layer

### Incremental Delivery (Recommended)

**Goal**: Add value incrementally, validate independently

1. Setup + US0 (Foundational) → 1-2 days → **Foundatio2. Add US1 (API Tests) → 3-5 days → **API validated** ✅
3. Add US2 (Worker Tests) → 3-5 days → **Worker validated** ✅
4. Add US3 (Database Tests) → 2-3 days → **Infrastructure validated** ✅
5. Add US4 (Complete Features) → 5 days → **Features complete** ✅
6. Add US5 (Service Tests) → 2-3 days → **Services validated** ✅
7. Add US6 (Performance) → 2-3 days → **Performance validated** ✅
8. Add US7 (MVP Assessment) → 1 day → **MVP READY** 🎯

**Timeline**: 20-28 days sequential, 10-15 days with parallel execution

### Parallel Team Strategy (Fastest)

**Goalximum parallelization with 5 developers

1. **Week 1**: All team completes Setup + Uogether (1-2 days)
2. **Week 2**: Once US0 done, split team:
   - Dev A: US1 (API Tests)
   - Dev B: US2 (Worker Tests)
   - Dev C: US3 (Database Tests)
   - Dev D: US4 (Complete Features)
   - Dev E: US5 (Service Tests)
3. **Week 3**: 
   - All devs: US6 (Performance) together
   - All devs: US7 (MVP Assessment) together
   - All devs: Polish together

**Timeline**: 15-18 days with 5 developers

---

## Task Summary

**Total Tasks**: 115
- Setup: 5 tasks
- US0 (Foundational): 6 tasks
- US1 (API Tests): 12 tasks
- US2 (Worker Tests): 15 tasks
- US3 (Database Tests): 10 tasks
- US4 (Complete Features): 24 tasks
- US5 (Service Tests): 10 tasks
- US6 (Performance): 9 tasks
- US7 (MVP Assessment): 14 tasks
- Polish: 10 tasks

**Parallelizable Tasks**: 78 tasks marked [P] (68%)

**Critical Path Tasks**: 37 tasks (32%)

**Estimated Effort**:
- Sequential: 20-28 days
- Parallel (5 devs): 10-15 days
- MVP Only (US0 + US1): 5-8 days

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story is indepmpletable and testable
- US0 is the critical blocker - prioritize fixing it first
- After US0, all othean proceed in parallel
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Generate coverage reports frequently to track progress
- Target: API 80%, Worker 80%, Service 70%, Infrastructure 70%, Domain 70%

---

**Tasks Status**: ✅ Complete - Ready for implementation
**Next Step**: Begin Phase 1 (Setup) or prioritize US0 (Critical Blocker)
**MVP Scope**: US0 + US1 (5-8 days) for minimum viable testing
**Full MVP**: US0 + US1 + US2 + US3 + US4 + US5 + US7 (20-28 days sequential, 10-15 days parallel)
