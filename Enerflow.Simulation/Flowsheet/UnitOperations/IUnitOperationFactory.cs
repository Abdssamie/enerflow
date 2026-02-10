using Enerflow.Domain.Entities.UnitOperations;
using Enerflow.Domain.Enums;
using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;

namespace Enerflow.Simulation.Flowsheet.UnitOperations;

/// <summary>
/// Factory for creating and configuring DWSIM unit operation objects.
/// </summary>
public interface IUnitOperationFactory
{
    /// <summary>
    /// Gets the DWSIM graphic object type for visualization purposes.
    /// </summary>
    ObjectType GetGraphicObjectType(UnitOperationType type);

    /// <summary>
    /// Configures a DWSIM unit operation object based on the domain entity.
    /// </summary>
    /// <param name="domainObject">The domain entity containing configuration data.</param>
    /// <param name="flowsheet">The DWSIM flowsheet containing the unit operation.</param>
    /// <param name="compoundNames">Mapping of compound IDs to names.</param>
    void Configure(UnitOperationObject domainObject, IFlowsheet flowsheet, IReadOnlyDictionary<Guid, string> compoundNames);

    /// <summary>
    /// Creates and configures multiple unit operations in the flowsheet.
    /// This is a two-step process: first create all objects, then configure them.
    /// </summary>
    void CreateAndConfigureUnitOperations(
        IFlowsheet flowsheet,
        IEnumerable<UnitOperationObject> unitOperations,
        IReadOnlyDictionary<Guid, string> compoundNames);

    /// <summary>
    /// Configures unit operations that require post-connection setup.
    /// This includes operations like setting splitter ratios that depend on connection order.
    /// </summary>
    /// <param name="flowsheet">The DWSIM flowsheet containing the unit operations.</param>
    /// <param name="simulation">The domain simulation entity with configuration data.</param>
    void ConfigurePostConnection(IFlowsheet flowsheet, Enerflow.Domain.Entities.Simulation simulation);
}
