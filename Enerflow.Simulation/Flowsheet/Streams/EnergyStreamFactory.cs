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
