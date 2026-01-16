using Enerflow.Domain.DTOs;
using DWSIM.Interfaces;

namespace Enerflow.Simulation.Flowsheet.Compounds;

/// <summary>
/// Interface for managing compound addition and validation in DWSIM flowsheets.
/// </summary>
public interface ICompoundManager
{
    /// <summary>
    /// Adds a compound to the flowsheet.
    /// </summary>
    void AddCompound(IFlowsheet flowsheet, CompoundDto compound);

    /// <summary>
    /// Validates that a compound exists in the DWSIM compound database.
    /// </summary>
    bool ValidateCompound(string compoundName);
}
