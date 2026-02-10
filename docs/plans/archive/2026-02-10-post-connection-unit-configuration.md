# Post-Connection Unit Operation Configuration Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Move splitter ratio configuration from ConnectionFactory to UnitOperationFactory using a two-phase configuration pattern that scales to future unit operations requiring post-connection setup.

**Architecture:** Introduce `ConfigurePostConnection()` method to `IUnitOperationFactory` that handles unit-specific configuration after connections are established. This maintains Single Responsibility Principle (ConnectionFactory only connects, UnitOperationFactory owns all unit operation concerns) and follows Open/Closed Principle (add new unit types without modifying ConnectionFactory).

**Tech Stack:** C# 10, .NET 10, DWSIM, xUnit

---

## Task 1: Add ConfigurePostConnection Method to Interface

**Files:**
- Modify: `Enerflow.Simulation/Flowsheet/UnitOperations/IUnitOperationFactory.cs:34`

**Step 1: Add new method to interface**

Add this method signature after line 33 (after `CreateAndConfigureUnitOperations`):

```csharp
    /// <summary>
    /// Configures unit operations that require post-connection setup.
    /// This includes operations like setting splitter ratios that depend on connection order.
    /// </summary>
    /// <param name="flowsheet">The DWSIM flowsheet containing the unit operations.</param>
    /// <param name="simulation">The domain simulation entity with configuration data.</param>
    void ConfigurePostConnection(IFlowsheet flowsheet, Enerflow.Domain.Entities.Simulation simulation);
```

**Step 2: Verify compilation fails**

Run: `dotnet build Enerflow.Simulation/Enerflow.Simulation.csproj`

Expected: FAIL with "UnitOperationFactory does not implement interface member 'IUnitOperationFactory.ConfigurePostConnection'"

**Step 3: Commit interface change**

```bash
git add Enerflow.Simulation/Flowsheet/UnitOperations/IUnitOperationFactory.cs
git commit -m "feat: add ConfigurePostConnection to IUnitOperationFactory interface"
```

---

## Task 2: Implement ConfigurePostConnection in UnitOperationFactory

**Files:**
- Modify: `Enerflow.Simulation/Flowsheet/UnitOperations/UnitOperationFactory.cs:34` (after CreateAndConfigureUnitOperations method)

**Step 1: Add using statement**

Add at top of file after existing usings:

```csharp
using SimulationEntity = Enerflow.Domain.Entities.Simulation;
```

**Step 2: Implement ConfigurePostConnection method**

Add this method after the `CreateAndConfigureUnitOperations` method (around line 60):

```csharp
    public void ConfigurePostConnection(IFlowsheet flowsheet, SimulationEntity simulation)
    {
        _logger.LogDebug("Configuring unit operations after connections...");

        foreach (var unit in simulation.UnitOperations)
        {
            switch (unit)
            {
                case SplitterObject splitter:
                    ConfigureSplitterRatios(splitter, flowsheet);
                    break;
                // Future: Add other unit types that need post-connection configuration
                // case DistillationColumnObject column:
                //     ConfigureDistillationColumn(column, flowsheet);
                //     break;
            }
        }
    }
```

**Step 3: Add ConfigureSplitterRatios private method**

Add this private method after `ConfigurePostConnection`:

```csharp
    private void ConfigureSplitterRatios(SplitterObject domainSplitter, IFlowsheet flowsheet)
    {
        _logger.LogDebug("Configuring splitter ratios for {Name}...", domainSplitter.Name);

        // Get the DWSIM splitter object
        var splitterId = domainSplitter.Id.ToString();
        if (!flowsheet.SimulationObjects.TryGetValue(splitterId, out var obj))
        {
            _logger.LogWarning("Splitter {Name} (ID: {Id}) not found in flowsheet during post-connection config.",
                domainSplitter.Name, domainSplitter.Id);
            return;
        }

        var dwsimSplitter = (Splitter)obj;

        // Set ratios based on OutputStreamIds order
        // ConnectionFactory connects streams in the order they appear in OutputStreamIds
        // So OutputStreamIds[i] is connected to port i
        for (int i = 0; i < domainSplitter.OutputStreamIds.Count; i++)
        {
            var streamId = domainSplitter.OutputStreamIds[i];
            
            if (domainSplitter.SplitRatios.TryGetValue(streamId, out var ratio))
            {
                // Ensure Ratios list has enough capacity
                while (dwsimSplitter.Ratios.Count <= i)
                {
                    dwsimSplitter.Ratios.Add(0.0);
                }
                
                dwsimSplitter.Ratios[i] = ratio;
                
                _logger.LogDebug("Set Splitter {Name} Port {Port} (Stream {StreamId}) Ratio to {Ratio}",
                    domainSplitter.Name, i, streamId, ratio);
            }
            else
            {
                _logger.LogWarning("No split ratio defined for stream {StreamId} in splitter {Name}",
                    streamId, domainSplitter.Name);
            }
        }
    }
```

