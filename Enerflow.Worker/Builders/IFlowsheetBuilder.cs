using DWSIM.Interfaces;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Builders;

/// <summary>
/// Responsible for building DWSIM Flowsheets from Enerflow Simulation entities.
/// </summary>
public interface IFlowsheetBuilder
{
    /// <summary>
    /// Creates and configures a DWSIM Flowsheet based on the provided simulation definition.
    /// </summary>
    /// <param name="simulation">The simulation entity containing the topology and configuration.</param>
    /// <returns>A configured DWSIM Flowsheet object.</returns>
    IFlowsheet BuildFlowsheet(SimulationEntity simulation);
}
