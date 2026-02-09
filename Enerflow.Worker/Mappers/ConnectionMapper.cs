using DWSIM.Interfaces;
using Enerflow.Domain.Entities.UnitOperations;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;
using Splitter = DWSIM.UnitOperations.UnitOperations.Splitter;

namespace Enerflow.Worker.Mappers;

public class ConnectionMapper : IConnectionMapper
{
    private readonly ILogger<ConnectionMapper> _logger;

    public ConnectionMapper(ILogger<ConnectionMapper> logger)
    {
        _logger = logger;
    }

    public void MapConnections(SimulationEntity simulation, IFlowsheet flowsheet)
    {
        _logger.LogDebug("Mapping Connections...");

        // 1. Map Material Streams to Units
        foreach (var unit in simulation.UnitOperations)
        {
            // Builder creates objects with ID as the key
            var unitId = unit.Id.ToString();
            if (!flowsheet.SimulationObjects.TryGetValue(unitId, out var simObj))
            {
                _logger.LogWarning("Unit Operation {Name} (ID: {Id}) not found in flowsheet.", unit.Name, unit.Id);
                continue;
            }

            // Input Streams
            for (var i = 0; i < unit.InputStreamIds.Count; i++)
            {
                var streamId = unit.InputStreamIds[i];
                var streamEntity = simulation.MaterialStreams.FirstOrDefault(s => s.Id == streamId);
                
                if (streamEntity != null)
                {
                    // Use stream ID instead of name for lookup
                    ConnectStreamToUnit(flowsheet, streamEntity.Id.ToString(), simObj, isInput: true, portIndex: i);
                }
            }

            // Output Streams
            for (var i = 0; i < unit.OutputStreamIds.Count; i++)
            {
                var streamId = unit.OutputStreamIds[i];
                var streamEntity = simulation.MaterialStreams.FirstOrDefault(s => s.Id == streamId);
                
                if (streamEntity != null)
                {
                    // Use stream ID instead of name for lookup
                    ConnectStreamToUnit(flowsheet, streamEntity.Id.ToString(), simObj, isInput: false, portIndex: i);
                }
            }
        }
        
        // 2. Map Energy Streams to Units (Recycles, Heaters, etc.)
        // Usually Energy Streams are handled similarly but attached to Energy Connectors.
        // Domain UnitOperationObject doesn't strictly separate EnergyInputIds from InputIds in the base class array,
        // but typically they are separate properties or mixed.
        // Let's assume UnitOperationObject has specific handling or we iterate EnergyStreams.
        
        // Actually, Enerflow.Domain UnitOperationObject definition (checked earlier) likely has InputIds/OutputIds
        // which refer to Material Streams, and maybe Energy Input/Output?
        // Let's check `UnitOperationObject` definition or `HeaterObject`.
        // HeaterObject has HeatDuty property, but maybe an Energy Stream connection?
        // If EnergyStream is used as a connection (e.g. for Heater), we need to connect it.
        // Assuming EnergyStreams are also in InputIds/OutputIds OR special properties.
        // DWSIM connects Energy Streams to specific "Energy Connector".
        
        // Let's check `Simulation.EnergyStreams` usage.
        // If an EnergyStream is connected to a unit, the Unit must reference it.
        // Domain model might need `EnergyInputId` / `EnergyOutputId`?
        // Or `InputIds` contains ALL inputs?
        // For strongly typed solver, usually Energy connections are explicit.
        
        // Checking `HeaterObject.cs` content previously read...
        // It has `HeatDuty`.
        // If `CalcMode` is `EnergyStream`, then we expect a connection.
        // Let's assume for now we scan EnergyStreams to see if they reference the Unit?
        // Or we iterate units and check generic connections.
        
        // Given DWSIM's specific port logic (AttachEnergyStreamToPort), we might need to handle this.
        // For now, mapping Material connections is the critical Pass 2. 
        // Energy connections can be added if we see how they are modeled in Domain.
        // If domain model just has `InputIds` for material, we skip energy connections for now 
        // unless they are critical for the solver (e.g. Recycle of Energy).
        
        // 3. Configure Splitter Ratios (Post-Connection)
        // Splitter ratios must be set AFTER connections are made because they depend on 
        // the connection order (which port connects to which stream)
        ConfigureSplitterRatios(simulation, flowsheet);
    }
    
