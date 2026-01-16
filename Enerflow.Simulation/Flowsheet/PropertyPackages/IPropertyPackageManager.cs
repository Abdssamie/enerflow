using System.Text.Json;
using Enerflow.Domain.Enums;
using DWSIM.Interfaces;

namespace Enerflow.Simulation.Flowsheet.PropertyPackages;

/// <summary>
/// Interface for managing DWSIM property packages (thermodynamic models).
/// </summary>
public interface IPropertyPackageManager
{
    /// <summary>
    /// Creates a DWSIM property package instance based on the package type.
    /// </summary>
    IPropertyPackage CreatePropertyPackage(PropertyPackage packageType);

    /// <summary>
    /// Adds a property package to the flowsheet.
    /// </summary>
    void AddToFlowsheet(IFlowsheet flowsheet, IPropertyPackage package);

    /// <summary>
    /// Sets the flash algorithm for the property package.
    /// </summary>
    void SetFlashAlgorithm(IPropertyPackage package, IFlashAlgorithm flashAlgorithm);
}
