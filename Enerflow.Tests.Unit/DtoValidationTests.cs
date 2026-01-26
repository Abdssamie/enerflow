using System.ComponentModel.DataAnnotations;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Enums;
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
    public void CreateSimulationRequest_WithValidData_ShouldPassValidation()
    {
        var request = new CreateSimulationRequest
        {
            Name = "My Simulation",
            ThermoPackage = "Peng-Robinson",
            FlashAlgorithm = "Nested Loops",
            SystemOfUnits = "SI"
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateSimulationRequest_WithShortName_ShouldFailValidation()
    {
        var request = new CreateSimulationRequest
        {
            Name = "A", // Too short (min 3)
            ThermoPackage = "Peng-Robinson",
            FlashAlgorithm = "Nested Loops",
            SystemOfUnits = "SI"
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSimulationRequest.Name)));
    }

    [Fact]
    public void CreateSimulationRequest_WithLongName_ShouldFailValidation()
    {
        var request = new CreateSimulationRequest
        {
            Name = new string('A', 101), // Too long (max 100)
            ThermoPackage = "Peng-Robinson",
            FlashAlgorithm = "Nested Loops",
            SystemOfUnits = "SI"
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSimulationRequest.Name)));
    }

    [Fact]
    public void AddStreamRequest_WithInvalidTemperature_ShouldFailValidation()
    {
        var request = new AddStreamRequest
        {
            Name = "Stream 1",
            Temperature = -5, // Invalid range (0-5000)
            Pressure = 101325,
            MassFlow = 1.0
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddStreamRequest.Temperature)));
    }

    [Fact]
    public void AddStreamRequest_WithInvalidPressure_ShouldFailValidation()
    {
        var request = new AddStreamRequest
        {
            Name = "Stream 1",
            Temperature = 300,
            Pressure = -1, // Invalid range
            MassFlow = 1.0
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddStreamRequest.Pressure)));
    }

     [Fact]
    public void AddStreamRequest_WithInvalidMassFlow_ShouldFailValidation()
    {
        var request = new AddStreamRequest
        {
            Name = "Stream 1",
            Temperature = 300,
            Pressure = 101325,
            MassFlow = -0.1 // Invalid range
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddStreamRequest.MassFlow)));
    }
}