**Step 4: Verify compilation succeeds**

Run: `dotnet build Enerflow.Simulation/Enerflow.Simulation.csproj`

Expected: SUCCESS

**Step 5: Commit implementation**

```bash
git add Enerflow.Simulation/Flowsheet/UnitOperations/UnitOperationFactory.cs
git commit -m "feat: implement ConfigurePostConnection with splitter ratio configuration"
```

---

## Task 3: Update DWSIMFlowsheetBuilder to Call ConfigurePostConnection

**Files:**
- Modify: `Enerflow.Simulation/Flowsheet/Builders/DWSIMFlowsheetBuilder.cs:89`

**Step 1: Add post-connection configuration call**

After line 89 (`_coactory.ConnectFlowsheet(simulation, flowsheet);`), add:

```csharp
       
       // 10. Post-Connection Configuration (e.g., splitter ratios)
       _logger.LogInformation("Configuring post-connection settings for simulation {Id}", simulation.Id);
       _unitOperationFactory.ConfigurePostConnection(flowsheet, simulation);
       _logger.LogInformation("Post-connection configuration completed for simulation {Id}", simulation.Id);
```

**Step 2: Update comment numbering**

Change line 92 comment from `// 10. Validate` to `// 11. Validate`

**Step 3: Verify compilation succeeds**

Run: `dotnet build Enerflow.Simulation/Enerflow.Simulation.csproj`

Expected: SUCCESS

**Step 4: Commit builder update**

```bash
git add Enerflow.Simulation/Flowsheet/Builders/DWSIMFlowsheetBuilder.cs
git commit -m "feat: add post-connection configuration step to builder"
```

---

## Task 4: Remove Splitter Configuration from ConnectionFactory

**Files:**
- Modify: `Enerflow.Simulation/Flowsheet/Connections/ConnectionFactory.cs`

**Step 1: Remove ConfigureSplitterRatios call**

Delete lines 93-96:

```csharp
       // 3. ConSplitter Ratios (Post-Connection)
       // Splitter ratios must be set AFTER connections are made because they depend on
       // the connection order (which port connects to which stream)
       ConfigureSplitterRatios(domainSimulation, flowsheet);
```

**Step 2: Remove ConfigureSplitterRatios method**

Delete lines 99-112 (entire `ConfigureSplitterRatios` method)

**Step 3: Remove ConfigureSingleSplitterRatios method**

Delete lines 114-165 (entire `ConfigureSingleSplitterRatios` method)

**Step 4: Remove unused using statement**

Remove this line from the top of the file:

```csharp
using Splitter = DWSIM.UnitOperations.UnitOperations.Splitter;
```

**Step 5: Verify compilation succeeds**

Run: `dotnet build Enerflow.Simulation/Enerflow.Simulation.csproj`

Expected: SUCCESS

**Step 6: Commit cleanup**

```bash
git add Enerflow.Simulation/Flowsheet/Connections/ConnectionFactory.cs
git commit -m "refactor: remove splitter configuration from ConnectionFactory"
```

---

## Task 5: Run Integration Tests

**Files:**
- Test: `Enerflow.Tests.Integration/FlowsheetBuilderIntegrationTests.cs`

**Step 1: Run integration tests**

Run: `dotnet test Enerflow.Tests.Integration/Enerflow.Tests.Integration.csproj --verbosity normal`

