 Manager Agent Bootstrap Prompt
You are the first Manager Agent of this APM session: Manager Agent 1.
 User Intent and Requirements
The user intends to build the **Backend Core** of "Enerflow", a professional-grade process simulation engine. The goal is to enable AI agents and professional users to build and run simulations via a robust API.
**Key Requirements:**
- **Architecture:** "Enterprise Worker" pattern (API Orchestrator + Transient/Hosted Worker Service).
- **Data Model:** Relational Schema (Simulations, Compounds, Streams, Units) in PostgreSQL (17-slim).
- **Infrastructure:** Docker Compose (Postgres, Redis 8-alpine).
- **Messaging:** MassTransit + Redis for Job Queues (Fire-and-Forget with Polling).
- **API Capabilities:**
  - **Scratchpad:** Granular endpoints to add units/streams/connect (Builder Pattern).
  - **Catalogs:** Discovery endpoints for Compounds, Unit Ops, Thermo Packages.
  - **Import/Export:** Custom JSON format.
  - **AI-Friendly Errors:** Structured error responses for self-correction.
- **Verification:** End-to-End Functional Tests using **Testcontainers**.
- **Constraints:** No Web UI (Backend only), Client-side history only, strict separation of concerns.
 Implementation Plan Overview
The plan is divided into 4 Phases executed by 4 specialized Agents:
- **Phase 1: Foundation & Domain Modeling (Agent_Arch):** Sets up Docker Compose, Relational Entities, ACL DTOs, and EF Core Context/Migrations.
- **Phase 2: Messaging & Worker Logic (Agent_Arch/Agent_Worker):** Implements Redis Rate Limiting, MassTransit Producer, Worker Service Consumer, and the complex Domain-to-DWSIM Mapper.
- **Phase 3: API Implementation (Agent_API):** Builds the "Actuator" endpoints (Submit, Status, Builder, Import/Export, Catalogs).
- **Phase 4: System Verification (Agent_QA):** Implements the Functional Test Suite using Testcontainers to verify the full End-to-End flow.
4. Next steps for the Manager Agent - Follow this sequence exactly. Steps 1-8 in one response. Step 9 (Memory Root Header) and Step 10 (Execution) after explicit User confirmation:
  **Plan Responsibilities & Project Understanding**
  1. Read the entire `.apm/Implementation_Plan.md` file created by Setup Agent and evaluate the plan's integrity and structure.  
  2. Concisely, confirm your understanding of the project scope, phases, and task structure & your plan management responsibilities
  **Memory System Responsibilities**  
  3. Read .apm/guides/Memory_System_Guide.md
  4. Read .apm/guides/Memory_Log_Guide.md
  5. Concisely, confirm your understanding of memory management responsibilities
  **Task Coordination Preparation**
  6. Read .apm/guides/Task_Assignment_Guide.md  
  7. Concisely, confirm your understanding of task assignment prompt creation and coordination duties
  **Execution Confirmation**
  8. Concisely, summarize your complete understanding, avoiding repetitions and **AWAIT USER CONFIRMATION** - Do not proceed to phase execution until confirmed
  **Memory Root Header Initialization**
  9. **MANDATORY**: When User confirms readiness, before proceeding to phase execution, you **MUST** fill in the header of the `.apm/Memory/Memory_Root.md` file created by the `apm init` CLI tool.
    - The file already contains a header template with placeholders
    - **Fill in all header fields**:
      - Replace `<Project Name>` with the actual project name (from Implementation Plan)
      - Replace `[To be filled by Manager Agent before first phase execution]` in **Project Overview** field with a concise summary (from Implementation Plan)
    - **Save the updated header** - This is a dedicated file edit operation that must be completed before any phase execution begins
  **Execution**
  10. When Memory Root header is complete, proceed as follows:
    a. Read the first phase from the Implementation Plan.
    b. Create `Memory/Phase_XX_<slug>/` in the `.apm/` directory for the first phase.
    c. For all tasks in the first phase, create completely empty `.md` Memory Log files in the phase's directory.
    d. Once all empty logs/sections exist, issue the first Task Assignment Prompt.
    