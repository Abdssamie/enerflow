using System.Text.Json;
using Microsoft.Extensions.Logging;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Enums;
using DomainMaterialStream = Enerflow.Domain.Entities.MaterialStream;
using DwsimMaterialStream = DWSIM.Thermodynamics.Streams.MaterialStream;
using DwsimStreamSpec = DWSIM.Thermodynamics.Streams.StreamSpec;

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

    public DwsimMaterialStream CreateMaterialStream(MaterialStreamDto streamDto, SystemOfUnits systemOfUnits)
    {
        try
        {
            var stream = new DwsimMaterialStream(streamDto.Name, "");
            Configure(stream, streamDto, systemOfUnits);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create material stream: {Name}", streamDto.Name);
            throw;
        }
    }

    public void Configure(DwsimMaterialStream stream, MaterialStreamDto streamDto, SystemOfUnits systemOfUnits)
    {
        try
        {
            _ = systemOfUnits;

            var tempK = streamDto.Temperature;
            var pressPa = streamDto.Pressure;
            var massFlowKgS = streamDto.MassFlow;

            stream.SpecType = DwsimStreamSpec.Temperature_and_Pressure;

            if (tempK > 0)
            {
                stream.Phases[0].Properties.temperature = tempK;
            }

            if (pressPa > 0)
            {
                stream.Phases[0].Properties.pressure = pressPa;
            }

            if (massFlowKgS > 0)
            {
                stream.Phases[0].Properties.massflow = massFlowKgS;
            }

            foreach (var (compoundName, moleFraction) in streamDto.MolarCompositions)
            {
                if (stream.Phases[0].Compounds.ContainsKey(compoundName))
                {
                    stream.Phases[0].Compounds[compoundName].MoleFraction = moleFraction;
                }
                else
                {
                    _logger.LogWarning(
                        "Compound {Compound} not found in stream {Name} - skipping composition.",
                        compoundName,
                        streamDto.Name
                    );
                }
            }

            _logger.LogDebug("Configured material stream: {Name} (T={T}K, P={P}Pa, F={F}kg/s)",
                streamDto.Name, tempK, pressPa, massFlowKgS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure material stream: {Name}", streamDto.Name);
            throw;
        }
    }

    public void Configure(DwsimMaterialStream stream, DomainMaterialStream streamEntity, SystemOfUnits systemOfUnits)
    {
        try
        {
            _ = systemOfUnits;

            var compositions = DeserializeMolarCompositions(streamEntity.MolarCompositions, streamEntity.Name);

            var tempK = streamEntity.Temperature;
            var pressPa = streamEntity.Pressure;
            var massFlowKgS = streamEntity.MassFlow;

            stream.SpecType = DwsimStreamSpec.Temperature_and_Pressure;

            if (tempK > 0)
            {
                stream.Phases[0].Properties.temperature = tempK;
            }

            if (pressPa > 0)
            {
                stream.Phases[0].Properties.pressure = pressPa;
            }

            if (massFlowKgS > 0)
            {
                stream.Phases[0].Properties.massflow = massFlowKgS;
            }

            foreach (var (compoundName, moleFraction) in compositions)
            {
                if (stream.Phases[0].Compounds.ContainsKey(compoundName))
                {
                    stream.Phases[0].Compounds[compoundName].MoleFraction = moleFraction;
                }
                else
                {
                    _logger.LogWarning(
                        "Compound {Compound} not found in stream {Name} - skipping composition.",
                        compoundName,
                        streamEntity.Name
                    );
                }
            }

            _logger.LogDebug("Configured material stream: {Name} (T={T}K, P={P}Pa, F={F}kg/s)",
                streamEntity.Name, tempK, pressPa, massFlowKgS);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure material stream: {Name}", streamEntity.Name);
            throw;
        }
    }

    private IReadOnlyDictionary<string, double> DeserializeMolarCompositions(JsonDocument? compositions, string streamName)
    {
        if (compositions is null)
        {
            return new Dictionary<string, double>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, double>>(compositions.RootElement.GetRawText())
                ?? new Dictionary<string, double>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse molar compositions for stream {Name}.", streamName);
            return new Dictionary<string, double>();
        }
    }

}
