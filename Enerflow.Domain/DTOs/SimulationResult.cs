using System.Text.Json;

namespace Enerflow.Domain.DTOs;

// --- Worker Result DTOs ---

public record SimulationResult
{
    public required Guid JobId { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionTime { get; init; }

    public List<StreamResultDto> StreamResults { get; init; } = new();
    public List<UnitResultDto> UnitResults { get; init; } = new();
}

public record StreamResultDto
{
    public required Guid StreamId { get; init; }
    public double Temperature { get; init; }
    public double Pressure { get; init; }
    public double MassFlow { get; init; }
    public Dictionary<string, double> MolarCompositions { get; init; } = new();
    public string? Phase { get; init; }
}

public record UnitResultDto
{
    public required Guid UnitId { get; init; }
    // Polymorphic results (e.g., Calculated Duty, Efficiency, etc.)
    public JsonDocument? CalculatedParams { get; init; }
}
