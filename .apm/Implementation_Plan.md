# Enerflow Backend Core – APM Implementation Plan
**Memory Strategy:** Dynamic-MD
**Last Modification:** Task 2.3 executed. Updated messaging transport from Redis to PostgreSQL (SQL Transport).
**Project Overview:** Professional-grade process simulation backend enabling AI agent interaction via a "Scratchpad" API and DWSIM Worker execution. Features Postgres/Redis infra, MassTransit messaging, and comprehensive End-to-End verification.

## Phase 1: Foundation & Domain Modeling
**Goal:** Establish the Data Model, Infrastructure, and Shared Types.

### Task 1.1 – Infrastructure Setup (Docker Compose) - Agent_Arch
**Objective:** Create the local runtime environment for PostgreSQL and Redis using Docker Compose.
**Output:** `docker-compose.yml` and `.env.example` files.
**Guidance:** Ensure ports and volumes are configured for persistence and easy access.

- Create `docker-compose.yml` defining `postgres:17-bookworm-slim` and `redis:8-alpine` services.
- Configure persistent volumes (e.g., `postgres_data`) to ensure data survival across restarts.
- Define health checks for both services (critical for future Testcontainers parity).
- Create a `.env.example` file documenting the necessary environment variables (ConnectionString, RedisConfiguration).

### Task 1.2 – Domain Entity Definition (Relational) - Agent_Arch
**Objective:** Define the core relational Domain Entities for Simulations, Compounds, Streams, and Units.
**Output:** Entity classes in `Enerflow.Domain`.
**Guidance:** Corresponds to Linear Issue MNDSK-49. Use relational model, not single JSONB.

- Create `Simulation` entity (Id, Name, ThermoPackage, SystemOfUnits, State) as the aggregate root.
- Create `Compound` entity (Id, SimId, Name, ConstantProperties [JSON]).
- Create `MaterialStream` entity (Id, SimId, T, P, MassFlow, MolarCompositions [JSON], PhaseInfo).
- Create `EnergyStream` entity (Id, SimId, EnergyFlow, IsInput).
- Create `UnitOperation` entity (Id, SimId, Type, InputStreams [Array], OutputStreams [Array], ConfigParams [JSON]).
- Ensure strict use of `.NET 10.0` features and proper foreign key relationships.

### Task 1.3 – ACL & Shared DTOs - Agent_Arch
**Objective:** Implement the Anti-Corruption Layer (Enums and DTOs) to decouple Domain from DWSIM details.
**Output:** Enum and DTO files in `Enerflow.Domain`.
**Guidance:** Corresponds to Linear Issue MNDSK-50. strict separation from DWSIM binaries.

- Define Enums: `UnitOperationType` (Mixer, Splitter, Pump, etc.) and `PropertyPackageType`.
- Define DTOs: `SimulationJob` (Input for Worker) and `SimulationResult` (Output from Worker).
- Create Validation DTOs for the "Actuator" layer (e.g., `AddUnitRequest`, `ConnectStreamRequest`) if they belong in Domain.
- Ensure strict POCO nature (no DWSIM dependencies).

### Task 1.4 – Database Context & Migrations - Agent_Arch
**Objective:** Configure EF Core with Npgsql and generate the initial schema migration.
**Output:** `EnerflowDbContext.cs` and Migration files.
**Guidance:** Ensure JSONB mapping is correctly configured in `OnModelCreating`.
**Depends on: Task 1.2 Output**
**Depends on: Task 1.1 Output**

1. Implement `EnerflowDbContext` inheriting from `DbContext` in `Enerflow.Infrastructure`.
2. Configure `OnModelCreating` to map the relational schema:
    - `Simulation` (Root)
    - `Compound` (1:N with Simulation)
    - `MaterialStream` (1:N with Simulation, stores Temp/Pressure/Flow/Compositions)
    - `EnergyStream` (1:N with Simulation)
    - `UnitOperation` (1:N with Simulation, Polymorphic `config_params` JSONB, Input/Output arrays)
3. Register `EnerflowDbContext` in the Dependency Injection container.
4. Run `dotnet ef migrations add InitialCreate` to generate the migration files.

## Phase 2: Messaging & Worker Logic
**Goal:** Build the "Engine" that executes simulations using MassTransit and DWSIM.

### Task 2.1 – Redis Rate Limiting Implementation - Agent_Arch
**Objective:** Protect the simulation engine from overload using Redis-backed rate limiting.
**Output:** Middleware configuration in `Enerflow.API`.
**Guidance:** Corresponds to Linear Issue MNDSK-52.

- Implement Rate Limiting using ASP.NET Core 7+ `AddRateLimiter` or specific Middleware.
- Configure a Redis-backed policy (e.g., Fixed Window: 10 requests/minute) using `StackExchange.Redis`.
- Apply the rate limiting policy to the Simulation Job Submission endpoints.
- Ensure proper fallback headers (`Retry-After`) are sent.

