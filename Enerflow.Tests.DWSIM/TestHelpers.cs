using Serilog;
using DWSIM.Interfaces;
using DWSIM.Thermodynamics.Streams;

namespace Enerflow.Tests.DWSIM;

/// <summary>
/// Helper utilities for DWSIM API tests.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Logs comprehensive properties of a material stream.
    /// </summary>
    public static void LogStreamProperties(MaterialStream? stream, ILogger logger)
    {
        if (stream == null)
        {
            logger.Warning("Stream is null");
            return;
        }

        try
        {
            // Get overall phase properties (Phase 0 is the mixture)
            var phase0 = stream.Phases[0];

            logger.Information("    Temperature: {Temp:F2} K ({TempC:F2} °C)",
                phase0.Properties.temperature ?? 0,
                (phase0.Properties.temperature ?? 273.15) - 273.15);

            logger.Information("    Pressure: {Pressure:F2} Pa ({PressureBar:F2} bar)",
                phase0.Properties.pressure ?? 0,
                (phase0.Properties.pressure ?? 0) / 100000.0);

            logger.Information("    Mass Flow: {MassFlow:F4} kg/s", phase0.Properties.massflow ?? 0);
            logger.Information("    Molar Flow: {MolarFlow:F4} mol/s", phase0.Properties.molarflow ?? 0);
            logger.Information("    Volumetric Flow: {VolumetricFlow:F6} m³/s", phase0.Properties.volumetric_flow ?? 0);
            logger.Information("    Enthalpy: {Enthalpy:F2} kJ/kg", (phase0.Properties.enthalpy ?? 0) / 1000.0);
            logger.Information("    Entropy: {Entropy:F2} kJ/[kg.K]", (phase0.Properties.entropy ?? 0) / 1000.0);

            // Log vapor fraction
            logger.Information("    Vapor Fraction in moles: {VaporFrac:F4}", phase0.Properties.molarfraction ?? 0);

            // Log composition
            if (phase0.Compounds != null && phase0.Compounds.Any())
            {
                logger.Information("    Composition (Mole Fraction):");
                foreach (var compound in phase0.Compounds.OrderByDescending(c => c.Value.MoleFraction ?? 0))
                {
                    logger.Information("      {Compound}: {MoleFrac:F6}",
                        compound.Key,
                        compound.Value.MoleFraction ?? 0);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to log stream properties for {StreamName}", stream.Name);
        }
    }

    /// <summary>
    /// Asserts that a stream's temperature is within the expected range.
    /// </summary>
    public static void AssertTemperatureInRange(MaterialStream stream, double minK, double maxK, ILogger logger)
    {
        var temp = stream.Phases[0].Properties.temperature ?? 0;

        if (temp < minK || temp > maxK)
        {
            logger.Error("Temperature {Temp:F2} K is outside expected range [{Min:F2}, {Max:F2}] K",
                temp, minK, maxK);
            Assert.Fail($"Temperature {temp:F2} K is outside expected range [{minK:F2}, {maxK:F2}] K");
        }

        logger.Information("✓ Temperature {Temp:F2} K is within expected range [{Min:F2}, {Max:F2}] K",
            temp, minK, maxK);
    }

    /// <summary>
    /// Asserts that a stream's pressure is within the expected range.
    /// </summary>
    public static void AssertPressureInRange(MaterialStream stream, double minPa, double maxPa, ILogger logger)
    {
        var pressure = stream.Phases[0].Properties.pressure ?? 0;

        if (pressure < minPa || pressure > maxPa)
        {
            logger.Error("Pressure {Pressure:F2} Pa is outside expected range [{Min:F2}, {Max:F2}] Pa",
                pressure, minPa, maxPa);
            Assert.Fail($"Pressure {pressure:F2} Pa is outside expected range [{minPa:F2}, {maxPa:F2}] Pa");
        }

        logger.Information("✓ Pressure {Pressure:F2} Pa ({PressureBar:F2} bar) is within expected range",
            pressure, pressure / 100000.0);
    }

    /// <summary>
    /// Simple mass balance check: sum of all mass flows in should roughly equal sum of mass flows out.
    /// </summary>
    public static void CheckGlobalMassBalance(IFlowsheet flowsheet, ILogger logger, double toleranceKgPerS = 1e-3)
    {
        try
        {
            double totalInflow = 0;
            double totalOutflow = 0;

            foreach (var obj in flowsheet.SimulationObjects.Values)
            {
                if (obj is MaterialStream ms)
                {
                    var massFlow = ms.Phases[0].Properties.massflow ?? 0;

                    // Simple heuristic: if it has no input connections, it's likely an inlet
                    // if it has no output connections, it's likely an outlet
                    // This is a simplified check - proper implementation would need graph traversal
                    var graphicObj = ms.GraphicObject;

                    if (graphicObj.InputConnectors?.Count == 0)
                    {
                        totalInflow += massFlow;
                        logger.Debug("Inlet stream {Name}: {MassFlow:F6} kg/s", ms.Name, massFlow);
                    }
                    else if (graphicObj.OutputConnectors?.Count == 0)
                    {
                        totalOutflow += massFlow;
                        logger.Debug("Outlet stream {Name}: {MassFlow:F6} kg/s", ms.Name, massFlow);
                    }
                }
            }

            var imbalance = Math.Abs(totalInflow - totalOutflow);
            logger.Information("Mass Balance Check:");
            logger.Information("  Total Inflow:  {Inflow:F6} kg/s", totalInflow);
            logger.Information("  Total Outflow: {Outflow:F6} kg/s", totalOutflow);
            logger.Information("  Imbalance:     {Imbalance:E3} kg/s", imbalance);

            if (imbalance > toleranceKgPerS)
            {
                logger.Warning("Mass imbalance exceeds tolerance: {Imbalance:E3} > {Tolerance:E3} kg/s",
                    imbalance, toleranceKgPerS);
            }
            else
            {
                logger.Information("✓ Mass balance within tolerance");
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to check mass balance");
        }
    }
}
