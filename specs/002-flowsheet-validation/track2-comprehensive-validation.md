# Track 2: Comprehensive Validation - Physical Properties, Compounds, Unit Operations

**Assigned To**: Agent B  
**Priority**: P1-P2  
**Estimated Time**: 2 days  
**Goal**: Implement production-ready validation with comprehensive rules and 100% test coverage

---

## Objective

Extend the FlowsheetValidator created in Track 1 with comprehensive validation rules for physical properties, compound consistency, and unit operation configurations.

---

## Prerequisites

**Depends on Track 1**:
- `IFlowsheetValidator` interface exists
- `ValidationResult`, `ValidationError` value objects exist
- `FlowsheetValidator` class exists with `ValidateTopology()` method
- Integration with DWSIMFlowsheetBuilder complete

---

## Tasks

### T011 [P1] Implement Physical Property Validation - Temperature
**File**: `Enerflow.Worker/Validation/FlowsheetValidator.cs`

Add method to FlowsheetValidator:
```csharp
private List<ValidationError> ValidatePhysicalProperties(Simulation simulation)
{
    var errors = new List<ValidationError>();
    
    // Validate material stream temperatures
    foreach (var stream in simulation.MaterialStreams)
    {
        if (stream.Temperature <= 0)
        {
            errors.Add(new ValidationError(
                "INVALID_TEMPERATURE",
                $"Stream '{stream.Name}' has invalid temperature {stream.Temperature} K (must be > 0 K)",
                "MaterialStream",
                stream.Name
            ));
        }
    }
    
    return errors;
}
```

Update `Validate()` method to call this:
```csharp
public ValidationResult Validate(Simulation simulation, IFlowsheet flowsheet)
{
    var errors = new List<ValidationError>();
    
    errors.AddRange(ValidateTopology(simulation));
    errors.AddRange(ValidatePhysicalProperties(simulation)); // NEW
    
    return new ValidationResult(errors);
}
```

**Acceptance**: Temperature validation detects values <= 0 K

---

### T012 [P1] Extend Physical Property Validation - Pressure
**File**: `Enerflow.Worker/Validation/FlowsheetValidator.cs`

Add to `ValidatePhysicalProperties()`:
```csharp
// Validate material stream pressures
foreach (var stream in simulation.MaterialStreams)
{
    if (stream.Pressure <= 0)
    {
        errors.Add(new ValidationError(
            "INVALID_PRESSURE",
            $"Stream '{stream.Name}' has invalid pressure {stream.Pressure} Pa (must be > 0 Pa)",
            "MaterialStream",
            stream.Name
        ));
    }
}
```

**Acceptance**: Pressure validation detects values <= 0 Pa

---

### T013 [P1] Extend Physical Property Validation - Mass Flow
**File**: `Enerflow.Worker/Validation/FlowsheetValidator.cs`

Add to `ValidatePhysicalProperties()`:
```csharp
// Validate material stream mass flows
foreach (var stream in simulation.MaterialStreams)
{
    if (stream.MassFlow < 0)
    {
        errors.Add(new ValidationError(
            "INVALID_MASS_FLOW",
            $"Stream '{stream.Name}' has negative mass flow {stream.MassFlow} kg/s",
            "MaterialStream",
            stream.Name
        ));
    }
}
```

**Acceptance**: Mass flow validation detects negative values

---

### T014 [P1] Extend Physical Property Validation - Composition Sum
**File**: `Enerflow.Worker/Validation/FlowsheetValidator.cs`

Add to `ValidatePhysicalProperties()`:
```csharp
// Validate material stream compositions
foreach (var stream in simulation.MaterialStreams)
{
    if (stream.Composition != null && stream.Composition.Any())
    {
        double compositionSum = stream.Composition.Values.Sum();
        double tolerance = 0.01; // 1% tolerance
        
        if (Math.Abs(compositionSum - 1.0) > tolerance)
        {
            errors.Add(new ValidationError(
                "INVALID_COMPOSITION",
                $"Stream '{stream.Name}' composition sums to {compositionSum:F4} (must be 1.0 ± {tolerance})",
                "MaterialStream",
                stream.Name
            ));
        }
    }
}
```

**Acceptance**: Composition validation detects sums not equal to 1.0 ± 0.01

---

### T015 [P1] Implement alidation
**File**: `Enerflow.Worker/Validation/FlowsheetValidator.cs`

