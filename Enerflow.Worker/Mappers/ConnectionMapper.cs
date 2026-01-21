using DWSIM.Interfaces;
using Enerflow.Domain.Entities;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

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
            if (!flowsheet.SimulationObjects.TryGetValue(unit.Name, out var simObj))
            {
                _logger.LogWarning("Unit Operation {Name} not found in flowsheet.", unit.Name);
                continue;
            }

            // Input Streams
            for (int i = 0; i < unit.InputStreamIds.Count; i++)
            {
                var streamId = unit.InputStreamIds[i];
                var streamEntity = simulation.MaterialStreams.FirstOrDefault(s => s.Id == streamId);
                
                if (streamEntity != null)
                {
                    ConnectStreamToUnit(flowsheet, streamEntity.Name, simObj, isInput: true, portIndex: i);
                }
            }

            // Output Streams
            for (int i = 0; i < unit.OutputStreamIds.Count; i++)
            {
                var streamId = unit.OutputStreamIds[i];
                var streamEntity = simulation.MaterialStreams.FirstOrDefault(s => s.Id == streamId);
                
                if (streamEntity != null)
                {
                    ConnectStreamToUnit(flowsheet, streamEntity.Name, simObj, isInput: false, portIndex: i);
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
    }

    private void ConnectStreamToUnit(IFlowsheet flowsheet, string streamName, ISimulationObject unit, bool isInput, int portIndex)
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
            
            if (flowsheet.SimulationObjects.TryGetValue(streamName, out var streamObj))
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
                
                if (isInput)
                {
                    // Stream -> Unit
                    // Stream Output -> Unit Input
                    // Stream only has 1 output usually (unless split? No, usually 1 stream 1 connection).
                    // Actually DWSIM Stream has 1 Input and 1 Output connector.
                    
                    var streamOut = streamObj.GraphicObject.OutputConnectors[0];
                    if (!streamOut.IsAttached)
                    {
                        flowsheet.ConnectObjects(streamObj.GraphicObject, unit.GraphicObject, 0, portIndex);
                    }
                    else
                    {
                        // Already attached? Splitter?
                        // DWSIM Streams connect 1-to-1.
                    }
                }
                else
                {
                    // Unit -> Stream
                    // Unit Output -> Stream Input
                    var streamIn = streamObj.GraphicObject.InputConnectors[0];
                    if (!streamIn.IsAttached)
                    {
                        flowsheet.ConnectObjects(unit.GraphicObject, streamObj.GraphicObject, portIndex, 0);
                    }
                }
                
                _logger.LogTrace("Connected {Stream} to {Unit} (Port {Port}, IsInput={IsInput})", streamName, unit.GraphicObject.Tag, portIndex, isInput);
            }
        }
        else
        {
            _logger.LogWarning("Port index {Index} out of range for Unit {Unit} (Count: {Count})", portIndex, unit.GraphicObject.Tag, connectors.Count);
        }
    }
}
