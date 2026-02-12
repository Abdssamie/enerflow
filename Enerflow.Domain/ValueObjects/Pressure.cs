namespace Enerflow.Domain.ValueObjects;

using Enerflow.Domain.Enums;

/// <summary>
/// Represents a pressure value with automatic unit conversion to Pascal (SI).
/// </summary>
public class Pressure : IParameter
{
    /// <summary>
    /// Gets the pressure value in Pascal (SI unit).
    /// </summary>
    public double Value { get; private set; }

    /// <summary>
    /// Gets the system of units used for this pressure.
    /// </summary>
    public SystemOfUnits SystemOfUnits { get; private set; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private Pressure() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Pressure"/> class.
    /// </summary>
    /// <param name="value">The pressure value in the specified unit system.</param>
    /// <param name="system">The system of units (default: SI).</param>
    public Pressure(double value, SystemOfUnits system = SystemOfUnits.SI)
    {
        SystemOfUnits = system;
        Value = ConvertToSI(value, system);
    }

    /// <summary>
    /// Converts the pressure value to Pascal (SI).
    /// </summary>
    /// <param name="value">The pressure value in the specified unit system.</param>
    /// <param name="system">The system of units.</param>
    /// <returns>The pressure in Pascal.</returns>
    private static double ConvertToSI(double value, SystemOfUnits system)
    {
        return system switch
        {
            SystemOfUnits.SI => value, // Already in Pascal
            SystemOfUnits.English => value * 6894.76, // psi to Pa
            SystemOfUnits.CGS => value * 10, // dyne/cm² to Pa
            _ => throw new ArgumentException($"Unsupported unit system: {system}", nameof(system))
        };
    }

    /// <summary>
    /// Converts the pressure from Pascal (SI) to the specified unit system.
    /// </summary>
    /// <param name="pascal">The pressure value in Pascal.</param>
    /// <param name="system">The target system of units.</param>
    /// <returns>The pressure in the specified unit system.</returns>
    public static double ConvertFromSI(double pascal, SystemOfUnits system)
    {
        return system switch
        {
            SystemOfUnits.SI => pascal, // Already in Pascal
            SystemOfUnits.English => pascal / 6894.76, // Pa to psi
            SystemOfUnits.CGS => pascal / 10, // Pa to dyne/cm²
            _ => throw new ArgumentException($"Unsupported unit system: {system}", nameof(system))
        };
    }

    /// <summary>
    /// Gets the pressure value in the original unit system it was created with.
    /// </summary>
    public double ValueInOriginalUnits => ConvertFromSI(Value, SystemOfUnits);
}
