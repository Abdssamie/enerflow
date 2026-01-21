using Enerflow.Domain.Entities;
using Enerflow.Domain.DTOs;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Solvers;

public interface ISimulationSolver
{
    SimulationResult Solve(SimulationEntity simulation, ConvergenceConfig? config = null);
}

public class ConvergenceConfig
{
    public int MaxIterations { get; set; } = 50;
    public double Tolerance { get; set; } = 1e-4;
}
