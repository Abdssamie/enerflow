using DomainEnergyStream = Enerflow.Domain.Entities.Streams.EnergyStream;
using DwsimEnergyStream = DWSIM.UnitOperations.Streams.EnergyStream;
using Microsoft.Extensions.Logging;
using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;

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

    public void CreateAndConfigureStreams(
        IFlowsheet flowsheet,
        IEnumerable<DomainEnergyStream> streams)
    {
        var streamList = streams.ToList();
        
        foreach (var streamEntity in streamList)
        {
            var dwsimStream = flowsheet.AddObject(
                ObjectType.EnergyStream,
                0,
                0,
                streamEntity.Id.ToString(),
                streamEntity.Name) as DwsimEnergyStream;

            if (dwsimStream != null)
            {
                Configure(dwsimStream, streamEntity);
            }
        }

        _logger.LogInformation("Created and configured {Count} energy streams", streamList.Count);
    }
}
