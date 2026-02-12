using Enerflow.Domain.ValueObjects;

namespace Enerflow.Domain.Entities.Streams;

public class EnergyStream : SimulationObject
{
    public EnergyFlow EnergyFlow { get; set; } = new(0.0); // kW

    public override void Validate()
    {
        if (EnergyFlow.Value < 0)
            throw new ArgumentException("EnergyFlow cannot be negative.", nameof(EnergyFlow));
    }
}
