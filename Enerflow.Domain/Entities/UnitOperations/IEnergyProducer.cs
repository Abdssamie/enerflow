namespace Enerflow.Domain.Entities.UnitOperations;

/// <summary>
/// Marker interface for unit operations that produce energy via an energy stream output.
/// Examples: Expander, Reactor
/// </summary>
public interface IEnergyProducer
{
    /// <summary>
    /// Optional Energy Stream output connection ID
    /// </summary>
    Guid? EnergyOutputId { get; set; }
}
