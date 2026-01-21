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

    public IPropertyPackage CreatePropertyPackage(PropertyPackageType packageType)
    {
        IPropertyPackage pp = packageType switch
        {
            PropertyPackageType.PengRobinson => new DWSIMPropertyPackage.PengRobinsonPropertyPackage(),
            PropertyPackageType.SoaveRedlichKwong => new DWSIMPropertyPackage.SRKPropertyPackage(),
            PropertyPackageType.NRTL => new DWSIMPropertyPackage.NRTLPropertyPackage(),
            PropertyPackageType.UNIQUAC => new DWSIMPropertyPackage.UNIQUACPropertyPackage(),
            PropertyPackageType.RaoultsLaw => new DWSIMPropertyPackage.RaoultPropertyPackage(),
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

    public void SetFlashAlgorithm(IPropertyPackage package, IFlashAlgorithm flashAlgorithm)
    {
        try
        {
            package.FlashAlgorithm = flashAlgorithm;
            _logger.LogDebug("Set flash algorithm: {AlgorithmName}", flashAlgorithm.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set flash algorithm");
            throw;
        }
    }
}
