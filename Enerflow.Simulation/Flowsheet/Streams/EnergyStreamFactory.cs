using Enerflow.Domain.DTOs;
using DomainEnergyStream = Enerflow.Domain.Entities.Streams.EnergyStream;
using DwsimEnergyStream = DWSIM.UnitOperations.Streams.EnergyStream;
using Microsoft.Extensions.Logging;

namespace Enerflow.Simulation.Flowsheet.Streams;

/// <summary>
/// Factory for creating and configuring DWSIM energy streams.
/// </summary>
public class EnergyStreamFactory : IEnergyStreamFactory
{
    private readonly ILogger<EnergyStreamFactory> _logger;

    public EnergyStreamFactory(ILogger<EnergyStreamFactory> logger)
    {
        _logger = logger;
    }

    public DwsimEnergyStream CreateEnergyStream(EnergyStreamDto streamDto)
    {
        try
        {
            var stream = new DWSIM.UnitOperations.Streams.EnergyStream(streamDto.Name, "");
            Configure(stream, streamDto);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create energy stream: {Name}", streamDto.Name);
            throw;
        }
    }

    public void Configure(DwsimEnergyStream stream, EnergyStreamDto streamDto)
    {
        try
        {
            stream.EnergyFlow = streamDto.EnergyFlow;
            _logger.LogDebug("Configured energy stream: {Name}", streamDto.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure energy stream: {Name}", streamDto.Name);
            throw;
        }
    }

    public void Configure(DwsimEnergyStream stream, DomainEnergyStream streamEntity)
    {
        try
        {
            stream.EnergyFlow = streamEntity.EnergyFlow;
            _logger.LogDebug("Configured energy stream: {Name}", streamEntity.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure energy stream: {Name}", streamEntity.Name);
            throw;
        }
    }
}
