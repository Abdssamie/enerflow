using Enerflow.Domain.DTOs;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Solvers;

public interface ISimulationSolver
{
    SimulationResult Solve(SimulationEntity simulation);
}
