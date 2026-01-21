using DWSIM.Interfaces;
using Enerflow.Domain.Entities;
using Enerflow.Domain.Entities.UnitOperations;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;
using DWSIMSplitter = DWSIM.UnitOperations.UnitOperations.Splitter;

namespace Enerflow.Worker.Mappers;

public interface IPostConnectionConfigurator
{
    void ConfigurePostConnection(SimulationEntity simulation, IFlowsheet flowsheet);
}

public class PostConnectionConfigurator : IPostConnectionConfigurator
{
    private readonly ILogger<PostConnectionConfigurator> _logger;

    public PostConnectionConfigurator(ILogger<PostConnectionConfigurator> logger)
    {
        _logger = logger;
    }

    public void ConfigurePostConnection(SimulationEntity simulation, IFlowsheet flowsheet)
    {
        _logger.LogDebug("Running Post-Connection Configuration...");

        foreach (var unit in simulation.UnitOperations)
        {
            if (unit is SplitterObject splitterDomain && 
                flowsheet.SimulationObjects.TryGetValue(splitterDomain.Name, out var simObj) && 
                simObj is DWSIMSplitter splitterDWSIM)
            {
                ConfigureSplitterRatios(splitterDomain, splitterDWSIM, flowsheet, simulation);
            }
        }
    }

    private void ConfigureSplitterRatios(SplitterObject domain, DWSIMSplitter dwsim, IFlowsheet flowsheet, SimulationEntity simulation)
    {
        // Splitter Ratios in Domain: Dictionary<Guid, double> (StreamId -> Ratio)
        // Splitter Ratios in DWSIM: List/Array of doubles corresponding to Output Ports (0, 1, 2...)
        
        // 1. Get connected output streams from DWSIM object
        var connectedStreams = new List<DWSIM.Thermodynamics.Streams.MaterialStream?>();
        // Iterate Output Connectors
        foreach (var connector in dwsim.GraphicObject.OutputConnectors)
        {
            if (connector.IsAttached)
            {
                // Safest way to get the attached object
                if (flowsheet.SimulationObjects.TryGetValue(connector.AttachedConnector.AttachedTo.Name, out var obj) && 
                    obj is DWSIM.Thermodynamics.Streams.MaterialStream ms)
                {
                    connectedStreams.Add(ms);
                }
                else
                {
                    connectedStreams.Add(null); 
                }
            }
            else
            {
                connectedStreams.Add(null);
            }
        }
        
        // 2. Iterate ports/streams and find matching ratio
        var streamIdToRatio = domain.SplitRatios;
        
        for (int i = 0; i < connectedStreams.Count; i++)
        {
            var dwsimStream = connectedStreams[i];
            if (dwsimStream == null) continue; // Port not connected

            // Find this stream in Domain to get its ID
            var domainStream = simulation.MaterialStreams.FirstOrDefault(s => s.Name == dwsimStream.Name);
            
            if (domainStream != null && streamIdToRatio.TryGetValue(domainStream.Id, out double ratio))
            {
                 if (dwsim.Ratios.Count > i)
                 {
                     dwsim.Ratios[i] = ratio;
                 }
                 else
                 {
                     dwsim.Ratios.Add(ratio);
                 }
                 
                 _logger.LogDebug("Set Splitter {Name} Port {Port} (Stream {Stream}) Ratio to {Ratio}", 
                     domain.Name, i, domainStream.Name, ratio);
            }
        }
    }
}
