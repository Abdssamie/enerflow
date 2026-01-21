using DWSIM.Interfaces;
using Enerflow.Domain.Entities.Streams;

namespace Enerflow.Worker.Mappers;

public interface IStreamMapper
{
    void MapMaterialStream(MaterialStream domainStream, IFlowsheet flowsheet);
    void MapEnergyStream(EnergyStream domainStream, IFlowsheet flowsheet);
}
