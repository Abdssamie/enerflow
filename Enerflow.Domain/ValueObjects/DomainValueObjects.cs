using Enerflow.Domain.Common;

namespace Enerflow.Domain.ValueObjects;

public record Coordinates(double X, double Y);

// Removed PropertyPackage in favor of strictly typed ThermodynamicsConfiguration

public record Compound(string Name, string CasNumber, string Formula, double MolarWeight);
