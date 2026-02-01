using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.UnitOperations.SpecialOps;
using DWSIM.UnitOperations.UnitOperations;
using Enerflow.Domain.Entities.UnitOperations;
using Enerflow.Domain.Enums;
using Microsoft.Extensions.Logging;
namespace Enerflow.Worker.Mappers;

public class UnitOperationMapper : IUnitOperationMapper
{
    private readonly ILogger<UnitOperationMapper> _logger;

    public UnitOperationMapper(ILogger<UnitOperationMapper> logger)
    {
        _logger = logger;
    }

    public void Map(UnitOperationObject domainObject, IFlowsheet flowsheet, IReadOnlyDictionary<Guid, string> compoundNames)
    {
        _logger.LogDebug("Mapping Unit Operation: {Name} ({Type})", domainObject.Name, domainObject.Type);

        switch (domainObject)
        {
            case HeaterObject heater:
                MapHeater(heater, flowsheet);
                break;
            case CoolerObject cooler:
                MapCooler(cooler, flowsheet);
                break;
            case ValveObject valve:
                MapValve(valve, flowsheet);
                break;
            case MixerObject mixer:
                MapMixer(mixer, flowsheet);
                break;
            case SplitterObject splitter:
                MapSplitter(splitter, flowsheet);
                break;
            case FlashDrumObject flash:
                MapFlashDrum(flash, flowsheet);
                break;
            case ShortcutColumnObject column:
                MapShortcutColumn(column, flowsheet, compoundNames);
                break;
            case RecycleObject recycle:
                MapRecycle(recycle, flowsheet);
                break;
            default:
                _logger.LogWarning("Unit Operation type {Type} not yet supported.", domainObject.Type);
                break;
        }
    }

    private void MapHeater(HeaterObject domainHeater, IFlowsheet flowsheet)
    {
        var obj = flowsheet.AddObject(ObjectType.Heater, 0, 0, domainHeater.Name);
        var heater = (Heater)obj;

        // CRITICAL: Set CalcMode FIRST
        heater.CalcMode = MapHeaterCalcMode(domainHeater.CalcMode);

        // Set Properties
        heater.Efficiency = domainHeater.Efficiency * 100.0; // %
        heater.PressureDrop = domainHeater.PressureDrop; // Pa

        switch (domainHeater.CalcMode)
        {
            case HeaterCalculationMode.OutletTemperature:
                heater.OutletTemperature = domainHeater.OutletTemperature; // K
                break;
            case HeaterCalculationMode.HeatDuty:
                heater.HeatDuty = domainHeater.HeatDuty; // kW
                break;
            case HeaterCalculationMode.TemperatureDrop:
                heater.TemperatureChange = domainHeater.TemperatureChange; // K
                break;
            case HeaterCalculationMode.EnergyStream:
                break;
        }
    }

    private void MapCooler(CoolerObject domainCooler, IFlowsheet flowsheet)
    {
        var obj = flowsheet.AddObject(ObjectType.Cooler, 0, 0, domainCooler.Name);
        var cooler = (Cooler)obj;

        // CRITICAL: Set CalcMode FIRST
        cooler.CalcMode = MapCoolerCalcMode(domainCooler.CalcMode);

        // Set Properties
        cooler.Efficiency = domainCooler.Efficiency * 100.0; // %
        cooler.PressureDrop = domainCooler.PressureDrop; // Pa

        switch (domainCooler.CalcMode)
        {
            case HeaterCalculationMode.OutletTemperature:
                cooler.OutletTemperature = domainCooler.OutletTemperature; // K
                break;
            case HeaterCalculationMode.HeatDuty:
                cooler.HeatDuty = domainCooler.HeatDuty; // kW
                break;
            case HeaterCalculationMode.TemperatureDrop:
                cooler.TemperatureChange = domainCooler.TemperatureChange; // K
                break;
            case HeaterCalculationMode.EnergyStream:
                break;
        }
    }

    private void MapValve(ValveObject domainValve, IFlowsheet flowsheet)
    {
        var obj = flowsheet.AddObject(ObjectType.Valve, 0, 0, domainValve.Name);
        var valve = (Valve)obj;

        // CRITICAL: Set CalcMode FIRST
        valve.CalcMode = domainValve.CalcMode switch
        {
            ValveCalculationMode.OutletPressure => Valve.CalculationMode.OutletPressure,
            ValveCalculationMode.PressureDrop => Valve.CalculationMode.DeltaP,
            _ => Valve.CalculationMode.OutletPressure
        };

        if (domainValve.CalcMode == ValveCalculationMode.OutletPressure)
        {
            valve.OutletPressure = domainValve.OutletPressure;
        }
        else
        {
            valve.DeltaP = domainValve.PressureDrop;
        }
    }

    private void MapMixer(MixerObject domainMixer, IFlowsheet flowsheet)
    {
        // Mixer has no specific properties to map beyond creation
        flowsheet.AddObject(ObjectType.Mixer, 0, 0, domainMixer.Name);
    }

    private void MapSplitter(SplitterObject domainSplitter, IFlowsheet flowsheet)
    {
        var obj = flowsheet.AddObject(ObjectType.Splitter, 0, 0, domainSplitter.Name);
        var splitter = (Splitter)obj;

        // Ensure we are in SplitRatios mode
        splitter.OperationMode = Splitter.OpMode.SplitRatios;

        // Note: Split ratios depend on connection order. 
        // We cannot reliably set them here without knowing which port connects to which stream.
        // The ConnectionMapper or a post-connection step should handle ratio assignment 
        // by matching OutputStreamIds to the ports.
        _logger.LogDebug("Created Splitter {Name}. Ratios must be set after connections.", domainSplitter.Name);
    }

