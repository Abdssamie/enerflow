namespace Enerflow.Domain.ValueObjects;

/// <summary>
/// Represents a 2D coordinate on the flowsheet canvas.
/// </summary>
/// <param name="X">Horizontal position</param>
/// <param name="Y">Vertical position</param>
public record struct Position(int X, int Y);
