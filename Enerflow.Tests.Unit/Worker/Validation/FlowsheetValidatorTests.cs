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

public sealed class FlowsheetValidatorTests
{
    private readonly FlowsheetValidator _validator;
    
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
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.DisconnectedUnit);
        var disconnectedError = result.Errors.First(e => e.Code == ValidationErrorCodes.DisconnectedUnit);
        disconnectedError.EntityName.Should().Be("LonelyMixer");
        disconnectedError.EntityType.Should().Be("UnitOperation");
        disconnectedError.Message.Should().Contain("has no connected streams");
    }
    
    [Fact]
    public void Validate_OrphanedMaterialStream_ReturnsError()
    {
        // Arrange
        var simulation = CreateSimulationWithOrphanedMaterialStream();
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ValidationErrorCodes.OrphanedStream);
        result.Errors[0].EntityType.Should().Be("MaterialStream");
        result.Errors[0].EntityName.Should().Be("OrphanStream");
    }
    
    [Fact]
    public void Validate_OrphanedEnergyStream_ReturnsError()
    {
        // Arrange
        var simulation = CreateSimulationWithOrphanedEnergyStream();
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be(ValidationErrorCodes.OrphanedStream);
        result.Errors[0].EntityType.Should().Be("EnergyStream");
        result.Errors[0].EntityName.Should().Be("OrphanEnergy");
    }
    
    [Fact]
    public void Validate_ProperlyConnectedFlowsheet_ReturnsValid()
    {
        // Arrange
        var simulation = CreateValidSimulation();
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeTrue();
    result.Errors.Should().BeEmpty();
    }
    
    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var simulation = CreateSimulationWithMultipleErrors();
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.DisconnectedUnit);
        result.Errors.Should().Contain(e => e.Code == ValidationErrorCodes.OrphanedStream);
    }
    
    [Fact]
    public void Validate_EmptySimulation_ReturnsValid()
    {
        // Arrange
        var simulation = CreateEmptySimulation();
        
        // Act
        var result = _validator.Validate(simulation, null!);
        
        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
    
    // Helper methods to create test simulations
    
    private SimulationEntity CreateSimulationWithDisconnectedUnit()
    {
        var sim = new SimulationEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation",
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI,
            Status = SimulationStatus.Created
        };
        
        // Add compound to pass compound validation
        sim.Compounds.Add(new Compound
        {
            Id = Guid.NewGuid(),
            Name = "Water",
            SimulationId = sim.Id
        });
        
        // Add a mixer with no connected streams
        sim.UnitOperations.Add(new MixerObject
        {
            Id = Guid.NewGuid(),
            Name = "LonelyMixer",
            SimulationId = sim.Id,
            InputStreamIds = new List<Guid>(),
            OutputStreamIds = new List<Guid>()
        });
        
        return sim;
    }
    
    private SimulationEntity CreateSimulationWithOrphanedMaterialStream()
    {
        var sim = new SimulationEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation",
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI,
            Status = SimulationStatus.Created
        };
        
        // Add compound to pass compound validation
        sim.Compounds.Add(new Compound
        {
            Id = Guid.NewGuid(),
            Name = "Water",
            SimulationId = sim.Id
        });
        
        // Add a stream that's not connected to any unit
        sim.MaterialStreams.Add(new MaterialStream
        {
            Id = Guid.NewGuid(),
            Name = "OrphanStream",
            SimulationId = sim.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double>()
        });
        
        return sim;
    }
    
    private SimulationEntity CreateSimulationWithOrphanedEnergyStream()
    {
        var sim = new SimulationEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation",
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI,
            Status = SimulationStatus.Created
        };
        
        // Add compound to pass compound validation
        sim.Compounds.Add(new Compound
        {
            Id = Guid.NewGuid(),
            Name = "Water",
            SimulationId = sim.Id
        });
        
        // Add an energy stream that's not connected to any unit
        sim.EnergyStreams.Add(new EnergyStream
        {
            Id = Guid.NewGuid(),
            Name = "OrphanEnergy",
            SimulationId = sim.Id,
            EnergyFlow = 1000.0
        });
        
        return sim;
    }
    
    private SimulationEntity CreateValidSimulation()
    {
        var sim = new SimulationEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation",
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI,
            Status = SimulationStatus.Created
        };
        
        // Add compound to pass compound validation
        sim.Compounds.Add(new Compound
        {
            Id = Guid.NewGuid(),
            Name = "Water",
            SimulationId = sim.Id
        });
        
        var stream1Id = Guid.NewGuid();
        var stream2Id = Guid.NewGuid();
        
        // Add streams
        sim.MaterialStreams.Add(new MaterialStream
        {
            Id = stream1Id,
            Name = "Inlet",
            SimulationId = sim.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double>()
        });
        
        sim.MaterialStreams.Add(new MaterialStream
        {
            Id = stream2Id,
            Name = "Outlet",
            SimulationId = sim.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double>()
        });
        
        // Add a heater connected to both streams
        sim.UnitOperations.Add(new HeaterObject
        {
            Id = Guid.NewGuid(),
            Name = "Heater1",
            SimulationId = sim.Id,
            InputStreamIds = new List<Guid> { stream1Id },
            OutputStreamIds = new List<Guid> { stream2Id }
        });
        
        return sim;
    }
    
    private SimulationEntity CreateSimulationWithMultipleErrors()
    {
        var sim = new SimulationEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Simulation",
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI,
            Status = SimulationStatus.Created
        };
        
        // Add compound to pass compound validation
        sim.Compounds.Add(new Compound
        {
            Id = Guid.NewGuid(),
            Name = "Water",
            SimulationId = sim.Id
        });
        
        // Add a disconnected unit
        sim.UnitOperations.Add(new MixerObject
        {
            Id = Guid.NewGuid(),
            Name = "DisconnectedMixer",
            SimulationId = sim.Id,
            InputStreamIds = new List<Guid>(),
            OutputStreamIds = new List<Guid>()
        });
        
        // Add an orphaned stream
        sim.MaterialStreams.Add(new MaterialStream
        {
            Id = Guid.NewGuid(),
            Name = "OrphanStream",
            SimulationId = sim.Id,
            Temperature = 298.15,
            Pressure = 101325,
            MassFlow = 1.0,
            Composition = new Dictionary<string, double>()
        });
        
        return sim;
    }
    
    private SimulationEntity CreateEmptySimulation()
    {
        return new SimulationEntity
        {
            Id = Guid.NewGuid(),
            Name = "Empty Simulation",
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI,
            Status = SimulationStatus.Created,
            Compounds = new List<Compound>
            {
                new Compound
                {
                    Id = Guid.NewGuid(),
                    Name = "Water",
                    SimulationId = Guid.NewGuid()
                }
            }
        };
    }
}
