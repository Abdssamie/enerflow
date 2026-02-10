using DomainEnergyStream = Enerflow.Domain.Entities.EnergyStream;
using DwsimEnergyStream = DWSIM.UnitOperations.Streams.EnergyStream;
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
    DwsimEnergyStream CreateEnergyStream(EnergyStreamDto streamDto);

    /// <summary>
    /// Configures an existing DWSIM energy stream instance.
    /// Used when the stream is created via flowsheet.AddObject().
    /// </summary>
    void Configure(DwsimEnergyStream stream, EnergyStreamDto streamDto);

    /// <summary>
    /// Configures an existing DWSIM energy stream instance.
    /// Used when the stream is created via flowsheet.AddObject().
    /// </summary>
    void Configure(DwsimEnergyStream stream, DomainEnergyStream streamEntity);
}
