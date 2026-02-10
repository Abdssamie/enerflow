using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities.UnitOperations;

public class FlashDrumObject : UnitOperationObject, IEnergyConsumer
{
   public override UnitOperationType Type => UnitOperationType.FlashDrum;

   public double OutletTemperature { get; set; } // K
   public double OutletPressure { get; set; } // Pa
   public FlashCalculationType FlashType { get; set; } = FlashCalculationType.PressureTemperature;

   /// <summary>
   /// Optional Energy Stream connection for energy input (connects to port 6)
   /// </summary>
   public Guid? EnergyInputId { get; set; }

   public override void Validate()
   {
      base.Validate();

      if (InputStreamIds.Count < 1)
         throw new InvalidOperationException("FlashDrum must have at least one input stream.");

      if (OutputStreamIds.Count < 2)
         throw new InvalidOperationException("FlashDrum must have at least two output streams (Vapor/Liquid).");

      if (OutletTemperature < 0)
         throw new ArgumentException("OutletTemperature must be non-negative.", nameof(OutletTemperature));

      if (OutletPressure < 0)
         throw new ArgumentException("OutletPressure must be non-negative.", nameof(OutletPressure));
   }
}
