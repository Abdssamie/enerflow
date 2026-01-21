using Enerflow.Domain.Entities;
using DWSIM.Interfaces;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Mappers;

public interface IConnectionMapper
{
    void MapConnections(SimulationEntity simulation, IFlowsheet flowsheet);
}
