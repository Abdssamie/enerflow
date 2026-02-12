namespace Enerflow.Domain.ValueObjects;

using Enerflow.Domain.Enums;

/// <summary>
/// Represents a mass flow rate value with automatic unit conversion to kg/s (SI).
/// </summary>
public class MassFlow : IParameter
{
    /// <summary>
    /// Gets the mass flow rate value in kg/s (SI unit).
    /// </summary>
    public double Value { get; private set; }

    /// <summary>
    /// Gets the system of units used for this mass flow rate.
    /// </summary>
    public SystemOfUnits SystemOfUnits { get; private set; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private MassFlow() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MassFlow"/> class.
    /// </summary>
    /// <param name="value">The mass flow rate value in the specified unit system.</param>
    /// <param name="system">The system of units (default: SI).</param>
    public MassFlow(double value, SystemOfUnits system = SystemOfUnits.SI)
    {
        SystemOfUnits = system;
        Value = ConvertToSI(value, system);
    }

    /// <summary>
    /// Converts the mass flow rate value to kg/s (SI).
    /// </summary>
    /// <param name="value">The mass flow rate value in the specified unit system.</param>
    /// <param name="system">The system of units.</param>
    /// <returns>The mass flow rate in kg/s.</returns>
    private static double ConvertToSI(double value, SystemOfUnits system)
    {
        return system switch
        {
            SystemOfUnits.SI => value, // Already in kg/s
            SystemOfUnits.English => value * 0.453592 / 3600, // lb/h to kg/s
            SystemOfUnits.CGS => value / 1000, // g/s to kg/s
            _ => throw new ArgumentException($"Unsupported unit system: {system}", nameof(system))
        };
    }

    /// <summary>
    /// Converts the mass flow rate from kg/s (SI) to the specified unit system.
    /// </summary>
    /// <param name="kgPerSec">The mass flow rate value in kg/s.</param>
    /// <param name="system">The target system of units.</param>
    /// <returns>The mass flow rate in the specified unit system.</returns>
    public static double ConvertFromSI(double kgPerSec, SystemOfUnits system)
    {
        return system switch
        {
            SystemOfUnits.SI => kgPerSec, // Already in kg/s
            SystemOfUnits.English => kgPerSec * 3600 / 0.453592, // kg/s to lb/h
            SystemOfUnits.CGS => kgPerSec * 1000, // kg/s to g/s
            _ => throw new ArgumentException($"Unsupported unit system: {system}", nameof(system))
        };
    }

    /// <summary>
    /// Gets the mass flow rate value in the original unit system it was created with.
    /// </summary>
    public double ValueInOriginalUnits => ConvertFromSI(Value, SystemOfUnits);
}
