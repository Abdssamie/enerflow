using Enerflow.Domain.Enums;
using DWSIM.Interfaces;
using DomainMaterialStream = Enerflow.Domain.Entities.Streams.MaterialStream;
using DwsimMaterialStream = DWSIM.Thermodynamics.Streams.MaterialStream;

namespace Enerflow.Simulation.Flowsheet.Streams;

/// <summary>
/// Interface for creating and configuring DWSIM material streams.
/// </summary>
public interface IMaterialStreamFactory
{
    /// <summary>
    /// Configures an existing DWSIM material stream from a domain entity.
    /// Used when the stream is created via flowsheet.AddObject().
    /// </summary>
    void Configure(
        DwsimMaterialStream stream,
        DomainMaterialStream streamEntity,
        SystemOfUnits systemOfUnits
    );

    /// <summary>
    /// Creates and configures multiple material streams in the flowsheet.
    /// </summary>
    void CreateAndConfigureStreams(
        IFlowsheet flowsheet,
        IEnumerable<DomainMaterialStream> streams,
        SystemOfUnits systemOfUnits);
}