    private void MapFlashDrum(FlashDrumObject domainFlash, IFlowsheet flowsheet)
    {
        var obj = flowsheet.AddObject(ObjectType.Vessel, 0, 0, domainFlash.Name);
        var vessel = (Vessel)obj;

        // Map Flash Calculation Type
        switch (domainFlash.FlashType)
        {
            case FlashCalculationType.PressureTemperature:
                vessel.CalculationMode = Vessel.CalculationModes.Legacy;
                vessel.OverrideP = true;
                vessel.OverrideT = true;
                vessel.FlashPressure = domainFlash.OutletPressure;
                vessel.FlashTemperature = domainFlash.OutletTemperature;
                break;
            case FlashCalculationType.PressureEnthalpy:
                // Adiabatic flash (Heat Duty = 0)
                vessel.CalculationMode = Vessel.CalculationModes.Adiabatic;
                vessel.OverrideP = false;
                vessel.OverrideT = false;
                break;
            default:
                _logger.LogWarning("Flash Type {Type} not fully supported for FlashDrum {Name}. Defaulting to Adiabatic.", domainFlash.FlashType, domainFlash.Name);
                vessel.CalculationMode = Vessel.CalculationModes.Adiabatic;
                break;
        }
    }

    private void MapShortcutColumn(ShortcutColumnObject domainColumn, IFlowsheet flowsheet, IReadOnlyDictionary<Guid, string> compoundNames)
    {
        var obj = flowsheet.AddObject(ObjectType.ShortcutColumn, 0, 0, domainColumn.Name);
        var column = (ShortcutColumn)obj;

        column.m_refluxratio = domainColumn.RefluxRatio;
        column.m_condenserpressure = domainColumn.CondenserPressure;
        column.m_boilerpressure = domainColumn.ReboilerPressure;
        
        // Map Keys
        if (compoundNames.TryGetValue(domainColumn.LightKey, out var lkName))
        {
            column.m_lightkey = lkName;
        }
        else
        {
            _logger.LogError("Light Key compound {Id} not found in lookup.", domainColumn.LightKey);
        }

        if (compoundNames.TryGetValue(domainColumn.HeavyKey, out var hkName))
        {
            column.m_heavykey = hkName;
        }
        else
        {
            _logger.LogError("Heavy Key compound {Id} not found in lookup.", domainColumn.HeavyKey);
        }

        column.m_lightkeymolarfrac = domainColumn.LightKeyFraction;
        column.m_heavykeymolarfrac = domainColumn.HeavyKeyFraction;
        
        // Condenser Type? Domain doesn't have it yet. Defaulting to Total.
        column.condtype = ShortcutColumn.CondenserType.TotalCond;
    }

    private void MapRecycle(RecycleObject domainRecycle, IFlowsheet flowsheet)
    {
        var obj = flowsheet.AddObject(ObjectType.OT_Recycle, 0, 0, domainRecycle.Name);
        var recycle = (Recycle)obj;

        recycle.MaximumIterations = domainRecycle.MaxIterations;
        
        // Map Tolerance (assuming Mass Flow tolerance is the primary one, or set all)
        recycle.ConvergenceParameters.VazaoMassica = domainRecycle.Tolerance;
        recycle.ConvergenceParameters.Temperatura = domainRecycle.Tolerance * 100; // Just a heuristic scaling
        recycle.ConvergenceParameters.Pressao = domainRecycle.Tolerance * 1000;

        recycle.AccelerationMethod = domainRecycle.Acceleration switch
        {
            RecycleAccelerationMethod.Wegstein => DWSIM.Interfaces.Enums.AccelMethod.Wegstein,
            RecycleAccelerationMethod.Direct => DWSIM.Interfaces.Enums.AccelMethod.None,
            RecycleAccelerationMethod.DominantEigenvalue => DWSIM.Interfaces.Enums.AccelMethod.Dominant_Eigenvalue,
            _ => DWSIM.Interfaces.Enums.AccelMethod.Wegstein
        };
    }

    private Heater.CalculationMode MapHeaterCalcMode(HeaterCalculationMode mode)
    {
        return mode switch
        {
            HeaterCalculationMode.OutletTemperature => Heater.CalculationMode.OutletTemperature,
            HeaterCalculationMode.HeatDuty => Heater.CalculationMode.HeatAdded,
            HeaterCalculationMode.EnergyStream => Heater.CalculationMode.EnergyStream,
            HeaterCalculationMode.TemperatureDrop => Heater.CalculationMode.TemperatureChange,
            _ => Heater.CalculationMode.OutletTemperature
        };
    }

    private Cooler.CalculationMode MapCoolerCalcMode(HeaterCalculationMode mode)
    {
        return mode switch
        {
            HeaterCalculationMode.OutletTemperature => Cooler.CalculationMode.OutletTemperature,
            HeaterCalculationMode.HeatDuty => Cooler.CalculationMode.HeatRemoved,
            HeaterCalculationMode.EnergyStream => Cooler.CalculationMode.EnergyStream,
            HeaterCalculationMode.TemperatureDrop => Cooler.CalculationMode.TemperatureChange,
            _ => Cooler.CalculationMode.OutletTemperature
        };
    }
}
