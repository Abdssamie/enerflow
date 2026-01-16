using Enerflow.Domain.ValueObjects;

namespace Enerflow.Domain.DTOs;

public class SimulationJob
{
    public required string SimulationFilePath { get; set; }
    public int TimeoutSeconds { get; set; } = 60;
    
    // Key: Object Tag, Value: Dictionary of PropertyName -> Value
    public Dictionary<string, Dictionary<string, double>> Overrides { get; set; } = new();
    
    // Specific overrides for Material Streams (simpler than generic overrides)
    public Dictionary<string, StreamState> StreamOverrides { get; set; } = new();
}

public class SimulationResult
{
    public bool Success { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public double ExecutionTimeMs { get; set; }
    
    public Dictionary<string, StreamState> Streams { get; set; } = new();
    public List<string> LogMessages { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
}
