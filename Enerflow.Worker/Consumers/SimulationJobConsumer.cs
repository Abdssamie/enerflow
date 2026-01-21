using System.Text.Json;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Entities;
using Enerflow.Domain.Entities.Streams;
using Enerflow.Domain.Entities.UnitOperations;
using Enerflow.Domain.Enums;
using Enerflow.Infrastructure.Persistence;
using Enerflow.Worker.Solvers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Consumers;

/// <summary>
/// MassTransit consumer for processing simulation jobs from the message queue.
/// Orchestrates the full simulation lifecycle using the Solver Engine.
/// </summary>
public class SimulationJobConsumer : IConsumer<SimulationJob>
{
    private readonly ILogger<SimulationJobConsumer> _logger;
    private readonly ISimulationSolver _solver;
    private readonly EnerflowDbContext _dbContext;

    public SimulationJobConsumer(
        ILogger<SimulationJobConsumer> logger,
        ISimulationSolver solver,
        EnerflowDbContext dbContext)
    {
        _logger = logger;
        _solver = solver;
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
            await UpdateStatusAsync(job.SimulationId, SimulationStatus.Running, null, cancellationToken);

            // Step 1: Deserialize/Map DTO to Domain Entity
            _logger.LogDebug("Mapping job definition to domain model for Job {JobId}", job.JobId);
            var simulation = MapToDomain(job);

            // Step 2: Solve
            _logger.LogInformation("Starting solver for Job {JobId}", job.JobId);
            var config = new ConvergenceConfig(); // Use defaults
            
            // Note: Solve is currently synchronous (CPU-bound)
            var result = _solver.Solve(simulation, config);

            // Step 3: Persist Results
            var status = result.Success ? SimulationStatus.Converged : SimulationStatus.Failed;
            _logger.LogInformation("Solver completed for Job {JobId}. Status: {Status}", job.JobId, status);

            await PersistResultAsync(job.SimulationId, result, status, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error processing Job {JobId}", job.JobId);
            await UpdateStatusAsync(job.SimulationId, SimulationStatus.Failed, $"Critical error: {ex.Message}", cancellationToken);
        }
    }

