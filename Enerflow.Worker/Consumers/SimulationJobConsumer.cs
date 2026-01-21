// TODO: CRITICAL LINUX STABILITY VERIFICATION REQUIRED
// The DWSIM thermodynamics engine and GDI+ dependencies are unverified in this Linux environment.
// Before MVP deployment, must verify:
// 1. Successful convergence of a flowsheet with a Recycle loop (Thermodynamic Stress).
// 2. Process stability over 500+ consecutive flash calculations.
// 3. Graceful recovery/timeout when DWSIM's solver hangs (RequestCalculation).
// 4. Resource cleanup validation (ReleaseResources) to prevent container OOM.

using System.Text.Json;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Entities;
using Enerflow.Domain.Enums;
using Enerflow.Domain.Interfaces;
using Enerflow.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Enerflow.Worker.Consumers;

/// <summary>
/// MassTransit consumer for processing simulation jobs from the message queue.
/// Orchestrates the full simulation lifecycle: Build -> Solve -> Collect -> Persist.
/// </summary>
public class SimulationJobConsumer : IConsumer<SimulationJob>
{
    private readonly ILogger<SimulationJobConsumer> _logger;
    private readonly ISimulationService _simulationService;
    private readonly EnerflowDbContext _dbContext;

    public SimulationJobConsumer(
        ILogger<SimulationJobConsumer> logger,
        ISimulationService simulationService,
        EnerflowDbContext dbContext)
    {
        _logger = logger;
        _simulationService = simulationService;
        _dbContext = dbContext;
    }

    public async Task Consume(ConsumeContext<SimulationJob> context)
    {
        var job = context.Message;
        var cancellationToken = context.CancellationToken;

        _logger.LogInformation(
            "Processing Job {JobId} for Simulation {SimulationId} - Definition: {DefinitionName}",
            job.JobId,
            job.SimulationId,
            job.Definition.Name);

        try
        {
            // Update simulation status to Running
            await UpdateSimulationStatusAsync(job.SimulationId, SimulationStatus.Running, null, null, cancellationToken);

            // Step 1: Build the flowsheet from the definition
            _logger.LogInformation("Step 1/4: Building flowsheet for Job {JobId}", job.JobId);
            var buildSuccess = _simulationService.BuildFlowsheet(job.Definition);

            if (!buildSuccess)
            {
                var errors = string.Join("; ", _simulationService.GetErrorMessages());
                _logger.LogError("Failed to build flowsheet for Job {JobId}: {Errors}", job.JobId, errors);
                await UpdateSimulationStatusAsync(job.SimulationId, SimulationStatus.Failed, $"Build failed: {errors}", null, cancellationToken);
                return;
            }

            _logger.LogDebug("Flowsheet built successfully for Job {JobId}", job.JobId);

            // Step 2: Solve the flowsheet
            _logger.LogInformation("Step 2/4: Solving flowsheet for Job {JobId}", job.JobId);
            var solveSuccess = _simulationService.Solve();

            if (!solveSuccess)
            {
                var errors = string.Join("; ", _simulationService.GetErrorMessages());
                _logger.LogWarning("Flowsheet solved with errors for Job {JobId}: {Errors}", job.JobId, errors);
                // Continue to collect partial results even with errors
            }

            // Step 3: Collect results
            _logger.LogInformation("Step 3/4: Collecting results for Job {JobId}", job.JobId);
            var results = _simulationService.CollectResults();

            _logger.LogDebug(
                "Collected results for Job {JobId}: {StreamCount} streams, {UnitOpCount} unit operations",
                job.JobId,
                results.MaterialStreams.Count,
                results.UnitOperations.Count);

            // Step 4: Persist results to database
            _logger.LogInformation("Step 4/4: Persisting results for Job {JobId}", job.JobId);
            await PersistResultsAsync(job.SimulationId, results, solveSuccess, cancellationToken);

            _logger.LogInformation(
                "Job {JobId} completed successfully. Status: {Status}",
                job.JobId,
                solveSuccess ? "Converged" : "Converged with warnings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error processing Job {JobId}", job.JobId);

            // Update simulation status to Failed
            await UpdateSimulationStatusAsync(
                job.SimulationId,
                SimulationStatus.Failed,
                $"Critical error: {ex.Message}",
                null,
                cancellationToken);
        }
        finally
        {
            // Dispose the simulation service to clean up DWSIM resources
            _simulationService.Dispose();
        }
    }

