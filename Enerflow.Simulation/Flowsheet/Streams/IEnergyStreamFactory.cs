using DomainEnergyStream = Enerflow.Domain.Entities.Streams.EnergyStream;
using DwsimEnergyStream = DWSIM.UnitOperations.Streams.EnergyStream;

namespace Enerflow.Simulation.Flowsheet.Streams;

/// <summary>
/// Interface for creating and configuring DWSIM energy streams.
/// </summary>
public interface IEnergyStreamFactory
{
    /// <summary>
    /// Configures an existing DWSIM energy stream instance.
    /// Used when the stream is created via flowsheet.AddObject().
    /// </summary>
    void Configure(DwsimEnergyStream stream, DomainEnergyStream streamEntity);
}
