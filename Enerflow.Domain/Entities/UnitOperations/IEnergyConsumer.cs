namespace Enerflow.Domain.Entities.UnitOperations;

/// <summary>
/// Marker interface for unit operations that consume energy via an energy stream input.
/// Examples: Heater, Cooler, Compressor, Pump, FlashDrum, PipeSegment
/// </summary>
public interface IEnergyConsumer
{
    /// <summary>
    /// Optional Energy Stream input connection ID
    /// </summary>
    Guid? EnergyInputId { get; set; }
}
