namespace Enerflow.Domain.Entities;

public class EnergyStream
{
    public required Guid Id { get; set; }
    public required Guid SimulationId { get; set; }
    public required string Name { get; set; }
    public double EnergyFlow { get; set; }
}
