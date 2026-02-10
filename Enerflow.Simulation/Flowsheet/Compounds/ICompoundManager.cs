using DWSIM.Interfaces;
using Enerflow.Domain.Entities;

namespace Enerflow.Simulation.Flowsheet.Compounds;

/// <summary>
/// Interface for managing compound addition and validation in DWSIM flowsheets.
/// </summary>
public interface ICompoundManager
{
    /// <summary>
    /// Adds a single compound to the flowsheet.
    /// </summary>
    void AddCompound(IFlowsheet flowsheet, Compound compound);

    /// <summary>
    /// Adds multiple compounds to the flowsheet.
    /// </summary>
    void AddCompounds(IFlowsheet flowsheet, IEnumerable<Compound> compounds);

    /// <summary>
    /// Validates that a compound exists in the DWSIM compound database.
    /// </summary>
    bool ValidateCompound(string compoundName);
}
