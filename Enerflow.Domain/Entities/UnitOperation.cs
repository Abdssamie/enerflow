using System.Text.Json;

namespace Enerflow.Domain.Entities;

public class UnitOperation
{
    public Guid Id { get; set; } = Common.IdGenerator.NextGuid();
    public required Guid SimulationId { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }

    // Topology
    public List<Guid> InputStreamIds { get; set; } = new();
    public List<Guid> OutputStreamIds { get; set; } = new();

    // Unit-specific parameters
    public JsonDocument? ConfigParams { get; set; }
}
