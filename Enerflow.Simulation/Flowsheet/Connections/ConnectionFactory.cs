using DWSIM.Interfaces;
using Enerflow.Domain.Entities.UnitOperations;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Simulation.Flowsheet.Connections;

#pragma warning disable CA1873

public class ConnectionFactory : IConnectionFactory
{
   private readonly ILogger<ConnectionFactory> _logger;

   public ConnectionFactory(ILogger<ConnectionFactory> logger)
   {
      _logger = logger;
   }

   public void ConnectFlowsheet(SimulationEntity domainSimulation, IFlowsheet flowsheet)
   {
      _logger.LogDebug("Mapping Connections...");

      // 1. Map Material Streams to Units
      foreach (var unit in domainSimulation.UnitOperations)
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
            var streamEntity = domainSimulation.MaterialStreams.FirstOrDefault(s => s.Id == streamId);

            if (streamEntity == null) continue;

            // Use stream ID instead of name for lookup
            ConnectStreamToUnit(flowsheet, streamEntity.Id.ToString(), simObj, isInput: true, portIndex: i);
         }

         // Output Streams
         for (var i = 0; i < unit.OutputStreamIds.Count; i++)
         {
            var streamId = unit.OutputStreamIds[i];
            var streamEntity = domainSimulation.MaterialStreams.FirstOrDefault(s => s.Id == streamId);

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
   }

   private void ConnectStreamToUnit(IFlowsheet flowsheet, string streamId, ISimulationObject unit, bool isInput, int portIndex)
   {
      var connectors = isInput ? unit.GraphicObject.InputConnectors : unit.GraphicObject.OutputConnectors;

      if (portIndex < connectors.Count)
      {
         if (!flowsheet.SimulationObjects.TryGetValue(streamId, out var streamObj)) return;

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
      else
      {
         _logger.LogWarning("Port index {Index} out of range for Unit {Unit} (Count: {Count})", portIndex, unit.GraphicObject.Tag, connectors.Count);
      }
   }
}
