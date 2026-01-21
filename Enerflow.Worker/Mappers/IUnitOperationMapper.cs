using Enerflow.Domain.Entities.UnitOperations;
using DWSIM.Interfaces;

namespace Enerflow.Worker.Mappers;

public interface IUnitOperationMapper
{
    void Map(UnitOperationObject domainObject, IFlowsheet flowsheet, IReadOnlyDictionary<Guid, string> compoundNames);
}
