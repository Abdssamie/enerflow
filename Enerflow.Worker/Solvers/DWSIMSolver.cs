using System.Diagnostics;
using DWSIM.Automation;
using DWSIM.Interfaces;
using Enerflow.Worker.Builders;
using Enerflow.Simulation.Flowsheet.UnitOperations;
using Enerflow.Simulation.Flowsheet.Connections;
using Enerflow.Domain.DTOs;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Solvers;

public class DWSIMSolver : ISimulationSolver
{
    private readonly Automation3 _automation;
    private readonly IFlowsheetBuilder _flowsheetBuilder;
    private readonly IUnitOperationConfigurator _unitOpConfigurator;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IResultCollector _resultCollector;
    private readonly ILogger<DWSIMSolver> _logger;

    public DWSIMSolver(
        Automation3 automation,
        IFlowsheetBuilder flowsheetBuilder,
        IUnitOperationConfigurator unitOpConfigurator,
        IConnectionFactory connectionFactory,
        IResultCollector resultCollector,
        ILogger<DWSIMSolver> logger)
    {
        _automation = automation;
        _flowsheetBuilder = flowsheetBuilder;
        _unitOpConfigurator = unitOpConfigurator;
        _connectionFactory = connectionFactory;
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

        // 2. Configure Unit Operations
        var compoundLookup = simulation.Compounds.ToDictionary(c => c.Id, c => c.Name);

        _logger.LogInformation("Configuring {Count} unit operations for Job {JobId}", simulation.UnitOperations.Count,
            simulation.Id);
        foreach (var unit in simulation.UnitOperations)
        {
            _unitOpConfigurator.Configure(unit, flowsheet, compoundLookup);
        }

        _logger.LogInformation("Unit operations configured successfully for Job {JobId}", simulation.Id);

        // 3. Connect Flowsheet (includes post-connection configuration like splitter ratios)
        _logger.LogInformation("Connecting flowsheet for Job {JobId}", simulation.Id);
        _connectionFactory.ConnectFlowsheet(simulation, flowsheet);
        _logger.LogInformation("Flowsheet connected successfully for Job {JobId}", simulation.Id);

        // 4. Solve using DWSIM's modern CalculateFlowsheet4 API
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
