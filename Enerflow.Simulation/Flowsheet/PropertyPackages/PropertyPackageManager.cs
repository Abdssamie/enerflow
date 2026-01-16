using Enerflow.Domain.Enums;
using DWSIM.Interfaces;
using DWSIMPropertyPackage = DWSIM.Thermodynamics.PropertyPackages;
using Microsoft.Extensions.Logging;

namespace Enerflow.Simulation.Flowsheet.PropertyPackages;

/// <summary>
/// Manages property package creation and configuration for DWSIM flowsheets.
/// </summary>
public class PropertyPackageManager : IPropertyPackageManager
{
    private readonly ILogger<PropertyPackageManager> _logger;

    public PropertyPackageManager(ILogger<PropertyPackageManager> logger)
    {
        _logger = logger;
    }

    public IPropertyPackage CreatePropertyPackage(PropertyPackage packageType)
    {
        IPropertyPackage pp = packageType switch
        {
            PropertyPackage.PengRobinson => new DWSIMPropertyPackage.PengRobinsonPropertyPackage(),
            PropertyPackage.SoaveRedlichKwong => new DWSIMPropertyPackage.SRKPropertyPackage(),
            PropertyPackage.NRTL => new DWSIMPropertyPackage.NRTLPropertyPackage(),
            PropertyPackage.UNIQUAC => new DWSIMPropertyPackage.UNIQUACPropertyPackage(),
            PropertyPackage.RaoultsLaw => new DWSIMPropertyPackage.RaoultPropertyPackage(),
            PropertyPackage.SteamTables => new DWSIMPropertyPackage.SteamTablesPropertyPackage(),
            _ => new DWSIMPropertyPackage.PengRobinsonPropertyPackage()
        };

        _logger.LogDebug("Created property package: {PackageType}", packageType);
        return pp;
    }

    public void AddToFlowsheet(IFlowsheet flowsheet, IPropertyPackage package)
    {
        try
        {
            flowsheet.AddPropertyPackage(package);
            _logger.LogDebug("Added property package to flowsheet");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add property package to flowsheet");
            throw;
        }
    }
}
