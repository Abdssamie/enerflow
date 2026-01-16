using System.Text.Json;
using Enerflow.Domain.Enums;
using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;

namespace Enerflow.Simulation.Flowsheet.UnitOperations;

/// <summary>
/// Interface for creating DWSIM unit operation objects.
/// </summary>
public interface IUnitOperationFactory
{
    /// <summary>
    /// Creates a DWSIM unit operation based on the UnitOperation enum type.
    /// </summary>
    ISimulationObject? CreateUnitOperation(UnitOperationType type, string name, JsonDocument? configParams = null);

    /// <summary>
    /// Gets the DWSIM graphic object type for visualization purposes.
    /// </summary>
    ObjectType GetGraphicObjectType(UnitOperationType type);
}
