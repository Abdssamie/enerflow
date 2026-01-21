using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.Thermodynamics.PropertyPackages;
using DWSIM.Thermodynamics.Streams;
using DWSIM.UnitOperations.UnitOperations;

namespace Enerflow.Tests.DWSIM.Scenarios;

/// <summary>
/// Test 01: Simple Heating Process
/// Scenario: Feed (Methane @ 25°C, 1 bar) → Heater (+50°C) → Product (75°C)
/// Tests: Peng-Robinson property package, NestedLoops flash, temperature increase, energy duty
/// </summary>
public class Test01SimpleHeating : TestBase
{
    [Fact]
    public void SimpleHeating_MethaneStream_ConvergesSuccessfully()
    {
        // Arrange
        Logger.Information("TEST 01: Simple Heating Process");
        Logger.Information("Scenario: Methane stream heated from 25°C to 75°C");
        Logger.Information("========================================");

        var flowsheet = Automation.CreateFlowsheet();
        Logger.Information("Flowsheet created");

        // Add Methane compound
        var methane = flowsheet.AvailableCompounds["Methane"];
        flowsheet.SelectedCompounds.Add("Methane", methane);
        Logger.Information("Added compound: Methane");

        // Set SI units
        var siUnits = flowsheet.AvailableSystemsOfUnits.First(u => u.Name.Contains("SI"));
        flowsheet.FlowsheetOptions.SelectedUnitSystem = siUnits;
        Logger.Information("Set unit system: {Units}", siUnits.Name);

        // Add Peng-Robinson property package
        var pr = new PengRobinsonPropertyPackage();
        flowsheet.AddPropertyPackage(pr);
        Logger.Information("Property Package: Peng-Robinson");

        // Create inlet stream (Feed)
        var feed = flowsheet.AddObject(ObjectType.MaterialStream, 100, 100, "Feed") as MaterialStream;
        Assert.NotNull(feed);
        flowsheet.AddCompoundsToMaterialStream(feed);

        // Set feed conditions: 25°C (298.15 K), 1 bar (101325 Pa), 1 kg/s of pure methane
        feed.Phases[0].Properties.temperature = 298.15; // 25°C
        feed.Phases[0].Properties.pressure = 101325;     // 1 bar
        feed.Phases[0].Properties.massflow = 1.0;        // 1 kg/s
        feed.Phases[0].Compounds["Methane"].MoleFraction = 1.0; // Pure methane

        Logger.Information("Created feed stream:");
        Logger.Information("  Temperature: 25°C (298.15 K)");
        Logger.Information("  Pressure: 1 bar (101325 Pa)");
        Logger.Information("  Mass Flow: 1 kg/s");
        Logger.Information("  Composition: 100% Methane");

        // Create heater
        var heater = flowsheet.AddObject(ObjectType.Heater, 200, 100, "Heater") as Heater;
        Assert.NotNull(heater);
        heater.CalcMode = Heater.CalculationMode.OutletTemperature;
        heater.OutletTemperature = 348.15; // 75°C
        Logger.Information("Created Heater: Outlet Temperature = 75°C (348.15 K)");

        // Create outlet stream (Product)
        var product = flowsheet.AddObject(ObjectType.MaterialStream, 300, 100, "Product") as MaterialStream;
        Assert.NotNull(product);
        flowsheet.AddCompoundsToMaterialStream(product);
        Logger.Information("Created product stream");

        // Connect: Feed → Heater → Product
        flowsheet.ConnectObjects(feed.GraphicObject, heater.GraphicObject, 0, 0);
        flowsheet.ConnectObjects(heater.GraphicObject, product.GraphicObject, 0, 0);
        Logger.Information("Connected: Feed → Heater → Product");

        // Act
        Logger.Information("========================================");
        Logger.Information("Solving flowsheet...");
        var errors = Automation.CalculateFlowsheet2(flowsheet);

        if (errors != null && errors.Any())
        {
            Logger.Warning("Calculation returned {Count} errors/warnings:", errors.Count);
            foreach (var error in errors)
            {
                Logger.Warning("  {Error}", error.Message);
            }
        }

        // Assert
        AssertConverged(flowsheet);

        Logger.Information("========================================");
        Logger.Information("RESULTS:");
        Logger.Information("========================================");

        Logger.Information("FEED STREAM:");
        TestHelpers.LogStreamProperties(feed, Logger);

        Logger.Information("");
        Logger.Information("PRODUCT STREAM:");
        TestHelpers.LogStreamProperties(product, Logger);

        Logger.Information("");
        Logger.Information("HEATER:");
        Logger.Information("  Heat Duty: {Duty:F2} kW", heater.DeltaQ ?? 0);
        Logger.Information("  Calculated: {Calculated}", heater.Calculated);

        // Verify temperature increased
        var productTemp = product.Phases[0].Properties.temperature ?? 0;
        TestHelpers.AssertTemperatureInRange(product, 347, 349, Logger); // 75°C ± small tolerance

        // Verify pressure is approximately the same (heater should not change pressure significantly)
        var productPressure = product.Phases[0].Properties.pressure ?? 0;
        TestHelpers.AssertPressureInRange(product, 90000, 110000, Logger); // ~1 bar ± tolerance

        // Verify mass flow is conserved
        var productMassFlow = product.Phases[0].Properties.massflow ?? 0;
        Assert.InRange(productMassFlow, 0.99, 1.01); // 1 kg/s ± 1%
        Logger.Information("✓ Mass flow conserved: {MassFlow:F4} kg/s", productMassFlow);

        // Verify heater duty is positive (heat added)
        var heatDuty = heater.DeltaQ ?? 0;
        Assert.True(heatDuty > 0, "Heat duty should be positive (energy added)");
        Logger.Information("✓ Heat duty is positive: {Duty:F2} kW", heatDuty);

        LogFlowsheetSummary(flowsheet);

        Logger.Information("========================================");
        Logger.Information("Test 01: PASSED ✓");
        Logger.Information("========================================");
    }
}
