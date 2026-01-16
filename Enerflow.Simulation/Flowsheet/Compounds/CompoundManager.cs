using Enerflow.Domain.DTOs;
using DWSIM.Interfaces;
using Microsoft.Extensions.Logging;

namespace Enerflow.Simulation.Flowsheet.Compounds;

/// <summary>
/// Manages compound operations for DWSIM flowsheets.
/// </summary>
public class CompoundManager : ICompoundManager
{
    private readonly ILogger<CompoundManager> _logger;

    public CompoundManager(ILogger<CompoundManager> logger)
    {
        _logger = logger;
    }

    public void AddCompound(IFlowsheet flowsheet, CompoundDto compound)
    {
        try
        {
            flowsheet.AddCompound(compound.Name);
            _logger.LogDebug("Added compound: {Name}", compound.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add compound: {Name}", compound.Name);
            throw;
        }
    }

    public bool ValidateCompound(string compoundName)
    {
        // Placeholder for future validation logic
        // DWSIM will throw an exception if compound doesn't exist
        return !string.IsNullOrWhiteSpace(compoundName);
    }
}
