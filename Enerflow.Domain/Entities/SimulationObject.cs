using Enerflow.Domain.Common;
using Enerflow.Domain.ValueObjects;

namespace Enerflow.Domain.Entities;

/// <summary>
/// Base class for all simulation objects (MaterialStreams, UnitOperations, etc).
/// </summary>
public abstract class SimulationObject
{
    /// <summary>
    /// Unique identifier. Generated sequentially for DB performance.
    /// </summary>
    public Guid Id { get; set; } = IdGenerator.NextGuid();

    /// <summary>
    /// Foreign Key to the parent Simulation.
    /// </summary>
    public required Guid SimulationId { get; set; }

    /// <summary>
    /// User-friendly name of the object (e.g., "Feed Stream", "Mixer-1").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Visual coordinates on the flowsheet.
    /// </summary>
    public Position Position { get; set; }

    /// <summary>
    /// Validates the object state. Implementation should throw ValidationException if invalid.
    /// </summary>
    public abstract void Validate();
}
