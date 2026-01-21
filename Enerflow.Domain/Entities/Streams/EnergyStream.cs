namespace Enerflow.Domain.Entities.Streams;

public class EnergyStream : SimulationObject
{
    public double EnergyFlow { get; set; } // kW

    public override void Validate()
    {
        if (EnergyFlow < 0)
            throw new ArgumentException("EnergyFlow cannot be negative.", nameof(EnergyFlow));
    }
}
