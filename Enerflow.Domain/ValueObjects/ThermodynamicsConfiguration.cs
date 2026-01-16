using Enerflow.Domain.Enums;

namespace Enerflow.Domain.ValueObjects;

// Represents a strictly typed property package configuration
// This is what the API/UI uses. The Worker translates this to DWSIM strings.
public record ThermodynamicsConfiguration
{
    public PropertyPackageType PackageType { get; init; }
    public FlashAlgorithmType FlashAlgorithm { get; init; } = FlashAlgorithmType.NestedLoops;
    
    // Custom interaction parameters or settings (e.g. "Use BIPs")
    public Dictionary<string, double> InteractionParameters { get; init; } = new();
    public Dictionary<string, bool> Settings { get; init; } = new();
}