Expected: All 2 tests PASS
- `Build_Simple_Simulation`
- `Build_Recycle_Simulation`

**Step 2: If tests fail, check logs**

Look for errors related to:
- Splitter ratio configuration
- Connection issues
- Missing method calls

**Step 3: Run all DWSIM scenario tests**

Run: `dotnet test Enerflow.Tests.DWSIM/Enerflow.Tests.DWSIM.csproj --verbosity normal`

Expected: All 11 tests PASS, especially:
- `Test10_RecycleLoop.RecycleLoop_SimpleProcess_ConvergesWithMassBalance` (uses splitter)

**Step 4: Run full test suite**

Run: `dotnet test --verbosity normal`

Expected: All 68 tests PASS

**Step 5: Commit test verifican**

```bash
git add -A
git commit -m "test: verify all tests pass with post-connection configuration"
```

---

## Task 6: Update Integration Test to Use Manual DI (if needed)

**Files:**
- Check: `Enerflow.Tests.Integration/FlowsheetBuilderIntegrationTests.cs:45`

**Step 1: Verify test setup includes all dependencies**

The test manually constructs `DWSIMFlowsheetBuilder` with all dependencies. Verify it compiles and runs correctly with the new interface method.

**Step 2: No changes needed**

The test uses the interface, so it automatically gets the new method. No changes required.

**Step 3: Verify test still passes**

Run: `dotnet test Enerflow.Tests.Integration/Enerflow.Tests.Integration.csproj --verbosity normal`

Expected: All 2 tests PASS

---

## Task 7: Verify DI Registration (No Changes Needed)

**Files:**
- Check: `Enerflow.Worker/Program.cs:41`
- Check: `Enerflow.Tests.Functional/IntegrationTestWebAppFactory.cs`

**Step 1: Verify Worker DI registration**

Check that `IUnitOperationFactory` is registered (line 41):

```csharp
builder.Services.AddSingleton<IUnitOperationFactory, UnitOperationFactory>();
```

No changes needed - interface implementation is automatically used.

**Step 2: Verify Functional Test DI registration**

Check `IntegrationTestWebAppFactory.cs` has correct registrations.

No changes needed - uses same DI setup as Worker.

**Step 3: Run functional tests**

Run: `dotnet test Enerflow.Tests.Functional/Enerflow.Tests.Functional.csproj --verbosity normal`

Expected: All 2 tests PASS

---

## Task 8: Update Architecture Documentation

**Files:**
- Modify: `docs/ARCHITECTURE/SOLVER_PIPELINE.md`

**Step 1:date Builder section**

Find the section describing `DWSIMFlowsheetBuilder` and update the flowsheet building steps to include:

```markdown
9. **Connect Flowsheet** - `ConnectionFactory.ConnectFlowsheet()` wires streams to unit operations
10. **Post-Connection Configuration** - `UnitOperationFactory.ConfigurePostConnection()` handles unit-specific setup that depends on connection order (e.g., splitter ratios)
11. **Validate** - `FlowsheetValidator.Validate()` ensures flowsheet is ready to solve
```

**Step 2: Add note about post-connection pattern**

Add a subsection:

```markdown
### Post-Connection Configuration Pattern

Some unit operations require configuration that depends on connection order:

- **Splitter ratios**: DWSIM uses port-indexed arrays, but users specify ratios by stream ID
- **Future examples**: Distillation column feed tray location, reactor inlet specifications

The `ConfigurePostConnection()` method in `UnitOperationFactory` handles these cases after `ConnectionFactory` establishes all connections.

**Design principle**: Each factory owns its domain. `ConnectionFactory` only connects; `UnitOperationFactory` owns all unit operation configuration.
```

