using DWSIM.Interfaces;
using Enerflow.Domain.Entities.UnitOperations;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Validation;

/// <summary>
/// Validates flowsheet configurations before execution.
/// Uses an adapter pattern to orchestrate entity-level validation and convert exceptions to structured errors.
/// </summary>
public sealed class FlowsheetValidator : IFlowsheetValidator
{
    private readonly ILogger<FlowsheetValidator> _logger;
    
    public FlowsheetValidator(ILogger<FlowsheetValidator> logger)
    {
        _logger = logger;
    }
    
    public ValidationResult Validate(SimulationEntity simulation, IFlowsheet flowsheet)
    {
        var errors = new List<ValidationError>();
        
        _logger.LogInformation(
            "Starting comprehensive validation for simulation {Id}: {Name}",
            simulation.Id, simulation.Name);
        
        // Phase 1: Topology validation (disconnected units, orphaned streams)
        var topologyErrors = ValidateTopology(simulation);
        errors.AddRange(topologyErrors);
        _logger.LogDebug("Topology validation: {Count} errors", topologyErrors.Count);
        
        // Phase 2: Compound validation (no compounds, undefined references)
        var compoundErrors = ValidateCompounds(simulation);
        errors.AddRange(compoundErrors);
        _logger.LogDebug("Compound validation: {Count} errors", compoundErrors.Count);
        
        // Phase 3: Physical properties validation (temperature, pressure, flow, composition)
        var physicalErrors = ValidatePhysicalProperties(simulation);
        errors.AddRange(physicalErrors);
        _logger.LogDebug("Physical properties validation: {Count} errors", physicalErrors.Count);
        
        // Phase 4: Unit operations validation (configuration, topology)
        var unitErrors = ValidateUnitOperations(simulation);
        errors.AddRange(unitErrors);
        _logger.LogDebug("Unit operations validation: {Count} errors", unitErrors.Count);
        
        _logger.LogInformation(
            "Validation completed for simulation {Id}. Total errors: {ErrorCount}",
            simulation.Id, errors.Count);
        
        return new ValidationResult(errors);
    }
    
    #region Topology Validation
    
    private List<ValidationError> ValidateTopology(SimulationEntity simulation)
    {
        var errors = new List<ValidationError>();
        
        // Check for disconnected unit operations
        foreach (var unit in simulation.UnitOperations)
        {
            if (unit.InputStreamIds.Count == 0 && unit.OutputStreamIds.Count == 0)
            {
                errors.Add(new ValidationError(
                    ValidationErrorCodes.DisconnectedUnit,
                    $"Unit operation '{unit.Name}' has no connected streams",
                    "UnitOperation",
                    unit.Name
                ));
            }
        }
        
        // Check for orphaned streams
        var connectedStreamIds = simulation.UnitOperations
            .SelectMany(u => u.InputStreamIds.Concat(u.OutputStreamIds))
            .ToHashSet();
            
        foreach (var stream in simulation.MaterialStreams)
        {
            if (!connectedStreamIds.Contains(stream.Id))
            {
                errors.Add(new ValidationError(
                    ValidationErrorCodes.OrphanedStream,
                    $"Stream '{stream.Name}' is not connected to any unit operation",
                    "MaterialStream",
                    stream.Name
                ));
            }
        }
        
        foreach (var stream in simulation.EnergyStreams)
        {
            if (!connectedStreamIds.Contains(stream.Id))
            {
                errors.Add(new ValidationError(
                    ValidationErrorCodes.OrphanedStream,
                    $"Stream '{stream.Name}' is not connected to any unit operation",
                    "EnergyStream",
                    stream.Name
                ));
            }
        }
        
        return errors;
    }
    
    #endregion
    
    #region Compound Validation
    