Add method:
```csharp
private List<ValidationError> ValidateCompounds(Simulation simulation)
{
    var errors = new List<ValidationError>();
    
    // Get defined compound names
    var definedCompounds = simulation.Compounds.Select(c => c.Name).ToHashSet();
    
    if (!definedCompounds.Any())
    {
        errors.Add(new ValidationError(
            "NO_COMPOUNDS",
            "Simulation has no compounds defined",
            "Simulation",
            simulation.Name
        ));
        return errors;
    }
    
    // Validate material streams reference defined compounds
    foreach (var stream in simulation.MaterialStreams)
    {
        if (stream.Composition != null)
        {
            foreach (var compoundName in stream.Composition.Keys)
            {
                if (!definedCompounds.Contains(compoundName))
                {
                    errors.Add(new ValidationError(
                        "UNDEFINED_COMPOUND",
                        $"Stream '{stream.Name}' references undefined compound '{compoundName}'",
                        "MaterialStream",
                        stream.Name
                    ));
                }
            }
        }
    }
    
    return errors;
}
```

Update `Validate()`:
```csharp
errors.AddRange(ValidateCompounds(simulation)); // NEW
```

**Acceptance**: 
- Detects simulations with no compounds
- Detects streams referencing undefined compounds

---

### T016 [P2] Implement Unit Operation Validation - Heater
**File**: `Enerflow.Worker/Validation/FlowsheetValidator.cs`

Add method:
```csharp
private List<ValidationError> ValidateUnitOperations(Simulation simulation)
{
    var errors = new List<ValidationError>();
    
    foreach (var unit in simulation.UnitOperations)
    {
        switch (unit)
      {
            case HeaterObject heater:
                errors.AddRange(ValidateHeater(heater));
                break;
            // Add other unit types as needed
        }
    }
    
    return errors;
}

private List<ValidationError> ValidateHeater(HeaterObject heater)
{
    var errors = new List<ValidationError>();
    
    // Heater must have either temperature or duty specified
    bool hasTemperature = heater.ConfigParams?.ContainsKey("Temperature") == true;
    bool hasDuty = heater.ConfigParams?.ContainsKey("Duty") == true;
    
    if (!hasTemperature && !hasDuty)
    {
        errors.Add(new ValidationError(
            "HEATER_NO_SPEC",
            $"Heater '{heater.Name}' requires either temperature or duty specification",
            "HeaterObject",
            heater.Name
        ));
    }
    
    return errors;
}
```

Update `Validate()`:
```csharp
errors.AddRange(ValidateUnitOperations(simulation)); // NEW
```

**Acceptance**: Heater validation detects missing temperature/duty

---

### T017 [P2] Extend Unit Operation Validation - Splitter
**File**: `Enerflow.Worker/Validation/FlowsheetValidator.cs`

Add to `ValidateUnitOperations()` switch:
```csharp
case Splitterlitter:
    errors.AddRange(ValidateSplitter(splitter));
    break;
```

Add method:
```csharp
private List<ValidationError> ValidateSplitter(SplitterObject splitter)
{
    var errors = new List<ValidationError>();
    
    // Splitter split ratios must sum to 1.0
    if (splitter.ConfigParams?.ContainsKey("SplitRatios") == true)
    {
        var ratios = splitter.ConfigParams["SplitRatios"] as double[];
        if (ratios != null)
        {
            double sum = ratios.Sum();
            double tolerance = 0.01;
               if (Math.Abs(sum - 1.0) > tolerance)
            {
              s.Add(new ValidationError(
                    "SPLITTER_INVALID_RATIOS",
                    $"Splitter '{splitter.Name}' split ratios sum to {sum:F4} (must be 1.0 ± {tolerance})",
                    "SplitterObject",
                    splitter.Name
                ));
            }
        }
    }
    
    return errors;
}
```

**Acceptance**: Splitter validation detects invalid split ratios

---

### T018 [P1] Create Comprehensive Unit Tests - Physical Properties
**File**: `Enerflow.Tests.Unit/Worker/Validation/PhysicalPropertyValidationTests.cs`

