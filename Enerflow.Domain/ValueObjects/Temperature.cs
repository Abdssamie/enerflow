namespace Enerflow.Domain.ValueObjects;

using Enerflow.Domain.Enums;

/// <summary>
/// Represents a temperature value with automatic unit conversion to Kelvin (SI).
/// </summary>
public class Temperature : IParameter
{
    /// <summary>
    /// Gets the temperature value in Kelvin (SI unit).
    /// </summary>
    public double Value { get; private set; }

    /// <summary>
    /// Gets the system of units used for this temperature.
    /// </summary>
    public SystemOfUnits SystemOfUnits { get; private set; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private Temperature() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Temperature"/> class.
    /// </summary>
    /// <param name="value">The temperature value in the specified unit system.</param>
    /// <param name="system">The system of units (default: SI).</param>
    public Temperature(double value, SystemOfUnits system = SystemOfUnits.SI)
    {
        SystemOfUnits = system;
        Value = ConvertToSI(value, system);
    }

    /// <summary>
    /// Converts the temperature value to Kelvin (SI).
    /// </summary>
    /// <param name="value">The temperature value in the specified unit system.</param>
    /// <param name="system">The system of units.</param>
    /// <returns>The temperature in Kelvin.</returns>
    private static double ConvertToSI(double value, SystemOfUnits system)
    {
        return system switch
        {
            SystemOfUnits.SI => value, // Already in Kelvin
            SystemOfUnits.English => (value - 32) * 5.0 / 9.0 + 273.15, // °F to K
            SystemOfUnits.CGS => value + 273.15, // °C to K
            _ => throw new ArgumentException($"Unsupported unit system: {system}", nameof(system))
        };
    }

    /// <summary>
    /// Converts the temperature from Kelvin (SI) to the specified unit system.
    /// </summary>
    /// <param name="kelvin">The temperature value in Kelvin.</param>
    /// <param name="system">The target system of units.</param>
    /// <returns>The temperature in the specified unit system.</returns>
    public static double ConvertFromSI(double kelvin, SystemOfUnits system)
    {
        return system switch
        {
            SystemOfUnits.SI => kelvin, // Already in Kelvin
            SystemOfUnits.English => (kelvin - 273.15) * 9.0 / 5.0 + 32, // K to °F
            SystemOfUnits.CGS => kelvin - 273.15, // K to °C
            _ => throw new ArgumentException($"Unsupported unit system: {system}", nameof(system))
        };
    }

    /// <summary>
    /// Gets the temperature value in the original unit system it was created with.
    /// </summary>
    public double ValueInOriginalUnits => ConvertFromSI(Value, SystemOfUnits);
}
