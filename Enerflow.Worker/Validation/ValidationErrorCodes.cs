namespace Enerflow.Worker.Validation;

/// <summary>
/// Centralized validation error codes to avoid magic strings.
/// Each code represents a specific validation failure with a clear semantic meaning.
/// </summary>
public static class ValidationErrorCodes
{
    // Topology Validation Errors
    public const string DisconnectedUnit = "DISCONNECTED_UNIT";
    public const string OrphanedStream = "ORPHANED_STREAM";
    
    // Physical Property Validation Errors - Temperature
    public const string InvalidTemperature = "INVALID_TEMPERATURE";
    public const string InvalidOutletTemperature = "INVALID_OUTLET_TEMPERATURE";
    
    // Physical Property Validation Errors - Pressure
    public const string InvalidPressure = "INVALID_PRESSURE";
    public const string InvalidOutletPressure = "INVALID_OUTLET_PRESSURE";
    public const string InvalidCondenserPressure = "INVALID_CONDENSER_PRESSURE";
    public const string InvalidReboilerPressure = "INVALID_REBOILER_PRESSURE";
    public const string InvalidPressureDrop = "INVALID_PRESSURE_DROP";
    
    // Physical Property Validation Errors - Flow
    public const string InvalidMassFlow = "INVALID_MASS_FLOW";
    public const string InvalidMolarFlow = "INVALID_MOLAR_FLOW";
    public const string InvalidEnergyFlow = "INVALID_ENERGY_FLOW";
    
    // Composition Validation Errors
    public const string InvalidCompositionSum = "INVALID_COMPOSITION_SUM";
    public const string NegativeComposition = "NEGATIVE_COMPOSITION";
    
    // Compound Validation Errors
    public const string NoCompoundsDefined = "NO_COMPOUNDS_DEFINED";
    public const string UndefinedCompoundReference = "UNDEFINED_COMPOUND_REFERENCE";
    public const string InvalidLightKeyReference = "INVALID_LIGHT_KEY_REFERENCE";
    public const string InvalidHeavyKeyReference = "INVALID_HEAVY_KEY_REFERENCE";
    
    // Unit Operation Configuration Errors
    public const string InvalidEfficiency = "INVALID_EFFICIENCY";
    public const string InvalidHeatDuty = "INVALID_HEAT_DUTY";
    public const string InvalidRefluxRatio = "INVALID_REFLUX_RATIO";
    public const string InvalidStagesCount = "INVALID_STAGES_COUNT";
    public const string InvalidTolerance = "INVALID_TOLERANCE";
    public const string InvalidMaxIterations = "INVALID_MAX_ITERATIONS";
    public const string SplitterInvalidRatios = "SPLITTER_INVALID_RATIOS";
    
    // Unit Operation Topology Errors
    public const string UnitRequiresSingleInput = "UNIT_REQUIRES_SINGLE_INPUT";
    public const string UnitRequiresSingleOutput = "UNIT_REQUIRES_SINGLE_OUTPUT";
    public const string UnitRequiresMultipleInputs = "UNIT_REQUIRES_MULTIPLE_INPUTS";
    public const string UnitRequiresMultipleOutputs = "UNIT_REQUIRES_MULTIPLE_OUTPUTS";
    public const string UnitRequiresInput = "UNIT_REQUIRES_INPUT";
    public const string UnitRequiresTwoOutputs = "UNIT_REQUIRES_TWO_OUTPUTS";
    
    // Generic Errors
    public const string NullReferenceError = "NULL_REFERENCE_ERROR";
    public const string ValidationError = "VALIDATION_ERROR";
}
