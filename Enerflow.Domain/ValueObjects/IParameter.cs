namespace Enerflow.Domain.ValueObjects;

using Enerflow.Domain.Enums;

/// <summary>
/// Represents a physical parameter with automatic unit conversion to SI.
/// All implementing types store values in SI units internally.
/// </summary>
public interface IParameter
{
    /// <summary>
    /// Gets the parameter value in SI units.
    /// </summary>
    double Value { get; }

    /// <summary>
    /// Gets the system of units used for this parameter.
    /// </summary>
    SystemOfUnits SystemOfUnits { get; }
}
