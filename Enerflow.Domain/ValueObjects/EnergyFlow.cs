namespace Enerflow.Domain.ValueObjects;

using Enerflow.Domain.Enums;

/// <summary>
/// Represents an energy flow rate with automatic unit conversion to Watts (SI).
/// Provides ValueInKW property for DWSIM compatibility.
/// </summary>
public class EnergyFlow : IParameter
{
    /// <summary>
    /// Gets the energy flow rate value in Watts (SI unit).
    /// </summary>
    public double Value { get; private set; }

    /// <summary>
    /// Gets the energy flow rate value in kilowatts (kW) for DWSIM compatibility.
    /// </summary>
    public double ValueInKW => Value / 1000;

    /// <summary>
    /// Gets the system of units used for this energy flow rate.
    /// </summary>
    public SystemOfUnits SystemOfUnits { get; private set; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private EnergyFlow() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnergyFlow"/> class.
    /// </summary>
    /// <param name="value">The energy flow rate value in the specified unit system.</param>
    /// <param name="system">The system of units (default: SI).</param>
    public EnergyFlow(double value, SystemOfUnits system = SystemOfUnits.SI)
    {
        SystemOfUnits = system;
        Value = ConvertToSI(value, system);
    }

    /// <summary>
    /// Converts the energy flow rate value to Watts (SI).
    /// </summary>
    /// <param name="value">The energy flow rate value in the specified unit system.</param>
    /// <param name="system">The system of units.</param>
    /// <returns>The energy flow rate in Watts.</returns>
    private static double ConvertToSI(double value, SystemOfUnits system)
    {
        return system switch
        {
            SystemOfUnits.SI => value * 1000, // kW to W (DWSIM uses kW)
            SystemOfUnits.English => value * 0.293071, // BTU/h to W
            SystemOfUnits.CGS => value * 4.184, // cal/s to W
            _ => throw new ArgumentException($"Unsupported unit system: {system}", nameof(system))
        };
    }

    /// <summary>
    /// Converts the energy flow rate from Watts (SI) to the specified unit system.
    /// </summary>
    /// <param name="watts">The energy flow rate value in Watts.</param>
    /// <param name="system">The target system of units.</param>
    /// <returns>The energy flow rate in the specified unit system.</returns>
    public static double ConvertFromSI(double watts, SystemOfUnits system)
    {
        return system switch
        {
            SystemOfUnits.SI => watts / 1000, // W to kW (DWSIM uses kW)
            SystemOfUnits.English => watts / 0.293071, // W to BTU/h
            SystemOfUnits.CGS => watts / 4.184, // W to cal/s
            _ => throw new ArgumentException($"Unsupported unit system: {system}", nameof(system))
        };
    }

    /// <summary>
    /// Gets the energy flow rate value in the original unit system it was created with.
    /// </summary>
    public double ValueInOriginalUnits => ConvertFromSI(Value, SystemOfUnits);
}
