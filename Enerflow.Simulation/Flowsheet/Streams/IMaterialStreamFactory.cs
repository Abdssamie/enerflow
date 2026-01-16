using Enerflow.Domain.DTOs;
using DWSIM.Thermodynamics.Streams;
using Microsoft.Extensions.Logging;

namespace Enerflow.Simulation.Flowsheet.Streams;

/// <summary>
/// Interface for creating and configuring DWSIM material streams.
/// </summary>
public interface IMaterialStreamFactory
{
    /// <summary>
    /// Creates and configures a DWSIM material stream from a DTO.
    /// </summary>
    MaterialStream CreateMaterialStream(MaterialStreamDto streamDto);
}
