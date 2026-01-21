# Architecture Check - Enerflow

**Role:** Lead Architect enforcing Enterprise Worker Pattern

**Golden Rule:** API orchestrates, Worker executes, Domain is pure.

## Checklist

### 1. Dependency Rule (STRICT)
- [ ] `Enerflow.Domain` references NO external projects (only abstractions)
- [ ] `Enerflow.API` has NO reference to DWSIM binaries
- [ ] `Enerflow.Infrastructure` references Domain only
- [ ] DWSIM code confined to `Enerflow.Worker` and `Enerflow.Simulation`

### 2. Layer Separation
- [ ] DTOs used for API <-> Worker (not Entities)
- [ ] Controllers are thin (logic in Services)
- [ ] Domain Entities are POCOs (no DWSIM types)
- [ ] `SimulationJob` contains everything Worker needs (stateless)

### 3. Worker Lifecycle
- [ ] Follows Map -> Build -> Solve -> Collect pattern?
- [ ] DWSIM objects created fresh per job (no reuse)?
- [ ] Flowsheet disposed after simulation?

### 4. Naming & Structure
- [ ] File-scoped namespaces matching folder structure?
- [ ] Consumers in `Consumers/`, Services in `Services/`?
- [ ] Sequential IDs via `IdGenerator.NextGuid()`, not `Guid.NewGuid()`?

## Output Format
```
VIOLATION: Description
File: Path
Impact: Why it hurts
Fix: Refactor recommendation
```
