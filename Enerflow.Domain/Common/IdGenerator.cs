using MassTransit;

namespace Enerflow.Domain.Common;

/// <summary>
/// Provides a centralized way to generate sequential unique identifiers using NewId.
/// Sequential IDs improve database performance and provide built-in timestamps.
/// </summary>
public static class IdGenerator
{
    /// <summary>
    /// Generates a sequential Guid.
    /// </summary>
    public static Guid NextGuid() => NewId.NextGuid();

    /// <summary>
    /// Generates a sequential NewId object.
    /// </summary>
    public static NewId Next() => NewId.Next();
}