**Step 3: Commit documentation**
ngit add docs/ARCHITECTURE/SOLVER_PIPELINE.md
git commit -m "docs: update architecture docs with post-connection configuration pattern"
```

---

## Task 9: Add Code Comments for Future Developers

**Files:**
- Modify: `Enerflow.Simulation/Flowsheet/UnitOperations/UnitOperationFactory.cs`

**Step 1: Add comment above ConfigurePostConnection**

Add this comment block above the `ConfigurePostConnection` method:

```csharp
    /// <summary>
    /// Configures unit operations after connections are established.
    /// 
    /// WHY THIS EXISTS:
    /// Some unit operations have configuration that depends on connection order.
    /// For example, DWSIM splitters use port-indexed ratio arrays [0.8, 0.2],
    /// but users specify ratios by stream ID {streamA: 0.8, streamB: 0.2}.
    /// We can only map stream IDs to port indices after connections are made.
    /// 
    /// WHEN TO ADD NEW CASES:
    /// - Distillation columns (feed tray location depends on which port the feed connects to)
    /// - Reactors (inlet specifications depend on connection order)
    /// - Any unit where DWSIM uses port indices but users specify by stream/component ID
    /  /// ARCHITECTURE:
    /// This maintains Single Responsibility - ConnectionFactory only connects,
    /// UnitOperationFactory owns ALL unit operation configuration concerns.
    /// </summary>
```

**Step 2: Commit comments**

```bash
git add Enerflow.Simulation/Flowsheet/UnitOperations/UnitOperationFactory.cs
git commit -m "docs: add explanatory comments for post-connection configuration pattern"
```

---

## Task 10: Final Verification and Summary

**Step 1: Run complete test suite**

Run: `dotnet test --verbosity normal`

Expected: All 68 tests PASS
- 53 unit tests
- 2 integration tests
- 11 DWSIM scenario tests
- 2 functional tests

**Step 2: Verify code metrics**

Run: `find Enerflow.Simulation/Flowsheet/Connections -name "*.cs" -exec wc -l {} +`

Expected: `ConnectionFactory.cs` should be ~60-70 lines (down from 207 lines)

Run: `find Enerflow.Simulation/Flowsheet/UnitOperations -name "*.cs" -exec wc -l {} +`

Expected: `UnitOperationFactory.cs` should be ~250-270 lines (up from ~200 lines)

**Step 3: Check git log**

Run: `git log --oneline -10`

Expected: See all commits from this refactoring

**Step 4: Create summary commit**

```bash
git add -A
git commit -m "refactor: move splitter configuration to UnitOperationFactory post-connection phase

- Add ConfigurePostConnection() to IUnitOperationFactory interface
- Implement two-phase configuration: pre-connection and post-connection
- Move splitter ratio logic from ConnectionFactory to UnitOperationFactory
- Remove ~140 lines from ConnectionFactory (complex reverse lookup logic)
- Add ~50 lines to UnitOperationFactory (simple forward mapping)
- Update DWSIMFlowsheetBuilder to call post-connection configuration
- Update architecture documentation
- All 68 tests passing

Benefits:
- Single Responsibility: ConnectionFactory only connects
- Open/Closed: Add new unit types without modifying ConnectionFactory
- Scalable: Pattern supports future units (distillation, reactors, etc.)
- Simpler: Direct mapping instead of reverse lookup

Net change: -90 lines of code"
```

**Step 5: Verify final state**

Run: `git status`

Expected: Clean working directory

Run: `dotnet build`

Expected: SUCCESS with no warnings

---

## Success Criteria

✅ All 68 tests pass  
✅ `ConnectionFactory` reduced from 207 to ~60 lines  
✅ `UnitOperationFactory` has new `ConfigurePostConnection()` method  
✅ Splitter ratios configured correctly in all tests  
✅ Architecture documentation updated  
✅ Code comments explain pattern for future developers  
✅ Net reduction of ~90 lines of code  
✅ Single Responsibility Principle maintained  
✅ Open/Closed Principle maintained  

## Rollback Plan

If tests fail:

1. Check which test is failing
2. Add debug logging to `ConfigureSplitterRatios()` to see actual vs expected ratios
3. Verify `OutputStreamIds` order matches connection order in `ConnectionFactory`
4. If fundamental issue found, revert commits: `git revert HEAD~10..HEAD`

## Future Extensions

This pattern supports:

- **Distillation columns**: Feed tray location based on connection port
- **Reactors**: Inlet specifications based on connection order
- **Component separators**: Split fractions per component per port
- **Heat exchangers**: Shell/tube side assignment based on connections

Add new cases to the `switch` statement in `ConfigurePostConnection()`.
