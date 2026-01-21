# APM 0.6.0-token-efficient - Memory System Guide 
This guide explains how APM sessions store and evolve memory using the **Dynamic-MD** system.

Memory duties are assigned to the *Manager Agent* - who maintains the system. Details on individual Memory Log files reside in .apm/guides/Memory_Log_Guide.md.

## 1  Memory System Overview
The Dynamic-MD Memory System organizes memory with the following structure:

- **Storage layout:** Folder `.apm/Memory/` + `Memory_Root.md` + sub-folders `Phase_XX_<slug>/` in the `.apm/` directory
- **Log format:** One `Task_XX_<slug>.md` Memory Log per task
- **Summarization:** Use **Progressive Summarization**. The `Memory_Root.md` maintains a rolling 500-token summary of the project state + open issues + blockers. Detailed phase summaries are condensed after 2 phases to prevent context bloat.

## 🛠 Architectural Decision Log (ADL) Protocol
To prevent "Context Rot" regarding critical project logic:
1. **Definition**: An Architectural Decision is any choice that affects cross-agent compatibility, security, or core performance (e.g., "Why we used Redis for race conditions").
2. **Persistence**: The ADL section in `Memory_Root.md` is **IMMUTABLE** during summarization. It must be carried forward in full.
3. **Trigger**: If an Implementation Agent marks `important_findings: true` in their log, the Manager MUST evaluate if it requires an ADL entry.

**Memory Logs** capture granular, task-level context and are written by Implementation Agents after each task completion. See .apm/guides/Memory_Log_Guide.md for schemas and writing rules.

## 2  Manager Agent Responsibilities
Main responsibilities of the Manager Agent when maintaining the Memory System during an APM session:

1. **Memory Root Header Initialization (First Session Only)**: Before starting the first phase execution, fill in the header of `.apm/Memory/Memory_Root.md`. The file is pre-created by the `agentic-pm` CLI tool using `apm init`, with a header template containing placeholders. Replace all placeholders with actual values before proceeding to phase execution.

2. Keep the Memory System structure (folders/logs) in sync with the current Implementation Plan. Update as Phases or Tasks change.

3. After each phase, update the **Rolling Summary** in `Memory_Root.md`.

4. **Handover Compression**: When initiating a handover, condense the last 3 task logs into a single 300-token "Context Delta" to save tokens in the next session.

### Phase and Task Management
**Note**: The Memory Root header must be filled before the first phase execution begins (see responsibility #1 above).

1. On phase entry, create `.apm/Memory/Phase_XX_<slug>/` if missing. For each task in the phase, create a **completely empty** Memory Log, following .apm/guides/Memory_Log_Guide.md:
    - `Task_Y_Z_<slug>.md`

**All Memory Logs for the current phase must be created BEFORE the first Task Assignment Prompt for each task.**
**Use task ID and title from Implementation Plan (exclude agent assignment).**
**Example: Task "Task 2.1 - Deploy Updates | Agent_Backend" → `Task_2_1_Deploy_Updates.md`**

2. After each task execution, review the Memory Log **populated by the Implementation Agent**, provided via the User.
   - If the log contains `important_findings: true` or `compatibility_issues: true`, you **MUST** inspect the referenced output files/artifacts to validate the findings before making a decision.
   

3. At phase end, append a summary to `.apm/Memory/Memory_Root.md`:
    ```markdown
    ## Phase XX – <Phase Name> Summary 
    * Outcome summary (≤ 200 words)
    * List of involved Agents
    * Links to all phase task logs
    ```
    Keep summaries ≤30 lines.

---

**End of Guide**