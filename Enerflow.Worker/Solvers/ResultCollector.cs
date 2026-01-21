using System.Text.Json;
using DWSIM.Interfaces;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Entities;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;
using DWSIMMaterialStream = DWSIM.Thermodynamics.Streams.MaterialStream;
using DWSIMEnergyStream = DWSIM.UnitOperations.Streams.EnergyStream;

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
            if (flowsheet.SimulationObjects.TryGetValue(domainStream.Name, out var simObj) && simObj is DWSIMMaterialStream dwsimStream)
            {
                var composition = new Dictionary<string, double>();
                if (dwsimStream.Phases.Count > 0)
                {
                    foreach (var kvp in dwsimStream.Phases[0].Compounds)
                    {
                        composition[kvp.Key] = kvp.Value.MoleFraction ?? 0.0;
                    }
                }

                result.StreamResults!.Add(new StreamResultDto
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
                _logger.LogWarning("Material Stream {Name} not found in flowsheet or invalid type.", domainStream.Name);
            }
        }

        // 2. Extract Unit Operation Results
        foreach (var unit in simulation.UnitOperations)
        {
            if (flowsheet.SimulationObjects.TryGetValue(unit.Name, out var simObj))
            {
                var calculatedParams = new Dictionary<string, object>();
                
                // Generic extraction of common calculated properties
                // We can extend this switch for specific unit types to get detailed results
                
                calculatedParams["Calculated"] = simObj.Calculated;
                if (!string.IsNullOrEmpty(simObj.ErrorMessage))
                {
                    calculatedParams["Error"] = simObj.ErrorMessage;
                }

                // Example specific extraction
                if (simObj is DWSIMEnergyStream es)
                {
                    calculatedParams["EnergyFlow"] = es.EnergyFlow;
                }
                
                // We could iterate `simObj.GetProperties(PropertyType.RO)` but that's expensive.
                // Just capturing status for now as per requirements "Extract specific calculated values".
                // TODO: Add more specific property extraction based on Unit Types if needed.

                result.UnitResults!.Add(new UnitResultDto
                {
                    UnitId = unit.Id,
                    CalculatedParams = JsonSerializer.SerializeToDocument(calculatedParams)
                });
            }
        }
    }
}
