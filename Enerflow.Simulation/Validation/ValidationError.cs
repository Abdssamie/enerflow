namespace Enerflow.Simulation.Validation;

public class ValidationError
{
    public string Code { get; init; }
    public string Message { get; init; }
    public string EntityType { get; init; }
    public string EntityName { get; init; }
    public ErrorSeverity Severity { get; init; }
    
    public ValidationError(string code, string message, string entityType, string entityName, ErrorSeverity severity = ErrorSeverity.Error)
    {
        Code = code;
        Message = message;
        EntityType = entityType;
        EntityName = entityName;
        Severity = severity;
    }
}

public class ValidationWarning
{
    public string Code { get; init; }
    public string Message { get; init; }
    
    public ValidationWarning(string code, string message)
    {
        Code = code;
        Message = message;
    }
}

public enum ErrorSeverity
{
    Error,    // Blocks execution
    Warning   // Logged but doesn't block
}
