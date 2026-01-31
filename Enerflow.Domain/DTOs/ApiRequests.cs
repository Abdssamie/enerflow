using System.ComponentModel.DataAnnotations;
using Enerflow.Domain.Enums;

namespace Enerflow.Domain.DTOs;

// --- API Request/Validation DTOs ---

public record AddUnitRequest
{
    [Required]
    [StringLength(100)]
    public required string Name { get; init; }

    [Required]
    public required UnitOperationType UnitOperation { get; init; }

    public double PositionX { get; init; } = 0;
    public double PositionY { get; init; } = 0;
}

public record ConnectStreamRequest
{
    [Required]
    public required Guid UnitId { get; init; }

    [Required]
    public required Guid StreamId { get; init; }

    [Required]
    public required PortType PortType { get; init; }

    [StringLength(100)]
    public string? PortName { get; init; } // Optional: Specific port name on the unit (e.g. "Inlet 1")
}

public record SubmitJobRequest
{
    [Required]
    public required Guid SimulationId { get; init; }
}

public record CreateSimulationRequest
{
    [Required]
    [StringLength(100)]
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

    [Range(0d, double.MaxValue)]
    public double Temperature { get; init; } = 298.15; // K

    [Range(0d, double.MaxValue)]
    public double Pressure { get; init; } = 101325;    // Pa

    [Range(0d, double.MaxValue)]
    public double MassFlow { get; init; } = 1.0;       // kg/s

    public Dictionary<string, double> MolarCompositions { get; init; } = new();
}

public record AddCompoundRequest
{
    [Required]
    [StringLength(100)]
    public required string Name { get; init; }
}
