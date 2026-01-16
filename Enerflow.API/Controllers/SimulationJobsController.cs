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
}
