using DWSIM.Interfaces.Enums;
using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.Thermodynamics.PropertyPackages;
using DWSIM.Thermodynamics.Streams;

namespace Enerflow.Tests.DWSIM.Scenarios;

/// <summary>
/// Test 04: Ethanol/Water VLE - Property Package Comparison
/// Scenario: Compute bubble point of 50/50 Ethanol/Water mixture using NRTL vs Peng-Robinson.
/// Tests: Property package selection, VLE calculations, non-ideal behavior verification.
/// </summary>
public class Test04EthanolWaterVle : TestBase
{
    [Fact]
    public void EthanolWater_ComparePropertyPackages_ResultsDiffer()
    {
        // --------------------------------------------------------------------------------
        // PART 1: NRTL (Recommended for Ethanol/Water)
        // --------------------------------------------------------------------------------
        Logger.Information("TEST 04: Ethanol/Water VLE - Property Package Comparison");
        Logger.Information("========================================");
        Logger.Information("PART 1: Simulation with NRTL (Non-Random Two-Liquid)");

        var flowsheetNRTL = Automation.CreateFlowsheet();
        Assert.NotNull(flowsheetNRTL);

        // Add compounds
        flowsheetNRTL.SelectedCompounds.Add("Ethanol", flowsheetNRTL.AvailableCompounds["Ethanol"]);
        flowsheetNRTL.SelectedCompounds.Add("Water", flowsheetNRTL.AvailableCompounds["Water"]);
        Logger.Information("Added compounds: Ethanol, Water");

        // Set SI units
        var siUnits = flowsheetNRTL.AvailableSystemsOfUnits.First(u => u.Name.Contains("SI"));
        flowsheetNRTL.FlowsheetOptions.SelectedUnitSystem = siUnits;

        // Add NRTL Property Package
        var nrtl = new NRTLPropertyPackage();
        flowsheetNRTL.AddPropertyPackage(nrtl);
        Logger.Information("Property Package chosen: NRTL");

        // Create Stream
        var streamNRTL = flowsheetNRTL.AddObject(ObjectType.MaterialStream, 100, 100, "Feed_NRTL") as MaterialStream;
        Assert.NotNull(streamNRTL);

        // Conditions: 1 bar, 50/50 molar
        streamNRTL.Phases[0].Properties.pressure = 101325; // 1 atm
        streamNRTL.Phases[0].Properties.massflow = 1.0;
        streamNRTL.Phases[0].Compounds["Ethanol"].MoleFraction = 0.5;
        streamNRTL.Phases[0].Compounds["Water"].MoleFraction = 0.5;

        // Specify Vapor Fraction = 0 to find Bubble Point Temperature
        streamNRTL.SpecType = StreamSpec.Pressure_and_VaporFraction; // P-VF specification
        streamNRTL.Phases[0].Properties.molarfraction = 0.0;

        Logger.Information("Stream Config: 1 bar, VF=0 (Bubble Point), 50/50 mol%");

        // Solve NRTL
        Automation.CalculateFlowsheet2(flowsheetNRTL);
        AssertConverged(flowsheetNRTL);

        // Get Bubble Point Temperature (NRTL)
        double tempNRTL = streamNRTL.Phases[0].Properties.temperature ?? 0;
        Logger.Information("NRTL Results:");
        Logger.Information("  Bubble Point Temperature: {Temp:F2} K ({TempC:F2} °C)", tempNRTL, tempNRTL - 273.15);


        // --------------------------------------------------------------------------------
        // PART 2: Peng-Robinson (Equation of State - Less accurate for this system)
        // --------------------------------------------------------------------------------
        Logger.Information("========================================");
        Logger.Information("PART 2: Simulation with Peng-Robinson");

        var flowsheetPR = Automation.CreateFlowsheet();
        Assert.NotNull(flowsheetPR);

        flowsheetPR.SelectedCompounds.Add("Ethanol", flowsheetPR.AvailableCompounds["Ethanol"]);
        flowsheetPR.SelectedCompounds.Add("Water", flowsheetPR.AvailableCompounds["Water"]);
        flowsheetPR.FlowsheetOptions.SelectedUnitSystem = siUnits;

        var pr = new PengRobinsonPropertyPackage();
        flowsheetPR.AddPropertyPackage(pr);
        Logger.Information("Property Package chosen: Peng-Robinson");

        var streamPR = flowsheetPR.AddObject(ObjectType.MaterialStream, 100, 100, "Feed_PR") as MaterialStream;
        Assert.NotNull(streamPR);

        streamPR.Phases[0].Properties.pressure = 101325;
        streamPR.Phases[0].Properties.massflow = 1.0;
        streamPR.Phases[0].Compounds["Ethanol"].MoleFraction = 0.5;
        streamPR.Phases[0].Compounds["Water"].MoleFraction = 0.5;
        streamPR.SpecType = StreamSpec.Pressure_and_VaporFraction;
        streamPR.Phases[0].Properties.molarfraction = 0.0;

        // Solve PR
        Automation.CalculateFlowsheet2(flowsheetPR);
        AssertConverged(flowsheetPR);

        double tempPR = streamPR.Phases[0].Properties.temperature ?? 0;
        Logger.Information("Peng-Robinson Results:");
        Logger.Information("  Bubble Point Temperature: {Temp:F2} K ({TempC:F2} °C)", tempPR, tempPR - 273.15);

        // --------------------------------------------------------------------------------
        // COMPARISON & ASSERTIONS
        // --------------------------------------------------------------------------------
        Logger.Information("========================================");
        Logger.Information("Comparison:");
        Logger.Information("  NRTL T_bubble: {T1:F2} K", tempNRTL);
        Logger.Information("  PR   T_bubble: {T2:F2} K", tempPR);
        Logger.Information("  Difference:    {Diff:F2} K", Math.Abs(tempNRTL - tempPR));

        // Expect significant difference for Ethanol/Water azeotropic system
        // NRTL handles the non-ideality/polarity better. PR treats it more ideally.
        Assert.True(Math.Abs(tempNRTL - tempPR) > 0.5, "Expected significant difference between NRTL and PR for Ethanol/Water mixture");

        Logger.Information("✓ Confirmed property packages yield different results for non-ideal mixture");
        Logger.Information("========================================");
    }
}