    private async Task UpdateStatusAsync(Guid simulationId, SimulationStatus status, string? errorMessage, CancellationToken ct)
    {
        try
        {
            var simulation = await _dbContext.Simulations
                .FirstOrDefaultAsync(s => s.Id == simulationId, ct);

            if (simulation != null)
            {
                simulation.Status = status;
                if (errorMessage != null)
                {
                    simulation.ErrorMessage = errorMessage;
                }
                simulation.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(ct);
            }
            else
            {
                _logger.LogWarning("Simulation {SimulationId} not found for status update", simulationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for Simulation {SimulationId}", simulationId);
        }
    }

    private async Task PersistResultAsync(
        Guid simulationId, 
        SimulationResult result, 
        SimulationStatus status, 
        CancellationToken ct)
    {
        try
        {
            var simulation = await _dbContext.Simulations
                .FirstOrDefaultAsync(s => s.Id == simulationId, ct);

            if (simulation != null)
            {
                simulation.Status = status;
                simulation.ErrorMessage = result.ErrorMessage;
                simulation.ResultJson = JsonSerializer.SerializeToDocument(result);
                simulation.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(ct);
                _logger.LogDebug("Persisted results for Simulation {SimulationId}", simulationId);
            }
            else
            {
                _logger.LogWarning("Simulation {SimulationId} not found for result persistence", simulationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist results for Simulation {SimulationId}", simulationId);
            // Attempt to mark as failed if persistence fails
            await UpdateStatusAsync(simulationId, SimulationStatus.Failed, "Failed to persist results", ct);
        }
    }

    private SimulationEntity MapToDomain(SimulationJob job)
    {
        var def = job.Definition;
        var sim = new SimulationEntity
        {
            Id = job.SimulationId,
            Name = def.Name,
            ThermoPackage = def.PropertyPackageType,
            FlashAlgorithm = def.FlashAlgorithm,
            SystemOfUnits = def.SystemOfUnits,
            Status = SimulationStatus.Running
        };

        // Map Compounds
        foreach (var c in def.Compounds)
        {
            sim.Compounds.Add(new Compound
            {
                Id = c.Id,
                SimulationId = sim.Id,
                Name = c.Name,
                ConstantProperties = c.ConstantProperties
            });
        }

        // Map Material Streams
        foreach (var ms in def.MaterialStreams)
        {
            sim.MaterialStreams.Add(new MaterialStream
            {
                Id = ms.Id,
                SimulationId = sim.Id,
                Name = ms.Name,
                Temperature = ms.Temperature,
                Pressure = ms.Pressure,
                MassFlow = ms.MassFlow,
                Composition = ms.MolarCompositions
            });
        }

        // Map Energy Streams
        foreach (var es in def.EnergyStreams)
        {
            sim.EnergyStreams.Add(new EnergyStream
            {
                Id = es.Id,
                SimulationId = sim.Id,
                Name = es.Name,
                EnergyFlow = es.EnergyFlow
            });
        }

        // Map Unit Operations
        foreach (var uo in def.UnitOperations)
        {
            var unit = CreateUnitOperation(uo, sim.Id);
            if (unit != null)
            {
                sim.UnitOperations.Add(unit);
            }
            else
            {
                _logger.LogWarning("Skipping unsupported unit operation type: {Type}", uo.Type);
            }
        }

        return sim;
    }

    private UnitOperationObject? CreateUnitOperation(UnitOperationDto dto, Guid simulationId)
    {
        // Polymorphic creation based on Type
        return dto.Type switch
        {
            UnitOperationType.Mixer => new MixerObject 
            { 
                Id = dto.Id, SimulationId = simulationId, Name = dto.Name, 
                InputStreamIds = dto.InputStreamIds, OutputStreamIds = dto.OutputStreamIds, 
                ConfigParams = dto.ConfigParams 
            },
            UnitOperationType.Splitter => new SplitterObject 
            { 
                Id = dto.Id, SimulationId = simulationId, Name = dto.Name, 
                InputStreamIds = dto.InputStreamIds, OutputStreamIds = dto.OutputStreamIds, 
                ConfigParams = dto.ConfigParams 
            },
            UnitOperationType.Heater => new HeaterObject 
            { 
                Id = dto.Id, SimulationId = simulationId, Name = dto.Name, 
                InputStreamIds = dto.InputStreamIds, OutputStreamIds = dto.OutputStreamIds, 
                ConfigParams = dto.ConfigParams 
            },
            UnitOperationType.Cooler => new CoolerObject 
            { 
                Id = dto.Id, SimulationId = simulationId, Name = dto.Name, 
                InputStreamIds = dto.InputStreamIds, OutputStreamIds = dto.OutputStreamIds, 
                ConfigParams = dto.ConfigParams 
            },
            UnitOperationType.Valve => new ValveObject 
            { 
                Id = dto.Id, SimulationId = simulationId, Name = dto.Name, 
                InputStreamIds = dto.InputStreamIds, OutputStreamIds = dto.OutputStreamIds, 
                ConfigParams = dto.ConfigParams 
            },
            UnitOperationType.FlashDrum => new FlashDrumObject 
            { 
                Id = dto.Id, SimulationId = simulationId, Name = dto.Name, 
                InputStreamIds = dto.InputStreamIds, OutputStreamIds = dto.OutputStreamIds, 
                ConfigParams = dto.ConfigParams 
            },
            UnitOperationType.ShortcutColumn => new ShortcutColumnObject 
            { 
                Id = dto.Id, SimulationId = simulationId, Name = dto.Name, 
                InputStreamIds = dto.InputStreamIds, OutputStreamIds = dto.OutputStreamIds, 
                ConfigParams = dto.ConfigParams 
            },
            UnitOperationType.Recycle => new RecycleObject 
            { 
                Id = dto.Id, SimulationId = simulationId, Name = dto.Name, 
                InputStreamIds = dto.InputStreamIds, OutputStreamIds = dto.OutputStreamIds, 
                ConfigParams = dto.ConfigParams 
            },
            _ => null
        };
    }
}
