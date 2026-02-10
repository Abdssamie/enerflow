using DWSIM.Interfaces;
using Enerflow.Domain.Entities;
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

    public void AddCompound(IFlowsheet flowsheet, Compound compound)
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

    public void AddCompounds(IFlowsheet flowsheet, IEnumerable<Compound> compounds)
    {
        var compoundList = compounds.ToList();
        _logger.LogInformation("Adding {Count} compounds to flowsheet", compoundList.Count);

        foreach (var compound in compoundList)
        {
            AddCompound(flowsheet, compound);
        }

        _logger.LogInformation("Successfully added {Count} compounds", compoundList.Count);
    }

    public bool ValidateCompound(string compoundName)
    {
        // Placeholder for future validation logic
        // DWSIM will throw an exception if compound doesn't exist
        return !string.IsNullOrWhiteSpace(compoundName);
    }
}
