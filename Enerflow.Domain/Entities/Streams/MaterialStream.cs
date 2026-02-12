using Enerflow.Domain.Enums;
using Enerflow.Domain.ValueObjects;

namespace Enerflow.Domain.Entities.Streams;

public class MaterialStream : SimulationObject
{
    public Temperature Temperature { get; init; } = new(298.15); // K
    public Pressure Pressure { get; init; } = new(101325.0); // Pa
    public required MassFlow MassFlow { get; init; } = new(0.0); // kg/s
    public double MolarFlow { get; init; } // mol/s

    public PhaseType Phase { get; init; } = PhaseType.Mixed;

    public Dictionary<string, double> Composition { get; init; } = [];

    public override void Validate()
    {
        if (Temperature.Value <= 0)
            throw new ArgumentException("Temperature must be greater than 0 K.", nameof(Temperature));

        if (Pressure.Value <= 0)
            throw new ArgumentException("Pressure must be greater than 0 Pa.", nameof(Pressure));

        if (MassFlow.Value < 0)
            throw new ArgumentException("MassFlow cannot be negative.", nameof(MassFlow));

        if (MolarFlow < 0)
            throw new ArgumentException("MolarFlow cannot be negative.", nameof(MolarFlow));
    }
}
