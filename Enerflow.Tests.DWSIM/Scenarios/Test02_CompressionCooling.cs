using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.Thermodynamics.PropertyPackages;
using DWSIM.Thermodynamics.Streams;
using DWSIM.UnitOperations.UnitOperations;

namespace Enerflow.Tests.DWSIM.Scenarios;

/// <summary>
/// Test 02: Compression and Cooling
/// Scenario: Natural gas (5 bar, 25°C) → Compressor (→ 20 bar) → Cooler (→ 25°C) → Product
/// Tests: Peng-Robinson, compressor work, temperature rise from compression, cooling duty
/// </summary>
public class Test02CompressionCooling : TestBase
{
    [Fact]
    public void CompressionCooling_NaturalGas_ConvergesSuccessfully()
    {
        // Arrange
        Logger.Information("TEST 02: Compression and Cooling");
        Logger.Information("Scenario: Natural gas compressed from 5 bar to 20 bar, then cooled to 25°C");
        Logger.Information("========================================");

        var flowsheet = Automation.CreateFlowsheet();
        Logger.Information("Flowsheet created");

        // Add Natural Gas components (simplified: CH4, C2H6, C3H8)
        flowsheet.SelectedCompounds.Add("Methane", flowsheet.AvailableCompounds["Methane"]);
        flowsheet.SelectedCompounds.Add("Ethane", flowsheet.AvailableCompounds["Ethane"]);
        flowsheet.SelectedCompounds.Add("Propane", flowsheet.AvailableCompounds["Propane"]);
        Logger.Information("Added compounds: Methane, Ethane, Propane");

        // Set SI units
        var siUnits = flowsheet.AvailableSystemsOfUnits.First(u => u.Name.Contains("SI"));
        flowsheet.FlowsheetOptions.SelectedUnitSystem = siUnits;

        // Add Peng-Robinson property package
        var pr = new PengRobinsonPropertyPackage();
        flowsheet.AddPropertyPackage(pr);
        Logger.Information("Property Package: Peng-Robinson");

        // Create inlet stream (Feed) - 5 bar, 25°C, natural gas composition
        var feed = flowsheet.AddObject(ObjectType.MaterialStream, 100, 100, "Feed") as MaterialStream;
        Assert.NotNull(feed);

        feed.Phases[0].Properties.temperature = 298.15; // 25°C
        feed.Phases[0].Properties.pressure = 500000;    // 5 bar
        feed.Phases[0].Properties.massflow = 1.0;       // 1 kg/s

        // Natural gas composition (approximate)
        feed.Phases[0].Compounds["Methane"].MoleFraction = 0.85;
        feed.Phases[0].Compounds["Ethane"].MoleFraction = 0.10;
        feed.Phases[0].Compounds["Propane"].MoleFraction = 0.05;

        Logger.Information("Feed: 5 bar, 25°C, 1 kg/s, 85% CH4 / 10% C2H6 / 5% C3H8");

        // Create compressor (5 bar → 20 bar, 75% efficiency)
        var compressor = flowsheet.AddObject(ObjectType.Compressor, 200, 100, "Compressor") as Compressor;
        Assert.NotNull(compressor);
        compressor.CalcMode = Compressor.CalculationMode.OutletPressure;
        compressor.POut = 2000000; // 20 bar
        compressor.AdiabaticEfficiency = 0.75; // 75% efficiency
        Logger.Information("Compressor: 5 bar → 20 bar, 75% efficiency");

        // Create intermediate stream (after compressor, before cooler)
        var compressed = flowsheet.AddObject(ObjectType.MaterialStream, 300, 100, "Compressed") as MaterialStream;
        Assert.NotNull(compressed);

        // Create cooler (cool back to 25°C)
        var cooler = flowsheet.AddObject(ObjectType.Cooler, 400, 100, "Cooler") as Cooler;
        Assert.NotNull(cooler);
        cooler.CalcMode = Cooler.CalculationMode.OutletTemperature;
        cooler.OutletTemperature = 298.15; // 25°C
        Logger.Information("Cooler: Outlet Temperature = 25°C");

        // Create outlet stream (Product)
        var product = flowsheet.AddObject(ObjectType.MaterialStream, 500, 100, "Product") as MaterialStream;
        Assert.NotNull(product);

        // Connect: Feed → Compressor → Compressed → Cooler → Product
        flowsheet.ConnectObjects(feed.GraphicObject, compressor.GraphicObject, 0, 0);
        flowsheet.ConnectObjects(compressor.GraphicObject, compressed.GraphicObject, 0, 0);
        flowsheet.ConnectObjects(compressed.GraphicObject, cooler.GraphicObject, 0, 0);
        flowsheet.ConnectObjects(cooler.GraphicObject, product.GraphicObject, 0, 0);
        Logger.Information("Connected: Feed → Compressor → Compressed → Cooler → Product");

        // Act
        Logger.Information("========================================");
        Logger.Information("Solving flowsheet...");
        Automation.CalculateFlowsheet2(flowsheet);

        // Assert
        AssertConverged(flowsheet);

        Logger.Information("========================================");
        Logger.Information("RESULTS:");
        Logger.Information("========================================");

        Logger.Information("FEED (5 bar, 25°C):");
        TestHelpers.LogStreamProperties(feed, Logger);

        Logger.Information("");
        Logger.Information("COMPRESSED (20 bar, elevated temp):");
        TestHelpers.LogStreamProperties(compressed, Logger);

        Logger.Information("");
        Logger.Information("PRODUCT (20 bar, 25°C):");
        TestHelpers.LogStreamProperties(product, Logger);

        Logger.Information("");
        Logger.Information("COMPRESSOR:");
        Logger.Information("  Power Required: {Power:F2} kW", compressor.DeltaQ);
        Logger.Information("  Calculated: {Calculated}", compressor.Calculated);

        Logger.Information("");
        Logger.Information("COOLER:");
        Logger.Information("  Cooling Duty: {Duty:F2} kW", cooler.DeltaQ ?? 0);
        Logger.Information("  Calculated: {Calculated}", cooler.Calculated);

        // Verify compressed stream pressure increased
        var compressedPressure = compressed.Phases[0].Properties.pressure ?? 0;
        TestHelpers.AssertPressureInRange(compressed, 1900000, 2100000, Logger); // ~20 bar

        // Verify compressed stream temperature increased (compression heats gas)
        var compressedTemp = compressed.Phases[0].Properties.temperature ?? 0;
        Assert.True(compressedTemp > 320, $"Compressed temp {compressedTemp:F2} K should be > 320 K (compression heats gas)");
        Logger.Information("✓ Compression increased temperature to {Temp:F2} K ({TempC:F2} °C)",
            compressedTemp, compressedTemp - 273.15);

        // Verify product is cooled back to ~25°C
        TestHelpers.AssertTemperatureInRange(product, 297, 300, Logger);

        // Verify product pressure is ~20 bar
        TestHelpers.AssertPressureInRange(product, 1900000, 2100000, Logger);

        // Verify compressor power is positive
        var compPower = compressor.DeltaQ;
        Assert.True(compPower > 0, "Compressor power should be positive");
        Logger.Information("✓ Compressor power: {Power:F2} kW", compPower);

        // Verify cooling duty is significant (heat removed - DWSIM reports as positive magnitude)
        var coolingDuty = cooler.DeltaQ ?? 0;
        Assert.True(coolingDuty > 0, "Cooling duty should be positive (DWSIM reports magnitude of heat removed)");
        Logger.Information("Cooling duty (heat removed): {Duty:F2} kW", coolingDuty);

        LogFlowsheetSummary(flowsheet);
        TestHelpers.CheckGlobalMassBalance(flowsheet, Logger);

        Logger.Information("========================================");
        Logger.Information("Test 02: PASSED ✓");
        Logger.Information("========================================");
    }
}
