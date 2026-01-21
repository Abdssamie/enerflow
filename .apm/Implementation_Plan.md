# Enerflow Strongly-Typed Solver – APM Implementation Plan
**Memory Strategy:** Dynamic-MD  
**Last Modification:** 2026-01-21 - Phase 1 execution started (T1.1)

## Current Status
- **Phase 1 (Domain):** T1.1 Assigned to Agent_Arch. Legacy code exists but will be superseded by strongly-typed architecture.
- **Phase 2 (Mapping):** Pending Phase 1.
- **Phase 3 (Solver):** Pending Phase 2.

## Implementation Plan (Compact JSON)  
**Project Overview:** Transform DWSIM API verification (Tests 01-10) into production-grade strongly-typed domain architecture with custom Wegstein convergence solver. Replaces generic JSON approach with compile-time type safety across Domain, Worker mapping layer, and solver logic.

---

## Technical Domains
1. **Strongly-Typed Domain Modeling** - Object-oriented hierarchy for unit operations with compile-time safety
2. **DWSIM API Integration** - Translation layer (Domain → DWSIM) using verified API patterns from tests 01-10
3. **Convergence Algorithms** - Custom Wegstein-accelerated solver for recycle topology handling
4. **Validation Framework** - Property constraints and thermodynamic feasibility checks
5. **Worker Execution** - Thread-safe simulation orchestration with MassTransit consumer

## Architecture Blueprint
```
┌─────────────────────────────────────────────────────────────┐
│ Enerflow.Domain (Strongly-Typed)                            │
│  ├─ SimulationObject (base)                                 │
│  ├─ UnitOperations/ (Heater, Mixer, Valve, Flash, etc.)    │
│  ├─ Streams/ (MaterialStream, EnergyStream)                 │
│  ├─ Enums/ (FlashAlgorithm, CalcMode, PropertyPackage)     │
│  └─ Validation/ (FluentValidation rules)                    │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Enerflow.Worker (Solver Engine)                             │
│  ├─ DWSIMSolver (Wegstein loop coordinator)                │
│  ├─ Mappers/ (Domain → DWSIM.Automation builders)          │
│  │   ├─ UnitOperationMapper (polymorphic dispatch)         │
│  │   ├─ StreamMapper (T/P/Flow/Composition)                │
│  │   └─ PropertyPackageMapper                              │
│  ├─ Convergence/ (WegsteinAccelerator, ErrorCalculator)    │
│  └─ SimulationJobConsumer (MassTransit entry point)        │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ DWSIM.Automation (External Headless API)                    │
│  └─ Flowsheet execution (single-threaded constraint)        │
└─────────────────────────────────────────────────────────────┘
```

**Data Flow:** Domain Objects → Mappers → DWSIM Flowsheet → Solve Loop (Wegstein) → Result DTOs

---

