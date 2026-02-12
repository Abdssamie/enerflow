using System.ComponentModel.DataAnnotations;
using Enerflow.Domain.Enums;
using Enerflow.Domain.ValueObjects;

namespace Enerflow.Domain.DTOs;

// --- API Request/Validation DTOs ---

public record AddUnitRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }
    public required UnitOperationType UnitOperation { get; init; }

    public Position Position { get; set; } = new (0, 0);
}

public record ConnectStreamRequest
{
    public required Guid UnitId { get; init; }
    public required Guid StreamId { get; init; }
    public required PortType PortType { get; init; }
    public string? PortName { get; init; } // Optional: Specific port name on the unit (e.g. "Inlet 1")
}

public record SubmitJobRequest
{
    public required Guid SimulationId { get; init; }
}

public record CreateSimulationRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }
    public required PropertyPackageType PropertyPackage { get; init; }
    public required FlashAlgorithm FlashAlgorithm { get; init; }
    public required SystemOfUnits SystemOfUnits { get; init; }
}

public record AddStreamRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Range(0d, 10000d)]
    public double Temperature { get; init; } = 298.15; // Raw value in specified unit system

    [Range(0d, 100000000d)]
    public double Pressure { get; init; } = 101325;    // Raw value in specified unit system

    [Range(0d, 1000000d)]
    public double MassFlow { get; init; } = 1.0;       // Raw value in specified unit system
    
    public SystemOfUnits SystemOfUnits { get; init; } = SystemOfUnits.SI;  // Default to SI
    
    public Dictionary<string, double> Composition { get; init; } = new();
}

public record AddCompoundRequest
{
    public AddCompoundRequest(string name)
    {
        Name = name;
    }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }
}
