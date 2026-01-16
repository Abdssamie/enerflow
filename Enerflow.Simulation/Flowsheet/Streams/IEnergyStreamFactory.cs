using Enerflow.Domain.DTOs;
using Microsoft.Extensions.Logging;

namespace Enerflow.Simulation.Flowsheet.Streams;

/// <summary>
/// Interface for creating and configuring DWSIM energy streams.
/// </summary>
public interface IEnergyStreamFactory
{
    /// <summary>
    /// Creates and configures a DWSIM energy stream from a DTO.
    /// </summary>
    DWSIM.UnitOperations.Streams.EnergyStream CreateEnergyStream(EnergyStreamDto streamDto);
}
