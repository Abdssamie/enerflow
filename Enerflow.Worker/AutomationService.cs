using System.Diagnostics;
using System.Xml.Linq;
using DWSIM.Automation;
using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Entities;
using Enerflow.Domain.ValueObjects;

namespace Enerflow.Worker;

public class AutomationService : IDisposable
{
    private Automation3? _automationManager;
    private IFlowsheet? _flowsheet;
    private readonly string _path;

    public AutomationService(string inputPath)
    {
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"Input file not found: {inputPath}");

        _automationManager = new Automation3();
        _path = inputPath;
        
        Console.WriteLine($"Loading flowsheet from {_path}...");
        _flowsheet = _automationManager.LoadFlowsheet(_path);
        
        if (_flowsheet == null)
            throw new Exception("Failed to load flowsheet.");
            
        Console.WriteLine("Flowsheet loaded successfully.");
    }

    public SimulationResult RunJob(SimulationJob job)
    {
        if (_flowsheet == null) throw new InvalidOperationException("Flowsheet is not loaded.");

        var result = new SimulationResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Apply Diffs (Stream Overrides)
            ApplyStreamOverrides(job.StreamOverrides);

            // 2. Apply Generic Overrides (if any)
            ApplyGenericOverrides(job.Overrides);

            // 3. Solve (Standard)
            Console.WriteLine($"Calculating Flowsheet...");
            _automationManager!.CalculateFlowsheet2(_flowsheet);
            
            // Errors are not returned, so we assume success if Solved is true
            List<Exception>? errors = null;

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            // 4. Collect Logs/Errors
            if (errors != null && errors.Count > 0)
            {
                result.Success = false;
                result.StatusMessage = "Simulation finished with errors.";
                foreach (var err in errors)
                {
                    result.LogMessages.Add(err.Message);
                    Console.Error.WriteLine($"- Error: {err.Message}");
                }
            }
            else if (!_flowsheet.Solved)
            {
                 result.Success = false;
                 result.StatusMessage = "Simulation did not converge (No explicit errors).";
                 result.LogMessages.Add("Solver finished but Solved flag is false.");
            }
            else
            {
                result.Success = true;
                result.StatusMessage = "Converged successfully.";
                Console.WriteLine("Converged successfully.");
            }

            // 5. Collect Results
            result.Streams = GetResults();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.StatusMessage = $"Worker Exception: {ex.Message}";
            result.LogMessages.Add(ex.ToString());
            Console.Error.WriteLine($"Critical Error: {ex}");
        }

        return result;
    }

    private void ApplyStreamOverrides(Dictionary<string, StreamState> overrides)
    {
        foreach (var (tag, state) in overrides)
        {
            var obj = _flowsheet!.GetObject(tag);
            if (obj is IMaterialStream stream)
            {
                Console.WriteLine($"Updating Stream '{tag}': T={state.Temperature}K, P={state.Pressure}Pa, Flow={state.MassFlow}kg/s");
                stream.SetTemperature(state.Temperature);
                stream.SetPressure(state.Pressure);
                stream.SetMassFlow(state.MassFlow);

                if (state.MoleFractions != null && state.MoleFractions.Length > 0)
                {
                    stream.SetOverallComposition(state.MoleFractions);
                }
            }
            else
            {
                Console.WriteLine($"Warning: Override target '{tag}' not found or not a stream.");
            }
        }
    }

    private void ApplyGenericOverrides(Dictionary<string, Dictionary<string, double>> overrides)
    {
        foreach (var (tag, props) in overrides)
        {
            var obj = _flowsheet!.GetObject(tag);
            if (obj != null)
            {
                foreach (var (propName, value) in props)
                {
                    // Generic property setting via DWSIM's helper or Reflection could go here.
                    // For MVP, we'll stick to SetPropertyValue if available, or skip.
                    try
                    {
                        // Note: DWSIM objects often implement GetPropertyValue/SetPropertyValue
                        // But precise API varies. For now, logging limitation.
                        Console.WriteLine($"[TODO] Setting generic property {tag}.{propName} = {value}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to set {tag}.{propName}: {ex.Message}");
                    }
                }
            }
        }
    }

    public Dictionary<string, StreamState> GetResults()
    {
        var results = new Dictionary<string, StreamState>();
        if (_flowsheet == null) return results;

        foreach (var obj in _flowsheet.SimulationObjects.Values)
        {
            if (obj is IMaterialStream stream)
            {
                var state = StreamState.Create(
                    stream.GetTemperature(),
                    stream.GetPressure(),
                    stream.GetMassFlow(),
                    stream.GetMolarFlow(),
                    stream.GetMassEnthalpy(),
                    stream.GetOverallComposition()
                );
                results[((ISimulationObject)stream).GraphicObject.Tag] = state;
            }
        }
        return results;
    }

    public void Dispose()
    {
        _flowsheet = null;
        _automationManager = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
