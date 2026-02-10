using DWSIM.Interfaces;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Simulation.Validation;

public interface IFlowsheetValidator
{
    ValidationResult Validate(SimulationEntity simulation, IFlowsheet flowsheet);
}
