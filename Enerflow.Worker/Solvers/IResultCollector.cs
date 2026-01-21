using DWSIM.Interfaces;
using Enerflow.Domain.DTOs;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Solvers;

public interface IResultCollector
{
    void ExtractResults(IFlowsheet flowsheet, SimulationEntity simulation, SimulationResult result);
}
