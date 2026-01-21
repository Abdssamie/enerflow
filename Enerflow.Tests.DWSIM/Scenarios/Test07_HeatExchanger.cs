using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.Thermodynamics.PropertyPackages;
using DWSIM.Thermodynamics.Streams;
using DWSIM.UnitOperations.UnitOperations;

namespace Enerflow.Tests.DWSIM.Scenarios;

/// <summary>
/// Test 07: Heat Exchanger
/// Scenario: Counter-current heat exchange between hot and cold methane streams
/// Tests: Heat exchanger calculation modes, energy balance, approach temperature
/// </summary>
public class Test07HeatExchanger : TestBase
{
    [Fact]
    public void HeatExchanger_CounterCurrent_EnergyBalanceHolds()
    {
        Logger.Information("TEST 07: Heat Exchanger");
        Logger.Information("Scenario: Counter-current heat exchange between hot and cold methane streams");
        Logger.Information("========================================");

        var flowsheet = Automation.CreateFlowsheet();
        Assert.NotNull(flowsheet);

        // Add Methane compound
        flowsheet.SelectedCompounds.Add("Methane", flowsheet.AvailableCompounds["Methane"]);
        Logger.Information("Added compound: Methane");

        // Set SI units
        var siUnits = flowsheet.AvailableSystemsOfUnits.First(u => u.Name.Contains("SI"));
        flowsheet.FlowsheetOptions.SelectedUnitSystem = siUnits;

        // Add Peng-Robinson property package
        var pr = new PengRobinsonPropertyPackage();
        flowsheet.AddPropertyPackage(pr);
        Logger.Information("Property Package: Peng-Robinson");

        // Create hot stream inlet: 400 K, 10 bar, 1 kg/s
        var hotIn = flowsheet.AddObject(ObjectType.MaterialStream, 100, 50, "HotIn") as MaterialStream;
        Assert.NotNull(hotIn);
        hotIn.Phases[0].Properties.temperature = 400; // 400 K
        hotIn.Phases[0].Properties.pressure = 1000000; // 10 bar
        hotIn.Phases[0].Properties.massflow = 1.0;
        hotIn.Phases[0].Compounds["Methane"].MoleFraction = 1.0;
        Logger.Information("Hot Inlet: 400 K (127C), 10 bar, 1 kg/s Methane");

        // Create cold stream inlet: 300 K, 10 bar, 1 kg/s
        var coldIn = flowsheet.AddObject(ObjectType.MaterialStream, 100, 150, "ColdIn") as MaterialStream;
        Assert.NotNull(coldIn);
        coldIn.Phases[0].Properties.temperature = 300; // 300 K
        coldIn.Phases[0].Properties.pressure = 1000000; // 10 bar
        coldIn.Phases[0].Properties.massflow = 1.0;
        coldIn.Phases[0].Compounds["Methane"].MoleFraction = 1.0;
        Logger.Information("Cold Inlet: 300 K (27C), 10 bar, 1 kg/s Methane");

        // Create heat exchanger
        var hx = flowsheet.AddObject(ObjectType.HeatExchanger, 200, 100, "HeatExchanger") as HeatExchanger;
        Assert.NotNull(hx);

        // Set calculation mode: Calculate both outlet temperatures given UA
        // Using CalcBothTemp_UA mode - requires specifying UA (Overall Heat Transfer Coefficient * Area)
        hx.CalculationMode = HeatExchangerCalcMode.CalcBothTemp_UA;
        hx.OverallCoefficient = 500; // W/(m2.K)
        hx.Area = 10; // m2
        hx.FlowDir = FlowDirection.CounterCurrent;
        Logger.Information("Heat Exchanger: CalcBothTemp_UA mode, U=500 W/(m2.K), A=10 m2, Counter-current");

        // Create outlet streams
        var hotOut = flowsheet.AddObject(ObjectType.MaterialStream, 300, 50, "HotOut") as MaterialStream;
        Assert.NotNull(hotOut);

        var coldOut = flowsheet.AddObject(ObjectType.MaterialStream, 300, 150, "ColdOut") as MaterialStream;
        Assert.NotNull(coldOut);

        // Connect streams to heat exchanger
        // Heat exchanger connections: 
        // Input 0: Hot inlet, Input 1: Cold inlet
        // Output 0: Hot outlet, Output 1: Cold outlet
        flowsheet.ConnectObjects(hotIn.GraphicObject, hx.GraphicObject, 0, 0);   // Hot in
        flowsheet.ConnectObjects(coldIn.GraphicObject, hx.GraphicObject, 0, 1);  // Cold in
        flowsheet.ConnectObjects(hx.GraphicObject, hotOut.GraphicObject, 0, 0);  // Hot out
        flowsheet.ConnectObjects(hx.GraphicObject, coldOut.GraphicObject, 1, 0); // Cold out
        Logger.Information("Connected: HotIn + ColdIn -> HX -> HotOut + ColdOut");

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

        Logger.Information("HOT INLET:");
        TestHelpers.LogStreamProperties(hotIn, Logger);

        Logger.Information("");
        Logger.Information("HOT OUTLET:");
        TestHelpers.LogStreamProperties(hotOut, Logger);

        Logger.Information("");
        Logger.Information("COLD INLET:");
        TestHelpers.LogStreamProperties(coldIn, Logger);

        Logger.Information("");
        Logger.Information("COLD OUTLET:");
        TestHelpers.LogStreamProperties(coldOut, Logger);

        // Get temperatures
        var hotInTemp = hotIn.Phases[0].Properties.temperature ?? 0;
        var hotOutTemp = hotOut.Phases[0].Properties.temperature ?? 0;
        var coldInTemp = coldIn.Phases[0].Properties.temperature ?? 0;
        var coldOutTemp = coldOut.Phases[0].Properties.temperature ?? 0;

        Logger.Information("");
        Logger.Information("Temperature Summary:");
        Logger.Information("  Hot Side:  {HotIn:F2} K -> {HotOut:F2} K (Delta = {DeltaHot:F2} K)",
            hotInTemp, hotOutTemp, hotInTemp - hotOutTemp);
        Logger.Information("  Cold Side: {ColdIn:F2} K -> {ColdOut:F2} K (Delta = {DeltaCold:F2} K)",
            coldInTemp, coldOutTemp, coldOutTemp - coldInTemp);

        // Verify hot stream cooled down
        Assert.True(hotOutTemp < hotInTemp,
            $"Hot outlet ({hotOutTemp:F2} K) should be cooler than inlet ({hotInTemp:F2} K)");
        Logger.Information("Hot stream temperature decreased (correct)");

        // Verify cold stream heated up
        Assert.True(coldOutTemp > coldInTemp,
            $"Cold outlet ({coldOutTemp:F2} K) should be warmer than inlet ({coldInTemp:F2} K)");
        Logger.Information("Cold stream temperature increased (correct)");

        // Verify no temperature cross (thermodynamic constraint)
        // For counter-current: Hot out should be >= Cold in, Cold out should be <= Hot in
        // (otherwise we'd have impossible heat transfer)
        Assert.True(hotOutTemp >= coldInTemp,
            $"Hot outlet ({hotOutTemp:F2} K) should be >= Cold inlet ({coldInTemp:F2} K)");
        Assert.True(coldOutTemp <= hotInTemp,
            $"Cold outlet ({coldOutTemp:F2} K) should be <= Hot inlet ({hotInTemp:F2} K)");
        Logger.Information("No temperature crossing (thermodynamically valid)");

        // Verify energy balance (Q_hot = Q_cold within tolerance)
        // For same fluid and same mass flow, temperature changes should be equal
        var deltaT_hot = hotInTemp - hotOutTemp;
        var deltaT_cold = coldOutTemp - coldInTemp;

        // Since both streams are same fluid (Methane) with same mass flow,
        // the temperature changes should be approximately equal
        var energyBalanceError = Math.Abs(deltaT_hot - deltaT_cold);
        Logger.Information("");
        Logger.Information("Energy Balance Check:");
        Logger.Information("  Hot side Delta T: {DeltaHot:F2} K", deltaT_hot);
        Logger.Information("  Cold side Delta T: {DeltaCold:F2} K", deltaT_cold);
        Logger.Information("  Difference: {Diff:F4} K", energyBalanceError);

        Assert.True(energyBalanceError < 5.0,
            $"Energy balance error ({energyBalanceError:F4} K) exceeds tolerance");
        Logger.Information("Energy balance within tolerance");

        // Log heat exchanger properties
        Logger.Information("");
        Logger.Information("Heat Exchanger Properties:");
        Logger.Information("  Heat Duty (Q): {Q:F2} kW", hx.Q ?? 0);
        Logger.Information("  UA: {UA:F2} W/K", (hx.OverallCoefficient ?? 0) * (hx.Area ?? 0));
        Logger.Information("  LMTD: {LMTD:F2} K", hx.LMTD);

        // Verify heat duty is positive (heat transferred)
        var heatDuty = hx.Q ?? 0;
        Assert.True(heatDuty > 0, "Heat duty should be positive");
        Logger.Information("Heat duty is positive ({Q:F2} kW)", heatDuty);

        // Verify approach temperatures are reasonable
        var hotApproach = hotOutTemp - coldInTemp;  // Counter-current hot end approach
        var coldApproach = hotInTemp - coldOutTemp; // Counter-current cold end approach

        Logger.Information("");
        Logger.Information("Approach Temperatures:");
        Logger.Information("  Hot end approach: {HotApproach:F2} K", hotApproach);
        Logger.Information("  Cold end approach: {ColdApproach:F2} K", coldApproach);

        Assert.True(hotApproach > 0, "Hot end approach must be positive (no temperature cross)");
        Assert.True(coldApproach > 0, "Cold end approach must be positive (no temperature cross)");
        Logger.Information("Approach temperatures are positive (valid heat exchanger design)");

        LogFlowsheetSummary(flowsheet);

        Logger.Information("========================================");
        Logger.Information("Test 07: PASSED - Heat exchanger energy balance verified");
        Logger.Information("========================================");
    }
}
