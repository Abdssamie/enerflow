using System.Text.Json;
using DWSIM.Interfaces;
using Enerflow.Domain.DTOs;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;
using DWSIMMaterialStream = DWSIM.Thermodynamics.Streams.MaterialStream;
using DWSIMEnergyStream = DWSIM.UnitOperations.Streams.EnergyStream;
using DWSIM.UnitOperations.UnitOperations;

namespace Enerflow.Worker.Solvers;

public class ResultCollector : IResultCollector
{
   private readonly ILogger<ResultCollector> _logger;

   public ResultCollector(ILogger<ResultCollector> logger)
   {
      _logger = logger;
   }

   public void ExtractResults(IFlowsheet flowsheet, SimulationEntity simulation, SimulationResult result)
   {
      _logger.LogDebug("Extracting results for simulation {Id}", simulation.Id);

      // 1. Extract Material Stream Results
      foreach (var domainStream in simulation.MaterialStreams)
      {
         // Builder creates objects with ID as the key
         var streamId = domainStream.Id.ToString();
         if (flowsheet.SimulationObjects.TryGetValue(streamId, out var simObj) && simObj is DWSIMMaterialStream dwsimStream)
         {
            var composition = new Dictionary<string, double>();
            if (dwsimStream.Phases.Count > 0)
            {
               foreach (var kvp in dwsimStream.Phases[0].Compounds)
               {
                  composition[kvp.Key] = kvp.Value.MoleFraction ?? 0.0;
               }
            }

            result.StreamResults.Add(new StreamResultDto
            {
               StreamId = domainStream.Id,
               Temperature = dwsimStream.GetTemperature(),
               Pressure = dwsimStream.GetPressure(),
               MassFlow = dwsimStream.GetMassFlow(),
               Phase = dwsimStream.Phases[0].Name, // "Mixture" usually
               MolarCompositions = composition
            });
         }
         else
         {
            _logger.LogWarning("Material Stream {Name} (ID: {Id}) not found in flowsheet or invalid type.", domainStream.Name, domainStream.Id);
         }
      }

      // 2. Extract Unit Operation Results
      foreach (var unit in simulation.UnitOperations)
      {
         // Builder creates objects with ID as the key
         var unitId = unit.Id.ToString();
         if (flowsheet.SimulationObjects.TryGetValue(unitId, out var simObj))
         {
            var calculatedParams = new Dictionary<string, object>
            {
               ["Calculated"] = simObj.Calculated
            };

            if (!string.IsNullOrEmpty(simObj.ErrorMessage))
            {
               calculatedParams["Error"] = simObj.ErrorMessage;
            }

            // Type-specific extraction
            switch (simObj)
            {
               case Heater heater:
                  calculatedParams["DeltaQ"] = heater.DeltaQ.GetValueOrDefault();
                  calculatedParams["DeltaT"] = heater.DeltaT.GetValueOrDefault();
                  calculatedParams["OutletTemperature"] = heater.OutletTemperature.GetValueOrDefault();
                  calculatedParams["Efficiency"] = double.IsNaN(heater.Efficiency) ? 0.0 : heater.Efficiency;
                  break;

               case Cooler cooler:
                  calculatedParams["DeltaQ"] = cooler.DeltaQ.GetValueOrDefault();
                  calculatedParams["DeltaT"] = cooler.DeltaT.GetValueOrDefault();
                  calculatedParams["OutletTemperature"] = cooler.OutletTemperature.GetValueOrDefault();
                  calculatedParams["Efficiency"] = double.IsNaN(cooler.Efficiency) ? 0.0 : cooler.Efficiency;
                  break;

               case Valve valve:
                  calculatedParams["DeltaP"] = valve.DeltaP.GetValueOrDefault();
                  calculatedParams["OutletPressure"] = valve.OutletPressure.GetValueOrDefault();
                  calculatedParams["DeltaT"] = valve.DeltaT.GetValueOrDefault();
                  break;

               case Splitter splitter:
                  if (splitter.Ratios != null)
                  {
                     var ratios = splitter.Ratios.Cast<double>().ToList();
                     calculatedParams["Ratios"] = ratios;
                  }
                  break;

               case Vessel vessel:
                  calculatedParams["DeltaQ"] = vessel.DeltaQ.GetValueOrDefault();
                  break;

               case DWSIMEnergyStream es:
                  calculatedParams["EnergyFlow"] = es.EnergyFlow.GetValueOrDefault();
                  break;
            }

            result.UnitResults.Add(new UnitResultDto
            {
               UnitId = unit.Id,
               CalculatedParams = JsonSerializer.SerializeToDocument(calculatedParams)
            });
         }
         else
         {
            _logger.LogWarning("Unit Operation {Name} (ID: {Id}) not found in flowsheet.", unit.Name, unit.Id);
         }
      }
   }
}
