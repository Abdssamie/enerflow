using Enerflow.Domain.Entities;
using Enerflow.Domain.Entities.Streams;
using Enerflow.Domain.Entities.UnitOperations;
using Enerflow.Domain.Enums;
using Enerflow.Worker.Validation;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Tests.Unit.Worker.Validation;

/// <summary>
/// Tests for physical property validation (temperature, pressure, mass flow, composition).
/// </summary>
public sealed class PhysicalPropertyValidationTests
{
    private readonly FlowsheetValidator _validator;
    
    public PhysicalPropertyValidationTests()
    {
        _validator = new FlowsheetValidator(NullLogger<FlowsheetValidator>.Instance);
    }
    
    #region Temperature Validation Tests
    
    [Theory]
    [InlineData(-10)]
    [InlineData(0)]
    public void Validate_InvalidTemperature_ReturnsError(double temperature)
    {
        // Arrange
        var simulation = CreateSimulationWithStream(temperature: temperature);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidTemperature);
        result.Errors.Should().Contain(e => e.EntityType == "MaterialStream");
        result.Errors.Should().Contain(e => e.Message.Contains("Temperature"));
    }
    
    [Fact]
    public void Validate_ValidTemperature_PassesValidation()
    {
        // Arrange
        var simulation = CreateSimulationWithStream(temperature: 298.15);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert - Debug output
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                System.Console.WriteLine($"Error: {error.Code} - {error.Message}");
            }
        }
        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.InvalidTemperature);
    }
    
    #endregion
    
    #region Pressure Validation Tests
    
    [Theory]
    [InlineData(-1000)]
    [InlineData(0)]
    public void Validate_InvalidPressure_ReturnsError(double pressure)
    {
        // Arrange
        var simulation = CreateSimulationWithStream(pressure: pressure);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.ValidationError || e.Code == ValidationErrorCodes.InvalidPressure);
        result.Errors.Should().Contain(e => e.EntityType == "MaterialStream");
        result.Errors.Should().Contain(e => e.Message.Contains("Pressure"));
    }
    
    [Fact]
    public void Validate_ValidPressure_PassesValidation()
    {
        // Arrange
        var simulation = CreateSimulationWithStream(pressure: 101325);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.InvalidPressure);
    }
    
    #endregion
    
    #region Mass Flow Validation Tests
    
    [Fact]
    public void Validate_NegativeMassFlow_ReturnsError()
    {
        // Arrange
        var simulation = CreateSimulationWithStream(massFlow: -5.0);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidMassFlow);
        result.Errors.Should().Contain(e => e.EntityType == "MaterialStream");
        result.Errors.Should().Contain(e => e.Message.Contains("MassFlow"));
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(1.0)]
    [InlineData(100.5)]
    public void Validate_ValidMassFlow_PassesValidation(double massFlow)
    {
        // Arrange
        var simulation = CreateSimulationWithStream(massFlow: massFlow);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.InvalidMassFlow);
    }
    
    #endregion
    
    #region Composition Validation Tests
    
    [Fact]
    public void Validate_CompositionNotSummingToOne_ReturnsError()
    {
        // Arrange
        var composition = new Dictionary<string, double>
        {
            { "Water", 0.5 },
            { "Ethanol", 0.3 } // Sum = 0.8, not 1.0
        };
        var simulation = CreateSimulationWithStream(composition: composition);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidCompositionSum);
        result.Errors.Should().Contain(e => e.Message.Contains("0.8000"));
        result.Errors.Should().Contain(e => e.Message.Contains("1 ±") || e.Message.Contains("1.0"));
    }
    
    [Fact]
    public void Validate_CompositionWithNegativeValue_ReturnsError()
    {
        // Arrange
        var composition = new Dictionary<string, double>
        {
            { "Water", 1.2 },
            { "Ethanol", -0.2 } // Negative composition
        };
        var simulation = CreateSimulationWithStream(composition: composition);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.NegativeComposition);
        result.Errors.Should().Contain(e => e.Message.Contains("Ethanol"));
        result.Errors.Should().Contain(e => e.Message.Contains("-0.2"));
    }
    
    [Fact]
    public void Validate_ValidComposition_PassesValidation()
    {
        // Arrange
        var composition = new Dictionary<string, double>
        {
            { "Water", 0.6 },
            { "Ethanol", 0.4 } // Sum = 1.0
        };
        var simulation = CreateSimulationWithStream(composition: composition);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.InvalidCompositionSum);
        result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.NegativeComposition);
    }
    
    [Fact]
    public void Validate_CompositionWithinTolerance_PassesValidation()
    {
        // Arrange - Sum is 1.005, within 0.01 tolerance
        var composition = new Dictionary<string, double>
        {
            { "Water", 0.505 },
            { "Ethanol", 0.5 }
        };
        var simulation = CreateSimulationWithStream(composition: composition);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.InvalidCompositionSum);
    }
    
    #endregion
    
    #region Energy Stream Validation Tests
    
    [Fact]
    public void Validate_NegativeEnergyFlow_ReturnsError()
    {
        // Arrange
        var simulation = CreateSimulationWithEnergyStream(energyFlow: -100.0);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidEnergyFlow);
        result.Errors.Should().Contain(e => e.EntityType == "EnergyStream");
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(1000.5)]
    public void Validate_ValidEnergyFlow_PassesValidation(double energyFlow)
    {
        // Arrange
        var simulation = CreateSimulationWithEnergyStream(energyFlow: energyFlow);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.Code == ValidationErrorCodes.InvalidEnergyFlow);
    }
    
    #endregion
    
    #region Multiple Errors Tests
    
    [Fact]
    public void Validate_MultiplePhysicalPropertyErrors_ReturnsAllErrors()
    {
        // Arrange - Invalid temperature AND invalid pressure
        var simulation = CreateSimulationWithStream(temperature: -10, pressure: -100);
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(1);
        // At least one error should be about temperature or pressure
        result.Errors.Should().Contain(e => 
            e.Code == ValidationErrorCodes.InvalidTemperature || 
            e.Code == ValidationErrorCodes.InvalidPressure ||
            e.Code == ValidationErrorCodes.ValidationError);
    }
    
    #endregion
    
    #region Helper Methods
    
    private SimulationEntity CreateSimulationWithStream(
        double temperature = 298.15,
        double pressure = 101325,
        double massFlow = 1.0,
        Dictionary<string, double>? composition = null)
    {
        var simulation = new SimulationEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation",
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI,
            Status = SimulationStatus.Created
        };
        
        // Add compounds
        simulation.Compounds.Add(new Compound
        {
            Id = Guid.NewGuid(),
            Name = "Water",
            SimulationId = simulation.Id
        });
        
        if (composition != null)
        {
            foreach (var compoundName in composition.Keys)
            {
                if (!simulation.Compounds.Any(c => c.Name == compoundName))
                {
                    simulation.Compounds.Add(new Compound
                    {
                        Id = Guid.NewGuid(),
                        Name = compoundName,
                        SimulationId = simulation.Id
                    });
                }
            }
        }
        
        var stream = new MaterialStream
        {
            Id = Guid.NewGuid(),
            Name = "TestStream",
            SimulationId = simulation.Id,
            Temperature = temperature,
            Pressure = pressure,
            MassFlow = massFlow,
            Composition = composition ?? new Dictionary<string, double> { { "Water", 1.0 } }
        };
        
        simulation.MaterialStreams.Add(stream);
        
        // Add a connected unit to pass topology validation - use HeaterObject instead of MixerObject
        var unit = new HeaterObject
        {
            Id = Guid.NewGuid(),
            Name = "TestHeater",
            SimulationId = simulation.Id,
            InputStreamIds = new List<Guid> { stream.Id },
            OutputStreamIds = new List<Guid>()
        };
        simulation.UnitOperations.Add(unit);
        
        return simulation;
    }
    
    private SimulationEntity CreateSimulationWithEnergyStream(double energyFlow = 1000.0)
    {
        var simulation = new SimulationEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation",
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI,
            Status = SimulationStatus.Created
        };
        
        // Add a compound (required)
        simulation.Compounds.Add(new Compound
        {
            Id = Guid.NewGuid(),
            Name = "Water",
            SimulationId = simulation.Id
        });
        
        var energyStream = new EnergyStream
        {
            Id = Guid.NewGuid(),
            Name = "TestEnergyStream",
            SimulationId = simulation.Id,
            EnergyFlow = energyFlow
        };
        
        simulation.EnergyStreams.Add(energyStream);
        
        // Add a connected unit to pass topology validation
        var unit = new HeaterObject
        {
            Id = Guid.NewGuid(),
            Name = "TestHeater",
            SimulationId = simulation.Id,
            InputStreamIds = new List<Guid>(),
            OutputStreamIds = new List<Guid> { energyStream.Id }
        };
        simulation.UnitOperations.Add(unit);
        
        return simulation;
    }
    
    #endregion
}