## Implementation Plan (Compact JSON)
```json
{
  "project_name": "Enerflow Strongly-Typed Solver",
  "phases": [
    {
      "id": "P1",
      "title": "Domain Type System",
      "tasks": [
        {
          "id": "T1.1",
          "spec": "Create `SimulationObject` base class in Domain with Id (Guid), Name (string), Position (x,y ints). Add abstract `Validate()` method. Use primary constructor pattern for immutable properties where applicable.",
          "deps": [],
          "test_criteria": ["Base class compiles", "Properties are strongly typed", "Sequential Guid generation"],
          "est_tokens": 400,
          "status": "completed"
        },
        {
          "id": "T1.2",
          "spec": "Implement MaterialStream and EnergyStream classes inheriting SimulationObject. MaterialStream: T, P, MassFlow, MolarFlow, Composition (Dictionary<string,double>), PhaseType. EnergyStream: EnergyFlow (kW). Add validation (T>0, P>0, flows≥0).",
          "deps": ["T1.1"],
          "test_criteria": ["Stream classes compile", "Validation throws for invalid values", "Composition normalizes to 1.0"],
          "est_tokens": 600,
          "status": "completed"
        },
        {
          "id": "T1.3",
          "spec": "Create UnitOperation base class (extends SimulationObject) with InputStreams/OutputStreams (List<Guid>), CalcMode enum. Implement Heater/Cooler classes: Efficiency (0-1), OutletTemperature (K), PressureDrop (Pa), HeaterCalcMode enum (OutletTemp, HeatDuty). Add FluentValidation rules.",
          "deps": ["T1.1"],
          "test_criteria": ["Heater/Cooler compile", "Efficiency validated [0,1]", "CalcMode enum enforced"],
          "est_tokens": 700,
          "status": "completed"
        },,
        {
          "id": "T1.4",
          "spec": "Implement Mixer/Splitter classes. Mixer: no special properties (outputs weighted average). Splitter: SplitRatios (Dictionary<Guid,double>) must sum to 1.0. Add validation in Validate() override.",
          "deps": ["T1.3"],
          "test_criteria": ["SplitRatios validation enforces sum=1", "Mixer accepts N inputs", "Classes serialize correctly"],
          "est_tokens": 500,
          "status": "completed"
        },
        {
          "id": "T1.5",
          "spec": "Implement Valve, FlashDrum, ShortcutColumn classes. Valve: OutletPressure, ValveCalcMode. Flash: Temperature, Pressure, FlashAlgorithm enum. Column: RefluxRatio, LightKeyCompound, HeavyKeyCompound, CondenserPressure, ReboilerPressure, NumberOfStages. Add all enums and validation.",
          "deps": ["T1.3"],
          "test_criteria": ["All properties strongly typed", "Enums match DWSIM test cases", "RefluxRatio > 0 validated"],
          "est_tokens": 900,
          "status": "completed"
        },
        {
          "id": "T1.6",
          "spec": "Implement Recycle class with Tolerance (double, default 1e-4), MaxIterations (int, default 50), AccelerationMethod enum (None, Wegstein, DirectSubstitution). Create PropertyPackageType enum (PengRobinson, SRK, NRTL, Ideal) matching Test_02 verified packages.",
          "deps": ["T1.3"],
          "test_criteria": ["Recycle properties compile", "PropertyPackage enum matches DWSIM API", "Defaults applied correctly"],
          "est_tokens": 450,
          "status": "completed"
        },,
        {
          "id": "T1.7",
          "spec": "Create Simulation aggregate root with ThermoPackage (PropertyPackageType), SystemOfUnits enum, Compounds (List<string>), SimulationObjects (Dictionary<Guid, SimulationObject>). Add GetTopologicalOrder() method (throws if cyclic without Recycle). Use Graph traversal (Kahn's algorithm).",
          "deps": ["T1.2", "T1.6"],
          "test_criteria": ["Topological sort detects cycles", "Recycle breaks cycles correctly", "Simulation validates object graph"],
          "est_tokens": 800,
          "status": "completed"
        }
      ]
    },
    {
      "id": "P2",
      "title": "DWSIM Mapping Layer",
      "tasks": [
        {
          "id": "T2.1",
          "spec": "Create IFlowsheetBuilder interface with BuildFlowsheet(Simulation) → DWSIM.FlowSheet. Implement DWSIMFlowsheetBuilder: initialize AutomationMode=true, set PropertyPackage, add Compounds using verified API patterns from Test_01/02.",
          "deps": ["T1.7"],
          "test_criteria": ["Flowsheet initializes headless", "Compounds added without duplication error", "PropertyPackage set correctly"],
          "est_tokens": 700,
          "status": "completed"
        },
        {
          "id": "T2.2",
          "spec": "Implement StreamMapper with MapMaterialStream(MaterialStream, Flowsheet) and MapEnergyStream(EnergyStream, Flowsheet). Use flowsheet.AddObject(ObjectType.MaterialStream), set T/P/Flow/Composition using Phases[0].Compounds per Test_03/04 patterns. DO NOT call AddCompoundsToMaterialStream.",
          "deps": ["T2.1"],
          "test_criteria": ["Streams created in DWSIM", "Composition set via Phases[0]", "No duplicate compound errors"],
          "est_tokens": 650,
          "status": "completed"
        },
        {
          "id": "T2.3",
          "spec": "Implement UnitOperationMapper with polymorphic Map(UnitOperation) → DWSIM.SimulationObjects.UnitOps.UnitOpBaseClass. Use pattern matching on type. Implement MapHeater/MapCooler: set CalcMode BEFORE properties (OutletTemperature, Efficiency, PressureDrop) per Test_05.",
          "deps": ["T2.1"],
          "test_criteria": ["Heater/Cooler mapped correctly", "CalcMode set first", "Properties applied per DWSIM API"],
          "est_tokens": 800,
          "status": "completed"
        },
        {
          "id": "T2.4",
          "spec": "Add MapMixer, MapSplitter, MapValve to UnitOperationMapper. Mixer: connect all InputStreams. Splitter: set SplitRatios via Ratios property. Valve: set CalcMode then OutletPressure. Verify against Test_06 patterns.",
          "deps": ["T2.3"],
          "test_criteria": ["Mixer accepts N inputs", "Splitter ratios applied", "Valve pressure set correctly"],
          "est_tokens": 700,
          "status": "completed"
        },
        {
          "id": "T2.5",
          "spec": "Add MapFlashDrum, MapShortcutColumn to UnitOperationMapper. Flash: set FlashAlgorithm, T, P per Test_07. Column: set all properties (RefluxRatio, LightKey, HeavyKey, CondenserP, ReboilerP, Stages) matching Test_08/09 verified configurations.",
          "deps": ["T2.3"],
          "test_criteria": ["Flash converges with correct algorithm", "Column properties match test cases", "LightKey/HeavyKey strings resolved"],
          "est_tokens": 850,
          "status": "completed"
        },
        {
          "id": "T2.6",
          "spec": "Implement ConnectionMapper with ConnectStreams(Simulation, Flowsheet). Iterate InputStreams/OutputStreams on each UnitOperation, call flowsheet.ConnectObjects(sourceId, targetId, inletPort, outletPort) using DWSIM API. Handle port indexing (0-based).",
          "deps": ["T2.2", "T2.5"],
          "test_criteria": ["All connections established", "Port indices correct", "Graph connectivity validated"],
          "est_tokens": 600,
          "status": "completed"
        }
      ]
    },
    {
      "id": "P3",
      "title": "Wegstein Convergence Solver",
      "tasks": [
        {
          "id": "T3.1",
          "spec": "Create ConvergenceConfiguration class (MaxIterations, Tolerance, Lambda) and IConvergenceAccelerator interface. Implement WegsteinAccelerator: Accelerate(X_old, X_calc) → X_next using formula λ·X_calc + (1-λ)·X_old. Support vector inputs (T, P, Flow arrays).",
          "deps": [],
          "test_criteria": ["Wegstein formula implemented", "Lambda ∈ [0,1] validated", "Vector math correct"],
          "est_tokens": 550,
          "status": "completed"
        },
        {
          "id": "T3.2",
          "spec": "Implement ErrorCalculator service: CalculateError(Flowsheet) → double. Extract all MaterialStream mass balances, compute ΣIn - ΣOut for each component. Return max absolute relative error. Use flowsheet.SimulationObjects.Values filtered by Type.",
          "deps": [],
          "test_criteria": ["Error calculated for all streams", "Returns max deviation", "Handles zero flows"],
          "est_tokens": 500,
          "status": "completed"
        },
        {
          "id": "T3.3",
          "spec": "Create DWSIMSolver service (ISimulationSolver interface). Implement Solve(Simulation, ConvergenceConfig) → SimulationResult. Algorithm: (1) Build flowsheet via IFlowsheetBuilder, (2) Identify recycle streams, (3) Iterate: CalculateFlowsheet2(), check error, accelerate recycle streams, (4) Validate convergence < tolerance or throw.",
          "deps": ["T2.6", "T3.1", "T3.2"],
          "test_criteria": ["Converges for Test_10 recycle case", "Throws on MaxIterations", "Updates recycle streams correctly"],
          "est_tokens": 1100,
          "status": "completed"
        },
        {
          "id": "T3.4",
          "spec": "Add ResultCollector service: ExtractResults(Flowsheet) → SimulationResult DTO. Iterate all MaterialStreams/EnergyStreams, extract T, P, Flow, Composition (Phases[0].Compounds MoleFraction), PhaseType. Create dictionary keyed by object Name. Check flowsheet.Solved and ErrorMessage.",
          "deps": ["T3.3"],
          "test_criteria": ["All stream results captured", "Compositions extracted correctly", "ErrorMessage logged if !Solved"],
          "est_tokens": 650,
          "status": "completed"
        }
      ]
    },
    {
      "id": "P4",
      "title": "Worker Integration",
      "tasks": [
        {
          "id": "T4.1",
          "spec": "Refactor SimulationJobConsumer in Worker: Inject ISimulationSolver, ILogger. In Consume(): Deserialize SimulationJob → Simulation entity, call Solve() with default ConvergenceConfig, handle exceptions (update DB status to Failed), persist result on success. Maintain SemaphoreSlim wrapper.",
          "deps": ["T3.4"],
          "test_criteria": ["Consumer compiles", "Solver called with correct config", "Exceptions caught and logged"],
          "est_tokens": 700,
          "status": "completed"
        },
        {
          "id": "T4.2",
          "spec": "Register all services in Worker Program.cs DI: AddSingleton<IFlowsheetBuilder, DWSIMFlowsheetBuilder>(), AddScoped<ISimulationSolver, DWSIMSolver>(), AddScoped mappers. Ensure DWSIM AutomationMode set in Program.cs before Host.Run(). Verify ConcurrentMessageLimit=1 in MassTransit config.",
          "deps": ["T4.1"],
          "test_criteria": ["All DI registrations compile", "AutomationMode=true at startup", "MassTransit concurrency=1"],
          "est_tokens": 500,
          "status": "completed"
        },
        {
          "id": "T4.3",
          "spec": "Create Worker unit tests (Enerflow.Tests.Unit/WorkerTests/): Mock IFlowsheetBuilder, test Consume() happy path (job → Solve → success), test failure path (exception → status=Failed). Use xUnit + Moq. Verify SemaphoreSlim blocks concurrent calls.",
          "deps": ["T4.2"],
          "test_criteria": ["Happy path test passes", "Failure logged correctly", "Concurrency test blocks"],
          "est_tokens": 800
        }
      ]
    },
    {
      "id": "P5",
      "title": "Integration Verification",
      "tasks": [
        {
          "id": "T5.1",
          "spec": "Create Enerflow.Tests.Integration project. Implement FlowsheetBuilderIntegrationTests: For each Test_01-10 scenario, construct Simulation with strongly-typed objects, call BuildFlowsheet(), verify DWSIM object count and types match expected. Use real DWSIM binaries (AutomationMode=true).",
          "deps": ["T2.6"],
          "test_criteria": ["All 10 test scenarios replicated", "DWSIM objects created", "No API exceptions"],
          "est_tokens": 1200
        },
        {
          "id": "T5.2",
          "spec": "Implement SolverIntegrationTests: For Tests 05-10 (executable cases), construct Simulation, call DWSIMSolver.Solve(), verify convergence (error < 1e-4), validate result properties (T, P, Flow non-zero). Test_10 must use Wegstein acceleration and converge in <50 iterations.",
          "deps": ["T3.4", "T5.1"],
          "test_criteria": ["All cases converge", "Test_10 uses Wegstein", "Results match DWSIM expected outputs"],
          "est_tokens": 1300
        },
        {
          "id": "T5.3",
          "spec": "Add end-to-end Worker test (Functional): Use Testcontainers for Postgres, publish SimulationJob (Test_10 scenario) via MassTransit, verify Worker consumes, solves, persists result. Poll DB for status=Completed and validate result JSON. Timeout: 30s.",
          "deps": ["T4.3", "T5.2"],
          "test_criteria": ["Job processed end-to-end", "Result persisted in DB", "Status transitions correct"],
          "est_tokens": 1000
        }
      ]
    }
  ]
}
```

---

## Summary

**Total Tasks:** 20  
**Estimated Tokens:** ~15,400  
**Key Constraints:**
- DWSIM single-threaded (SemaphoreSlim + ConcurrentMessageLimit=1)
- CalcMode MUST be set before property values
- NO AddCompoundsToMaterialStream call (auto-added by DWSIM)
- Wegstein lambda default 0.7, configurable
- Sequential Guids via Common.IdGenerator.NextGuid()

**Success Criteria:** All DWSIM Tests 01-10 scenarios executable via strongly-typed domain objects, Worker processes jobs with Wegstein convergence, integration tests pass.

---

## Execution Notes
- **Parallelization:** Phase 1 tasks T1.2-T1.6 can run in parallel after T1.1
- **Critical Path:** T1.1 → T1.7 → T2.1 → T2.6 → T3.3 → T4.1 → T5.3
- **DWSIM API Verification:** Load skill `dwsim-api-verification` before implementing mappers