    /// <summary>
    /// Updates the simulation status in the database.
    /// </summary>
    private async Task UpdateSimulationStatusAsync(
        Guid simulationId,
        SimulationStatus status,
        string? errorMessage,
        JsonDocument? resultJson,
        CancellationToken cancellationToken)
    {
        try
        {
            var simulation = await _dbContext.Simulations
                .FirstOrDefaultAsync(s => s.Id == simulationId, cancellationToken);

            if (simulation != null)
            {
                simulation.Status = status;
                simulation.ErrorMessage = errorMessage;
                simulation.ResultJson = resultJson;
                simulation.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Updated Simulation {SimulationId} status to {Status}", simulationId, status);
            }
            else
            {
                _logger.LogWarning("Simulation {SimulationId} not found in database", simulationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update simulation status for {SimulationId}", simulationId);
        }
    }

    /// <summary>
    /// Persists simulation results to the database.
    /// Updates both the Simulation entity and individual MaterialStream entities.
    /// </summary>
    private async Task PersistResultsAsync(
        Guid simulationId,
        SimulationResultsDto results,
        bool solveSuccess,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get simulation and its streams
            var simulation = await _dbContext.Simulations
                .FirstOrDefaultAsync(s => s.Id == simulationId, cancellationToken);

            if (simulation == null)
            {
                _logger.LogWarning("Simulation {SimulationId} not found for result persistence", simulationId);
                return;
            }

            // Get material streams for this simulation
            var materialStreams = await _dbContext.MaterialStreams
                .Where(s => s.SimulationId == simulationId)
                .ToListAsync(cancellationToken);

            // Update each material stream with results
            foreach (var stream in materialStreams)
            {
                if (results.MaterialStreams.TryGetValue(stream.Name, out var streamResult))
                {
                    UpdateMaterialStreamFromResults(stream, streamResult);
                }
            }

            // Create result JSON blob from strongly-typed results
            var resultJson = JsonSerializer.SerializeToDocument(results);

            // Update simulation status
            simulation.Status = solveSuccess ? SimulationStatus.Converged : SimulationStatus.Failed;
            simulation.ResultJson = resultJson;
            simulation.ErrorMessage = solveSuccess ? null : string.Join("; ", _simulationService.GetErrorMessages());
            simulation.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Persisted results for Simulation {SimulationId}: {StreamCount} streams updated",
                simulationId,
                materialStreams.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist results for Simulation {SimulationId}", simulationId);

            // Try to at least update the status
            await UpdateSimulationStatusAsync(
                simulationId,
                SimulationStatus.Failed,
                $"Failed to persist results: {ex.Message}",
                null,
                cancellationToken);
        }
    }

    /// <summary>
    /// Updates a MaterialStream entity with results from the DWSIM simulation.
    /// </summary>
    private void UpdateMaterialStreamFromResults(MaterialStream stream, MaterialStreamResultDto result)
    {
        try
        {
            stream.Temperature = result.Temperature;
            stream.Pressure = result.Pressure;
            stream.MassFlow = result.MassFlow;
            stream.MolarCompositions = result.MolarCompositions;

            _logger.LogDebug("Updated stream {StreamName}: T={Temp}, P={Pres}, F={Flow}",
                stream.Name, stream.Temperature, stream.Pressure, stream.MassFlow);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update stream {StreamName} from results", stream.Name);
        }
    }
}