    private List<ValidationError> ValidateCompounds(SimulationEntity simulation)
    {
        var errors = new List<ValidationError>();
        
        // Check if compounds exist
        if (!simulation.Compounds.Any())
        {
            errors.Add(new ValidationError(
                ValidationErrorCodes.NoCompoundsDefined,
                "Simulation must have at least one compound defined. Add compounds before running the simulation.",
                "Simulation",
                simulation.Name
            ));
            return errors; // Early return - can't validate compound references without compounds
        }
        
        // Get defined compound names (case-insensitive comparison)
        var definedCompounds = simulation.Compounds
            .Select(c => c.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        // Validate streams reference defined compounds
        foreach (var stream in simulation.MaterialStreams)
        {
            if (stream.Composition != null)
            {
                foreach (var compoundName in stream.Composition.Keys)
                {
                    if (!definedCompounds.Contains(compoundName))
                    {
                        errors.Add(new ValidationError(
                            ValidationErrorCodes.UndefinedCompoundReference,
                            $"Stream '{stream.Name}' references undefined compound '{compoundName}'. " +
                            $"Available compounds: {string.Join(", ", definedCompounds)}",
                            "MaterialStream",
                            stream.Name
                        ));
                    }
                }
            }
        }
        
        // Validate ShortcutColumn light/heavy keys reference valid compounds
        var compoundIds = simulation.Compounds.Select(c => c.Id).ToHashSet();
        
        foreach (var unit in simulation.UnitOperations.OfType<ShortcutColumnObject>())
        {
            if (unit.LightKey != Guid.Empty && !compoundIds.Contains(unit.LightKey))
            {
                errors.Add(new ValidationError(
                    ValidationErrorCodes.InvalidLightKeyReference,
                    $"ShortcutColumn '{unit.Name}' references invalid LightKey compound ID: {unit.LightKey}",
                    "ShortcutColumn",
                    unit.Name
                ));
            }
            
            if (unit.HeavyKey != Guid.Empty && !compoundIds.Contains(unit.HeavyKey))
            {
                errors.Add(new ValidationError(
                    ValidationErrorCodes.InvalidHeavyKeyReference,
                    $"ShortcutColumn '{unit.Name}' references invalid HeavyKey compound ID: {unit.HeavyKey}",
                    "ShortcutColumn",
                    unit.Name
                ));
            }
        }
        
        return errors;
    }
    
    #endregion
    
    #region Physical Properties Validation
    
    private List<ValidationError> ValidatePhysicalProperties(SimulationEntity simulation)
    {
        var errors = new List<ValidationError>();
        
        // Validate material streams (delegates to entity validation)
        foreach (var stream in simulation.MaterialStreams)
        {
            try
            {
                stream.Validate();
            }
            catch (ArgumentException ex)
            {
                errors.Add(ConvertExceptionToError(ex, "MaterialStream", stream.Name));
            }
        }
        
        // Validate energy streams (delegates to entity validation)
        foreach (var stream in simulation.EnergyStreams)
        {
            try
            {
                stream.Validate();
            }
            catch (ArgumentException ex)
            {
                errors.Add(ConvertExceptionToError(ex, "EnergyStream", stream.Name));
            }
        }
        
        // Additional validation: Composition sum (not in entity)
        foreach (var stream in simulation.MaterialStreams)
        {
            if (stream.Composition != null && stream.Composition.Any())
            {
                double sum = stream.Composition.Values.Sum();
                
                if (Math.Abs(sum - ValidationConstants.ExpectedCompositionSum) > ValidationConstants.CompositionSumTolerance)
                {
                    errors.Add(new ValidationError(
                        ValidationErrorCodes.InvalidCompositionSum,
                        $"Stream '{stream.Name}' composition sums to {sum:F4} (must be {ValidationConstants.ExpectedCompositionSum} ± {ValidationConstants.CompositionSumTolerance}). " +
                        $"Please adjust mole fractions to sum to {ValidationConstants.ExpectedCompositionSum}.",
                        "MaterialStream",
                        stream.Name
                    ));
                }
                
                // Check for negative compositions
                foreach (var (compound, fraction) in stream.Composition)
                {
                    if (fraction < 0)
                    {
                        errors.Add(new ValidationError(
                            ValidationErrorCodes.NegativeComposition,
                            $"Stream '{stream.Name}' has negative composition for compound '{compound}': {fraction}. " +
                            $"Mole fractions must be non-negative.",
                            "MaterialStream",
                            stream.Name
                        ));
                    }
                }
            }
        }
        
        return errors;
    }
    
    #endregion
    
    #region Unit Operations Validation
    
    private List<ValidationError> ValidateUnitOperations(SimulationEntity simulation)
    {
        var errors = new List<ValidationError>();
        
        foreach (var unit in simulation.UnitOperations)
        {
            try
            {
                unit.Validate();
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
            {
                errors.Add(ConvertExceptionToError(ex, unit.Type.ToString(), unit.Name));
            }
        }
        
        return errors;
    }
    
    #endregion
    
    #region Exception to Error Conversion (Adapter Pattern)
    
    /// <summary>
    /// Converts domain entity validation exceptions to structured ValidationError objects.
    /// This adapter method maps exception messages to specific error codes for fine-grained error reporting.
    /// </summary>
    private ValidationError ConvertExceptionToError(
        Exception ex, 
        string entityType, 
        string entityName)
    {
        var message = ex.Message;
        
        // Fine-grained error code mapping based on exception message patterns
        string code = (message, ex) switch
        {
            // Temperature errors
            (var msg, _) when msg.Contains("Temperature") && msg.Contains("greater than 0") 
                => ValidationErrorCodes.InvalidTemperature,
            (var msg, _) when msg.Contains("OutletTemperature") 
                => ValidationErrorCodes.InvalidOutletTemperature,
            
            // Pressure errors
            (var msg, _) when msg.Contains("Pressure") && msg.Contains("non-negative") 
                => ValidationErrorCodes.InvalidPressure,
            (var msg, _) when msg.Contains("OutletPressure") 
                => ValidationErrorCodes.InvalidOutletPressure,
            (var msg, _) when msg.Contains("CondenserPressure") 
                => ValidationErrorCodes.InvalidCondenserPressure,
            (var msg, _) when msg.Contains("ReboilerPressure") 
                => ValidationErrorCodes.InvalidReboilerPressure,
            (var msg, _) when msg.Contains("PressureDrop") 
                => ValidationErrorCodes.InvalidPressureDrop,
            
            // Flow errors
            (var msg, _) when msg.Contains("MassFlow") 
                => ValidationErrorCodes.InvalidMassFlow,
            (var msg, _) when msg.Contains("MolarFlow") 
                => ValidationErrorCodes.InvalidMolarFlow,
            (var msg, _) when msg.Contains("EnergyFlow") 
                => ValidationErrorCodes.InvalidEnergyFlow,
            
            // Unit operation configuration errors
            (var msg, _) when msg.Contains("Efficiency") 
                => ValidationErrorCodes.InvalidEfficiency,
            (var msg, _) when msg.Contains("split ratio") 
                => ValidationErrorCodes.SplitterInvalidRatios,
            (var msg, _) when msg.Contains("RefluxRatio") 
                => ValidationErrorCodes.InvalidRefluxRatio,
            (var msg, _) when msg.Contains("Stages") 
                => ValidationErrorCodes.InvalidStagesCount,
            (var msg, _) when msg.Contains("Tolerance") 
                => ValidationErrorCodes.InvalidTolerance,
            (var msg, _) when msg.Contains("MaxIterations") 
                => ValidationErrorCodes.InvalidMaxIterations,
            (var msg, _) when msg.Contains("HeatDuty") 
                => ValidationErrorCodes.InvalidHeatDuty,
            
            // Topology errors (unit operation input/output requirements)
            (var msg, _) when msg.Contains("input stream") && msg.Contains("exactly one") 
                => ValidationErrorCodes.UnitRequiresSingleInput,
            (var msg, _) when msg.Contains("output stream") && msg.Contains("exactly one") 
                => ValidationErrorCodes.UnitRequiresSingleOutput,
            (var msg, _) when msg.Contains("input stream") && msg.Contains("at least two") 
                => ValidationErrorCodes.UnitRequiresMultipleInputs,
            (var msg, _) when msg.Contains("output stream") && msg.Contains("at least two") 
                => ValidationErrorCodes.UnitRequiresMultipleOutputs,
            (var msg, _) when msg.Contains("input stream") && msg.Contains("at least one") 
                => ValidationErrorCodes.UnitRequiresInput,
            (var msg, _) when msg.Contains("output stream") && msg.Contains("two output") 
                => ValidationErrorCodes.UnitRequiresTwoOutputs,
            
            // Null reference errors
            (_, ArgumentNullException) => ValidationErrorCodes.NullReferenceError,
            
            // Fallback for unmapped errors
            _ => ValidationErrorCodes.ValidationError
        };
        
        return new ValidationError(
            code, 
            message, 
            entityType, 
            entityName,
            ErrorSeverity.Error
        );
    }
    
    #endregion
}
