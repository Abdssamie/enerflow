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

	// 2. Map Energy Streams to Units
	foreach (var unit in domainSimulation.UnitOperations)
	{
		var unitId = unit.Id.ToString();
		
		if (!flowsheet.SimulationObjects.TryGetValue(unitId, out var simObj))
		{
			continue;
		}

		// Check for energy input connections (Heater, Cooler, Compressor, Pump)
		Guid? energyInputId = unit switch
		{
			HeaterObject heater => heater.EnergyInputId,
			CoolerObject cooler => cooler.EnergyInputId,
			_ => null
		};

		if (energyInputId.HasValue)
		{
			var energyStream = domainSimulation.EnergyStreams.FirstOrDefault(s => s.Id == energyInputId.Value);
			if (energyStream != null)
			{
				ConnectEnergyStreamToUnit(flowsheet, energyStream.Id.ToString(), simObj, isInput: true);
			}
		}

		// Check for energy output connections (Reactor, Expander)
		// TODO: Add when Reactor/Expander entities are implemented with EnergyOutputId property
	}
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

	private void ConnectEnergyStreamToUnit(IFlowsheet flowsheet, string energyStreamId, ISimulationObject unit, bool isInput)
	{
		if (!flowsheet.SimulationObjects.TryGetValue(energyStreamId, out var energyStreamObj)) return;

		try
		{
			// Energy streams connect to port 1 for Heater/Cooler (per CONNECTION_PORTS.md)
			const int energyPortIndex = 1;

			if (isInput)
			{
				// Energy Stream -> Unit
				flowsheet.ConnectObjects(energyStreamObj.GraphicObject, unit.GraphicObject, 0, energyPortIndex);
			}
			else
			{
				// Unit -> Energy Stream (for reactors, expanders)
				flowsheet.ConnectObjects(unit.GraphicObject, energyStreamObj.GraphicObject, energyPortIndex, 0);
			}

		_logger.LogInformation("Connected energy stream {StreamId} to unit {Unit} (IsInput={IsInput})",
			energyStreamId, unit.GraphicObject.Tag, isInput);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to connect energy stream {StreamId} to unit {Unit} (IsInput={IsInput})",
				energyStreamId, unit.GraphicObject.Tag, isInput);
			throw;
		}
	}
}
