using System.Text.Json;

namespace Enerflow.Domain.Entities;

public class Simulation
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string ThermoPackage { get; set; }
    public required string SystemOfUnits { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
