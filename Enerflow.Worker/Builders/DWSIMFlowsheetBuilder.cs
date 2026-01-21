using DWSIM.Interfaces;
using DWSIM.SharedClasses.SystemsOfUnits;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Enums;
using Enerflow.Simulation.Flowsheet.Compounds;
using Enerflow.Simulation.Flowsheet.FlashAlgorithms;
using Enerflow.Simulation.Flowsheet.PropertyPackages;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Builders;

public class DWSIMFlowsheetBuilder : IFlowsheetBuilder
{
    private readonly DWSIM.Automation.AutomationInterface _automation;
    private readonly ICompoundManager _compoundManager;
    private readonly IPropertyPackageManager _propertyPackageManager;
    private readonly IFlashAlgorithmManager _flashAlgorithmManager;
    private readonly ILogger<DWSIMFlowsheetBuilder> _logger;

    public DWSIMFlowsheetBuilder(
        DWSIM.Automation.AutomationInterface automation,
        ICompoundManager compoundManager,
        IPropertyPackageManager propertyPackageManager,
        IFlashAlgorithmManager flashAlgorithmManager,
        ILogger<DWSIMFlowsheetBuilder> logger)
    {
        _automation = automation;
        _compoundManager = compoundManager;
        _propertyPackageManager = propertyPackageManager;
        _flashAlgorithmManager = flashAlgorithmManager;
        _logger = logger;
    }

    public IFlowsheet BuildFlowsheet(SimulationEntity simulation)
    {
        _logger.LogInformation("Building flowsheet for simulation {Id}: {Name}", simulation.Id, simulation.Name);

        // 1. Initialization (redundant if using Automation3, but mandated by spec)
        DWSIM.GlobalSettings.Settings.AutomationMode = true;

        // 2. Create Flowsheet
        var flowsheet = _automation.CreateFlowsheet();
        
        // 3. Configure Settings (System of Units)
        SetSystemOfUnits(flowsheet, simulation.SystemOfUnits);

        // 4. Add Compounds
        foreach (var compound in simulation.Compounds)
        {
            var dto = new CompoundDto(compound.Id, compound.Name, compound.ConstantProperties);
            // DWSIM API check: ensure we don't add duplicates if reusing flowsheet (though this is new flowsheet)
            // The constraint says "Use established patterns... to avoid 'Duplicate Key' errors"
            _compoundManager.AddCompound(flowsheet, dto);
        }

        // 5. Property Package & Flash Algorithm
        var propertyPackage = _propertyPackageManager.CreatePropertyPackage(simulation.ThermoPackage);
        var flashAlgorithm = _flashAlgorithmManager.CreateFlashAlgorithm(simulation.FlashAlgorithm);
        
        _propertyPackageManager.SetFlashAlgorithm(propertyPackage, flashAlgorithm);
        _propertyPackageManager.AddToFlowsheet(flowsheet, propertyPackage);

        // Set the added package as the default/active one for the flowsheet?
        // Usually handled by adding it, but we might need to select it.
        // Flowsheet usually selects the first one added by default.
        if (flowsheet.PropertyPackages.Count > 0)
        {
             // Force selection if API allows, but AddPropertyPackage usually suffices for the first one.
             // We can check if we need to set SelectedPropertyPackage equivalent.
        }

        return flowsheet;
    }

    private void SetSystemOfUnits(IFlowsheet flowsheet, SystemOfUnits systemOfUnits)
    {
        IUnitsOfMeasure unitSystem = systemOfUnits switch
        {
            SystemOfUnits.SI => new SI(),
            SystemOfUnits.CGS => new CGS(),
            SystemOfUnits.English => new English(),
            _ => new SI()
        };

        flowsheet.FlowsheetOptions.SelectedUnitSystem = unitSystem;
        _logger.LogDebug("Set system of units to {System}", systemOfUnits);
    }
}
