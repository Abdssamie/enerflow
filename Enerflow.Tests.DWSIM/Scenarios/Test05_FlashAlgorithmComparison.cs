using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.Thermodynamics.PropertyPackages;
using DWSIM.Thermodynamics.PropertyPackages.Auxiliary.FlashAlgorithms;
using DWSIM.Thermodynamics.Streams;
using DWSIM.UnitOperations.UnitOperations;

namespace Enerflow.Tests.DWSIM.Scenarios;

/// <summary>
/// Test 05: Flash Algorithm Comparison
/// Scenario: Same separation problem solved with different flash algorithms (NestedLoops vs InsideOut)
/// Tests: Flash algorithm selection, convergence comparison, result consistency
/// </summary>
public class Test05FlashAlgorithmComparison : TestBase
{
    [Fact]
    public void FlashAlgorithmComparison_NestedLoopsVsInsideOut_ConvergesToSimilarResults()
    {
        // --------------------------------------------------------------------------------
        // PART 1: Simulation with NestedLoops Flash Algorithm
        // --------------------------------------------------------------------------------
        Logger.Information("TEST 05: Flash Algorithm Comparison");
        Logger.Information("Scenario: Propane/n-Butane flash separation at different conditions");
        Logger.Information("========================================");
        Logger.Information("PART 1: Simulation with NestedLoops Flash Algorithm");

        var flowsheetNL = Automation.CreateFlowsheet();
        Assert.NotNull(flowsheetNL);

        // Add compounds
        flowsheetNL.SelectedCompounds.Add("Propane", flowsheetNL.AvailableCompounds["Propane"]);
        flowsheetNL.SelectedCompounds.Add("n-Butane", flowsheetNL.AvailableCompounds["n-Butane"]);
        Logger.Information("Added compounds: Propane, n-Butane");

        // Set SI units
        var siUnits = flowsheetNL.AvailableSystemsOfUnits.First(u => u.Name.Contains("SI"));
        flowsheetNL.FlowsheetOptions.SelectedUnitSystem = siUnits;

        // Add Peng-Robinson with NestedLoops flash
        var prNL = new PengRobinsonPropertyPackage();
        prNL.FlashAlgorithm = new NestedLoops();
        flowsheetNL.AddPropertyPackage(prNL);
        Logger.Information("Property Package: Peng-Robinson with NestedLoops flash");

        // Create feed stream
        var feedNL = flowsheetNL.AddObject(ObjectType.MaterialStream, 100, 100, "Feed_NL") as MaterialStream;
        Assert.NotNull(feedNL);

        // Setup: 10 bar, 300 K, 1 kg/s, 50/50 mol% Propane/n-Butane
        feedNL.Phases[0].Properties.temperature = 300; // 300 K
        feedNL.Phases[0].Properties.pressure = 1000000; // 10 bar
        feedNL.Phases[0].Properties.massflow = 1.0;
        feedNL.Phases[0].Compounds["Propane"].MoleFraction = 0.5;
        feedNL.Phases[0].Compounds["n-Butane"].MoleFraction = 0.5;

        Logger.Information("Feed: 10 bar, 300 K, 1 kg/s, 50% Propane / 50% n-Butane");

        // Create valve for pressure reduction
        var valveNL = flowsheetNL.AddObject(ObjectType.Valve, 200, 100, "Valve_NL") as Valve;
        Assert.NotNull(valveNL);
        valveNL.CalcMode = Valve.CalculationMode.OutletPressure;
        valveNL.OutletPressure = 500000; // 5 bar
        Logger.Information("Valve: Flash to 5 bar");

        // Create outlet stream
        var flashedNL = flowsheetNL.AddObject(ObjectType.MaterialStream, 300, 100, "Flashed_NL") as MaterialStream;
        Assert.NotNull(flashedNL);

        // Connect
        flowsheetNL.ConnectObjects(feedNL.GraphicObject, valveNL.GraphicObject, 0, 0);
        flowsheetNL.ConnectObjects(valveNL.GraphicObject, flashedNL.GraphicObject, 0, 0);

        // Solve
        Logger.Information("Solving with NestedLoops...");
        var startNL = DateTime.Now;
        Automation.CalculateFlowsheet2(flowsheetNL);
        var timeNL = (DateTime.Now - startNL).TotalMilliseconds;
        AssertConverged(flowsheetNL);

        // Capture results
        double tempNL = flashedNL.Phases[0].Properties.temperature ?? 0;
        double vaporFracNL = flashedNL.Phases[0].Properties.molarfraction ?? 0;
        double propaneVaporNL = 0;
        double butaneVaporNL = 0;

        // Get vapor phase composition (Phase 2 is vapor)
        if (flashedNL.Phases[2].Properties.molarfraction > 0)
        {
            propaneVaporNL = flashedNL.Phases[2].Compounds["Propane"].MoleFraction ?? 0;
            butaneVaporNL = flashedNL.Phases[2].Compounds["n-Butane"].MoleFraction ?? 0;
        }

        Logger.Information("NestedLoops Results:");
        Logger.Information("  Calculation time: {Time:F2} ms", timeNL);
        Logger.Information("  Temperature: {Temp:F2} K ({TempC:F2} C)", tempNL, tempNL - 273.15);
        Logger.Information("  Vapor Fraction: {VF:F4}", vaporFracNL);
        Logger.Information("  Vapor Propane: {Prop:F4}", propaneVaporNL);
        Logger.Information("  Vapor n-Butane: {But:F4}", butaneVaporNL);

        // --------------------------------------------------------------------------------
        // PART 2: Simulation with InsideOut Flash Algorithm
        // --------------------------------------------------------------------------------
        Logger.Information("========================================");
        Logger.Information("PART 2: Simulation with InsideOut Flash Algorithm");

        var flowsheetIO = Automation.CreateFlowsheet();
        Assert.NotNull(flowsheetIO);

        // Add compounds
        flowsheetIO.SelectedCompounds.Add("Propane", flowsheetIO.AvailableCompounds["Propane"]);
        flowsheetIO.SelectedCompounds.Add("n-Butane", flowsheetIO.AvailableCompounds["n-Butane"]);

        flowsheetIO.FlowsheetOptions.SelectedUnitSystem = siUnits;

        // Add Peng-Robinson with InsideOut flash (BostonBrittInsideOut)
        var prIO = new PengRobinsonPropertyPackage();
        prIO.FlashAlgorithm = new BostonBrittInsideOut();
        flowsheetIO.AddPropertyPackage(prIO);
        Logger.Information("Property Package: Peng-Robinson with BostonBrittInsideOut flash");

        // Create feed stream (same conditions)
        var feedIO = flowsheetIO.AddObject(ObjectType.MaterialStream, 100, 100, "Feed_IO") as MaterialStream;
        Assert.NotNull(feedIO);

        feedIO.Phases[0].Properties.temperature = 300;
        feedIO.Phases[0].Properties.pressure = 1000000;
        feedIO.Phases[0].Properties.massflow = 1.0;
        feedIO.Phases[0].Compounds["Propane"].MoleFraction = 0.5;
        feedIO.Phases[0].Compounds["n-Butane"].MoleFraction = 0.5;

        // Create valve
        var valveIO = flowsheetIO.AddObject(ObjectType.Valve, 200, 100, "Valve_IO") as Valve;
        Assert.NotNull(valveIO);
        valveIO.CalcMode = Valve.CalculationMode.OutletPressure;
        valveIO.OutletPressure = 500000;

        // Create outlet stream
        var flashedIO = flowsheetIO.AddObject(ObjectType.MaterialStream, 300, 100, "Flashed_IO") as MaterialStream;
        Assert.NotNull(flashedIO);

        // Connect
        flowsheetIO.ConnectObjects(feedIO.GraphicObject, valveIO.GraphicObject, 0, 0);
        flowsheetIO.ConnectObjects(valveIO.GraphicObject, flashedIO.GraphicObject, 0, 0);

        // Solve
        Logger.Information("Solving with InsideOut...");
        var startIO = DateTime.Now;
        Automation.CalculateFlowsheet2(flowsheetIO);
        var timeIO = (DateTime.Now - startIO).TotalMilliseconds;
        AssertConverged(flowsheetIO);

        // Capture results
        double tempIO = flashedIO.Phases[0].Properties.temperature ?? 0;
        double vaporFracIO = flashedIO.Phases[0].Properties.molarfraction ?? 0;
        double propaneVaporIO = 0;
        double butaneVaporIO = 0;

        if (flashedIO.Phases[2].Properties.molarfraction > 0)
        {
            propaneVaporIO = flashedIO.Phases[2].Compounds["Propane"].MoleFraction ?? 0;
            butaneVaporIO = flashedIO.Phases[2].Compounds["n-Butane"].MoleFraction ?? 0;
        }

        Logger.Information("InsideOut Results:");
        Logger.Information("  Calculation time: {Time:F2} ms", timeIO);
        Logger.Information("  Temperature: {Temp:F2} K ({TempC:F2} C)", tempIO, tempIO - 273.15);
        Logger.Information("  Vapor Fraction: {VF:F4}", vaporFracIO);
        Logger.Information("  Vapor Propane: {Prop:F4}", propaneVaporIO);
        Logger.Information("  Vapor n-Butane: {But:F4}", butaneVaporIO);

        // --------------------------------------------------------------------------------
        // COMPARISON & ASSERTIONS
        // --------------------------------------------------------------------------------
        Logger.Information("========================================");
        Logger.Information("Comparison:");
        Logger.Information("  Temperature - NL: {T1:F2} K, IO: {T2:F2} K, Diff: {Diff:F4} K",
            tempNL, tempIO, Math.Abs(tempNL - tempIO));
        Logger.Information("  Vapor Frac  - NL: {V1:F4}, IO: {V2:F4}, Diff: {Diff:F6}",
            vaporFracNL, vaporFracIO, Math.Abs(vaporFracNL - vaporFracIO));
        Logger.Information("  Performance - NL: {T1:F2} ms, IO: {T2:F2} ms",
            timeNL, timeIO);

        // Both algorithms should converge to similar results (within tolerance)
        double tempTolerance = 0.5; // 0.5 K tolerance
        double vfTolerance = 0.01;  // 1% tolerance on vapor fraction
        double compTolerance = 0.01; // 1% tolerance on composition

        Assert.True(Math.Abs(tempNL - tempIO) < tempTolerance,
            $"Temperature difference ({Math.Abs(tempNL - tempIO):F4} K) exceeds tolerance ({tempTolerance} K)");
        Logger.Information("Temperature difference within tolerance ({Tol} K)", tempTolerance);

        Assert.True(Math.Abs(vaporFracNL - vaporFracIO) < vfTolerance,
            $"Vapor fraction difference ({Math.Abs(vaporFracNL - vaporFracIO):F6}) exceeds tolerance ({vfTolerance})");
        Logger.Information("Vapor fraction difference within tolerance ({Tol})", vfTolerance);

        // Verify vapor phase compositions match
        if (propaneVaporNL > 0 && propaneVaporIO > 0)
        {
            Assert.True(Math.Abs(propaneVaporNL - propaneVaporIO) < compTolerance,
                $"Propane composition difference exceeds tolerance");
            Logger.Information("Vapor propane composition within tolerance");
        }

        // Verify thermodynamic consistency
        Assert.True(tempNL < 300 && tempIO < 300, "Flash temperature should be below feed temperature (Joule-Thomson effect)");
        Logger.Information("Flash temperatures are below feed temperature (thermodynamically consistent)");

        Logger.Information("========================================");
        Logger.Information("Test 05: PASSED - Both flash algorithms converge to consistent results");
        Logger.Information("========================================");
    }
}