    private void ConfigureSplitterRatios(SimulationEntity simulation, IFlowsheet flowsheet)
    {
        _logger.LogDebug("Configuring splitter ratios after connections...");

        foreach (var unit in simulation.UnitOperations)
        {
            if (unit is SplitterObject splitterDomain &&
                flowsheet.SimulationObjects.TryGetValue(splitterDomain.Id.ToString(), out var simObj) &&
                simObj is Splitter splitterDWSIM)
            {
                ConfigureSingleSplitterRatios(splitterDomain, splitterDWSIM, flowsheet, simulation);
            }
        }
    }

    private void ConfigureSingleSplitterRatios(SplitterObject domain, Splitter dwsim, IFlowsheet flowsheet,
        SimulationEntity simulation)
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

            if (
                domainStream == null
                || !streamIdToRatio.TryGetValue(domainStream.Id, out var ratio)
            ) continue;

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

    private void ConnectStreamToUnit(IFlowsheet flowsheet, string streamId, ISimulationObject unit, bool isInput, int portIndex)
    {
        // DWSIM: flowSheet.AttachStreamToPort(streamName, unitName, portName, isInput)
        // Or simObj.AttachStream(stream, portIndex, isInput) -> Not standard API.
        
        // Standard Automation:
        // flowsheet.ConnectObjects(stream, unit, portIndex, isInput) ??
        // No, typically we find the unit's ports (InputConnectors / OutputConnectors) and Attach.
        
        var connectors = isInput ? unit.GraphicObject.InputConnectors : unit.GraphicObject.OutputConnectors;
        
        if (portIndex < connectors.Count)
        {
            var port = connectors[portIndex];
            // Flowsheet.ConnectObjects(streamName, unit.Name, ...) ?
            // In DWSIM Automation: `flowsheet.ConnectObjects(obj1, obj2)` often auto-guesses.
            // But we want precise port control.
            
            // `Flowsheet.AttachStreamToPort(streamName, unit.Name, portIndex, isInput)`? -> Let's check via grep if unsure.
            // Or manually:
            
            if (flowsheet.SimulationObjects.TryGetValue(streamId, out var streamObj))
            {
                // Connect
                // SimulationObject.AttachTo(port, stream)
                // Actually `flowsheet.Connect(streamId, unitId, portIndex)`
                
                // Let's try `flowsheet.ConnectObjects` if available, or manual connection.
                // Manual:
                // streamObj.GraphicObject.OutputConnectors[0].AttachTo(port) (if stream is input to unit)
                // port.AttachTo(streamObj.GraphicObject.InputConnectors[0]) ??
                
                // Safe way using Automation API usually provided:
                // `flowsheet.Connect(string streamName, string unitName, string portName)`
                
                // Let's use `Connect` if flowsheet exposes it, or use the graphic object connection logic which is standard DWSIM.
                
                try
                {
                    if (isInput)
                    {
                        // Stream -> Unit
                        // Stream Output -> Unit Input
                        flowsheet.ConnectObjects(streamObj.GraphicObject, unit.GraphicObject, 0, portIndex);
                    }
                    else
                    {
                        // Unit -> Stream
                        // Unit Output -> Stream Input
                        flowsheet.ConnectObjects(unit.GraphicObject, streamObj.GraphicObject, portIndex, 0);
                    }
                    
                    _logger.LogInformation("Connected stream {StreamId} to unit {Unit} (Port {Port}, IsInput={IsInput})", 
                        streamId, unit.GraphicObject.Tag, portIndex, isInput);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect stream {StreamId} to unit {Unit} (Port {Port}, IsInput={IsInput})", 
                        streamId, unit.GraphicObject.Tag, portIndex, isInput);
                    throw;
                }
            }
        }
        else
        {
            _logger.LogWarning("Port index {Index} out of range for Unit {Unit} (Count: {Count})", portIndex, unit.GraphicObject.Tag, connectors.Count);
        }
    }
}
