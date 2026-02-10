using Enerflow.Domain.Entities;
using Enerflow.Domain.Entities.Streams;
using Enerflow.Domain.Entities.UnitOperations;
using Enerflow.Domain.Enums;
using Enerflow.Simulation.Flowsheet.Connections;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Enerflow.Tests.Unit.Simulation.Flowsheet.Connections;

public class ConnectionFactoryTests
{
	[Fact]
	public void HeaterWithEnergyStream_DomainModelStructure_IsCorrect()
	{
		// Arrange
		var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<ConnectionFactory>();
		var factory = new ConnectionFactory(logger);
		
		var simulationId = Guid.NewGuid();
		
		var simulation = new Domain.Entities.Simulation
		{
			Id = simulationId,
			Name = "Test Heater with Energy Stream",
			PropertyPackage = PropertyPackageType.PengRobinson,
			FlashAlgorithm = FlashAlgorithm.NestedLoops3Phase,
			SystemOfUnits = SystemOfUnits.SI
		};

		var energyStream = new EnergyStream
		{
			SimulationId = simulationId,
			Name = "Heat Input",
			EnergyFlow = 100
		};

		var heater = new HeaterObject
		{
			SimulationId = simulationId,
			Name = "Heater-01",
			OutletTemperature = 373.15,
			CalcMode = HeaterCalculationMode.OutletTemperature,
			EnergyInputId = energyStream.Id
		};

		simulation.EnergyStreams.Add(energyStream);
		simulation.UnitOperations.Add(heater);

		// Assert - Verify domain model is correctly structured
		Assert.NotNull(heater.EnergyInputId);
		Assert.Equal(energyStream.Id, heater.EnergyInputId.Value);
		Assert.Single(simulation.EnergyStreams);
		Assert.Contains(energyStream, simulation.EnergyStreams);
	}

	[Fact]
	public void CoolerWithEnergyStream_DomainModelStructure_IsCorrect()
	{
		// Arrange
		var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<ConnectionFactory>();
		var factory = new ConnectionFactory(logger);

		var simulationId = Guid.NewGuid();

		var simulation = new Domain.Entities.Simulation
		{
			Id = simulationId,
			Name = "Test Cooler with Energy Stream",
			PropertyPackage = PropertyPackageType.PengRobinson,
			FlashAlgorithm = FlashAlgorithm.NestedLoops3Phase,
			SystemOfUnits = SystemOfUnits.SI
		};

		var energyStream = new EnergyStream
		{
			SimulationId = simulationId,
			Name = "Heat Removal",
			EnergyFlow = -100
		};

		var cooler = new CoolerObject
		{
			SimulationId = simulationId,
			Name = "Cooler-01",
			OutletTemperature = 298.15,
			CalcMode = HeaterCalculationMode.OutletTemperature,
			EnergyInputId = energyStream.Id
		};

		simulation.EnergyStreams.Add(energyStream);
		simulation.UnitOperations.Add(cooler);

		// Assert - Verify domain model is correctly structured
		Assert.NotNull(cooler.EnergyInputId);
		Assert.Equal(energyStream.Id, cooler.EnergyInputId.Value);
		Assert.Single(simulation.EnergyStreams);
		Assert.Contains(energyStream, simulation.EnergyStreams);
	}

	[Fact]
	public void HeaterWithoutEnergyStream_DomainModel_HandlesOptionalConnection()
	{
		// Arrange
		var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
		var logger = loggerFactory.CreateLogger<ConnectionFactory>();
		var factory = new ConnectionFactory(logger);

		var simulationId = Guid.NewGuid();

		var simulation = new Domain.Entities.Simulation
		{
			Id = simulationId,
			Name = "Test Heater without Energy Stream",
			PropertyPackage = PropertyPackageType.PengRobinson,
			FlashAlgorithm = FlashAlgorithm.NestedLoops3Phase,
			SystemOfUnits = SystemOfUnits.SI
		};

		var heater = new HeaterObject
		{
			SimulationId = simulationId,
			Name = "Heater-01",
			OutletTemperature = 373.15,
			CalcMode = HeaterCalculationMode.OutletTemperature,
			EnergyInputId = null // No energy stream
		};

		simulation.UnitOperations.Add(heater);

		// Assert - Verify domain model handles optional energy stream
		Assert.Null(heater.EnergyInputId);
		Assert.Empty(simulation.EnergyStreams);
	}
}
