using System.Text.Json;
using Enerflow.Domain.Common;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Entities;
using Enerflow.Domain.Enums;
using Enerflow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Enerflow.API.Controllers;

[ApiController]
[Route("api/v1/simulations")]
public class SimulationsController : ControllerBase
{
    private readonly EnerflowDbContext _context;
    private readonly ILogger<SimulationsController> _logger;

    public SimulationsController(
        EnerflowDbContext context,
        ILogger<SimulationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new simulation session (scratchpad).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSimulation([FromBody] CreateSimulationRequest request)
    {
        var simulation = new Simulation
        {
            Name = request.Name,
            ThermoPackage = request.ThermoPackage,
            FlashAlgorithm = request.FlashAlgorithm,
            SystemOfUnits = request.SystemOfUnits,
            Status = SimulationStatus.Created,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Simulations.Add(simulation);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created simulation {SimulationId} with name '{Name}'", simulation.Id, simulation.Name);

        return CreatedAtAction(nameof(GetSimulation), new { id = simulation.Id }, new
        {
            id = simulation.Id,
            name = simulation.Name,
            status = simulation.Status.ToString()
        });
    }

    /// <summary>
    /// Gets the full simulation graph including streams and units.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSimulation(Guid id)
    {
        var simulation = await _context.Simulations
            .Include(s => s.Compounds)
            .Include(s => s.MaterialStreams)
            .Include(s => s.EnergyStreams)
            .Include(s => s.UnitOperations)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (simulation == null)
        {
            return NotFound(new { code = "SimulationNotFound", message = $"Simulation with ID {id} not found." });
        }

        return Ok(new
        {
            id = simulation.Id,
            name = simulation.Name,
            thermoPackage = simulation.ThermoPackage,
            flashAlgorithm = simulation.FlashAlgorithm,
            systemOfUnits = simulation.SystemOfUnits,
            status = simulation.Status.ToString(),
            createdAt = simulation.CreatedAt,
            updatedAt = simulation.UpdatedAt,
            compounds = simulation.Compounds.Select(c => new { c.Id, c.Name }),
            materialStreams = simulation.MaterialStreams.Select(s => new
            {
                s.Id,
                s.Name,
                s.Temperature,
                s.Pressure,
                s.MassFlow,
                s.MolarCompositions
            }),
            energyStreams = simulation.EnergyStreams.Select(s => new { s.Id, s.Name, s.EnergyFlow }),
            unitOperations = simulation.UnitOperations.Select(u => new
            {
                u.Id,
                u.Name,
                u.Type,
                u.InputStreamIds,
                u.OutputStreamIds
            })
        });
    }

    /// <summary>
    /// Adds a unit operation to the simulation.
    /// </summary>
    [HttpPost("{id:guid}/units")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddUnit(Guid id, [FromBody] AddUnitRequest request)
    {
        var simulation = await _context.Simulations.FindAsync(id);
        if (simulation == null)
        {
            return NotFound(new { code = "SimulationNotFound", message = $"Simulation with ID {id} not found." });
        }

        var unit = new UnitOperation
        {
            Id = IdGenerator.NextGuid(),
            SimulationId = id,
            Name = request.Name,
            Type = request.UnitOperation.ToString()
        };

        _context.UnitOperations.Add(unit);
        simulation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added unit {UnitId} ({UnitType}) to simulation {SimulationId}", unit.Id, unit.Type, id);

        return CreatedAtAction(nameof(GetSimulation), new { id }, new
        {
            unitId = unit.Id,
            name = unit.Name,
            type = unit.Type
        });
    }

    /// <summary>
    /// Adds a material stream to the simulation.
    /// </summary>
    [HttpPost("{id:guid}/streams")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddStream(Guid id, [FromBody] AddStreamRequest request)
    {
        var simulation = await _context.Simulations.FindAsync(id);
        if (simulation == null)
        {
            return NotFound(new { code = "SimulationNotFound", message = $"Simulation with ID {id} not found." });
        }

        var stream = new MaterialStream
        {
            Id = IdGenerator.NextGuid(),
            SimulationId = id,
            Name = request.Name,
            Temperature = request.Temperature,
            Pressure = request.Pressure,
            MassFlow = request.MassFlow,
            MolarCompositions = request.MolarCompositions
        };

        _context.MaterialStreams.Add(stream);
        simulation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added stream {StreamId} to simulation {SimulationId}", stream.Id, id);

        return CreatedAtAction(nameof(GetSimulation), new { id }, new
        {
            streamId = stream.Id,
            name = stream.Name,
            temperature = stream.Temperature,
            pressure = stream.Pressure,
            massFlow = stream.MassFlow
        });
    }

    /// <summary>
    /// Connects a stream to a unit operation port.
    /// </summary>
    [HttpPut("{id:guid}/connect")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConnectStream(Guid id, [FromBody] ConnectStreamRequest request)
    {
        var simulation = await _context.Simulations.FindAsync(id);
        if (simulation == null)
        {
            return NotFound(new { code = "SimulationNotFound", message = $"Simulation with ID {id} not found." });
        }

        // Verify unit belongs to this simulation
        var unit = await _context.UnitOperations
            .FirstOrDefaultAsync(u => u.Id == request.UnitId && u.SimulationId == id);

        if (unit == null)
        {
            return NotFound(new { code = "UnitNotFound", message = $"Unit {request.UnitId} not found in simulation {id}." });
        }

        // Verify stream belongs to this simulation
        var streamExists = await _context.MaterialStreams
            .AnyAsync(s => s.Id == request.StreamId && s.SimulationId == id);

        if (!streamExists)
        {
            return NotFound(new { code = "StreamNotFound", message = $"Stream {request.StreamId} not found in simulation {id}." });
        }

        // Connect based on port type
        switch (request.PortType)
        {
            case PortType.Inlet:
                if (!unit.InputStreamIds.Contains(request.StreamId))
                {
                    unit.InputStreamIds.Add(request.StreamId);
                }
                break;

            case PortType.Outlet:
                if (!unit.OutputStreamIds.Contains(request.StreamId))
                {
                    unit.OutputStreamIds.Add(request.StreamId);
                }
                break;

            default:
                return BadRequest(new { code = "InvalidPortType", message = $"Unknown port type: {request.PortType}" });
        }

        simulation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Connected stream {StreamId} to unit {UnitId} ({PortType}) in simulation {SimulationId}",
            request.StreamId, request.UnitId, request.PortType, id);

        return Ok(new
        {
            unitId = unit.Id,
            streamId = request.StreamId,
            portType = request.PortType.ToString(),
            inputStreamIds = unit.InputStreamIds,
            outputStreamIds = unit.OutputStreamIds
        });
    }
}