```csharp
using Enerflotities;
using Enerflow.Domain.Entities.Streams;
using Enerflow.Worker.Validation;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Enerflow.Tests.Unit.Worker.Validation;

public class PhysicalPropertyValidationTests
{
    private readonly FlowsheetValidator _validator;
    
    public PhysicalPropertyValidationTests()
    {
        _validator = new FlowsheetValidator(NullLogger<FlowsheetValidator>.Instance);
    }
  [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    public void Validate_InvalidTemperature_ReturnsError(double temperature)
     // Arrange
        var simulation = CreateSimulationWithStream(temperature: temperature);
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "INVALID_TEMPERATURE");
    }
    
    [Theory]
    [InlineData(-1000)]
    [InlineData(0)]
    public void Validate_InvalidPressure_ReturnsError(double pressure)
    {
        // Arrange
        var simulation = CreateSimulationWithStream(pressure: pressure);
        
        // Act
        var result = _validator.Validimulation, null);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "INVALID_PRESSURE");
    }
    
    [Fact]
    public void Validate_NegativeMassFlow_ReturnsError()
    {
        // Arrange
        var simulation = CreateSimulationWithStream(massFlow: -5.0);
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "INVALID_MASS_FLOW");
    }
    
    [Fact]
    public voidmpositionNotSummingToOne_ReturnsError()
    {
        // Arrange
        var composition = new Dictionary<string, double>
        {
            { "Water", 0.5 },
            { "Ethanol", 0.3 } // Sum = 0.8, not 1.0
        };
        var simulation = CreateSimulationWithStream(composition: composition);
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "INVALID_COMPOSITION");
    }
    
    [Fact]
    public void Validate_ValidPhysicalProperties_ReturnsValid()
    {
        // Arrange
        var composition = new Dictionary<string, double> { { "Water", 1.0 } };
        var simulation = CreateSimulationWithStream(
            temperature: 300,
            pressure: 101325,
            massFlow: 1.0,
            composition: composition
        );
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }
    
    private Simulation CreateSimulationWithStream(
        double temperature = 300,
        double pressure = 101325,
        double massFlow = 1.0,
        Dictionary<string, double>? composition = null)
    {
        var simulation = new Simulation
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation"
        };
        
        simulation.Compounds.Add(new Compound
        {
            Id = Guid.NewGuid(),
            Name = "Water",
            SimulationId = simulation.Id
        });
        
        if (composition != null)
        {
            foreach (var compound in composition.Keys)
            {
                if (!simulation.Compounds.Any(c => c.Name == compound))
                {
                    simulation.Compounds.Add(new Compound
                    {
                        Id = Guid.NewGuid(),
                        Name = compound,
                        SimulationId = simulation.Id
                    });
                }
            }
        }
        
        var stream = new MaterialStrea  {
            Id = Guid.NewGuid(),
            Name = "TestStream",
            SimulationId = simulation.Id,
            Temperature = temperature,
            Pressure = pressure,
            MassFlow = massFlow,
            Composition = composition ?? new Dictionary<string, double> { { "Water", 1.0 } }
        };
        
        simulation.MaterialStreams.Add(stream);
        
        // Add a connected unit to pass topology validation
        var unit = new MixerObject
        {
            Id = Guid.NewGuid(),
            Name = "TestMixer",
            SimulationId = simulation.Id,
            InputStreamIds = new List<Guid> { stream.Id },
            OutputStreamIds = new List<Guid>()
        };
        simulation.UnitOperations.Add(unit);
        
        return simulation;
    }
}
```

**Acceptance**: All physical property validation tests pass

---

### T019 [P1] Create Unit Tests - Compound Validation
**File**: `Enerflow.Tests.Unit/Worker/Validation/CompoundValidationTests.cs`

```csharp
public class CompoundValidationTests
{
    private readonly FlowsheetValidator _validator;
    
    public CompoundValidationTests()
    {
        _validator = new FlowsheetValidator(NullLogger<FValidator>.Instance);
    }
    
    [Fact]
    public void Validate_NoCompounds_ReturnsError()
    {
        // Arrange
        var simulation = new Simulation
        {
            Id = Guid.NewGuid(),
            Name = "Test"
        };
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "NO_COMPOUNDS");
    }
    
    [Fact]
    public void Validate_UndefinedCompound_ReturnsError()
    {
        // Arrange
        var simulation = CtionWithUndefinedCompound();
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "UNDEFINED_COMPOUND");
    }
    
    [Fact]
    public void Validate_AllCompoundsDefined_ReturnsValid()
    {
        // Arrange
        var simulation = CreateValidSimulationWithCompounds();
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeTrue();
    }
    
    // Helper methods...
}
```

**Acceptance**: All compound validation tests pass

---

### T020 [P2] Create Unit Tests - Unit Operation Validation
**File**: `Enerflow.Tests.Unit/Worker/Validation/UnitOperationValidationTests.cs`

```csharp
public class UnitOperationValidationTests
{
    private readonly FlowsheetValidator _validator;
    
    public UnitOperationValidationTests()
    {
        _validator = new FlowsheetValidator(NullLogger<FlowsheetValidator>.Instance);
    }
    
    [Fact]
    public void Validate_HeaterWithoutSpec_ReturnsError()
    {
        // Arrange
        var simulation = CreateSimulationWithHeater(hasTemperature: false, hasDuty: false);
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "HEATER_NO_SPEC");
    }
    
    [Fact]
    public void Validate_SplitterInvalidRatios_ReturnsError()
    {
        // Arrange
        var simulation = CreateSimulationWithSplitter(splitRatios: new[] { 0.3, 0.3 }); // Sum = 0.6
        
        // Act
        var result = _validator.Validate(simulation, null);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "SPLITTER_INVALID_RATIOS");
    }
    
    // More tests...
}
```

**Acceptance**: All unit operation validation tests pass

---

