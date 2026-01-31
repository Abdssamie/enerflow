using System.ComponentModel.DataAnnotations;
using Enerflow.Domain.DTOs;
using Xunit;

namespace Enerflow.Tests.Unit;

public class DtoValidationTests
{
    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void CreateSimulationRequest_WithEmptyName_ShouldFailValidation()
    {
        // Arrange
        var request = new CreateSimulationRequest
        {
            Name = "", // Invalid
            ThermoPackage = "Peng-Robinson",
            FlashAlgorithm = "Nested Loops",
            SystemOfUnits = "SI"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void AddStreamRequest_WithNegativeMassFlow_ShouldFailValidation()
    {
        // Arrange
        var request = new AddStreamRequest
        {
            Name = "Stream 1",
            MassFlow = -1.0, // Invalid
            Temperature = 300,
            Pressure = 101325
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains("MassFlow"));
    }

    [Fact]
    public void AddStreamRequest_WithNegativePressure_ShouldFailValidation()
    {
        // Arrange
        var request = new AddStreamRequest
        {
            Name = "Stream 1",
            MassFlow = 1.0,
            Temperature = 300,
            Pressure = -500 // Invalid
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains("Pressure"));
    }

    [Fact]
    public void AddStreamRequest_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var request = new AddStreamRequest
        {
            Name = "Valid Stream",
            MassFlow = 10.5,
            Temperature = 298.15,
            Pressure = 101325
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Empty(results);
    }
}
