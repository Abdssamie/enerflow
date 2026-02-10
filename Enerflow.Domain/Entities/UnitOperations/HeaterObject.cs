using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities.UnitOperations;

public class HeaterObject : UnitOperationObject, IEnergyConsumer
{
    public override UnitOperationType Type => UnitOperationType.Heater;
    public double Efficiency { get; set; } = 1.0; // 0-1
    public double OutletTemperature { get; set; } // K
    public double HeatDuty { get; set; } // kW
    public double TemperatureChange { get; set; } // K
    public double PressureDrop { get; set; } // Pa
    
    public HeaterCalculationMode CalcMode { get; set; } = HeaterCalculationMode.OutletTemperature;

    /// <summary>
    /// Optional Energy Stream input connection ID
    /// </summary>
    public Guid? EnergyInputId { get; set; }

    public override void Validate()
    {
        base.Validate();
        
        if (Efficiency < 0 || Efficiency > 1)
            throw new ArgumentException("Efficiency must be between 0 and 1.", nameof(Efficiency));
            
        if (CalcMode == HeaterCalculationMode.OutletTemperature && OutletTemperature < 0)
             throw new ArgumentException("OutletTemperature must be greater than 0 K.", nameof(OutletTemperature));

        if (CalcMode == HeaterCalculationMode.HeatDuty && HeatDuty < 0)
             throw new ArgumentException("HeatDuty cannot be negative.", nameof(HeatDuty));
             
        if (PressureDrop < 0)
             throw new ArgumentException("PressureDrop cannot be negative.", nameof(PressureDrop));
    }
}
