namespace Enerflow.Worker.Validation;

public class FlowsheetValidationException : Exception
{
    public ValidationResult ValidationResult { get; }
    
    public FlowsheetValidationException(ValidationResult result)
        : base($"Flowsheet validation failed with {result.Errors.Count} error(s): {string.Join("; ", result.Errors.Select(e => e.Message))}")
    {
        ValidationResult = result;
    }
}
