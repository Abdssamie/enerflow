# Track 1: P1 Bug Fix - Topology Validation

**Assigned To**: Agent A  
**Priority**: P0 (Critical)  
**Estimated Time**: 1 day  
**Goal**: Fix critical bug where simulations converge with disconnected streams

---

## Objective

Implement minimal validation infrastructure to detect and reject flowsheets with disconnected streams or unit operations, fixing the failing functional test `Should_Fail_On_Disconnected_Stream`.

---

## Tasks

### T001 [P0] Create IFlowsheetValidator Interface
**File**: `Enerflow.Worker/Validation/IFlowsheetValidator.cs`

```csharp
using DWSIM.Interfaces;
using Enerflow.Domain.Entities;

namespace Enerflow.Worker.Validation;

public interface IFlowsheetValidator
{
    ValidationResult Validate(Simulation simulation, IFlowsheet flowsheet);
}
```

**Acceptance**: Interface compiles, follows naming conventions

---

### T002 [P0] Create ValidationResult Value Object
**File**: `Enerflow.Worker/Validation/ValidationResult.cs`

```csharp
namespace Enerflow.Worker.Validation;

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<ValidationError> Errors { get; }
    public List<ValidationWarning> Warnings { get; }
    
    public ValidationResult()
    {
        Errors = new List<ValidationError>();
        Warnings = new List<ValidationWarning>();
    }
    
    public ValidationResult(List<ValidationError> errors, List<ValidationWarning>? warnings = null)
    {
        Errors = errors ?? new List<ValidationError>();
        Warnings = warnings ?? new List<ValidationWarning>();
    }
}
```

**Acceptance**: Value object compiles, immutable after construction

---

### T003 [P0] Create ValidationError Value Object
**File**: `Enerflow.Worker/Validation/ValidationError.cs`

```csharp
namespace Enerflow.Worker.Validation;

public class ValidationError
{
    public string Code { get; init; }
    public string Message { get; init; }
    public string EntityType { get; init; }
    public string EntityName { get; init; }
    public ErrorSeverity Severity { get; init; }
    
    public ValidationError(string code, string message, string entityType, string entityName, ErrorSeverity severity = ErrorSeverity.Error)
    {
        Code = code;
        Message = message;
        EntityType = entityType;
        EntityName = entityName;
        Severity = severity;
    }
}

public class ValidationWarning
{
    public string Code { get; init; }
    public string Message { get; init; }
    
    public ValidationWarning(string code, string message)
    {
        Code = code;
        Message = message;
    }
}

public enum ErrorSeverity
{
    Error,    // Blocks execution
    Warninged but doesn't block
}
```

**Acceptance**: Value objects compile, use init-only properties

---

### T004 [P0] Create FlowsheetValidationException
**File**: `Enerflow.Worker/Validation/FlowsheetValidationException.cs`

```csharp
namespace Enerflow.Worker.Validation;

public class FlowsheetValidationException : Exception
{
    public ValidationResult ValidationResult { get; }
    
    public FlowsheetValidationException(ValidationResult result)
        : base($"Flowsheet validation failed with {result.Errors.Count} error(s): {string.Join("; ", result.Errors.Select(e => e.Message))}")
    {
        ValidationResult = result;
    }
}
```

**Acceptance**: Exception compiles, includes validation result

---

### T005 [P0] Implement FlowsheetValidator with Topology Validation
**File**: `Enerflow.Worker/Validation/FlowsheetValidator.cs`

```csharp
using DWSIM.Interfaces;
using Enerflow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Enerflow.Worker.Validation;

public class FlowsheetValidator : IFlowsheetValidator
{
    private readonly ILogger<FlowsheetValidator> _logger;
    
    public FlowsheetValidator(ILogger<FlowsheetValidator> logger)
    {
        _logger = logger;
    }
    
    public ValidationResult Validate(Simulation simulation, IFlowsheet flowsheet)
    {
        var errors = new List<ValidationError>();
        
        // Run topology validation
        errors.AddRange(ValidateTopology(simulation));
        
        return new ValidationResult(errors);
    }
    
    private List<ValidationError> ValidateTopology(Simulation simulation)
    {
        var errors = new List<ValidationError>();
        
        // Check for disconnected unit operations
        foreach (var unit in simulation.UnitOperations)
        {
            if (unit.InputStreamIds.Count == 0 && unit.OutputStreamIds.Count == 0)
            {
                errors.Add(new ValidationError(
                    "DISCONNECTED_UNIT",
                    $"Unit operation '{unit.Name}' has no connected streams",
          UnitOperation",
                    unit.Name
                ));
            }
        }
        
        // Check for orphaned streams
        var connectedStreamIds = simulation.UnitOperations
            .SelectMany(u => u.InputStreamIds.Concat(u.OutputStreamIds))
            .ToHashSet();
            
        foreach (var stream in simulation.MaterialStreams)
        {
            if (!connectedStreamIds.Contains(stream.Id))
            {
                errors.Add(new ValidationError(
                    "ORPHANED_STREAM",
                    $"Stream '{stream.Name}' is not connected to anyt operation",
                    "MaterialStream",
                    stream.Name
                ));
            }
        }
        
        foreach (var stream in simulation.EnergyStreams)
        {
            if (!connectedStreamIds.Contains(stream.Id))
            {
                errors.Add(new ValidationError(
                    "ORPHANED_STREAM",
                    $"Stream '{stream.Name}' is not connected to any unit operation",
                    "EnergyStream",
                    stream.Name
                ));
            }
        }
        return errors;
    }
}
```

**Acceptance**: 
- Detects disconnected unit operations
- Detects orphaned streams
- Returns detailed error messages with entity names

---

