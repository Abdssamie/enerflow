namespace Enerflow.Domain.Enums;

/// <summary>
/// Supported unit systems for simulation calculations.
/// Maps to DWSIM's available unit systems.
/// </summary>
public enum SystemOfUnits
{
    /// <summary>
    /// International System of Units (SI)
    /// Temperature: K, Pressure: Pa, Flow: kg/s
    /// </summary>
    SI,

    /// <summary>
    /// Centimeter-Gram-Second system
    /// </summary>
    CGS,

    /// <summary>
    /// English/Imperial unit system
    /// Temperature: °F, Pressure: psi, Flow: lb/h
    /// </summary>
    English
}
