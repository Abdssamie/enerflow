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

public sealed class UnitOperationValidationTests
{
    private readonly FlowsheetValidator _validator;
    
    public UnitOperationValidationTests()
    {
        _validator = new FlowsheetValidator(NullLogger<FlowsheetValidator>.Instance);
    }
    
    [Fact]
    public void Validate_HeaterWithInvalidEfficiency_ReturnsInvalidEfficiencyError()
    {
        var simulation = CreateSimulationWithHeater(efficiency: 1.5);
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidEfficiency);
    }
    
    [Fact]
    public void Validate_HeaterWithValidConfiguration_PassesValidation()
    {
        var simulation = CreateSimulationWithHeater(efficiency: 0.85, outletTemperature: 350.0);
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void Validate_MixerWithSingleInput_ReturnsUnitRequiresMultipleInputsError()
    {
        var simulation = CreateSimulationWithMixer(inputCount: 1);
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.UnitRequiresMultipleInputs);
    }
    
    [Fact]
    public void Validate_MixerWithValidTopology_PassesValidation()
    {
        var simulation = CreateSimulationWithMixer(inputCount: 2, outputCount: 1);
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void Validate_SplitterWithInvalidSplitRatios_ReturnsSplitterInvalidRatiosError()
    {
        var simulation = CreateSimulationWithSplitter(splitRatios: new[] { 0.4, 0.4 });
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.SplitterInvalidRatios);
    }
    
    [Fact]
    public void Validate_SplitterWithValidConfiguration_PassesValidation()
    {
        var simulation = CreateSimulationWithSplitter(splitRatios: new[] { 0.6, 0.4 });
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void Validate_ValveWithNegativeOutletPressure_ReturnsInvalidOutletPressureError()
    {
        var simulation = CreateSimulationWithValve(outletPressure: -1000.0);
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidPressure || e.Code == ValidationErrorCodes.InvalidOutletPressure);
        result.Errors.Should().Contain(e => e.EntityName == "Valve1");
    }
    
    [Fact]
    public void Validate_ShortcutColumnWithInvalidRefluxRatio_ReturnsInvalidRefluxRatioError()
    {
        var simulation = CreateSimulationWithShortcutColumn(refluxRatio: -1.0);
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidRefluxRatio);
    }
    
    [Fact]
    public void Validate_ShortcutColumnWithInvalidStages_ReturnsInvalidStagesCountError()
    {
        var simulation = CreateSimulationWithShortcutColumn(stages: 0);
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidStagesCount);
    }
    
    [Fact]
    public void Validate_RecycleWithInvalidTolerance_ReturnsInvalidToleranceError()
    {
        var simulation = CreateSimulationWithRecycle(tolerance: -0.001);
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidTolerance);
    }
    
    [Fact]
    public void Validate_RecycleWithInvalidMaxIterations_ReturnsInvalidMaxIterationsError()
    {
        var simulation = CreateSimulationWithRecycle(maxIterations: 0);
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.InvalidMaxIterations);
    }
    
    [Fact]
    public void Validate_FlashDrumWithNoInputs_ReturnsUnitRequiresInputError()
    {
        var simulation = CreateSimulationWithFlashDrum(inputCount: 0);
        var result = _validator.Validate(simulation, null!);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.UnitRequiresInput);
    }
    
    private SimulationEntity CreateBaseSimulation()
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
        
        simulation.Compounds.Add(new Compound
        {
            Id = Guid.NewGuid(),
            Name = "Water",
            SimulationId = simulation.Id
        });
        
        return simulation;
    }
    
    private SimulationEntity CreateSimulationWithHeater(double efficiency = 1.0, double outletTemperature = 350.0)
    {
        var simulation = CreateBaseSimulation();
        var inputStreamId = Guid.NewGuid();
        var outputStreamId = Guid.NewGuid();
        
        simulation.MaterialStreams.Add(new MaterialStream
        {
            Id = inputStreamId,
            Name = "Inlet",
            SimulationId = simulation.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double> { { "Water", 1.0 } }
        });
        
        simulation.MaterialStreams.Add(new MaterialStream
        {
            Id = outputStreamId,
            Name = "Outlet",
            SimulationId = simulation.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double> { { "Water", 1.0 } }
        });
        
        simulation.UnitOperations.Add(new HeaterObject
        {
            Id = Guid.NewGuid(),
            Name = "Heater1",
            SimulationId = simulation.Id,
            InputStreamIds = new List<Guid> { inputStreamId },
            OutputStreamIds = new List<Guid> { outputStreamId },
            Efficiency = efficiency,
            OutletTemperature = outletTemperature,
            CalcMode = HeaterCalculationMode.OutletTemperature
        });
        
        return simulation;
    }
    
    private SimulationEntity CreateSimulationWithMixer(int inputCount = 2, int outputCount = 1)
    {
        var simulation = CreateBaseSimulation();
        var inputStreamIds = new List<Guid>();
        
        for (int i = 0; i < inputCount; i++)
        {
            var streamId = Guid.NewGuid();
            inputStreamIds.Add(streamId);
            simulation.MaterialStreams.Add(new MaterialStream
            {
                Id = streamId,
                Name = $"Input{i + 1}",
                SimulationId = simulation.Id,
                Temperature = 298.15,
                Pressure = 101325,
                MassFlow = 1.0,
                Composition = new Dictionary<string, double> { { "Water", 1.0 } }
            });
        }
        
        var outputStreamIds = new List<Guid>();
        for (int i = 0; i < outputCount; i++)
        {
            var streamId = Guid.NewGuid();
            outputStreamIds.Add(streamId);
            simulation.MaterialStreams.Add(new MaterialStream
            {
                Id = streamId,
                Name = $"Output{i + 1}",
                SimulationId = simulation.Id,
                Temperature = 298.15,
                Pressure = 101325,
                MassFlow = 1.0,
                Composition = new Dictionary<string, double> { { "Water", 1.0 } }
            });
        }
        
        simulation.UnitOperations.Add(new MixerObject
        {
            Id = Guid.NewGuid(),
            Name = "Mixer1",
            SimulationId = simulation.Id,
            InputStreamIds = inputStreamIds,
            OutputStreamIds = outputStreamIds
        });
        
        return simulation;
    }
    
    private SimulationEntity CreateSimulationWithSplitter(double[] splitRatios = null, int outputCount = 2)
    {
        var simulation = CreateBaseSimulation();
        var inputStreamId = Guid.NewGuid();
        
        simulation.MaterialStreams.Add(new MaterialStream
        {
            Id = inputStreamId,
            Name = "Input",
            SimulationId = simulation.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double> { { "Water", 1.0 } }
        });
        
        var outputStreamIds = new List<Guid>();
        var ratioDict = new Dictionary<Guid, double>();
        splitRatios = splitRatios ?? new[] { 0.5, 0.5 };
        
        for (int i = 0; i < outputCount; i++)
        {
            var streamId = Guid.NewGuid();
            outputStreamIds.Add(streamId);
            simulation.MaterialStreams.Add(new MaterialStream
            {
                Id = streamId,
                Name = $"Output{i + 1}",
                SimulationId = simulation.Id,
                Temperature = 298.15,
                Pressure = 101325,
                MassFlow = 0.5,
                Composition = new Dictionary<string, double> { { "Water", 1.0 } }
            });
            
            if (i < splitRatios.Length)
            {
                ratioDict[streamId] = splitRatios[i];
            }
        }
        
        simulation.UnitOperations.Add(new SplitterObject
        {
            Id = Guid.NewGuid(),
            Name = "Splitter1",
            SimulationId = simulation.Id,
            InputStreamIds = new List<Guid> { inputStreamId },
            OutputStreamIds = outputStreamIds,
            SplitRatios = ratioDict
        });
        
        return simulation;
    }
    
    private SimulationEntity CreateSimulationWithValve(double outletPressure = 50000.0)
    {
        var simulation = CreateBaseSimulation();
        var inputStreamId = Guid.NewGuid();
        var outputStreamId = Guid.NewGuid();
        
        simulation.MaterialStreams.Add(new MaterialStream
        {
            Id = inputStreamId,
            Name = "Input",
            SimulationId = simulation.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double> { { "Water", 1.0 } }
        });
        
        simulation.MaterialStreams.Add(new MaterialStream
        {
            Id = outputStreamId,
            Name = "Output",
            SimulationId = simulation.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double> { { "Water", 1.0 } }
        });
        
        simulation.UnitOperations.Add(new ValveObject
        {
            Id = Guid.NewGuid(),
            Name = "Valve1",
            SimulationId = simulation.Id,
            InputStreamIds = new List<Guid> { inputStreamId },
            OutputStreamIds = new List<Guid> { outputStreamId },
            OutletPressure = outletPressure
        });
        
        return simulation;
    }
    
    private SimulationEntity CreateSimulationWithShortcutColumn(double refluxRatio = 2.0, int stages = 10)
    {
        var simulation = CreateBaseSimulation();
        var waterCompoundId = simulation.Compounds.First().Id;
        var ethanolCompoundId = Guid.NewGuid();
        
        simulation.Compounds.Add(new Compound
        {
            Id = ethanolCompoundId,
            Name = "Ethanol",
            SimulationId = simulation.Id
        });
        
        var inputStreamId = Guid.NewGuid();
        var outputStream1Id = Guid.NewGuid();
        var outputStream2Id = Guid.NewGuid();
        
        simulation.MaterialStreams.Add(new MaterialStream
        {
            Id = inputStreamId,
            Name = "Feed",
            SimulationId = simulation.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double> { { "Water", 0.5 }, { "Ethanol", 0.5 } }
        });
        
        simulation.MaterialStreams.Add(new MaterialStream
        {
            Id = outputStream1Id,
            Name = "Distillate",
            SimulationId = simulation.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 0.5,
            Composition = new Dictionary<string, double> { { "Ethanol", 1.0 } }
        });
        
        simulation.MaterialStreams.Add(new MaterialStream
        {
            Id = outputStream2Id,
            Name = "Bottoms",
            SimulationId = simulation.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 0.5,
            Composition = new Dictionary<string, double> { { "Water", 1.0 } }
        });
        
        simulation.UnitOperations.Add(new ShortcutColumnObject
        {
            Id = Guid.NewGuid(),
            Name = "Column1",
            SimulationId = simulation.Id,
            InputStreamIds = new List<Guid> { inputStreamId },
            OutputStreamIds = new List<Guid> { outputStream1Id, outputStream2Id },
            RefluxRatio = refluxRatio,
            Stages = stages,
            LightKey = ethanolCompoundId,
            HeavyKey = waterCompoundId,
            CondenserPressure = 101325,
            ReboilerPressure = 101325
        });
        
        return simulation;
    }
    
    private SimulationEntity CreateSimulationWithRecycle(double tolerance = 1e-4, int maxIterations = 50)
    {
        var simulation = CreateBaseSimulation();
        var inputStreamId = Guid.NewGuid();
        var outputStreamId = Guid.NewGuid();
        
        simulation.MaterialStreams.Add(new MaterialStream
        {
            Id = inputStreamId,
            Name = "Input",
            SimulationId = simulation.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double> { { "Water", 1.0 } }
        });
        
        simulation.MaterialStreams.Add(new MaterialStream
        {
            Id = outputStreamId,
            Name = "Output",
            SimulationId = simulation.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double> { { "Water", 1.0 } }
        });
        
        simulation.UnitOperations.Add(new RecycleObject
        {
            Id = Guid.NewGuid(),
            Name = "Recycle1",
            SimulationId = simulation.Id,
            InputStreamIds = new List<Guid> { inputStreamId },
            OutputStreamIds = new List<Guid> { outputStreamId },
            Tolerance = tolerance,
            MaxIterations = maxIterations
        });
        
        return simulation;
    }
    
    private SimulationEntity CreateSimulationWithFlashDrum(int inputCount = 1, int outputCount = 2)
    {
        var simulation = CreateBaseSimulation();
        var inputStreamIds = new List<Guid>();
        
        for (int i = 0; i < inputCount; i++)
        {
            var streamId = Guid.NewGuid();
            inputStreamIds.Add(streamId);
            simulation.MaterialStreams.Add(new MaterialStream
            {
                Id = streamId,
                Name = $"Input{i + 1}",
                SimulationId = simulation.Id,
                Temperature = 298.15,
                Pressure = 101325,
                MassFlow = 1.0,
                Composition = new Dictionary<string, double> { { "Water", 1.0 } }
            });
        }
        
        var outputStreamIds = new List<Guid>();
        for (int i = 0; i < outputCount; i++)
        {
            var streamId = Guid.NewGuid();
            outputStreamIds.Add(streamId);
            simulation.MaterialStreams.Add(new MaterialStream
            {
                Id = streamId,
                Name = $"Output{i + 1}",
                SimulationId = simulation.Id,
                Temperature = 298.15,
                Pressure = 101325,
                MassFlow = 0.5,
                Composition = new Dictionary<string, double> { { "Water", 1.0 } }
            });
        }
        
        simulation.UnitOperations.Add(new FlashDrumObject
        {
            Id = Guid.NewGuid(),
            Name = "FlashDrum1",
            SimulationId = simulation.Id,
            InputStreamIds = inputStreamIds,
            OutputStreamIds = outputStreamIds,
            OutletTemperature = 298.15,
            OutletPressure = 101325
        });
        
        return simulation;
    }
}
