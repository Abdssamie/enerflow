using DomainEnergyStream = Enerflow.Domain.Entities.Streams.EnergyStream;
using DwsimEnergyStream = DWSIM.UnitOperations.Streams.EnergyStream;
using Microsoft.Extensions.Logging;
using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;
using Enerflow.Domain.Enums;

namespace Enerflow.Simulation.Flowsheet.Streams;

#pragma warning disable CA1873

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

   public void Configure(DwsimEnergyStream stream, DomainEnergyStream streamEntity, SystemOfUnits systemOfUnits)
   {
      try
      {
         stream.EnergyFlow = streamEntity.EnergyFlow.ValueInKW;
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
       IEnumerable<DomainEnergyStream> streams,
       SystemOfUnits systemOfUnits)
   {
      var streamList = streams.ToList();

      foreach (var streamEntity in streamList)
      {
         if (flowsheet.AddObject(
             ObjectType.EnergyStream,
             0,
             0,
             streamEntity.Id.ToString(),
             streamEntity.Name) is DwsimEnergyStream dwsimStream)
         {
            Configure(dwsimStream, streamEntity, systemOfUnits);
         }
      }

      _logger.LogInformation("Created and configured {Count} energy streams", streamList.Count);
   }
}
