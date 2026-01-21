using Enerflow.Domain.Enums;

namespace Enerflow.Domain.DTOs;

// --- API Request/Validation DTOs ---

public record AddUnitRequest
{
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
    public required string Name { get; init; }
    public required string ThermoPackage { get; init; }
    public required string FlashAlgorithm { get; init; }
    public required string SystemOfUnits { get; init; }
}

public record AddStreamRequest
{
    public required string Name { get; init; }
    public double Temperature { get; init; } = 298.15; // K
    public double Pressure { get; init; } = 101325;    // Pa
    public double MassFlow { get; init; } = 1.0;       // kg/s
    public Dictionary<string, double> MolarCompositions { get; init; } = new();
}

public record AddCompoundRequest
{
    public required string Name { get; init; }
}
