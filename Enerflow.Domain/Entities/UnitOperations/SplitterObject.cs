using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities.UnitOperations;

public class SplitterObject : UnitOperationObject
{
    public override UnitOperationType Type => UnitOperationType.Splitter;
    
    // Dictionary mapping Output Stream Guid to Split Ratio (0.0 - 1.0)
    public Dictionary<Guid, double> SplitRatios { get; set; } = [];

    public override void Validate()
    {
        base.Validate();
        
        // Splitter: 1 input, N outputs
        if (InputStreamIds.Count != 1)
            throw new InvalidOperationException("Splitter must have exactly one input stream.");
            
        if (OutputStreamIds.Count < 2)
            throw new InvalidOperationException("Splitter must have at least two output streams.");

        if (SplitRatios == null || SplitRatios.Count == 0)
            throw new InvalidOperationException("Split ratios must be defined.");

        // Check sum of ratios
        double sum = SplitRatios.Values.Sum();
        if (Math.Abs(sum - 1.0) > 1e-6)
            throw new InvalidOperationException($"Sum of split ratios must be 1.0. Current sum: {sum}");
            
        // Check if all output streams have a ratio
        foreach (var outputId in OutputStreamIds)
        {
            if (!SplitRatios.ContainsKey(outputId))
                throw new InvalidOperationException($"Missing split ratio for output stream {outputId}");
        }
    }
}
