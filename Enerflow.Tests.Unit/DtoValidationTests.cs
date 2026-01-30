using System.ComponentModel.DataAnnotations;
using Enerflow.Domain.DTOs;
using Xunit;

namespace Enerflow.Tests.Unit;

public class DtoValidationTests
{
    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void Validate_CreateSimulationRequest_Valid()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "Test Simulation",
            ThermoPackage = "Raoult's Law",
            FlashAlgorithm = "Nested Loops",
            SystemOfUnits = "SI"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_CreateSimulationRequest_InvalidName()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "", // Invalid: MinimumLength = 1 (and Required)
            ThermoPackage = "Raoult's Law",
            FlashAlgorithm = "Nested Loops",
            SystemOfUnits = "SI"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Validate_CreateSimulationRequest_NameTooLong()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = new string('A', 101), // Invalid: MaxLength = 100
            ThermoPackage = "Raoult's Law",
            FlashAlgorithm = "Nested Loops",
            SystemOfUnits = "SI"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Validate_AddStreamRequest_InvalidTemperature()
    {
        // Arrange
        var request = new AddStreamRequest
        {
            Name = "Stream 1",
            Temperature = -10, // Invalid: Range(0, 10000)
            Pressure = 101325,
            MassFlow = 1
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains("Temperature"));
    }

    [Fact]
    public void Validate_AddStreamRequest_InvalidPressure()
    {
        // Arrange
        var request = new AddStreamRequest
        {
            Name = "Stream 1",
            Temperature = 300,
            Pressure = -1, // Invalid: Range(0, 1e9)
            MassFlow = 1
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains("Pressure"));
    }

    [Fact]
    public void Validate_AddUnitRequest_Valid()
    {
        // Arrange
        var request = new AddUnitRequest
        {
            Name = "Unit 1",
            UnitOperation = Enerflow.Domain.Enums.UnitOperationType.Mixer
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_AddUnitRequest_InvalidName()
    {
        // Arrange
        var request = new AddUnitRequest
        {
            Name = new string('A', 101), // Invalid
            UnitOperation = Enerflow.Domain.Enums.UnitOperationType.Mixer
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }
}
