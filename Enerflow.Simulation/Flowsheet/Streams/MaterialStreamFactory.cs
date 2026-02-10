using Microsoft.Extensions.Logging;
using Enerflow.Domain.DTOs;
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

   public DwsimMaterialStream CreateMaterialStream(MaterialStreamDto streamDto, SystemOfUnits systemOfUnits)
   {
      try
      {
      var stream = new DwsimMaterialStream(streamDto.Name, "");
         Configure(stream, streamDto, systemOfUnits);
         return stream;
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to create material stream: {Name}", streamDto.Name);
         throw;
      }
   }

   public void Configure(DwsimMaterialStream stream, MaterialStreamDto streamDto, SystemOfUnits systemOfUnits)
   {
      try
      {
         // Convert inputs to SI (Kelvin, Pascal, kg/s)
         var tempK = ConvertTemperatureToSI(streamDto.Temperature, systemOfUnits);
         var pressPa = ConvertPressureToSI(streamDto.Pressure, systemOfUnits);
         var massFlowKgS = ConvertMassFlowToSI(streamDto.MassFlow, systemOfUnits);

         // Set stream conditions (DWSIM always expects SI internally)
         stream.Phases[0].Properties.temperature = tempK;
         stream.Phases[0].Properties.pressure = pressPa;
         stream.Phases[0].Properties.massflow = massFlowKgS;

         // Set compositions
         foreach (var (compoundName, moleFraction) in streamDto.MolarCompositions)
         {
            if (stream.Phases[0].Compounds.TryGetValue(compoundName, out var _))
            {
               stream.Phases[0].Compounds[compoundName].MoleFraction = moleFraction;
            }
            else
            {
               _logger.LogWarning(
                   "Compound {Compound} not found in stream {Name} - skipping composition.",
                   compoundName,
                   streamDto.Name
               );
            }
         }

         _logger.LogDebug("Configured material stream: {Name} (T={T}K, P={P}Pa, F={F}kg/s)",
             streamDto.Name, tempK, pressPa, massFlowKgS);
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to configure material stream: {Name}", streamDto.Name);
         throw;
      }
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

   private double ConvertTemperatureToSI(double value, SystemOfUnits units)
   {
      return units switch
      {
         SystemOfUnits.SI => value, // Kelvin
         SystemOfUnits.CGS => value + 273.15, // Celsius to Kelvin
         SystemOfUnits.English => (value - 32) * 5 / 9 + 273.15, // Fahrenheit to Kelvin
         _ => value // Default assume SI
      };
   }

   private double ConvertPressureToSI(double value, SystemOfUnits units)
   {
      return units switch
      {
         SystemOfUnits.SI => value, // Pascal
         SystemOfUnits.CGS => value * 100000, // Bar to Pascal (approx) or atm? Assume Bar for CGS/Metric
         SystemOfUnits.English => value * 6894.76, // PSI to Pascal
         _ => value
      };
   }

   private double ConvertMassFlowToSI(double value, SystemOfUnits units)
   {
      return units switch
      {
         SystemOfUnits.SI => value, // kg/s
         SystemOfUnits.CGS => value / 3600.0, // kg/h to kg/s (Engineering Metric)
         SystemOfUnits.English => value * 0.45359237 / 3600.0, // lb/h to kg/s
         _ => value
      };
   }
}
