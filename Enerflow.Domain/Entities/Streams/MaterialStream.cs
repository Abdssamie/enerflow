using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities.Streams;

public class MaterialStream : SimulationObject
{
    public double Temperature { get; init; } = 298.15; // K
    public double Pressure { get; init; } = 101325.0; // Pa
    public double MassFlow { get; init; } // kg/s
    public double MolarFlow { get; init; } // mol/s
    
    public PhaseType Phase { get; init; } = PhaseType.Mixed;
    
    public Dictionary<string, double> Composition { get; init; } = [];

    public override void Validate()
    {
        if (Temperature <= 0)
            throw new ArgumentException("Temperature must be greater than 0 K.", nameof(Temperature));
        
        if (Pressure <= 0)
            throw new ArgumentException("Pressure must be greater than 0 Pa.", nameof(Pressure));
            
        if (MassFlow < 0)
            throw new ArgumentException("MassFlow cannot be negative.", nameof(MassFlow));
            
        if (MolarFlow < 0)
            throw new ArgumentException("MolarFlow cannot be negative.", nameof(MolarFlow));
    }
}
