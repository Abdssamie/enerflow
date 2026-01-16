using Enerflow.Domain.DTOs;

namespace Enerflow.Domain.Interfaces;

/// <summary>
/// Service interface for executing DWSIM simulations.
/// Abstracts the DWSIM automation API for testability.
/// </summary>
public interface ISimulationService : IDisposable
{
    /// <summary>
    /// Builds a DWSIM flowsheet from the simulation definition.
    /// </summary>
    /// <param name="definition">The simulation definition containing compounds, streams, and unit operations</param>
    /// <returns>True if the flowsheet was built successfully</returns>
    bool BuildFlowsheet(SimulationDefinitionDto definition);

    /// <summary>
    /// Solves the flowsheet (runs the simulation).
    /// </summary>
    /// <returns>True if the simulation converged successfully</returns>
    bool Solve();

    /// <summary>
    /// Collects the results from the solved flowsheet.
    /// </summary>
    /// <returns>Strongly-typed simulation results</returns>
    SimulationResultsDto CollectResults();

    /// <summary>
    /// Gets any error messages from the last operation.
    /// </summary>
    IReadOnlyList<string> GetErrorMessages();

    /// <summary>
    /// Gets log messages from the simulation.
    /// </summary>
    IReadOnlyList<string> GetLogMessages();
}