### Task 2.2 – MassTransit Infrastructure & Producer - Agent_Arch
**Objective:** Configure MassTransit with PostgreSQL Transport and implement the Job Producer.
**Output:** MassTransit setup in API and `IJobProducer` service.
**Guidance:** Corresponds to Linear Issue MNDSK-51. Updated to use PostgreSQL Transport.

- Install `MassTransit` and `MassTransit.SqlTransport.PostgreSQL` packages.
- Configure the Service Collection in `Program.cs` to use PostgreSQL Transport.
- Implement `IJobProducer` service that accepts `SimulationJob` DTO and publishes it to the `simulation-jobs` queue.
- Ensure serialization settings match the Worker's expectation (System.Text.Json).
- Use `dotnet add package` without version numbers to resolve latest stable.

### Task 2.3 – Worker Service Consumer Scaffolding - Agent_Worker
**Objective:** Transform the Worker into a hosted service that listens to the PostgreSQL queue.
**Output:** `Enerflow.Worker` configured as a Host with MassTransit Consumer.
**Guidance:** Corresponds to Linear Issue MNDSK-55. Depends on MassTransit configuration from Task 2.2.
**Depends on: Task 2.2 Output by Agent_Arch**

- Refactor `Enerflow.Worker` from a transient Console App to a `Host.CreateApplicationBuilder` (Worker Service) pattern.
- Implement `SimulationJobConsumer` class inheriting from `IConsumer<SimulationJob>`.
- Configure MassTransit in the Worker to connect to the same PostgreSQL instance and bind to the `simulation-jobs` queue.
- Implement graceful shutdown handling to prevent killing simulations mid-process.

### Task 2.4 – Domain-to-DWSIM Mapper Implementation - Agent_Worker
**Objective:** Implement the logic to translate Domain DTOs into DWSIM Flowsheet objects.
**Output:** Mapper classes in `Enerflow.Worker`.
**Guidance:** Corresponds to Linear Issue MNDSK-56. The "Brain" of the worker.

1. Initialize `DWSIM.Automation.AutomationManager` in Headless mode (ensure `AutomationMode = true`).
2. Implement `UnitOperationFactory` to map `UnitOperationType` enums to DWSIM classes (e.g., `Mixer` -> `DWSIM.UnitOperations.Mixer`).
3. Implement `StreamMapper` to create and configure Material/Energy streams (T, P, Flow) from the input DTO.
4. Implement `ConnectionMapper` to programmatically connect Stream IDs to Unit Ports (Inlet/Outlet) based on the graph definition.
5. Implement `ParameterMapper` to apply default Property Packages and specific unit parameters.

### Task 2.5 – Simulation Execution Logic - Agent_Worker
**Objective:** Execute the simulation solve loop, handle errors, and publish results.
**Output:** Complete `Consume` method logic.
**Guidance:** Corresponds to Linear Issue MNDSK-57.
**Depends on: Task 2.4 Output**
**Depends on: Task 2.3 Output**

1. Inject the Mapper (Task 2.4) into `SimulationJobConsumer`.
2. In the `Consume` method: Create the flowsheet, Map the input, and call `flowsheet.Solve()`.
3. Implement "Constraint-Driven" error handling: Check for mass balance errors or specific DWSIM error messages.
4. Map the DWSIM results back to `SimulationResult` DTO.
5. Persist the result to the Database (using `EnerflowDbContext` or API callback) and/or publish a `SimulationCompleted` event.

### Task 2.6 – Worker Concurrency Safety (Repair) - Agent_Worker
**Objective:** Enforce thread safety for the non-thread-safe DWSIM Automation engine.
**Output:** `SimulationJobConsumer` with SemaphoreSlim locking.
**Guidance:** DWSIM AutomationManager is a static singleton and not thread-safe.

- Add `static SemaphoreSlim _simulationLock = new(1, 1);` to `SimulationJobConsumer`.
- Wrap the critical simulation logic in `Consume` with `await _simulationLock.WaitAsync()` and `Release()`.
- Configure MassTransit `ConcurrentMessageLimit = 1` in `Program.cs`.

## Phase 3: API Implementation (The Actuator)
**Goal:** Expose the system to Users/Agents via REST endpoints for building and running simulations.

### Task 3.1 – Job Submission Endpoint - Agent_API
**Objective:** Implement the endpoint to submit simulation jobs to the queue.
**Output:** `SimulationJobsController` with Submit method.
**Guidance:** Corresponds to Linear Issue MNDSK-53. Uses the MassTransit Producer.
**Depends on: Task 2.2 Output by Agent_Arch**

- Implement `SimulationJobsController` with a `POST /api/jobs` endpoint.
- Validate the incoming request (ensure referenced Session/Definition exists).
- Use the `IJobProducer` service to publish the `SimulationJob` message via MassTransit.
- Return `202 Accepted` with a `jobId` to allow polling.

