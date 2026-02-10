# Fix Topology & Energy Stream Wiring

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement missing Energy Stream connection logic in `ConnectionMapper` and ensure reliable topology building.

**Context:**
The `ConnectionMapper` handles Material Streams but has commented-out/missing logic for Energy Streams. This prevents Heaters, Coolers, and Reactors from working correctly.

**Tech Stack:** C#, DWSIM API

---

## Task 1: Implement Energy Stream Connections

**Files:**
- Modify: `Enerflow.Worker/Mappers/ConnectionMapper.cs`

**Step 1: Implement `MapEnergyStreamConnections` (or equivalent logic)**
Uncomment or implement the logic to connect Energy Streams.
- Iterate through `simulation.UnitOperations`
- Check for `EnergyStreamInputIds` (if defined in DTO) or use the specific logic for Heaters/Coolers.
- Use `flowsheet.ConnectObjects` with the correct port indices (usually energy ports are distinct).
- **Reference:** See `docs/DWSIM/CONNECTION_PORTS.md` for energy port indices (often `0` for energy port, but need to check if it's a specific "Energy" port type in `ConnectObjects`).

**Step 2: Verify with Unit Test**
Create a test case with a Heater connected to an Energy Stream.
- Create `Enerflow.Tests.Unit/Mappers/ConnectionMapperTests.cs`
- Test: `MapConnections_HeaterWithEnergyStream_ConnectsSuccessfully`

**Step 3: Commit**
```bash
git add Enerflow.Worker/Mappers/ConnectionMapper.cs Enerflow.Tests.Unit/Mappers/ConnectionMapperTests.cs
git commit -m "feat: implement energy stream connections in ConnectionMapper"
```
