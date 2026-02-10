using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.SharedClasses.SystemsOfUnits;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Enums;
using Enerflow.Simulation.Flowsheet.Compounds;
using Enerflow.Simulation.Flowsheet.FlashAlgorithms;
using Enerflow.Simulation.Flowsheet.PropertyPackages;
using Enerflow.Simulation.Flowsheet.Streams;
using Enerflow.Simulation.Flowsheet.UnitOperations;
using Enerflow.Worker.Validation;
using Microsoft.Extensions.Logging;
using SimulationEntity = Enerflow.Domain.Entities.Simulation;

namespace Enerflow.Worker.Builders;

public class DWSIMFlowsheetBuilder : IFlowsheetBuilder
{
   private readonly DWSIM.Automation.Automation3 _automation;
   private readonly ICompoundManager _compoundManager;
   private readonly IPropertyPackageManager _propertyPackageManager;
   private readonly IFlashAlgorithmManager _flashAlgorithmManager;
   private readonly IMaterialStreamFactory _materialStreamFactory;
   private readonly IEnergyStreamFactory _energyStreamFactory;
   private readonly IUnitOperationFactory _unitOperationFactory;
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
      foreach (var compound in simulation.Compounds)
      {
         var dto = new CompoundDto(compound.Id, compound.Name, compound.ConstantProperties);
         _compoundManager.AddCompound(flowsheet, dto);
      }

      // 5. Property Package & Flash Algorithm
      var propertyPackage = _propertyPackageManager.CreatePropertyPackage(simulation.PropertyPackage);
      var flashAlgorithm = _flashAlgorithmManager.CreateFlashAlgorithm(simulation.FlashAlgorithm);

      _propertyPackageManager.SetFlashAlgorithm(propertyPackage, flashAlgorithm);
      _propertyPackageManager.AddToFlowsheet(flowsheet, propertyPackage);

      // Map to track Stream IDs to Names for connection
      var streamMap = new Dictionary<Guid, string>();

      // 6. Create Material Streams using flowsheet.AddObject
      foreach (var stream in simulation.MaterialStreams)
      {
         // Use flowsheet.AddObject to create the stream instance
         var dwsimObj = flowsheet.AddObject(
             ObjectType.MaterialStream,
             0, 0,  // x, y coordinates (not used in headless mode)
             id: stream.Id.ToString(),
             tag: stream.Name
         );

         // Cast to MaterialStream and configure using factory
         if (dwsimObj is DWSIM.Thermodynamics.Streams.MaterialStream ms)
         {
            _materialStreamFactory.Configure(ms, stream, simulation.SystemOfUnits);
         }

         streamMap[stream.Id] = stream.Name;
      }

      // 7. Create Energy Streams using flowsheet.AddObject
      foreach (var stream in simulation.EnergyStreams)
      {
         var dwsimObj = flowsheet.AddObject(
             ObjectType.EnergyStream,
             0, 0,
             stream.Id.ToString(),
             stream.Name
         );

         if (dwsimObj is DWSIM.UnitOperations.Streams.EnergyStream es)
         {
            _energyStreamFactory.Configure(es, stream);
         }

         streamMap[stream.Id] = stream.Name;
      }

      // 8. Create Unit Operations using flowsheet.AddObject
      foreach (var unit in simulation.UnitOperations)
      {
         var graphicObjectType = _unitOperationFactory.GetGraphicObjectType(unit.Type);

         var dwsimObj = flowsheet.AddObject(
             graphicObjectType,
             0, 0,
             unit.Id.ToString(),
             unit.Name
         );

         // Note: Unit operation parameters are configured by UnitOperationMapper after creation
      }

      // VALIDATE BEFORE RETURNING
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
