namespace Enerflow.Domain.Entities;

public class EnergyStream
{
    public Guid Id { get; set; } = Common.IdGenerator.NextGuid();
    public required Guid SimulationId { get; set; }
    public required string Name { get; set; }
    public double EnergyFlow { get; set; }
}
