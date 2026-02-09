using System.Diagnostics;
using DWSIM.Automation;
using DWSIM.Interfaces;
using Enerflow.Domain.DTOs;
using Enerflow.Worker.Builders;
using Enerflow.Worker.Mappers;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Solvers;

public class DWSIMSolver : ISimulationSolver
{
    private readonly Automation3 _automation;
    private readonly IFlowsheetBuilder _flowsheetBuilder;
    private readonly IStreamMapper _streamMapper;
    private readonly IUnitOperationMapper _unitOpMapper;
    private readonly IConnectionMapper _connectionMapper;
    private readonly IPostConnectionConfigurator _postConfigurator;
    private readonly IResultCollector _resultCollector;
    private readonly ILogger<DWSIMSolver> _logger;

    public DWSIMSolver(
        Automation3 automation,
        IFlowsheetBuilder flowsheetBuilder,
        IStreamMapper streamMapper,
        IUnitOperationMapper unitOpMapper,
        IConnectionMapper connectionMapper,
        IPostConnectionConfigurator postConfigurator,
        IResultCollector resultCollector,
        ILogger<DWSIMSolver> logger)
    {
        _automation = automation;
        _flowsheetBuilder = flowsheetBuilder;
        _streamMapper = streamMapper;
        _unitOpMapper = unitOpMapper;
        _connectionMapper = connectionMapper;
        _postConfigurator = postConfigurator;
        _resultCollector = resultCollector;
        _logger = logger;
    }

    public SimulationResult Solve(SimulationEntity simulation)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("Starting simulation solve for Job {JobId}", simulation.Id);

        // 1. Build Flowsheet (Foundation)
        IFlowsheet flowsheet = _flowsheetBuilder.BuildFlowsheet(simulation);
        _logger.LogInformation("Flowsheet built successfully for Job {JobId}", simulation.Id);

        // 2. Map Streams
        _logger.LogInformation("Mapping {Count} material streams for Job {JobId}", simulation.MaterialStreams.Count,
            simulation.Id);
        foreach (var ms in simulation.MaterialStreams)
        {
            _streamMapper.MapMaterialStream(ms, flowsheet);
        }

        _logger.LogInformation("Material streams mapped successfully for Job {JobId}", simulation.Id);

        _logger.LogInformation("Mapping {Count} energy streams for Job {JobId}", simulation.EnergyStreams.Count,
            simulation.Id);
        foreach (var es in simulation.EnergyStreams)
        {
            _streamMapper.MapEnergyStream(es, flowsheet);
        }

        _logger.LogInformation("Energy streams mapped successfully for Job {JobId}", simulation.Id);

        // 3. Map Unit Operations
        var compoundLookup = simulation.Compounds.ToDictionary(c => c.Id, c => c.Name);

        _logger.LogInformation("Mapping {Count} unit operations for Job {JobId}", simulation.UnitOperations.Count,
            simulation.Id);
        foreach (var unit in simulation.UnitOperations)
        {
            _unitOpMapper.Map(unit, flowsheet, compoundLookup);
        }

        _logger.LogInformation("Unit operations mapped successfully for Job {JobId}", simulation.Id);

        // 4. Map Connections
        _logger.LogInformation("Mapping connections for Job {JobId}", simulation.Id);
        _connectionMapper.MapConnections(simulation, flowsheet);
        _logger.LogInformation("Connections mapped successfully for Job {JobId}", simulation.Id);

        // 5. Post-Connection Configuration (Splitter Ratios, etc.)
        _logger.LogInformation("Configuring post-connection settings for Job {JobId}", simulation.Id);
        _postConfigurator.ConfigurePostConnection(simulation, flowsheet);
        _logger.LogInformation("Post-connection configuration completed for Job {JobId}", simulation.Id);

        // 6. Solve using DWSIM's modern CalculateFlowsheet4 API
        _logger.LogInformation("Starting DWSIM calculation for Job {JobId}", simulation.Id);

        try
        {
            // Use the modern CalculateFlowsheet4 API which:
            // - Activates the calculator
            // - Sets SolverBreakOnException = true
            // - Calls SolveFlowsheet2() internally
            // - Returns List<Exception> of any errors
            _logger.LogInformation("Calling DWSIM CalculateFlowsheet4 for Job {JobId}", simulation.Id);
            var errors = _automation.CalculateFlowsheet4(flowsheet);
            _logger.LogInformation("DWSIM CalculateFlowsheet4 completed for Job {JobId}", simulation.Id);

            // Check if flowsheet solved successfully
            if (!flowsheet.Solved)
            {
                var errorMsg = !string.IsNullOrEmpty(flowsheet.ErrorMessage)
                    ? flowsheet.ErrorMessage
                    : "Flowsheet failed to solve (no error message provided)";
                _logger.LogError("Flowsheet failed to solve: {Error}", errorMsg);
                
                sw.Stop();
                return new SimulationResult
                {
                    JobId = simulation.Id,
                    Success = false,
                    ErrorMessage = errorMsg,
                    ExecutionTime = sw.Elapsed
                };
            }

            // Log any calculation errors returned
            if (errors is { Count: > 0 })
            {
                _logger.LogWarning("DWSIM reported {Count} errors during calculation", errors.Count);
                foreach (var err in errors)
                {
                    _logger.LogWarning("DWSIM Error: {Message}", err.Message);
                }
            }

            // Check for individual object errors
            var objectErrors = new List<string>();
            foreach (var obj in flowsheet.SimulationObjects.Values)
            {
                if (!string.IsNullOrEmpty(obj.ErrorMessage))
                {
                    var errorMsg = $"{obj.GraphicObject?.Tag ?? obj.Name}: {obj.ErrorMessage}";
                    _logger.LogWarning("Object error: {Error}", errorMsg);
                    objectErrors.Add(errorMsg);
                }
            }

            sw.Stop();

            // If there are object errors, consider it a failure
            if (objectErrors.Count > 0)
            {
                var combinedErrors = string.Join("; ", objectErrors);
                _logger.LogError("Simulation completed but {Count} object(s) have errors", objectErrors.Count);
                
                return new SimulationResult
                {
                    JobId = simulation.Id,
                    Success = false,
                    ErrorMessage = $"Simulation completed but {objectErrors.Count} object(s) have errors: {combinedErrors}",
                    ExecutionTime = sw.Elapsed
                };
            }

            // Success - extract results
            _logger.LogInformation("Simulation {JobId} solved successfully in {Time:F2}s", 
                simulation.Id, sw.Elapsed.TotalSeconds);

            var result = new SimulationResult
            {
                JobId = simulation.Id,
                Success = true,
                ExecutionTime = sw.Elapsed,
                ErrorMessage = null
            };

            _resultCollector.ExtractResults(flowsheet, simulation, result);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Error solving simulation {Id}", simulation.Id);
            return new SimulationResult
            {
                JobId = simulation.Id,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTime = sw.Elapsed
            };
        }
    }
}

public class ConvergenceException : Exception
{
    public ConvergenceException(string message) : base(message)
    {
    }
}