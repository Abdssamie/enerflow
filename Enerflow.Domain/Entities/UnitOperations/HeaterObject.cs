using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities.UnitOperations;

public class HeaterObject : UnitOperationObject
{
    public override UnitOperationType Type => UnitOperationType.Heater;
    public double Efficiency { get; set; } = 1.0; // 0-1
    public double OutletTemperature { get; set; } // K
    public double PressureDrop { get; set; } // Pa
    
    public HeaterCalculationMode CalcMode { get; set; } = HeaterCalculationMode.OutletTemperature;

    public override void Validate()
    {
        base.Validate();
        
        if (Efficiency < 0 || Efficiency > 1)
            throw new ArgumentException("Efficiency must be between 0 and 1.", nameof(Efficiency));
            
        if (OutletTemperature < 0) // Basic physical check
             throw new ArgumentException("OutletTemperature must be greater than 0 K.", nameof(OutletTemperature));
             
        if (PressureDrop < 0)
             throw new ArgumentException("PressureDrop cannot be negative.", nameof(PressureDrop));
    }
}
