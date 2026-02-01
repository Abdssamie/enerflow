using Enerflow.Domain.DTOs;
using DWSIM.Thermodynamics.Streams;
using Enerflow.Domain.Enums;

namespace Enerflow.Simulation.Flowsheet.Streams;

/// <summary>
/// Interface for creating and configuring DWSIM material streams.
/// </summary>
public interface IMaterialStreamFactory
{
    /// <summary>
    /// Creates and configures a DWSIM material stream from a DTO.
    /// </summary>
    MaterialStream CreateMaterialStream(
        MaterialStreamDto streamDto,
        SystemOfUnits systemOfUnits
    );

    /// <summary>
    /// Configures an existing DWSIM material stream instance.
    /// Used when the stream is created via flowsheet.AddObject().
    /// </summary>
    void Configure(
        MaterialStream stream,
        MaterialStreamDto streamDto,
        SystemOfUnits systemOfUnits
    );
}