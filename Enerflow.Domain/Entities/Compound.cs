using System.Text.Json;

namespace Enerflow.Domain.Entities;

public class Compound
{
    public Guid Id { get; set; } = Common.IdGenerator.NextGuid();
    public required Guid SimulationId { get; set; }
    public required string Name { get; set; }
    
    // Storing chemical data flexibly
    public JsonDocument? ConstantProperties { get; set; }
}
