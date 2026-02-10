namespace Enerflow.Simulation.Validation;

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<ValidationError> Errors { get; }
    public List<ValidationWarning> Warnings { get; }
    
    public ValidationResult()
    {
        Errors = new List<ValidationError>();
        Warnings = new List<ValidationWarning>();
    }
    
    public ValidationResult(List<ValidationError> errors, List<ValidationWarning>? warnings = null)
    {
        Errors = errors ?? [];
        Warnings = warnings ?? new List<ValidationWarning>();
    }
}
