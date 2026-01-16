using System.Text.Json;
using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities;

public class Simulation
{
    public Guid Id { get; set; } = Common.IdGenerator.NextGuid();
    public required string Name { get; set; }
    public required string ThermoPackage { get; set; }
    public required string SystemOfUnits { get; set; }

    // Execution state
    public SimulationStatus Status { get; set; } = SimulationStatus.Created;
    public string? ErrorMessage { get; set; }

    // Results stored as JSON blob (for quick retrieval)
    public JsonDocument? ResultJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
