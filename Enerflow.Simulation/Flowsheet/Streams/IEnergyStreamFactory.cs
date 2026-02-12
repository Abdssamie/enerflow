using DomainEnergyStream = Enerflow.Domain.Entities.Streams.EnergyStream;
using DwsimEnergyStream = DWSIM.UnitOperations.Streams.EnergyStream;
using DWSIM.Interfaces;
using Enerflow.Domain.Enums;

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
    void Configure(DwsimEnergyStream stream, DomainEnergyStream streamEntity, SystemOfUnits systemOfUnits);

    /// <summary>
    /// Creates and configures multiple energy streams in the flowsheet.
    /// </summary>
    void CreateAndConfigureStreams(
        IFlowsheet flowsheet,
        IEnumerable<DomainEnergyStream> streams,
        SystemOfUnits systemOfUnits);
}
