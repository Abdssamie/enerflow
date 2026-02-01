using DWSIM.UnitOperations.Streams;
using Enerflow.Domain.DTOs;

namespace Enerflow.Simulation.Flowsheet.Streams;

/// <summary>
/// Interface for creating and configuring DWSIM energy streams.
/// </summary>
public interface IEnergyStreamFactory
{
    /// <summary>
    /// Creates and configures a DWSIM energy stream from a DTO.
    /// </summary>
    EnergyStream CreateEnergyStream(EnergyStreamDto streamDto);

    /// <summary>
    /// Configures an existing DWSIM energy stream instance.
    /// Used when the stream is created via flowsheet.AddObject().
    /// </summary>
    void Configure(EnergyStream stream, EnergyStreamDto streamDto);
}
