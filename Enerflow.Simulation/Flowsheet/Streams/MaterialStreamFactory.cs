using Microsoft.Extensions.Logging;
using Enerflow.Domain.Enums;
using DomainMaterialStream = Enerflow.Domain.Entities.Streams.MaterialStream;
using DwsimMaterialStream = DWSIM.Thermodynamics.Streams.MaterialStream;
using DwsimStreamSpec = DWSIM.Interfaces.Enums.StreamSpec;

namespace Enerflow.Simulation.Flowsheet.Streams;

/// <summary>
/// Factory for creating and configuring DWSIM material streams.
/// </summary>
public class MaterialStreamFactory : IMaterialStreamFactory
{
   private readonly ILogger<MaterialStreamFactory> _logger;

   public MaterialStreamFactory(ILogger<MaterialStreamFactory> logger)
   {
      _logger = logger;
   }

   public void Configure(DwsimMaterialStream stream, DomainMaterialStream streamEntity, SystemOfUnits systemOfUnits)
   {
      try
      {
         _ = systemOfUnits;

         var tempK = streamEntity.Temperature;
         var pressPa = streamEntity.Pressure;
         var massFlowKgS = streamEntity.MassFlow;

         stream.SpecType = DwsimStreamSpec.Temperature_and_Pressure;

         if (tempK > 0)
         {
            stream.Phases[0].Properties.temperature = tempK;
         }

         if (pressPa > 0)
         {
            stream.Phases[0].Properties.pressure = pressPa;
         }

         if (massFlowKgS > 0)
         {
            stream.Phases[0].Properties.massflow = massFlowKgS;
         }

         foreach (var (compoundName, moleFraction) in streamEntity.Composition)
         {
            if (stream.Phases[0].Compounds.ContainsKey(compoundName))
            {
               stream.Phases[0].Compounds[compoundName].MoleFraction = moleFraction;
            }
            else
            {
               _logger.LogWarning(
                   "Compound {Compound} not found in stream {Name} - skipping composition.",
                   compoundName,
                   streamEntity.Name
               );
            }
         }

         _logger.LogDebug("Configured material stream: {Name} (T={T}K, P={P}Pa, F={F}kg/s)",
             streamEntity.Name, tempK, pressPa, massFlowKgS);
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to configure material stream: {Name}", streamEntity.Name);
         throw;
      }
   }
}
