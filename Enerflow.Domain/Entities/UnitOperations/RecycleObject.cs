using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities.UnitOperations;

public class RecycleObject : UnitOperationObject
{
    public override UnitOperationType Type => UnitOperationType.Recycle;
    public double Tolerance { get; set; } = 1e-4;
    public int MaxIterations { get; set; } = 50;
    
    public RecycleAccelerationMethod Acceleration { get; set; } = RecycleAccelerationMethod.Wegstein;

    public override void Validate()
    {
        base.Validate();
        
        if (Tolerance <= 0)
            throw new ArgumentException("Tolerance must be greater than 0.", nameof(Tolerance));
            
        if (MaxIterations <= 0)
            throw new ArgumentException("MaxIterations must be greater than 0.", nameof(MaxIterations));
    }
}
