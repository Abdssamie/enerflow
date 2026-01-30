using System.ComponentModel.DataAnnotations;
using Enerflow.Domain.Enums;

namespace Enerflow.Domain.DTOs;

// --- API Request/Validation DTOs ---

public record AddUnitRequest
{
    [Required]
    [StringLength(100)]
    public required string Name { get; init; }
    public required UnitOperationType UnitOperation { get; init; }
    public double PositionX { get; init; } = 0;
    public double PositionY { get; init; } = 0;
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

    [Required]
    [StringLength(50)]
    public required string ThermoPackage { get; init; }

    [Required]
    [StringLength(50)]
    public required string FlashAlgorithm { get; init; }

    [Required]
    [StringLength(20)]
    public required string SystemOfUnits { get; init; }
}

public record AddStreamRequest
{
    [Required]
    [StringLength(100)]
    public required string Name { get; init; }

    [Range(0d, 10000d)]
    public double Temperature { get; init; } = 298.15; // K

    [Range(0d, 1000000000d)]
    public double Pressure { get; init; } = 101325;    // Pa

    [Range(0d, 1000000d)]
    public double MassFlow { get; init; } = 1.0;       // kg/s

    public Dictionary<string, double> MolarCompositions { get; init; } = new();
}

public record AddCompoundRequest
{
    [Required]
    [StringLength(100)]
    public required string Name { get; init; }
}
