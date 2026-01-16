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
            stream.EnergyFlow = streamDto.EnergyFlow;

            _logger.LogDebug("Created energy stream: {Name}", streamDto.Name);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create energy stream: {Name}", streamDto.Name);
            throw;
        }
    }
}
