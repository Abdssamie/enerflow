# Concurrency & Race Condition Prompt

**Role:** You are a Distributed Systems Engineer specializing in Thread Safety.

**Context:** Review `Enerflow.Worker` and `Enerflow.API` for race conditions and threading issues.

**Objective:** Ensure the single-threaded DWSIM engine is protected and the distributed system is consistent.

**Checklist:**

1.  **DWSIM Single-Threaded Constraint:**
    *   Is there *any* `Task.Run` or `Parallel.ForEach` wrapping DWSIM calls? (STRICTLY FORBIDDEN unless carefully locked).
    *   Is `ConcurrentMessageLimit = 1` set in the Consumer Definition?
    *   Are static variables used to hold Simulation state? (Risk of cross-job contamination).

2.  **Async/Await Pitfalls:**
    *   Are there any `.Result` or `.Wait()` calls? (Deadlock risk).
    *   Is `async void` used? (Crash risk - except for event handlers).
    *   Are `CancellationToken`s passed down to EF Core and HTTP calls?

3.  **Database Concurrency:**
    *   Are we updating the same `Simulation` entity from multiple places?
    *   Is Optimistic Concurrency (ETags/Versions) needed for editing Flowsheets?

4.  **MassTransit Idempotency:**
    *   What happens if the same `SimulationJob` is delivered twice?
    *   Is the process idempotent? (Re-running a solved simulation is generally fine, but verify side effects).

**Output Format:**
*   **Risk:** [Critical/Warning]
*   **Type:** [Race/Deadlock/Idempotency]
*   **Location:** FilePath:LineNumber
*   **Description:** The threading scenario.
*   **Fix:** Use Lock / Remove Parallel / Add Token.
