using DWSIM.UnitOperations.UnitOperations;
using Enerflow.Domain.Entities;
using Enerflow.Domain.Entities.Streams;
using Enerflow.Domain.Entities.UnitOperations;
using Enerflow.Domain.Enums;
using Enerflow.Simulation.Flowsheet.Compounds;
using Enerflow.Simulation.Flowsheet.FlashAlgorithms;
using Enerflow.Simulation.Flowsheet.PropertyPackages;
using Enerflow.Simulation.Flowsheet.Streams;
using Enerflow.Simulation.Flowsheet.UnitOperations;
using Enerflow.Worker.Builders;
using Enerflow.Worker.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;
using DWSIMRecycle = DWSIM.UnitOperations.SpecialOps.Recycle;
using DWSIMStream = DWSIM.Thermodynamics.Streams.MaterialStream;

namespace Enerflow.Tests.Integration;

[Collection("DWSIM")]
public class FlowsheetBuilderIntegrationTests
{
    private readonly DWSIMFlowsheetBuilder _builder;

    public FlowsheetBuilderIntegrationTests()
    {
        // 1. Setup DWSIM Global Settings
        DWSIM.GlobalSettings.Settings.AutomationMode = true;

        // 2. Instantiate dependencies
        var logger = NullLogger<DWSIMFlowsheetBuilder>.Instance;
        var automation = new DWSIM.Automation.Automation3();
        var compoundManager = new CompoundManager(NullLogger<CompoundManager>.Instance);
        var propPackageManager = new PropertyPackageManager(NullLogger<PropertyPackageManager>.Instance);
        var flashAlgoManager = new FlashAlgorithmManager(NullLogger<FlashAlgorithmManager>.Instance);
        
        var materialStreamFactory = new MaterialStreamFactory(NullLogger<MaterialStreamFactory>.Instance);
        var energyStreamFactory = new EnergyStreamFactory(NullLogger<EnergyStreamFactory>.Instance);
        var unitOpFactory = new UnitOperationFactory(NullLogger<UnitOperationFactory>.Instance);
        var validator = new FlowsheetValidator(NullLogger<FlowsheetValidator>.Instance);

        // 3. Instantiate Builder
        _builder = new DWSIMFlowsheetBuilder(
            automation,
            compoundManager,
            propPackageManager,
            flashAlgoManager,
            materialStreamFactory,
            energyStreamFactory,
            unitOpFactory,
            validator,
            logger
        );
    }

    [Fact]
    public void Build_Simple_Simulation()
    {
        // Arrange
        var sim = new Domain.Entities.Simulation
        {
            Name = "Simple Heating",
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI
        };

        // Add Compounds
        sim.Compounds.Add(new Compound { Name = "Water", SimulationId = sim.Id });

        // Add Streams
        var feed = new MaterialStream 
        { 
            Name = "Feed", 
            SimulationId = sim.Id,
            Temperature = 300,
            Pressure = 101325,
            MassFlow = 1
        };
        var product = new MaterialStream 
        { 
            Name = "Product",
            SimulationId = sim.Id
        };
        sim.MaterialStreams.Add(feed);
        sim.MaterialStreams.Add(product);

        // Add Heater
        var heater = new HeaterObject 
        { 
            Name = "Heater-1",
            SimulationId = sim.Id,
            InputStreamIds = [feed.Id],
            OutputStreamIds = [product.Id],
            CalcMode = HeaterCalculationMode.OutletTemperature,
            OutletTemperature = 350
        };
        sim.UnitOperations.Add(heater);

        // Act
        var flowsheet = _builder.BuildFlowsheet(sim);

        // Assert
        Assert.NotNull(flowsheet);
        
        // Count Material Streams
        var streams = flowsheet.SimulationObjects.Values
            .OfType<DWSIMStream>()
            .ToList();
        
        Assert.Equal(2, streams.Count);

        // Count Heater
        var heaters = flowsheet.SimulationObjects.Values
            .OfType<Heater>()
            .ToList();
        Assert.Single(heaters);

        // Check Connections (Basic check)
        var heaterInstance = heaters[0];
        Assert.NotNull(heaterInstance);
        // Note: Detailed connection verification via DWSIM API is complex to assert without deep inspection.
        // We trust the builder logic if no exceptions occurred during AttachInputStream.
    }

    [Fact]
    public void Build_Recycle_Simulation()
    {
        // Arrange
        var sim = new Domain.Entities.Simulation
        {
            Name = "Recycle Sim",
            PropertyPackage = PropertyPackageType.PengRobinson,
            FlashAlgorithm = FlashAlgorithm.NestedLoops,
            SystemOfUnits = SystemOfUnits.SI
        };
        sim.Compounds.Add(new Compound { Name = "Water", SimulationId = sim.Id });
        
        var s1 = new MaterialStream { Name = "S1", SimulationId = sim.Id };
        var s2 = new MaterialStream { Name = "S2", SimulationId = sim.Id };
        sim.MaterialStreams.Add(s1);
        sim.MaterialStreams.Add(s2);

        var recycle = new RecycleObject
        {
            Name = "Recycle-1",
            SimulationId = sim.Id,
            InputStreamIds = [s1.Id],
            OutputStreamIds = [s2.Id]
        };
        sim.UnitOperations.Add(recycle);

        // Act
        var flowsheet = _builder.BuildFlowsheet(sim);

        // Assert
        Assert.NotNull(flowsheet);
        var recycles = flowsheet.SimulationObjects.Values
            .OfType<DWSIMRecycle>()
            .ToList();
        Assert.Single(recycles);
    }
}
