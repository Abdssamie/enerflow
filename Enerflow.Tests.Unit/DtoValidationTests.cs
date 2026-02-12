using System.ComponentModel.DataAnnotations;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Enums;
using Enerflow.Domain.ValueObjects;
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
    public void AddStreamRequest_ValidData_PassesValidation()
    {
        var request = new AddStreamRequest
        {
            Name = "Valid Stream",
            Temperature = 300,
            Pressure = 101325,
            MassFlow = 10
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Fact]
    public void AddStreamRequest_NegativeTemperature_FailsValidation()
    {
        var request = new AddStreamRequest
        {
            Name = "Invalid Stream",
            Temperature = -10, // Invalid
            Pressure = 101325,
            MassFlow = 10
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddStreamRequest.Temperature)));
    }

    [Fact]
    public void AddStreamRequest_LongName_FailsValidation()
    {
        var request = new AddStreamRequest
        {
            Name = new string('A', 101), // Invalid (max 100)
            Temperature = 300,
            Pressure = 101325,
            MassFlow = 10
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AddStreamRequest.Name)));
    }

    [Fact]
    public void CreateSimulationRequest_LongName_FailsValidation()
    {
        var request = new CreateSimulationRequest
        {
            Name = new string('B', 101), // Invalid
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateSimulationRequest.Name)));
    }
}
