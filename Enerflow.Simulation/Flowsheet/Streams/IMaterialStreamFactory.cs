using Enerflow.Domain.DTOs;
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
    /// Creates and configures a DWSIM material stream from a DTO.
    /// </summary>
    DwsimMaterialStream CreateMaterialStream(
        MaterialStreamDto streamDto,
        SystemOfUnits systemOfUnits
    );

    /// <summary>
    /// Configures an existing DWSIM material stream instance.
    /// Used when the stream is created via flowsheet.AddObject().
    /// </summary>
    void Configure(
        DwsimMaterialStream stream,
        MaterialStreamDto streamDto,
        SystemOfUnits systemOfUnits
    );

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
