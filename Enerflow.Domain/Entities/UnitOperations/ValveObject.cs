using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities.UnitOperations;

public class ValveObject : UnitOperationObject
{
    public override UnitOperationType Type => UnitOperationType.Valve;
    
    public double OutletPressure { get; set; } // Pa
    public ValveCalculationMode CalcMode { get; set; } = ValveCalculationMode.OutletPressure;
    public double PressureDrop { get; set; } // Pa

    public override void Validate()
    {
        base.Validate();

        if (InputStreamIds.Count != 1)
             throw new InvalidOperationException("Valve must have exactly one input stream.");
        if (OutputStreamIds.Count != 1)
             throw new InvalidOperationException("Valve must have exactly one output stream.");
        
        if (OutletPressure < 0)
            throw new ArgumentException("OutletPressure must be non-negative.", nameof(OutletPressure));
            
        if (PressureDrop < 0)
            throw new ArgumentException("PressureDrop must be non-negative.", nameof(PressureDrop));
    }
}
