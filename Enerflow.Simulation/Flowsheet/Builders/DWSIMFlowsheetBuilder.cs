using DWSIM.Interfaces;
using DWSIM.SharedClasses.SystemsOfUnits;
using Enerflow.Domain.Enums;
using Enerflow.Simulation.Flowsheet.Compounds;
using Enerflow.Simulation.Flowsheet.Connections;
using Enerflow.Simulation.Flowsheet.FlashAlgorithms;
using Enerflow.Simulation.Flowsheet.PropertyPackages;
using Enerflow.Simulation.Flowsheet.Streams;
using Enerflow.Simulation.Flowsheet.UnitOperations;
using Enerflow.Simulation.Validation;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Simulation.Flowsheet.Builders;

public class DWSIMFlowsheetBuilder : IFlowsheetBuilder
{
   private readonly DWSIM.Automation.Automation3 _automation;
   private readonly ICompoundManager _compoundManager;
   private readonly IPropertyPackageManager _propertyPackageManager;
   private readonly IFlashAlgorithmManager _flashAlgorithmManager;
   private readonly IMaterialStreamFactory _materialStreamFactory;
   private readonly IEnergyStreamFactory _energyStreamFactory;
   private readonly IUnitOperationFactory _unitOperationFactory;
   private readonly IConnectionFactory _connectionFactory;
   private readonly IFlowsheetValidator _validator;
   private readonly ILogger<DWSIMFlowsheetBuilder> _logger;

   public DWSIMFlowsheetBuilder(
       DWSIM.Automation.Automation3 automation,
       ICompoundManager compoundManager,
       IPropertyPackageManager propertyPackageManager,
       IFlashAlgorithmManager flashAlgorithmManager,
       IMaterialStreamFactory materialStreamFactory,
       IEnergyStreamFactory energyStreamFactory,
       IUnitOperationFactory unitOperationFactory,
       IConnectionFactory connectionFactory,
       IFlowsheetValidator validator,
       ILogger<DWSIMFlowsheetBuilder> logger)
   {
      _automation = automation;
      _compoundManager = compoundManager;
      _propertyPackageManager = propertyPackageManager;
      _flashAlgorithmManager = flashAlgorithmManager;
      _materialStreamFactory = materialStreamFactory;
      _energyStreamFactory = energyStreamFactory;
      _unitOperationFactory = unitOperationFactory;
      _connectionFactory = connectionFactory;
      _validator = validator;
      _logger = logger;
   }

   public IFlowsheet BuildFlowsheet(SimulationEntity simulation)
   {
      _logger.LogInformation("Building flowsheet for simulation {Id}: {Name}", simulation.Id, simulation.Name);

      // 1. Initialization
      DWSIM.GlobalSettings.Settings.AutomationMode = true;

      // 2. Create Flowsheet
      var flowsheet = _automation.CreateFlowsheet();

      // 3. Configure Settings
      SetSystemOfUnits(flowsheet, simulation.SystemOfUnits);

      // 4. Add Compounds
      _compoundManager.AddCompounds(flowsheet, simulation.Compounds);

      // 5. Property Package & Flash Algorithm
      var propertyPackage = _propertyPackageManager.CreatePropertyPackage(simulation.PropertyPackage);
      var flashAlgorithm = _flashAlgorithmManager.CreateFlashAlgorithm(simulation.FlashAlgorithm);
      _flashAlgorithmManager.SetFlashAlgorithm(propertyPackage, flashAlgorithm);
      _propertyPackageManager.AddToFlowsheet(flowsheet, propertyPackage);

      // 6. Create and Configure Material Streams (batch - no loop)
      _materialStreamFactory.CreateAndConfigureStreams(flowsheet, simulation.MaterialStreams, simulation.SystemOfUnits);

      // 7. Create and Configure Energy Streams (batch - no loop)
      _energyStreamFactory.CreateAndConfigureStreams(flowsheet, simulation.EnergyStreams);

      // 8. Create and Configure Unit Operations (batch - no loop)
      var compoundLookup = simulation.Compounds.ToDictionary(c => c.Id, c => c.Name);
      _unitOperationFactory.CreateAndConfigureUnitOperations(flowsheet, simulation.UnitOperations, compoundLookup);

      // 9. Connect Flowsheet (batch - ConnectionFactory handles loops internally)
      #pragma warning disable CA1873 
      _logger.LogInformation("Connecting flowsheet for simulation {Id}", simulation.Id);
      
      _connectionFactory.ConnectFlowsheet(simulation, flowsheet);
       _logger.LogInformation("Flowsheet connected successfully for simulation {Id}", simulation.Id);
       
       // 10. Post-Connection Configuration (e.g., splitter ratios)
       _logger.LogInformation("Configuring post-connection settings for simulation {Id}", simulation.Id);
       _unitOperationFactory.ConfigurePostConnection(flowsheet, simulation);
       _logger.LogInformation("Post-connection configuration completed for simulation {Id}", simulation.Id);

       // 11. Validate
      _logger.LogInformation("Validating flowsheet for simulation {Id}", simulation.Id);
      var validationResult = _validator.Validate(simulation, flowsheet);

      if (!validationResult.IsValid)
      {
         _logger.LogWarning("Flowsheet validation failed for simulation {Id} with {Count} errors",
             simulation.Id, validationResult.Errors.Count);
         throw new FlowsheetValidationException(validationResult);
      }

      _logger.LogInformation("Flowsheet validation passed for simulation {Id}", simulation.Id);
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
