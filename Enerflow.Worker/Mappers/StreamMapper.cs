using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums;
using DWSIM.Interfaces.Enums.GraphicObjects;
using Enerflow.Domain.Entities.Streams;
using Microsoft.Extensions.Logging;
using DWSIMMaterialStream = DWSIM.Thermodynamics.Streams.MaterialStream;
using DWSIMEnergyStream = DWSIM.UnitOperations.Streams.EnergyStream;

namespace Enerflow.Worker.Mappers;

public class StreamMapper : IStreamMapper
{
    private readonly ILogger<StreamMapper> _logger;

    public StreamMapper(ILogger<StreamMapper> logger)
    {
        _logger = logger;
    }

    public void MapMaterialStream(MaterialStream domainStream, IFlowsheet flowsheet)
    {
        _logger.LogInformation("Mapping Material Stream: {Name} (ID: {Id})", domainStream.Name, domainStream.Id);

        // 1. Find existing stream (created by Builder)
        // The Builder creates streams with ID as the object ID
        var streamId = domainStream.Id.ToString();
        
        if (!flowsheet.SimulationObjects.ContainsKey(streamId))
        {
            _logger.LogError("Material stream {Name} (ID: {Id}) not found in flowsheet. Builder should have created it.", 
                domainStream.Name, domainStream.Id);
            throw new InvalidOperationException($"Material stream {domainStream.Name} not found in flowsheet");
        }

        var obj = flowsheet.SimulationObjects[streamId];
        var ms = (DWSIMMaterialStream)obj;

        // 2. Set Properties (SI Units: K, Pa, kg/s)
        ms.Phases[0].Properties.temperature = domainStream.Temperature;
        ms.Phases[0].Properties.pressure = domainStream.Pressure;
        
        // Only set mass flow if it's non-zero (inlet streams)
        // Outlet streams (MassFlow = 0) should be calculated by DWSIM
        if (domainStream.MassFlow > 0)
        {
            ms.Phases[0].Properties.massflow = domainStream.MassFlow;
        }

        // Note: If MolarFlow is provided and MassFlow is 0, we might want to set MolarFlow.
        // Domain entity has both, but usually one drives the other. 
        // DWSIM prioritizes MassFlow usually, or it depends on how it's solved.
        // If MassFlow is set, we use it.

        // 3. Set Composition
        // CRITICAL: Do NOT use AddCompoundsToMaterialStream.
        // Iterate existing compounds in the stream's phase and update fractions.
        
        // Normalize check: DWSIM expects fractions summing to 1? 
        // We assume domainStream.Composition is valid.
        
        // Reset all to 0 first? New stream usually defaults to 0 or equal?
        // Actually, let's just set what we have.
        
        // Warning: DWSIM might have compounds that are not in the domainStream if the flowsheet has extra compounds.
        // We should explicitly set those to 0 if needed, but for now we trust the domain stream matches the simulation compound list.
        
        foreach (var kvp in domainStream.Composition)
        {
            var compoundName = kvp.Key;
            var moleFraction = kvp.Value;

            if (ms.Phases[0].Compounds.ContainsKey(compoundName))
            {
                ms.Phases[0].Compounds[compoundName].MoleFraction = moleFraction;
            }
            else
            {
                _logger.LogWarning("Compound {Compound} not found in DWSIM stream {Stream}", compoundName, domainStream.Name);
            }
        }

        // 4. Spec Type handling
        // Default is Temperature_and_Pressure
        ms.SpecType = StreamSpec.Temperature_and_Pressure;
        
        // If PhaseType is explicit, we might consider FlashSpec changes, but typically T & P define the state.
        // The domain 'Phase' property is usually an output or a constraint check, not necessarily an input unless T/P are unknown.
    }

    public void MapEnergyStream(EnergyStream domainStream, IFlowsheet flowsheet)
    {
        _logger.LogInformation("Mapping Energy Stream: {Name} (ID: {Id})", domainStream.Name, domainStream.Id);

        // 1. Find existing stream (created by Builder)
        var streamId = domainStream.Id.ToString();
        
        if (!flowsheet.SimulationObjects.ContainsKey(streamId))
        {
            _logger.LogError("Energy stream {Name} (ID: {Id}) not found in flowsheet. Builder should have created it.", 
                domainStream.Name, domainStream.Id);
            throw new InvalidOperationException($"Energy stream {domainStream.Name} not found in flowsheet");
        }

        var obj = flowsheet.SimulationObjects[streamId];
        var es = (DWSIMEnergyStream)obj;

        // Set Energy Flow (Units: kW in DWSIM SI)
        es.EnergyFlow = domainStream.EnergyFlow;
    }
}