### T006 [P0] Register IFlowsheetValidator in DI Container
**File**: `Enerflow.Worker/Program.cs` (or wherever DI is configured)

Add to service registration:
```csharp
services.AddScoped<IFlowsheetValidator, FlowsheetValidator>();
```

**Acceptance**: Validator can be injected into other services

---

### T007 [P0] Integrate Validator into DWSIMFlowsheetBuilder
**File**: `Enerflow.Worker/Builders/DWSIMFlowsheetBuilder.cs`

1. Add constructor parameter:
```csharp
private readonly IFlowsheetValidator _validator;

public DWSIMFlowsheetBuilder(
    // ... existing parameters
    IFlowsheetValidator validator)
{
    // ... existing assignments
    _validator = validator;
}
```

2. Add validation at end of BuildFlowsheet method (before return):
```csharp
public IFlowsheet BuildFlowsheet(Simulation simulation)
{
    // ... existing build logic ...
    
    // VALIDATE BEFORE RETURNING
    _logger.LogDebug("Validating flowsheet for simulation {Id}", simulation.Id);
    var validationResult = _validator.Validate(simulation, flowsheet);
    
    if (!validationResult.IsValid)
    {
        _logger.LogWarning("Flowsheet validation failed for simulation {Id} with {Count} errors", 
            simulation.Id, validationResult.Errors.Count);
        throw new FlowsheetValidationException(validationResult);
    }
    
    _logger.LogDebug("Flowsheet validation passed for simulation {Id}", simulation.Id);
    return flowsheet;
}
```

**Acceptance**: 
- Validator is called before returning flowsheet
- Throws FlowsheetValidationException on validation failure
- Logs validation results

---

### T008 [P0] Update SimulationJobConsumer Error Handling
**File**: `Enerflow.Worker/Consumers/SimulationJobConsumer.cs`

Add catch block for FlowsheetValidationException:
```csharp
public async Task Consume(ConsumeContext<SimulationJob> context)
{
    var job = context.Message;
    var cancellationToken = context.CancellationToken;
    
    try
    {
        // ... existing logic ...
        var result = _solver.Solve(simulation, config);
        // ... existing logic ...
    }
    catch (FlowsheetValidationException ex)
    {
        _logger.LogWarning(ex, "Flowsheet validation failed for Job {JobId}", job.JobId);
        
        var errorMessage = string.Join("; ", ex.ValidationResult.Errors.Select(e => e.Message));
        await UpdateStatusAsync(job.SimulationId, SimulationStatus.Failed, 
            $"Validation error: {errorMessage}", cancellationToken);
    }
    catch (Exception ex)
    {
        // ... existing error handling ...
    }
}
```

**Acceptance**: 
- Catches FlowsheetValidationException
- Sets simulation status to "Failed"
- Includes validation error messages in status

---

### T009 [P0] Run Functional Test and Verify Fix
**Command**: 
```bash
dotnet test Enerflow.Tests.Functional --filter "Should_Fail_On_Disconnected_Stream"
```

**Expected Result**: 
- Test PASSES ✅
- Staled" (not "Converged")
- Error message includes "Unit operation 'LonelyMixer' has no connected streams"

**Acceptance**: Functional test passes, bug is fixed

---

### T010 [P0] Create Unit Tests for FlowsheetValidator
**File**: `Enerflow.Tests.Unit/Worker/Validation/FlowsheetValidatorTests.cs`

```csharp
using Enerflow.Domain.Entities;
using Enerflow.Worker.Validation;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enerflow.Tests.Unit.Worker.Validation;

public class FlowsheetValidatorTests
{
   ate readonly FlowsheetValidator _validator;
    
    public FlowsheetValidatorTests()
    {
        _validator = new FlowsheetValidator(NullLogger<FlowsheetValidator>.Instance);
    }
    
    [Fact]
    public void Validate_DisconnectedUnitOperation_ReturnsError()
    {
        // Arrange
        var simulation = CreateSimulationWithDisconnectedUnit();
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.e("DISCONNECTED_UNIT");
        result.Errors[0].EntityName.Should().Be("LonelyMixer");
    }
    
    [Fact]
    public void Validate_OrphanedStream_ReturnsError()
    {
        // Arrange
        var simulation = CreateSimulationWithOrphanedStream();
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("ORPHANED_STREAM");
    }
    
    [Fact]
    public void Validate_ProperlyConnectedFsheet_ReturnsValid()
    {
       range
        var simulation = CreateValidSimulation();
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
    
    // Helper methods to create test simulations...
}
```

**Acceptance**: 
- All unit tests pass
- Tests cover disconnected units, orphaned streams, and valid flowsheets

---

## Definition of Done

- [ ] All 10 tasks completed
- [ ] Functional test `Should_Fail_On_Disconnected_Stream` PASSES
- [ ] Unit tests for FlowsheetValidator pass with 100% coveran- [ ] Code compiles without warnings
- [ ] Validation errors include specific entity names
- [ ] Logging is appropriate (Debug for success, Warning for failures)
- [ ] No breaking changes to existing functionality

---

## Handoff to Track 2

**Interface Contract**: 
- `IFlowsheetValidator.Validate()` signature is stable
- `ValidationResult`, `ValidationError` structures are stable
- Track 2 can extend `FlowsheetValidator` with additional validation methods

**Integration Points**:
- DWSIMFlowsheetBuilder calls validator before returning
- SimulationJobConsumer catches FlowsheetValidationException
- DI container registers IFlowsheetValidator

**Next Steps for Track 2**:
- Add `ValidatePhysicalProperties()` method to FlowsheetValidator
- Add `ValidateCompounds()` method
- Add `ValidateUnitOperations()` method
- Create comprehensive unit tests for each validation rule

---

**Status**: Ready for Implementation  
**Estimated Completion**: 1 day
