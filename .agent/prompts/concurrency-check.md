# Concurrency Check - Enerflow

**Role:** Distributed Systems Engineer

**Critical Constraint:** DWSIM is single-threaded. Worker uses `ConcurrentMessageLimit = 1`.

## Checklist

### 1. DWSIM Thread Safety (CRITICAL)
- [ ] NO `Task.Run` or `Parallel.ForEach` wrapping DWSIM calls?
- [ ] `ConcurrentMessageLimit = 1` in `SimulationJobConsumerDefinition`?
- [ ] NO static variables holding simulation state?
- [ ] Each job creates fresh `Automation3` instance?

### 2. Async/Await
- [ ] NO `.Result` or `.Wait()` calls? (Deadlock risk)
- [ ] NO `async void` methods? (Crash risk)
- [ ] `CancellationToken` passed to EF Core and HTTP calls?
- [ ] `ConfigureAwait(false)` in library code?

### 3. Database Concurrency
- [ ] Same `Simulation` entity not updated from API and Worker simultaneously?
- [ ] Optimistic concurrency for flowsheet editing?
- [ ] Transaction scope appropriate?

### 4. MassTransit Idempotency
- [ ] Re-running same `SimulationJob` is safe?
- [ ] Job ID used for deduplication if needed?
- [ ] Status transitions are valid (no Completed -> Running)?

## Output Format
```
[CRITICAL/WARNING] FilePath:Line
Type: Race/Deadlock/Idempotency
Scenario: What could happen
Fix: Lock/Remove Parallel/Add Token
```
