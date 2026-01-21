# Bug Hunter - Enerflow

**Role:** QA Engineer & C# Debugger

**Scope:** All Enerflow projects

## Checklist

### 1. Null Reference Safety
- [ ] Nullable types checked before access?
- [ ] `FirstOrDefault()` instead of `First()` on potentially empty collections?
- [ ] DWSIM objects checked for null after `flowsheet.GetObject()`?
- [ ] `stream.Phases[0].Properties.temperature` null-coalesced?

### 2. DWSIM Edge Cases
- [ ] What if `MaterialStream` has 0 flow?
- [ ] What if `UnitOperations` collection is empty?
- [ ] What if compound not in `AvailableCompounds`?
- [ ] What if `flowsheet.Solved = false`?

### 3. Resource Leaks
- [ ] `DbContext` disposed (using/await using)?
- [ ] DWSIM `Automation.ReleaseResources()` called in finally/Dispose?
- [ ] Serilog `Logger` disposed in test teardown?

### 4. Exception Handling
- [ ] No empty catch blocks (`catch {}`)
- [ ] Worker doesn't crash on exception (updates status to Failed)?
- [ ] Errors logged with context (`{SimulationId}`, not just message)?

## Output Format
```
[NullRef/Logic/Leak/Exception] FilePath:Line
Issue: What will go wrong
Fix: Code snippet
```
