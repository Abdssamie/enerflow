using Enerflow.Domain.Common;
using Enerflow.Domain.Enums;
using Enerflow.Domain.ValueObjects;

namespace Enerflow.Domain.Entities;

public class MaterialStream : Entity
{
    public required string Tag { get; set; }
    public string? Description { get; set; }
    public Coordinates Position { get; set; } = new(0, 0);

    // Connectivity
    public Guid? SourceUnitId { get; set; }
    public string? SourcePortName { get; set; }
    public Guid? DestinationUnitId { get; set; }
    public string? DestinationPortName { get; set; }

    // Initial Conditions (Inputs)
    public double Temperature { get; set; } = 298.15; // K
    public double Pressure { get; set; } = 101325;    // Pa
    public double MassFlow { get; set; } = 1.0;       // kg/s
    
    // Composition (Mole Fractions)
    public Dictionary<string, double> Composition { get; set; } = new(); // CompoundId -> Fraction
}

public class EnergyStream : Entity
{
    public required string Tag { get; set; }
    public string? Description { get; set; }
    public Coordinates Position { get; set; } = new(0, 0);

    // Connectivity
    public Guid? SourceUnitId { get; set; } // e.g. Controller
    public string? SourcePortName { get; set; }
    public Guid? DestinationUnitId { get; set; } // e.g. Heater
    public string? DestinationPortName { get; set; }
    
    // Value
    public double EnergyFlow { get; set; } // kW
}
