using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities.UnitOperations;

public class ShortcutColumnObject : UnitOperationObject
{
    public override UnitOperationType Type => UnitOperationType.ShortcutColumn;
    
    public double RefluxRatio { get; set; }
    public Guid LightKey { get; set; } // Compound ID
    public Guid HeavyKey { get; set; } // Compound ID
    public double LightKeyFraction { get; set; } // Target fraction in top/bottom
    public double HeavyKeyFraction { get; set; } // Target fraction in top/bottom
    
    public double CondenserPressure { get; set; } // Pa
    public double ReboilerPressure { get; set; } // Pa
    
    public int Stages { get; set; } // Number of stages (if fixed) or result? 
    // Requirements say "Properties: Stages". Validation: Stages > 0.
    
    public override void Validate()
    {
        base.Validate();
        
        if (InputStreamIds.Count != 1)
             throw new InvalidOperationException("ShortcutColumn must have exactly one input stream.");
        if (OutputStreamIds.Count != 2)
             throw new InvalidOperationException("ShortcutColumn must have exactly two output streams (Distillate/Bottoms).");

        if (RefluxRatio < 0)
            throw new ArgumentException("RefluxRatio must be non-negative.", nameof(RefluxRatio));
            
        if (Stages <= 0)
            throw new ArgumentException("Stages must be greater than 0.", nameof(Stages));
            
        if (LightKey == Guid.Empty)
            throw new ArgumentException("LightKey must be specified.", nameof(LightKey));
            
        if (HeavyKey == Guid.Empty)
            throw new ArgumentException("HeavyKey must be specified.", nameof(HeavyKey));
            
        if (CondenserPressure < 0)
            throw new ArgumentException("CondenserPressure must be non-negative.", nameof(CondenserPressure));
            
        if (ReboilerPressure < 0)
             throw new ArgumentException("ReboilerPressure must be non-negative.", nameof(ReboilerPressure));
    }
}
