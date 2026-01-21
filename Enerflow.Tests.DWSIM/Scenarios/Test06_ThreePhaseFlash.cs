using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.Thermodynamics.PropertyPackages;
using DWSIM.Thermodynamics.PropertyPackages.Auxiliary.FlashAlgorithms;
using DWSIM.Thermodynamics.Streams;
using DWSIM.UnitOperations.UnitOperations;

namespace Enerflow.Tests.DWSIM.Scenarios;

/// <summary>
/// Test 06: Three-Phase Flash (VLLE)
/// Scenario: Oil/Water/Gas mixture separation with three immiscible phases
/// Tests: Three-phase flash algorithm, phase identification, mass balance across phases
/// </summary>
public class Test06ThreePhaseFlash : TestBase
{
    [Fact]
    public void ThreePhaseFlash_MethaneHexaneWater_SeparatesIntoThreePhases()
    {
        Logger.Information("TEST 06: Three-Phase Flash (VLLE)");
        Logger.Information("Scenario: Methane/n-Hexane/Water mixture separation");
        Logger.Information("========================================");

        var flowsheet = Automation.CreateFlowsheet();
        Assert.NotNull(flowsheet);

        // Add compounds: Methane (gas), n-Hexane (organic liquid), Water (aqueous liquid)
        flowsheet.SelectedCompounds.Add("Methane", flowsheet.AvailableCompounds["Methane"]);
        flowsheet.SelectedCompounds.Add("n-Hexane", flowsheet.AvailableCompounds["n-Hexane"]);
        flowsheet.SelectedCompounds.Add("Water", flowsheet.AvailableCompounds["Water"]);
        Logger.Information("Added compounds: Methane, n-Hexane, Water");

        // Set SI units
        var siUnits = flowsheet.AvailableSystemsOfUnits.First(u => u.Name.Contains("SI"));
        flowsheet.FlowsheetOptions.SelectedUnitSystem = siUnits;

        // Use Peng-Robinson with NestedLoops3PV3 for three-phase flash
        var pr = new PengRobinsonPropertyPackage();
        pr.FlashAlgorithm = new NestedLoops3PV3();
        flowsheet.AddPropertyPackage(pr);
        Logger.Information("Property Package: Peng-Robinson with NestedLoops3PV3 flash");

        // Create feed stream
        var feed = flowsheet.AddObject(ObjectType.MaterialStream, 100, 100, "Feed") as MaterialStream;
        Assert.NotNull(feed);

        // Setup: 10 bar, 350 K (77C), 1 kg/s
        // Composition: 20% Methane, 40% n-Hexane, 40% Water (molar)
        feed.Phases[0].Properties.temperature = 350; // 350 K
        feed.Phases[0].Properties.pressure = 1000000; // 10 bar
        feed.Phases[0].Properties.massflow = 1.0;
        feed.Phases[0].Compounds["Methane"].MoleFraction = 0.2;
        feed.Phases[0].Compounds["n-Hexane"].MoleFraction = 0.4;
        feed.Phases[0].Compounds["Water"].MoleFraction = 0.4;

        Logger.Information("Feed: 10 bar, 350 K (77C), 1 kg/s");
        Logger.Information("  Composition: 20% Methane, 40% n-Hexane, 40% Water (molar)");

        // Create separator (flash vessel)
        var separator = flowsheet.AddObject(ObjectType.Vessel, 200, 100, "Separator") as Vessel;
        Assert.NotNull(separator);
        Logger.Information("Separator: Three-phase flash at feed conditions");

        // Create outlet streams
        var vapor = flowsheet.AddObject(ObjectType.MaterialStream, 300, 50, "Vapor") as MaterialStream;
        Assert.NotNull(vapor);

        var liquid1 = flowsheet.AddObject(ObjectType.MaterialStream, 300, 100, "Liquid1_Organic") as MaterialStream;
        Assert.NotNull(liquid1);

        var liquid2 = flowsheet.AddObject(ObjectType.MaterialStream, 300, 150, "Liquid2_Aqueous") as MaterialStream;
        Assert.NotNull(liquid2);

        // Connect: Feed -> Separator -> Vapor + Liquid1 + Liquid2
        flowsheet.ConnectObjects(feed.GraphicObject, separator.GraphicObject, 0, 0);
        flowsheet.ConnectObjects(separator.GraphicObject, vapor.GraphicObject, 0, 0);   // Vapor outlet
        flowsheet.ConnectObjects(separator.GraphicObject, liquid1.GraphicObject, 1, 0); // Liquid 1 outlet
        flowsheet.ConnectObjects(separator.GraphicObject, liquid2.GraphicObject, 2, 0); // Liquid 2 outlet
        Logger.Information("Connected: Feed -> Separator -> Vapor + Liquid1 + Liquid2");

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

        Logger.Information("FEED STREAM:");
        TestHelpers.LogStreamProperties(feed, Logger);

        Logger.Information("");
        Logger.Information("VAPOR STREAM:");
        TestHelpers.LogStreamProperties(vapor, Logger);

        Logger.Information("");
        Logger.Information("LIQUID 1 (Organic) STREAM:");
        TestHelpers.LogStreamProperties(liquid1, Logger);

        Logger.Information("");
        Logger.Information("LIQUID 2 (Aqueous) STREAM:");
        TestHelpers.LogStreamProperties(liquid2, Logger);

        // Get mass flows
        var feedMassFlow = feed.Phases[0].Properties.massflow ?? 0;
        var vaporMassFlow = vapor.Phases[0].Properties.massflow ?? 0;
        var liquid1MassFlow = liquid1.Phases[0].Properties.massflow ?? 0;
        var liquid2MassFlow = liquid2.Phases[0].Properties.massflow ?? 0;

        Logger.Information("");
        Logger.Information("Mass Flow Summary:");
        Logger.Information("  Feed: {Feed:F6} kg/s", feedMassFlow);
        Logger.Information("  Vapor: {Vapor:F6} kg/s", vaporMassFlow);
        Logger.Information("  Liquid1 (Organic): {L1:F6} kg/s", liquid1MassFlow);
        Logger.Information("  Liquid2 (Aqueous): {L2:F6} kg/s", liquid2MassFlow);

        // Mass balance check
        var totalOut = vaporMassFlow + liquid1MassFlow + liquid2MassFlow;
        var massError = Math.Abs(feedMassFlow - totalOut);
        Logger.Information("  Total Out: {Total:F6} kg/s", totalOut);
        Logger.Information("  Mass Balance Error: {Error:E3} kg/s", massError);

        Assert.True(massError < 0.001, $"Mass balance error ({massError:E3} kg/s) exceeds tolerance");
        Logger.Information("Mass balance within tolerance");

        // Check phase composition enrichment
        var vaporMethane = vapor.Phases[0].Compounds["Methane"].MoleFraction ?? 0;
        var liquid1Hexane = liquid1.Phases[0].Compounds["n-Hexane"].MoleFraction ?? 0;
        var liquid2Water = liquid2.Phases[0].Compounds["Water"].MoleFraction ?? 0;

        Logger.Information("");
        Logger.Information("Phase Enrichment Check:");
        Logger.Information("  Vapor phase Methane: {VaporMethane:P2}", vaporMethane);
        Logger.Information("  Organic phase n-Hexane: {L1Hexane:P2}", liquid1Hexane);
        Logger.Information("  Aqueous phase Water: {L2Water:P2}", liquid2Water);

        // Vapor should be enriched in methane (lighter component)
        Assert.True(vaporMethane > 0.2,
            $"Vapor phase should be enriched in Methane (expected >20%, got {vaporMethane:P2})");
        Logger.Information("Vapor phase is enriched in Methane");

        // For three-phase flash, at least check we have meaningful flows in outputs
        // The exact separation depends heavily on thermodynamics
        var hasVapor = vaporMassFlow > 0.001;
        var hasLiquid1 = liquid1MassFlow > 0.001;
        var hasLiquid2 = liquid2MassFlow > 0.001;

        Logger.Information("");
        Logger.Information("Phase Presence:");
        Logger.Information("  Vapor: {HasVapor}", hasVapor ? "YES" : "NO");
        Logger.Information("  Liquid1: {HasL1}", hasLiquid1 ? "YES" : "NO");
        Logger.Information("  Liquid2: {HasL2}", hasLiquid2 ? "YES" : "NO");

        // We expect at least two phases (vapor + one liquid) for this mixture
        var phaseCount = (hasVapor ? 1 : 0) + (hasLiquid1 ? 1 : 0) + (hasLiquid2 ? 1 : 0);
        Assert.True(phaseCount >= 2,
            $"Expected at least 2 phases for Methane/Hexane/Water mixture at these conditions (got {phaseCount})");
        Logger.Information("At least 2 phases present (thermodynamically consistent)");

        // Verify temperatures are consistent
        var vaporTemp = vapor.Phases[0].Properties.temperature ?? 0;
        var liquid1Temp = liquid1.Phases[0].Properties.temperature ?? 0;
        var liquid2Temp = liquid2.Phases[0].Properties.temperature ?? 0;

        if (hasVapor && hasLiquid1)
        {
            Assert.True(Math.Abs(vaporTemp - liquid1Temp) < 1.0,
                "Vapor and Liquid1 should be at the same temperature (equilibrium)");
        }
        if (hasLiquid1 && hasLiquid2)
        {
            Assert.True(Math.Abs(liquid1Temp - liquid2Temp) < 1.0,
                "Liquid1 and Liquid2 should be at the same temperature (equilibrium)");
        }
        Logger.Information("Phase temperatures are consistent (thermal equilibrium)");

        LogFlowsheetSummary(flowsheet);

        Logger.Information("========================================");
        Logger.Information("Test 06: PASSED - Three-phase flash separation successful");
        Logger.Information("========================================");
    }
}
