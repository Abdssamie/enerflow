using Enerflow.Domain.DTOs;
using DWSIM.Thermodynamics.Streams;
using Microsoft.Extensions.Logging;

namespace Enerflow.Simulation.Flowsheet.Streams;

/// <summary>
/// Factory for creating and configuring DWSIM material streams.
/// </summary>
public class MaterialStreamFactory : IMaterialStreamFactory
{
    private readonly ILogger<MaterialStreamFactory> _logger;

    public MaterialStreamFactory(ILogger<MaterialStreamFactory> logger)
    {
        _logger = logger;
    }

    public MaterialStream CreateMaterialStream(MaterialStreamDto streamDto)
    {
        try
        {
            var stream = new MaterialStream(streamDto.Name, "");

            // Set stream conditions
            stream.Phases[0].Properties.temperature = streamDto.Temperature;
            stream.Phases[0].Properties.pressure = streamDto.Pressure;
            stream.Phases[0].Properties.massflow = streamDto.MassFlow;

            // Set compositions
            foreach (var (compoundName, moleFraction) in streamDto.MolarCompositions)
            {
                if (stream.Phases[0].Compounds.ContainsKey(compoundName))
                {
                    stream.Phases[0].Compounds[compoundName].MoleFraction = moleFraction;
                }
            }

            _logger.LogDebug("Created material stream: {Name}", streamDto.Name);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create material stream: {Name}", streamDto.Name);
            throw;
        }
    }
}
