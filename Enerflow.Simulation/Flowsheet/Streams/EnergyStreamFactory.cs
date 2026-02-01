using Enerflow.Domain.DTOs;
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

    public DWSIM.UnitOperations.Streams.EnergyStream CreateEnergyStream(EnergyStreamDto streamDto)
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

    public void Configure(DWSIM.UnitOperations.Streams.EnergyStream stream, EnergyStreamDto streamDto)
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
}
