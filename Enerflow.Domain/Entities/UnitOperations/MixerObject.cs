using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities.UnitOperations;

public class MixerObject : UnitOperationObject
{
    public override UnitOperationType Type => UnitOperationType.Mixer;

    public override void Validate()
    {
        base.Validate();
        
        // Mixer: N inputs, 1 output
        if (OutputStreamIds.Count != 1)
            throw new InvalidOperationException("Mixer must have exactly one output stream.");
        
        if (InputStreamIds.Count < 2) // Usually mix 2+ streams, but 1 is theoretically possible (trivial)
            throw new InvalidOperationException("Mixer must have at least two input streams.");
    }
}
