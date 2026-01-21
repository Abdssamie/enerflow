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
    /// Adds a compound to the simulation.
    /// </summary>
    [HttpPost("{id:guid}/compounds")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddCompound(Guid id, [FromBody] AddCompoundRequest request)
    {
        var simulation = await _context.Simulations.FindAsync(id);
        if (simulation == null)
        {
            return NotFound(new { code = "SimulationNotFound", message = $"Simulation with ID {id} not found." });
        }

        var compound = new Compound
        {
            Id = IdGenerator.NextGuid(),
            SimulationId = id,
            Name = request.Name
        };

        _context.Compounds.Add(compound);
        simulation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added compound {CompoundId} ({Name}) to simulation {SimulationId}", compound.Id, compound.Name, id);

        return CreatedAtAction(nameof(GetSimulation), new { id }, new
        {
            compoundId = compound.Id,
            name = compound.Name
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

    /// <summary>
    /// Exports a simulation as a JSON file.
    /// </summary>
    [HttpGet("{id:guid}/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportSimulation(Guid id)
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

        // Build export DTO
        var exportDto = new SimulationExportDto
        {
            Name = simulation.Name,
            ThermoPackage = simulation.ThermoPackage,
            FlashAlgorithm = simulation.FlashAlgorithm,
            SystemOfUnits = simulation.SystemOfUnits,
            Compounds = simulation.Compounds.Select(c => new CompoundExportDto
            {
                Id = c.Id,
                Name = c.Name,
                ConstantProperties = c.ConstantProperties
            }).ToList(),
            MaterialStreams = simulation.MaterialStreams.Select(s => new MaterialStreamExportDto
            {
                Id = s.Id,
                Name = s.Name,
                Temperature = s.Temperature,
                Pressure = s.Pressure,
                MassFlow = s.MassFlow,
                MolarCompositions = s.MolarCompositions
            }).ToList(),
            EnergyStreams = simulation.EnergyStreams.Select(s => new EnergyStreamExportDto
            {
                Id = s.Id,
                Name = s.Name,
                EnergyFlow = s.EnergyFlow
            }).ToList(),
            UnitOperations = simulation.UnitOperations.Select(u => new UnitOperationExportDto
            {
                Id = u.Id,
                Name = u.Name,
                Type = u.Type,
                InputStreamIds = u.InputStreamIds,
                OutputStreamIds = u.OutputStreamIds,
                ConfigParams = u.ConfigParams
            }).ToList()
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var jsonContent = JsonSerializer.Serialize(exportDto, jsonOptions);
        var fileName = $"{SanitizeFileName(simulation.Name)}.json";

        _logger.LogInformation("Exported simulation {SimulationId} as {FileName}", id, fileName);

        return File(
            System.Text.Encoding.UTF8.GetBytes(jsonContent),
            "application/json",
            fileName);
    }

    /// <summary>
    /// Imports a simulation from JSON, creating a new simulation with new IDs.
    /// </summary>
    [HttpPost("import")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportSimulation([FromBody] SimulationExportDto importDto)
    {
        if (importDto == null)
        {
            return BadRequest(new { code = "InvalidInput", message = "Import data is required." });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create new simulation with new ID
            var simulation = new Simulation
            {
                Name = importDto.Name,
                ThermoPackage = importDto.ThermoPackage,
                FlashAlgorithm = importDto.FlashAlgorithm,
                SystemOfUnits = importDto.SystemOfUnits,
                Status = SimulationStatus.Created,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Simulations.Add(simulation);
            await _context.SaveChangesAsync();

            // Map old IDs to new IDs for streams and units
            var streamIdMap = new Dictionary<Guid, Guid>();
            var unitIdMap = new Dictionary<Guid, Guid>();
            var compoundIdMap = new Dictionary<Guid, Guid>();

            // Import compounds
            foreach (var compoundDto in importDto.Compounds)
            {
                var newId = IdGenerator.NextGuid();
                compoundIdMap[compoundDto.Id] = newId;

                var compound = new Compound
                {
                    Id = newId,
                    SimulationId = simulation.Id,
                    Name = compoundDto.Name,
                    ConstantProperties = compoundDto.ConstantProperties
                };
                _context.Compounds.Add(compound);
            }

            // Import material streams
            foreach (var streamDto in importDto.MaterialStreams)
            {
                var newId = IdGenerator.NextGuid();
                streamIdMap[streamDto.Id] = newId;

                var stream = new MaterialStream
                {
                    Id = newId,
                    SimulationId = simulation.Id,
                    Name = streamDto.Name,
                    Temperature = streamDto.Temperature,
                    Pressure = streamDto.Pressure,
                    MassFlow = streamDto.MassFlow,
                    MolarCompositions = streamDto.MolarCompositions ?? new Dictionary<string, double>()
                };
                _context.MaterialStreams.Add(stream);
            }

            // Import energy streams
            foreach (var streamDto in importDto.EnergyStreams)
            {
                var newId = IdGenerator.NextGuid();
                // Energy streams don't need ID mapping for connections currently

                var stream = new EnergyStream
                {
                    Id = newId,
                    SimulationId = simulation.Id,
                    Name = streamDto.Name,
                    EnergyFlow = streamDto.EnergyFlow
                };
                _context.EnergyStreams.Add(stream);
            }

            // Import unit operations with remapped stream IDs
            foreach (var unitDto in importDto.UnitOperations)
            {
                var newId = IdGenerator.NextGuid();
                unitIdMap[unitDto.Id] = newId;

                // Remap stream IDs
                var remappedInputIds = unitDto.InputStreamIds
                    .Where(id => streamIdMap.ContainsKey(id))
                    .Select(id => streamIdMap[id])
                    .ToList();

                var remappedOutputIds = unitDto.OutputStreamIds
                    .Where(id => streamIdMap.ContainsKey(id))
                    .Select(id => streamIdMap[id])
                    .ToList();

                var unit = new UnitOperation
                {
                    Id = newId,
                    SimulationId = simulation.Id,
                    Name = unitDto.Name,
                    Type = unitDto.Type,
                    InputStreamIds = remappedInputIds,
                    OutputStreamIds = remappedOutputIds,
                    ConfigParams = unitDto.ConfigParams
                };
                _context.UnitOperations.Add(unit);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Imported simulation {SimulationId} with {StreamCount} streams and {UnitCount} units",
                simulation.Id, importDto.MaterialStreams.Count, importDto.UnitOperations.Count);

            return CreatedAtAction(nameof(GetSimulation), new { id = simulation.Id }, new
            {
                id = simulation.Id,
                name = simulation.Name,
                status = simulation.Status.ToString(),
                importedStreams = importDto.MaterialStreams.Count,
                importedUnits = importDto.UnitOperations.Count,
                importedCompounds = importDto.Compounds.Count
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to import simulation");
            return BadRequest(new { code = "ImportFailed", message = $"Failed to import simulation: {ex.Message}" });
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(sanitized) ? "simulation" : sanitized;
    }
}

// Export DTOs (matching import format for reversibility)
public record SimulationExportDto
{
    public required string Name { get; init; }
    public required string ThermoPackage { get; init; }
    public required string FlashAlgorithm { get; init; }
    public required string SystemOfUnits { get; init; }
    public List<CompoundExportDto> Compounds { get; init; } = new();
    public List<MaterialStreamExportDto> MaterialStreams { get; init; } = new();
    public List<EnergyStreamExportDto> EnergyStreams { get; init; } = new();
    public List<UnitOperationExportDto> UnitOperations { get; init; } = new();
}

public record CompoundExportDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public JsonDocument? ConstantProperties { get; init; }
}

public record MaterialStreamExportDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public double Temperature { get; init; }
    public double Pressure { get; init; }
    public double MassFlow { get; init; }
    public Dictionary<string, double>? MolarCompositions { get; init; }
}

public record EnergyStreamExportDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public double EnergyFlow { get; init; }
}

public record UnitOperationExportDto
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public List<Guid> InputStreamIds { get; init; } = new();
    public List<Guid> OutputStreamIds { get; init; } = new();
    public JsonDocument? ConfigParams { get; init; }
}
