namespace Enerflow.Domain.Enums;

/// <summary>
/// Defines the type of port on a unit operation for stream connections.
/// </summary>
public enum PortType
{
    /// <summary>
    /// Inlet port (stream flowing into the unit operation)
    /// </summary>
    Inlet,

    /// <summary>
    /// Outlet port (stream flowing out of the unit operation)
    /// </summary>
    Outlet
}
