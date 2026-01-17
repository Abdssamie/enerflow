# Enerflow Backend Core – APM Memory Root
**Memory Strategy:** Dynamic-MD
**Project Overview:** Professional-grade process simulation backend enabling AI agent interaction via a "Scratchpad" API and DWSIM Worker execution. Features Postgres/Redis infra, MassTransit messaging, and comprehensive End-to-End verification.

## Phase 1 – Foundation & Domain Modeling Summary
* **Outcome**: Established the core backend foundation. Docker infrastructure (Postgres 18/Redis 8) is configured. The Relational Domain Model (Simulation, Compound, Streams, UnitOps) was defined and mapped to the database via EF Core. The Anti-Corruption Layer (DTOs/Enums) was implemented to decouple the Domain from the Worker's DWSIM implementation.
* **Agents**: Agent_Arch
* **Logs**:
    * [.apm/Memory/Phase_1_Foundation_Domain_Modeling/Task_1_1_Infrastructure_Setup.md](Task_1_1_Infrastructure_Setup.md)
    * [.apm/Memory/Phase_1_Foundation_Domain_Modeling/Task_1_2_Domain_Entity_Definition.md](Task_1_2_Domain_Entity_Definition.md)
    * [.apm/Memory/Phase_1_Foundation_Domain_Modeling/Task_1_3_ACL_Shared_DTOs.md](Task_1_3_ACL_Shared_DTOs.md)
    * [.apm/Memory/Phase_1_Foundation_Domain_Modeling/Task_1_4_Database_Context_Migrations.md](Task_1_4_Database_Context_Migrations.md)

## Phase 2 – Messaging & Worker Logic Summary
* **Outcome**: Built the execution engine. Implemented rate limiting (Redis) and MassTransit infrastructure using PostgreSQL Transport. The Worker is now a robust Hosted Service that consumes `simulation-jobs`, enforcing strict concurrency (limit=1) to safely manage the non-thread-safe DWSIM automation engine. A complete Domain-to-DWSIM mapper (`SimulationService`) translates DTOs into flowsheet objects, solves them, and persists results directly to the database.
* **Agents**: Agent_Arch, Agent_Worker
* **Logs**:
    * [.apm/Memory/Phase_2_Messaging_Worker_Logic/Task_2_1_Redis_Rate_Limiting.md](Task_2_1_Redis_Rate_Limiting.md)
    * [.apm/Memory/Phase_2_Messaging_Worker_Logic/Task_2_2_MassTransit_Infrastructure.md](Task_2_2_MassTransit_Infrastructure.md)
    * [.apm/Memory/Phase_2_Messaging_Worker_Logic/Task_2_3_Worker_Service_Consumer.md](Task_2_3_Worker_Service_Consumer.md)
    * [.apm/Memory/Phase_2_Messaging_Worker_Logic/Task_2_4_Domain_DWSIM_Mapper.md](Task_2_4_Domain_DWSIM_Mapper.md)
    * [.apm/Memory/Phase_2_Messaging_Worker_Logic/Task_2_5_Simulation_Execution_Logic.md](Task_2_5_Simulation_Execution_Logic.md)
    * [.apm/Memory/Phase_2_Messaging_Worker_Logic/Task_2_6_Worker_Concurrency_Safety.md](Task_2_6_Worker_Concurrency_Safety.md)

## Phase 3 – API Implementation Summary
* **Outcome**: Implemented the "Actuator" layer. Exposed endpoints for Simulation Construction ("Scratchpad"), Job Submission, Status Polling, Result Retrieval, JSON Import/Export, and Catalog Discovery. The API is fully functional, supporting the "Submit -> Poll -> Read" pattern and incremental graph building.
* **Agents**: Agent_API
* **Logs**:
    * [.apm/Memory/Phase_3_API_Implementation/Task_3_1_Job_Submission_Endpoint.md](Task_3_1_Job_Submission_Endpoint.md)
    * [.apm/Memory/Phase_3_API_Implementation/Task_3_2_Status_Result_Endpoints.md](Task_3_2_Status_Result_Endpoints.md)
    * [.apm/Memory/Phase_3_API_Implementation/Task_3_3_Scratchpad_Builder_Endpoints.md](Task_3_3_Scratchpad_Builder_Endpoints.md)
    * [.apm/Memory/Phase_3_API_Implementation/Task_3_4_JSON_Import_Export.md](Task_3_4_JSON_Import_Export.md)
    * [.apm/Memory/Phase_3_API_Implementation/Task_3_5_Metadata_Catalog_Endpoints.md](Task_3_5_Metadata_Catalog_Endpoints.md)
