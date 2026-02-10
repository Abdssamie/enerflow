namespace Enerflow.Simulation.Validation;

/// <summary>
/// Centralized validation constants to avoid magic numbers and improve maintainability.
/// </summary>
public static class ValidationConstants
{
    /// <summary>
    /// Tolerance for composition sum validation (mole fractions must sum to 1.0 ± this value).
    /// </summary>
    public const double CompositionSumTolerance = 0.01;
    
    /// <summary>
    /// Tolerance used by SplitterObject for split ratio validation (internal entity validation uses 1e-6).
    /// We use the same value for consistency.
    /// </summary>
    public const double SplitRatioTolerance = 1e-6;
    
    /// <summary>
    /// Expected composition sum (mole fractions should sum to this value).
    /// </summary>
    public const double ExpectedCompositionSum = 1.0;
    
    /// <summary>
    /// Minimum valid temperature in Kelvin (absolute zero is the physical limit).
    /// </summary>
    public const double MinimumTemperature = 0.0;
    
    /// <summary>
    /// Minimum valid pressure in Pascal.
    /// </summary>
    public const double MinimumPressure = 0.0;
    
    /// <summary>
    /// Minimum valid mass flow in kg/s.
    /// </summary>
    public const double MinimumMassFlow = 0.0;
}
