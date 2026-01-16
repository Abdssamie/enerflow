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

public enum PortType
{
    Inlet,
    Outlet
}
