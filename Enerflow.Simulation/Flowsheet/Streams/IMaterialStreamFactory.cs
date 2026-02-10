using Enerflow.Domain.Enums;
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
}
