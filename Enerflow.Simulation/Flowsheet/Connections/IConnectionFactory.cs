using DWSIM.Interfaces;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Simulation.Flowsheet.Connections;

public interface IConnectionFactory
{
    void ConnectFlowsheet(SimulationEntity domainSimulation, IFlowsheet flowsheet);
}