### Task 3.2 – Status & Result Endpoints - Agent_API
**Objective:** Implement endpoints to retrieve job status and detailed simulation results.
**Output:** Status and Result methods in Controller.
**Guidance:** Corresponds to Linear Issues MNDSK-54 and MNDSK-61.

- Implement `GET /api/jobs/{id}/status` returning the `SimulationStatus` enum (Pending, Running, Completed, Failed).
- Implement `GET /api/jobs/{id}/result` returning the full `SimulationResult` DTO/JSON.
- Ensure that "Failed" jobs return the structured **AI-Friendly Error** object (Code, Message, Context) to enable agent self-correction.
- Optimize database queries to only fetch necessary fields (e.g., don't fetch heavy JSON for status check).

### Task 3.3 – Scratchpad Builder Endpoints - Agent_API
**Objective:** Implement the "Actuator" endpoints for incrementally building simulations via relational updates.
**Output:** `SimulationsController` with modification methods.
**Guidance:** Corresponds to Linear Issue MNDSK-59. Manages the relational data. Client handles History.

1. Implement `POST /api/simulations/{id}/units`: Create a `UnitOperation` entity linked to the simulation.
2. Implement `POST /api/simulations/{id}/streams`: Create a `MaterialStream` entity.
3. Implement `PUT /api/simulations/{id}/connect`: Update `InputStreams`/`OutputStreams` arrays on the Unit to link to Stream IDs.
4. Return the updated specific entity or lightweight graph summary.
5. **Note:** History management is offloaded to the client; no server-side "Undo" logic required here.

### Task 3.4 – JSON Import/Export Endpoint - Agent_API
**Objective:** Allow importing/exporting the simulation definition as a custom JSON format.
**Output:** Import/Export endpoints.
**Guidance:** Replaces MNDSK-58 (.dwxmz export). Use custom JSON format.
**Depends on: Task 3.3 Output**

- Implement `GET /api/simulations/{id}/export`: Serialize the full Entity Graph (Simulation + streams + units) to a structured JSON file.
- Implement `POST /api/simulations/import`: Accept the JSON structure, validate, and bulk-insert the Entities (Simulation, Compounds, Streams, Units) to recreate the state.
- Return the new Simulation ID.

### Task 3.5 – Metadata & Catalog Endpoints - Agent_API
**Objective:** Expose DWSIM capabilities (Property Packages, Unit Types, Compounds) to allow clients to discover valid configuration options.
**Output:** `CatalogsController` with discovery endpoints.
**Guidance:** Essential for frontend/agent discovery of what can be simulated.

- Implement `GET /api/catalogs/compounds`: Endpoint to search/list available chemical compounds (e.g., Water, Methane).
- Implement `GET /api/catalogs/unit-ops`: List supported Unit Operation types and their port requirements.
- Implement `GET /api/catalogs/property-packages`: List available Thermodynamics packages (e.g., Peng-Robinson, NRTL).
- Ensure this data is sourced from a static definition or DWSIM instance (if lightweight) without heavyweight Worker instantiation if possible.

## Phase 4: System Verification
**Goal:** Prove the system works End-to-End using functional tests with real infrastructure.

### Task 4.1 – Functional Test Infrastructure (Testcontainers) - Agent_QA
**Objective:** Setup the xUnit functional test project with Testcontainers for Postgres and Redis.
**Output:** `Enerflow.Tests.Functional` project and Fixture classes.
**Guidance:** Ensure the Worker Service is registered in the Test Host to enable full processing verification.
**Depends on: Task 3.5 Output by Agent_API**

- Create `Enerflow.Tests.Functional` xUnit project.
- Implement `IntegrationTestFixture` using `Testcontainers` to spin up `Postgres` and `Redis`.
- Configure `WebApplicationFactory` to replace real connection strings with container strings.
- **Critical:** Register the `Enerflow.Worker` services (Consumer) within the Test Host (or run it as a background service in the test) so that jobs published to MassTransit are actually processed during the test.

### Task 4.2 – End-to-End Verification Scenarios - Agent_QA
**Objective:** Implement comprehensive test scenarios matching the "AI Agent" user journey.
**Output:** Test classes (e.g., `SimulationFlowTests.cs`).
**Guidance:** Corresponds to Linear Issue MNDSK-62.
**Depends on: Task 4.1 Output**

1. Implement "Happy Path" Scenario: Create Session -> Add Mixer -> Add Streams -> Connect -> Submit -> Poll -> Verify Result (Mass Balance).
2. Implement "Error Path" Scenario: Create Invalid Graph (e.g., disconnected stream) -> Submit -> Verify **AI-Friendly Error** response.
3. Implement "Rate Limit" Scenario: Submit jobs rapidly -> Verify `429 Too Many Requests`.
4. Run `dotnet test` to confirm all scenarios pass.
