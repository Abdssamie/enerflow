using Enerflow.Domain.DTOs;
using Enerflow.Domain.Enums;
using Enerflow.Domain.Extensions;
using Enerflow.Domain.Interfaces;
using Enerflow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Enerflow.API.Controllers;

[ApiController]
[Route("api/v1/simulation_jobs")]
public class SimulationJobsController : ControllerBase
{
    private readonly EnerflowDbContext _context;
    private readonly IJobProducer _jobProducer;
    private readonly ILogger<SimulationJobsController> _logger;

    public SimulationJobsController(
        EnerflowDbContext context,
        IJobProducer jobProducer,
        ILogger<SimulationJobsController> logger)
    {
        _context = context;
        _jobProducer = jobProducer;
        _logger = logger;
    }

    /// <summary>
    /// Submits a simulation job to the queue.
    /// </summary>
    /// <param name="request">The job submission request containing the simulation ID.</param>
    /// <returns>Accepted (202) with job details, or error.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitJob([FromBody] SubmitJobRequest request)
    {
        var simulation = await _context.Simulations
            .Include(s => s.Compounds)
            .Include(s => s.MaterialStreams)
            .Include(s => s.EnergyStreams)
            .Include(s => s.UnitOperations)
            .FirstOrDefaultAsync(s => s.Id == request.SimulationId);

        if (simulation == null)
        {
            return NotFound($"Simulation with ID {request.SimulationId} not found.");
        }

        if (simulation.Status == SimulationStatus.Running || simulation.Status == SimulationStatus.Pending)
        {
            return Conflict($"Simulation is already in {simulation.Status} state.");
        }

        // Create Job DTO
        var job = simulation.ToSimulationJob();

        // Publish to Queue
        await _jobProducer.PublishJobAsync(job);

        // Update Entity Status
        simulation.Status = SimulationStatus.Pending;
        simulation.UpdatedAt = DateTime.UtcNow;
        // Clear any previous error message
        simulation.ErrorMessage = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Submitted simulation job {JobId} for simulation {SimulationId}", job.JobId, simulation.Id);

        return Accepted(new
        {
            jobId = job.JobId,
            simulationId = simulation.Id,
            status = simulation.Status.ToString()
        });
    }

    /// <summary>
    /// Gets the current status of a simulation job.
    /// </summary>
    /// <param name="id">The simulation ID.</param>
    /// <returns>Status and error message if applicable.</returns>
    [HttpGet("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobStatus(Guid id)
    {
        // Optimized query - only select needed fields, no heavy ResultJson or child entities
        var statusInfo = await _context.Simulations
            .Where(s => s.Id == id)
            .Select(s => new { s.Id, s.Status, s.ErrorMessage, s.UpdatedAt })
            .FirstOrDefaultAsync();

        if (statusInfo == null)
        {
            return NotFound(new { code = "SimulationNotFound", message = $"Simulation with ID {id} not found." });
        }

        return Ok(new
        {
            simulationId = statusInfo.Id,
            status = statusInfo.Status.ToString(),
            errorMessage = statusInfo.ErrorMessage,
            updatedAt = statusInfo.UpdatedAt
        });
    }

    /// <summary>
    /// Gets the result of a completed simulation.
    /// </summary>
    /// <param name="id">The simulation ID.</param>
    /// <returns>Simulation results or appropriate error.</returns>
    [HttpGet("{id:guid}/result")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetJobResult(Guid id)
    {
        var simulation = await _context.Simulations
            .Where(s => s.Id == id)
            .Select(s => new { s.Id, s.Status, s.ErrorMessage, s.ResultJson })
            .FirstOrDefaultAsync();

        if (simulation == null)
        {
            return NotFound(new { code = "SimulationNotFound", message = $"Simulation with ID {id} not found." });
        }

        // Handle based on status
        switch (simulation.Status)
        {
            case SimulationStatus.Converged:
                // Success - return results
                if (simulation.ResultJson == null)
                {
                    return NotFound(new { code = "ResultsNotAvailable", message = "Simulation converged but no results are stored." });
                }
                return Ok(new
                {
                    simulationId = simulation.Id,
                    status = simulation.Status.ToString(),
                    results = simulation.ResultJson
                });

            case SimulationStatus.Failed:
                // AI-Friendly Error structure
                return BadRequest(new
                {
                    code = "SimulationFailed",
                    message = simulation.ErrorMessage ?? "Simulation failed without an error message.",
                    context = new
                    {
                        simulationId = simulation.Id,
                        status = simulation.Status.ToString(),
                        suggestion = "Check the error message for details. Common issues include invalid property package settings, unconverged flash calculations, or missing stream compositions."
                    }
                });

            case SimulationStatus.Pending:
            case SimulationStatus.Running:
                // Still processing - return 202 Accepted
                return Accepted(new
                {
                    simulationId = simulation.Id,
                    status = simulation.Status.ToString(),
                    message = "Simulation is still being processed. Please poll again later."
                });

            default:
                // Created, Loaded, or other states - not yet submitted or ready
                return BadRequest(new
                {
                    code = "SimulationNotReady",
                    message = $"Simulation is in '{simulation.Status}' state. Submit the job first using POST /api/v1/simulation_jobs.",
                    context = new
                    {
                        simulationId = simulation.Id,
                        status = simulation.Status.ToString()
                    }
                });
        }
    }
}
