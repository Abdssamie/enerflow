using DWSIM.Interfaces;
using Enerflow.Domain.Entities.UnitOperations;

namespace Enerflow.Simulation.Flowsheet.UnitOperations;

/// <summary>
/// Configures DWSIM unit operation objects from Domain Entities.
/// </summary>
public interface IUnitOperationConfigurator
{
    /// <summary>
    /// Configures a DWSIM unit operation object based on the domain entity.
    /// </summary>
    /// <param name="domainObject">The domain entity containing configuration data.</param>
    /// <param name="flowsheet">The DWSIM flowsheet containing the unit operation.</param>
    /// <param name="compoundNames">Mapping of compound IDs to names.</param>
    void Configure(UnitOperationObject domainObject, IFlowsheet flowsheet, IReadOnlyDictionary<Guid, string> compoundNames);
}
