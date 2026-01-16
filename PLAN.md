# Enerflow: Chemical Simulation & AI Orchestration Plan

This document outlines the roadmap for documentation, system design, and robustness strategies for the Enerflow project.

## 1. System Architecture Design

### Hybrid Core Pattern
*   **Simulation Engine (ASP.NET Core 10.0):** Acts as the "Physical Controller". Directly interfaces with DWSIM DLLs. Responsible for thermodynamic precision and flowsheet convergence.
*   **Agentic Brain (FastAPI / Python):** Orchestrates AI agents using frameworks like LangChain or PydanticAI. Uses the C# API as a high-level toolset.

### Design Principles
*   **Stateless Scaling:** While DWSIM flowsheets are stateful, the API will manage state via a persistent registry and unique session IDs, allowing horizontal scaling of the API front-end.
*   **The "Validation Gate":** A dedicated middleware layer in C# to validate chemical inputs (T > 0K, P > 0, mole fractions sum to 1.0) before they reach the DWSIM solver to prevent engine crashes.
*   **Resource Management:** Automated unloading of idle flowsheets to manage the significant memory footprint of DWSIM objects.

## 2. Documentation Strategy

### Technical Documentation
*   **API Specification:** Automated OpenAPI (Swagger) generation for all simulation endpoints.
*   **Developer Guide:** Setup instructions for DWSIM binaries, including environment variable configurations and library pathing.
*   **Architecture Decision Records (ADRs):** Documenting why we chose the C#/Python hybrid approach and specific thermodynamic packages.

### Engineering Knowledge Base
*   **ChemE Walkthrough:** Documentation for non-chemical engineers (AI developers) explaining Tearing, Flash calculations, and Degrees of Freedom.
*   **Agent Tooling Specs:** Detailed schemas for AI agents to understand how to interact with chemical streams.

## 3. Robustness & Reliability Plan

### Solver Resilience
*   **Convergence Monitoring:** Instead of a binary success/fail, the API will return convergence deltas so agents can understand "how close" they are to a solution.
*   **Timeout & Circuit Breaker:** Prevent infinite loops in recycle streams by implementing strict calculation timeouts and circuit breakers for failing equipment blocks.

### Data Integrity
*   **Unit-Safe Boundary:** Strict enforcement of SI units internally, with a "Translation Layer" for user-facing units (Metric/Imperial).
*   **Snapshotting:** Capability to "save-point" a flowsheet state before a risky AI optimization, allowing instant rollback if the solver diverges.

## 4. Testing & Quality Assurance

*   **Unit Tests:** Testing the `FlowsheetService` logic and unit converters.
*   **Integration Tests:** End-to-end runs using standard DWSIM benchmarks (e.g., Water-Ethanol separation) to verify DLL output consistency.
*   **Agent Simulation Stress-Tests:** Running "chaotic agents" that purposefully push parameters to boundaries to test system stability.

## 5. Implementation Roadmap

### Phase 1: Foundation (Current)
- [x] Project scaffolding and DWSIM bootstrapping.
- [x] Flowsheet Registry and basic persistence.
- [ ] Stream-Flash property engine.

### Phase 2: Orchestration (Upcoming)
- [ ] FastAPI Agentic Service setup.
- [ ] API-to-Agent tool definition.
- [ ] Input Validation Gate implementation.

### Phase 3: Intelligence
- [ ] Optimization loops for Yield/Purity.
- [ ] Multi-agent collaboration for complex plant simulation.
