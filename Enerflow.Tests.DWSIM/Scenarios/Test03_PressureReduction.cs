using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.Thermodynamics.PropertyPackages;
using DWSIM.Thermodynamics.Streams;
using DWSIM.UnitOperations.UnitOperations;

namespace Enerflow.Tests.DWSIM.Scenarios;

/// <summary>
/// Test 03: Pressure Reduction and Phase Separation
/// Scenario: High-pressure Propane/nButane (20 bar, 40°C) → Valve (→ 5 bar) → Separator → Vapor + Liquid
/// Tests: Peng-Robinson, isenthalpic expansion, two-phase flash separation
/// </summary>
public class Test03PressureReduction : TestBase
{
    [Fact]
    public void PressureReduction_PropaneButaneMixture_ConvergesSuccessfully()
    {
        // Arrange
        Logger.Information("TEST 03: Pressure Reduction and Phase Separation");
        Logger.Information("Scenario: Propane/n-Butane (20 bar, 40°C) → Valve (5 bar) → Separator");
        Logger.Information("========================================");

        var flowsheet = Automation.CreateFlowsheet();

        // Add compounds: Propane and n-Butane
        flowsheet.SelectedCompounds.Add("Propane", flowsheet.AvailableCompounds["Propane"]);
        flowsheet.SelectedCompounds.Add("n-Butane", flowsheet.AvailableCompounds["n-Butane"]);
        Logger.Information("Added compounds: Propane, n-Butane");

        // Set SI units
        var siUnits = flowsheet.AvailableSystemsOfUnits.First(u => u.Name.Contains("SI"));
        flowsheet.FlowsheetOptions.SelectedUnitSystem = siUnits;

        // Add Peng-Robinson property package
        var pr = new PengRobinsonPropertyPackage();
        flowsheet.AddPropertyPackage(pr);
        Logger.Information("Property Package: Peng-Robinson");

        // Create high-pressure feed (20 bar, 40°C, 50/50 mol% Propane/n-Butane)
        var feed = flowsheet.AddObject(ObjectType.MaterialStream, 100, 100, "Feed") as MaterialStream;
        Assert.NotNull(feed);
        flowsheet.AddCompoundsToMaterialStream(feed);

        feed.Phases[0].Properties.temperature = 313.15; // 40°C
        feed.Phases[0].Properties.pressure = 2000000;   // 20 bar
        feed.Phases[0].Properties.massflow = 1.0;       // 1 kg/s

        feed.Phases[0].Compounds["Propane"].MoleFraction = 0.5;
        feed.Phases[0].Compounds["n-Butane"].MoleFraction = 0.5;

        Logger.Information("Feed: 20 bar, 40°C, 1 kg/s, 50% Propane / 50% n-Butane");

        // Create valve (pressure reduction from 20 bar to 5 bar)
        var valve = flowsheet.AddObject(ObjectType.Valve, 200, 100, "Valve") as Valve;
        Assert.NotNull(valve);
        valve.OutletPressure = 500000; // 5 bar
        Logger.Information("Valve: 20 bar → 5 bar (isenthalpic expansion)");

        // Create stream after valve (will be two-phase)
        var flashed = flowsheet.AddObject(ObjectType.MaterialStream, 300, 100, "Flashed") as MaterialStream;
        Assert.NotNull(flashed);
        flowsheet.AddCompoundsToMaterialStream(flashed);

        // Create separator (flash vessel)
        var separator = flowsheet.AddObject(ObjectType.Vessel, 400, 100, "Separator") as Vessel;
        Assert.NotNull(separator);
        // Flash at the outlet pressure and let DWSIM calculate the phases
        Logger.Information("Separator: Two-phase flash separation");

        // Create vapor outlet
        var vapor = flowsheet.AddObject(ObjectType.MaterialStream, 500, 50, "Vapor") as MaterialStream;
        Assert.NotNull(vapor);
        flowsheet.AddCompoundsToMaterialStream(vapor);

        // Create liquid outlet
        var liquid = flowsheet.AddObject(ObjectType.MaterialStream, 500, 150, "Liquid") as MaterialStream;
        Assert.NotNull(liquid);
        flowsheet.AddCompoundsToMaterialStream(liquid);

        // Connect: Feed → Valve → Flashed → Separator → Vapor + Liquid
        flowsheet.ConnectObjects(feed.GraphicObject, valve.GraphicObject, 0, 0);
        flowsheet.ConnectObjects(valve.GraphicObject, flashed.GraphicObject, 0, 0);
        flowsheet.ConnectObjects(flashed.GraphicObject, separator.GraphicObject, 0, 0);
        flowsheet.ConnectObjects(separator.GraphicObject, vapor.GraphicObject, 0, 0);  // Vapor outlet
        flowsheet.ConnectObjects(separator.GraphicObject, liquid.GraphicObject, 1, 0); // Liquid outlet
        Logger.Information("Connected flowsheet");

        // Act
        Logger.Information("========================================");
        Logger.Information("Solving flowsheet...");
        var errors = Automation.CalculateFlowsheet2(flowsheet);

        if (errors != null && errors.Any())
        {
            Logger.Warning("Calculation returned {Count} errors/warnings", errors.Count);
        }

        // Assert
        AssertConverged(flowsheet);

        Logger.Information("========================================");
        Logger.Information("RESULTS:");
        Logger.Information("========================================");

        Logger.Information("FEED (20 bar, 40°C):");
        TestHelpers.LogStreamProperties(feed, Logger);

        Logger.Information("");
        Logger.Information("FLASHED (5 bar, two-phase):");
        TestHelpers.LogStreamProperties(flashed, Logger);

        Logger.Information("");
        Logger.Information("VAPOR PRODUCT:");
        TestHelpers.LogStreamProperties(vapor, Logger);

        Logger.Information("");
        Logger.Information("LIQUID PRODUCT:");
        TestHelpers.LogStreamProperties(liquid, Logger);

        // Verify flashed stream has reduced pressure
        TestHelpers.AssertPressureInRange(flashed, 450000, 550000, Logger); // ~5 bar

        // Verify temperature dropped due to flash
        var flashedTemp = flashed.Phases[0].Properties.temperature ?? 0;
        Assert.True(flashedTemp < 313.15, "Temperature should drop during flash");
        Logger.Information("✓ Flash caused temperature drop to {Temp:F2} K ({TempC:F2} °C)",
            flashedTemp, flashedTemp - 273.15);

        // Verify vapor and liquid have same pressure and temperature
        var vaporPress = vapor.Phases[0].Properties.pressure ?? 0;
        var liquidPress = liquid.Phases[0].Properties.pressure ?? 0;
        Assert.InRange(Math.Abs(vaporPress - liquidPress), 0, 1000); // Should be approximately equal
        Logger.Information("✓ Vapor and liquid at same pressure: {Press:F0} Pa", vaporPress);

        // Verify we have both vapor and liquid (not 100% of either)
        var vaporFlow = vapor.Phases[0].Properties.massflow ?? 0;
        var liquidFlow = liquid.Phases[0].Properties.massflow ?? 0;

        Assert.True(vaporFlow > 0.01, "Should have significant vapor flow");
        Assert.True(liquidFlow > 0.01, "Should have significant liquid flow");
        Logger.Information("✓ Phase split: {Vapor:P1} vapor, {Liquid:P1} liquid",
            vaporFlow / (vaporFlow + liquidFlow),
            liquidFlow / (vaporFlow + liquidFlow));

        // Verify mass balance
        var totalOut = vaporFlow + liquidFlow;
        var feedFlow = feed.Phases[0].Properties.massflow ?? 0;
        Assert.InRange(totalOut, feedFlow * 0.99, feedFlow * 1.01);
        Logger.Information("✓ Mass balance: In = {In:F4} kg/s, Out = {Out:F4} kg/s", feedFlow, totalOut);

        // Verify composition difference (vapor should be richer in lighter component - Propane)
        var vaporPropane = vapor.Phases[0].Compounds["Propane"].MoleFraction ?? 0;
        var liquidPropane = liquid.Phases[0].Compounds["Propane"].MoleFraction ?? 0;

        Assert.True(vaporPropane > liquidPropane, "Vapor should be enriched in lighter component (Propane)");
        Logger.Information("✓ Propane enrichment in vapor: Vapor = {VaporProp:P2}, Liquid = {LiquidProp:P2}",
            vaporPropane, liquidPropane);

        LogFlowsheetSummary(flowsheet);

        Logger.Information("========================================");
        Logger.Information("Test 03: PASSED ✓");
        Logger.Information("========================================");
    }
}
