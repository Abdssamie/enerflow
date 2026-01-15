namespace Enerflow.Domain.Entities;

public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class SimulationRun : Common.Entity
{
    public Guid FlowsheetId { get; set; }
    public Enums.SimulationStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    
    // Snapshots of state
    public string InputJson { get; set; } = "{}";
    public string OutputJson { get; set; } = "{}";
}
