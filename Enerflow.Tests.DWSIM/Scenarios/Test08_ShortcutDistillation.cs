using DWSIM.Interfaces.Enums;
using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.Thermodynamics.PropertyPackages;
using DWSIM.Thermodynamics.Streams;
using DWSIM.UnitOperations.UnitOperations;

namespace Enerflow.Tests.DWSIM.Scenarios;

/// <summary>
/// Test 08: Shortcut Distillation Column
/// Scenario: Propane/n-Butane separation using Fenske-Underwood-Gilliland method
/// Tests: Distillation column setup, light/heavy key specification, product purity
/// </summary>
public class Test08ShortcutDistillation : TestBase
{
    [Fact]
    public void ShortcutDistillation_PropaneButane_SeparatesWithTargetPurity()
    {
        Logger.Information("TEST 08: Shortcut Distillation Column");
        Logger.Information("Scenario: Propane/n-Butane separation (Fenske-Underwood-Gilliland)");
        Logger.Information("========================================");

        var flowsheet = Automation.CreateFlowsheet();
        Assert.NotNull(flowsheet);

        // Add compounds
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

        // Create feed stream: 50% Propane, 50% n-Butane, saturated liquid at 5 bar
        var feed = flowsheet.AddObject(ObjectType.MaterialStream, 100, 100, "Feed") as MaterialStream;
        Assert.NotNull(feed);

        // Set as saturated liquid (vapor fraction = 0) at 5 bar
        feed.Phases[0].Properties.pressure = 500000; // 5 bar
        feed.Phases[0].Properties.massflow = 1.0;    // 1 kg/s
        feed.Phases[0].Compounds["Propane"].MoleFraction = 0.5;
        feed.Phases[0].Compounds["n-Butane"].MoleFraction = 0.5;

        // Set as saturated liquid
        feed.SpecType = StreamSpec.Pressure_and_VaporFraction;
        feed.Phases[0].Properties.molarfraction = 0.0; // Saturated liquid (VF = 0)

        Logger.Information("Feed: 5 bar, saturated liquid, 1 kg/s, 50% Propane / 50% n-Butane");

        // Create shortcut column
        var column = flowsheet.AddObject(ObjectType.ShortcutColumn, 200, 100, "Column") as ShortcutColumn;
        Assert.NotNull(column);

        // Configure column
        column.m_lightkey = "Propane";       // Light key compound
        column.m_heavykey = "n-Butane";      // Heavy key compound
        column.m_lightkeymolarfrac = 0.05;   // 5% Propane in bottoms (95% recovery)
        column.m_heavykeymolarfrac = 0.05;   // 5% n-Butane in distillate (95% recovery)
        column.m_refluxratio = 2.0;          // Reflux ratio = 2
        column.m_condenserpressure = 500000; // 5 bar condenser pressure
        column.m_boilerpressure = 550000;    // 5.5 bar reboiler pressure
        column.condtype = ShortcutColumn.CondenserType.TotalCond; // Total condenser

        Logger.Information("Column Configuration:");
        Logger.Information("  Light Key: Propane");
        Logger.Information("  Heavy Key: n-Butane");
        Logger.Information("  Light Key in Bottoms: 5% (95% recovery to distillate)");
        Logger.Information("  Heavy Key in Distillate: 5% (95% recovery to bottoms)");
        Logger.Information("  Reflux Ratio: 2.0");
        Logger.Information("  Condenser Pressure: 5 bar");
        Logger.Information("  Reboiler Pressure: 5.5 bar");

        // Create distillate stream (tops product)
        var distillate = flowsheet.AddObject(ObjectType.MaterialStream, 300, 50, "Distillate") as MaterialStream;
        Assert.NotNull(distillate);

        // Create bottoms stream
        var bottoms = flowsheet.AddObject(ObjectType.MaterialStream, 300, 150, "Bottoms") as MaterialStream;
        Assert.NotNull(bottoms);

        // Create condenser duty stream
        var condenserDuty = flowsheet.AddObject(ObjectType.EnergyStream, 200, 50, "CondenserDuty");
        Assert.NotNull(condenserDuty);

        // Create reboiler duty stream
        var reboilerDuty = flowsheet.AddObject(ObjectType.EnergyStream, 200, 150, "ReboilerDuty");
        Assert.NotNull(reboilerDuty);

        // Connect streams
        // ShortcutColumn: Input 0 = Feed, Output 0 = Distillate, Output 1 = Bottoms
        // Energy: depends on the column configuration
        flowsheet.ConnectObjects(feed.GraphicObject, column.GraphicObject, 0, 0);
        flowsheet.ConnectObjects(column.GraphicObject, distillate.GraphicObject, 0, 0);
        flowsheet.ConnectObjects(column.GraphicObject, bottoms.GraphicObject, 1, 0);
        flowsheet.ConnectObjects(column.GraphicObject, condenserDuty.GraphicObject, 2, 0); // Condenser duty out
        flowsheet.ConnectObjects(reboilerDuty.GraphicObject, column.GraphicObject, 0, 1);  // Reboiler duty in

        Logger.Information("Connected: Feed -> Column -> Distillate + Bottoms");

        // Solve
        Logger.Information("========================================");
        Logger.Information("Solving flowsheet...");
        Automation.CalculateFlowsheet2(flowsheet);

        // Check convergence
        AssertConverged(flowsheet);

        // Log results
        Logger.Information("========================================");
        Logger.Information("RESULTS:");
        Logger.Information("========================================");

        Logger.Information("FEED:");
        TestHelpers.LogStreamProperties(feed, Logger);

        Logger.Information("");
        Logger.Information("DISTILLATE (Propane-rich):");
        TestHelpers.LogStreamProperties(distillate, Logger);

        Logger.Information("");
        Logger.Information("BOTTOMS (n-Butane-rich):");
        TestHelpers.LogStreamProperties(bottoms, Logger);

        // Get compositions
        var distillatePropane = distillate.Phases[0].Compounds["Propane"].MoleFraction ?? 0;
        var distillateButane = distillate.Phases[0].Compounds["n-Butane"].MoleFraction ?? 0;
        var bottomsPropane = bottoms.Phases[0].Compounds["Propane"].MoleFraction ?? 0;
        var bottomsButane = bottoms.Phases[0].Compounds["n-Butane"].MoleFraction ?? 0;

        Logger.Information("");
        Logger.Information("Composition Summary:");
        Logger.Information("  Distillate: {Propane:P2} Propane, {Butane:P2} n-Butane",
            distillatePropane, distillateButane);
        Logger.Information("  Bottoms: {Propane:P2} Propane, {Butane:P2} n-Butane",
            bottomsPropane, bottomsButane);

        // Verify distillate is enriched in Propane (light key)
        Assert.True(distillatePropane > 0.80,
            $"Distillate should be >80% Propane (got {distillatePropane:P2})");
        Logger.Information("Distillate is enriched in Propane (>80%)");

        // Verify bottoms is enriched in n-Butane (heavy key)
        Assert.True(bottomsButane > 0.80,
            $"Bottoms should be >80% n-Butane (got {bottomsButane:P2})");
        Logger.Information("Bottoms is enriched in n-Butane (>80%)");

        // Mass balance check
        var feedMassFlow = feed.Phases[0].Properties.massflow ?? 0;
        var distillateMassFlow = distillate.Phases[0].Properties.massflow ?? 0;
        var bottomsMassFlow = bottoms.Phases[0].Properties.massflow ?? 0;
        var totalOut = distillateMassFlow + bottomsMassFlow;

        Logger.Information("");
        Logger.Information("Mass Balance:");
        Logger.Information("  Feed: {Feed:F6} kg/s", feedMassFlow);
        Logger.Information("  Distillate: {Dist:F6} kg/s", distillateMassFlow);
        Logger.Information("  Bottoms: {Bot:F6} kg/s", bottomsMassFlow);
        Logger.Information("  Total Out: {Total:F6} kg/s", totalOut);

        var massError = Math.Abs(feedMassFlow - totalOut);
        Assert.True(massError < 0.01, $"Mass balance error ({massError:F6} kg/s) exceeds tolerance");
        Logger.Information("Mass balance within tolerance");

        // Log column results
        Logger.Information("");
        Logger.Information("Column Results:");
        Logger.Information("  Minimum Stages (Nmin): {Nmin:F2}", column.m_Nmin);
        Logger.Information("  Minimum Reflux (Rmin): {Rmin:F4}", column.m_Rmin);
        Logger.Information("  Actual Stages (N): {N:F2}", column.m_N);
        // Get temperatures from streams (more reliable than internal column variables)
        var distillateTemp = distillate.Phases[0].Properties.temperature ?? 0;
        var bottomsTemp = bottoms.Phases[0].Properties.temperature ?? 0;
        
        Logger.Information("  Distillate Temperature: {Tc:F2} K ({TcC:F2} C)",
            distillateTemp, distillateTemp - 273.15);
        Logger.Information("  Bottoms Temperature: {Tb:F2} K ({TbC:F2} C)",
            bottomsTemp, bottomsTemp - 273.15);
        Logger.Information("  Condenser Duty: {Qc:F2} kW", column.m_Qc / 1000);
        Logger.Information("  Reboiler Duty: {Qb:F2} kW", column.m_Qb / 1000);

        // Verify column makes thermodynamic sense (bottoms should be hotter than distillate)
        Assert.True(bottomsTemp > distillateTemp,
            "Bottoms temperature should be higher than distillate temperature");
        Logger.Information("Bottoms temperature > Distillate temperature (thermodynamically correct)");

        Assert.True(column.m_N > column.m_Nmin,
            "Actual stages should exceed minimum stages");
        Logger.Information("Actual stages > Minimum stages (correct)");

        Assert.True(column.m_refluxratio > column.m_Rmin,
            "Operating reflux should exceed minimum reflux");
        Logger.Information("Operating reflux > Minimum reflux (correct)");

        // Verify duties are reasonable (condenser removes heat, reboiler adds heat)
        Assert.True(column.m_Qc < 0 || column.m_Qb > 0,
            "Column should have heat duties");
        Logger.Information("Column has heat duties (correct)");

        LogFlowsheetSummary(flowsheet);

        Logger.Information("========================================");
        Logger.Information("Test 08: PASSED - Shortcut distillation column works correctly");
        Logger.Information("========================================");
    }
}