### T021 [P1] Create Integration Tests
**File**: `Enerflow.Tests.Integration/Worker/Validation/FlowsheetValidationIntegrationTests.cs`

```csharp
using Enerflow.Worker.Builders;
using Enerflow.Worker.Validation;
using FluentAssertions;
using Xunit;

namespace Enerflow.Tests.Integration.Worker.Validation;

public class FlowsheetValidationIntegrationTests : IClassFixture<WorkerTestFixture>
{
    private readonly WorkerTestFixture _fixture;
    
    public FlowsheetValidationIntegrationTests(WorkerTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public void BuildFlowsheet_WithDisconnectedUnit_ThrowsValidationException()
    {
        // Arrange
        var simulation = CreateSimulationWithDisconnectedUnit();
        var builder = _fixture.GetService<IFlowsheetBuilder>();
        
        // Act & Assert
        var exception = Assert.Throws<FlowsheetValidationException>(() => 
            builder.Buillowsheet(simulation));
        
        exception.ValidationResult.Errors.Should().Contain(e => e.Code == "DISCONNECTED_UNIT");
    }
    
    [Fact]
    public void BuildFlowsheet_WithInvalidTemperature_ThrowsValidationException()
    {
        // Arrange
        var simulation = CreateSimulationWithInvalidTemperature();
        var builder = _fixture.GetService<IFlowsheetBuilder>();
        
        // Act & Assert
        var exception = Assert.Throws<FlowsheetValidationException>(() => 
            builder.BuildFlowsheet(simulation));
        
        exception.ValidationResult.Errors.Should().Contaiode == "INVALID_TEMPERATURE");
    }
    
    [Fact]
    public void BuildFlowsheet_WithValidFlowsheet_Succeeds()
    {
        // Arrange
        var simulation = CreateValidSimulation();
        var builder = _fixture.GetService<IFlowsheetBuilder>();
        
        // Act
        var flowsheet = builder.BuildFlowsheet(simulation);
        
        // Assert
        flowsheet.Should().NotBeNull();
    }
}
```

**Acceptance**: Integration tests verify end-to-end validation flow

---

### T022 [P1] Verify Test Coverage
**Command**:
```bash
dotnet test Enerflow.Tests.Unit --filter "FullyQualifiedName~Validation" /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

**Expected**: FlowsheetValidator has 100% line coverage

**Acceptance**: Coverage report shows 100% for FlowsheetValidator

---

### T023 [P2] Update Documentation
**File**: `ENERFLOW_SIMULATION_GUIDE.md`

Add section:
```markdown
## Flowsheet Validation

The system performs comprehensive validation before executing simulations:

### Topology Validation
- Every unit operation must have at least one connected stream
- Every stream must be connected to at least one unit operation

### Physical Property Validation
- Temperature must be > 0 K
- Pressure must be > 0 Pa
- Mass flow must be >= 0 kg/s
- Molar compositions must sum to 1.0 ± 0.01

### Compound Validation
- All compounds referenced in streams must be defined in the simulation
- At least one compound must be defined

### Unit Operation Validation
- Heater: Must specify either temperature or duty
- Splitter: Split ratios must sum to 1.0 ± 0.01

### Error Handling
When validation fails, the simulation status is set to "Failed" with detailed error messages.
```

**Acceptance**: Documentation is clear and complete

---

### T024 [P2] Create Architecture Decision Record
**File**: `specs/002-flowsheet-validation/adr-001-validation-architecture.md`

Document key architectural decisions:
- Why separate validator component
- Why post-build validation timing
- Why exception-based error handling
- Why phased implementation approach

**Acceptance**: ADR is complete and follows standard format

---

## Definition of Done

- [ ] All 14 tasks (T011-T024) completed
- [ ] Physical property validation implemented and tested
- [ ] Compound validation implemented and tested
- [ ] Unit operation validation implemented and tested
- [ ] FlowsheetValidator has 100% line coverage
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Documentation updated
- [ ] ADR created
- [ ] No breaking changes to Track 1 interface

---

## Merge Strategy

### Pre-Merge Checklist
1. Ensure Track 1 is complete and merged to branch
2. Rebase Track 2 branch on latest Track 1 changes
3. Run full test suite (unit + integration + functional)
4. Verify no conflicts in FlowsheetValidator.cs
5. Verify DI registration includes all dependencies

### Expected Merge Conflicts
- `FlowsheetValidator.Validate()` method - Track 2 adds more validation calls
- Resolution: Keep all validation method calls fromracks

### Post-Merge Verification
1. Run full test suite: `dotnet test`
2. Run functional test: `dotnet test --filter "Should_Fail_On_Disconnected_Stream"`
3. Verify coverage: `dotnet test /p:CollectCoverage=true`
4. Run integration tests with real DWSIM

---

**Status**: Ready for Implementation (after Track 1 completes)  
**Estimated Completion**: 2 days
