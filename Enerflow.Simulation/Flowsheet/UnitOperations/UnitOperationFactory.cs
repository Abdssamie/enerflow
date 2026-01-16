using System.Text.Json;
using Enerflow.Domain.Enums;
using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.UnitOperations.UnitOperations;
using Microsoft.Extensions.Logging;
using DWSIM.UnitOperations.SpecialOps;
using DWSIM.UnitOperations.Reactors;

namespace Enerflow.Simulation.Services;

/// <summary>
/// Factory for creating DWSIM unit operation objects from the UnitOperation enum.
/// </summary>
public class UnitOperationFactory
{
    private readonly ILogger<UnitOperationFactory> _logger;

    public UnitOperationFactory(ILogger<UnitOperationFactory> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a DWSIM unit operation based on the UnitOperation enum type.
    /// </summary>
    /// <param name="type">The unit operation type enum</param>
    /// <param name="name">The name for the unit operation</param>
    /// <param name="configParams">Optional configuration parameters as JSON</param>
    /// <returns>The created unit operation, or null if type is not supported</returns>
    public ISimulationObject? CreateUnitOperation(UnitOperationType type, string name, JsonDocument? configParams = null)
    {
        _logger.LogDebug("Creating unit operation: {Type} named {Name}", type, name);

        try
        {
            ISimulationObject? unitOp = type switch
            {
                // MVP Unit Operations
                UnitOperationType.Mixer => new Mixer { Name = name },
                UnitOperationType.Splitter => new Splitter { Name = name },
                UnitOperationType.Separator => new Vessel { Name = name },
                UnitOperationType.Tank => new Tank { Name = name },
                UnitOperationType.Pipe => new Pipe { Name = name },
                UnitOperationType.Valve => new Valve { Name = name },
                UnitOperationType.Pump => new Pump { Name = name },
                UnitOperationType.Compressor => new Compressor { Name = name },
                UnitOperationType.Expander => new Expander { Name = name },
                UnitOperationType.Heater => new Heater { Name = name },
                UnitOperationType.Cooler => new Cooler { Name = name },
                UnitOperationType.HeatExchanger => new HeatExchanger { Name = name },

                // Phase 2 Unit Operations
                UnitOperationType.ReactorConversion => new Reactor_Conversion { Name = name },
                UnitOperationType.ReactorEquilibrium => new Reactor_Equilibrium { Name = name },
                UnitOperationType.ReactorGibbs => new Reactor_Gibbs { Name = name },
                UnitOperationType.ReactorCSTR => new Reactor_CSTR { Name = name },
                UnitOperationType.ReactorPFR => new Reactor_PFR { Name = name },
                UnitOperationType.DistillationColumn => new DistillationColumn { Name = name },
                UnitOperationType.AbsorptionColumn => new AbsorptionColumn { Name = name },
                UnitOperationType.ComponentSeparator => new ComponentSeparator { Name = name },
                UnitOperationType.OrificePlate => new OrificePlate { Name = name },
                UnitOperationType.Recycle => new Recycle { Name = name },
                UnitOperationType.Adjust => new Adjust { Name = name },
                UnitOperationType.Spec => new Spec { Name = name },

                _ => null
            };

            if (unitOp == null)
            {
                _logger.LogWarning("Unsupported unit operation type: {Type}", type);
                return null;
            }

            // Apply configuration parameters if provided
            if (configParams != null)
            {
                ApplyConfigParams(unitOp, type, configParams);
            }

            _logger.LogDebug("Successfully created unit operation: {Type} named {Name}", type, name);
            return unitOp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create unit operation: {Type} named {Name}", type, name);
            return null;
        }
    }

    /// <summary>
    /// Applies configuration parameters from JSON to the unit operation.
    /// </summary>
    private void ApplyConfigParams(ISimulationObject unitOp, UnitOperationType type, JsonDocument configParams)
    {
        try
        {
            var root = configParams.RootElement;

            switch (type)
            {
                case UnitOperationType.Pump when unitOp is Pump pump:
                    if (root.TryGetProperty("deltaP", out var deltaP))
                        pump.DeltaP = deltaP.GetDouble();
                    if (root.TryGetProperty("efficiency", out var pumpEff))
                        pump.Eficiencia = pumpEff.GetDouble();
                    break;

                case UnitOperationType.Compressor when unitOp is Compressor comp:
                    if (root.TryGetProperty("efficiency", out var compEff))
                        comp.AdiabaticEfficiency = compEff.GetDouble();
                    if (root.TryGetProperty("outletPressure", out var compP))
                        comp.POut = compP.GetDouble();
                    break;

                case UnitOperationType.Expander when unitOp is Expander exp:
                    if (root.TryGetProperty("efficiency", out var expEff))
                        exp.AdiabaticEfficiency = expEff.GetDouble();
                    if (root.TryGetProperty("outletPressure", out var expP))
                        exp.POut = expP.GetDouble();
                    break;

                case UnitOperationType.Heater when unitOp is Heater heater:
                    if (root.TryGetProperty("outletTemperature", out var heaterT))
                        heater.OutletTemperature = heaterT.GetDouble();
                    if (root.TryGetProperty("heatDuty", out var heaterQ))
                        heater.DeltaQ = heaterQ.GetDouble();
                    break;

                case UnitOperationType.Cooler when unitOp is Cooler cooler:
                    if (root.TryGetProperty("outletTemperature", out var coolerT))
                        cooler.OutletTemperature = coolerT.GetDouble();
                    if (root.TryGetProperty("heatDuty", out var coolerQ))
                        cooler.DeltaQ = coolerQ.GetDouble();
                    break;

                case UnitOperationType.Valve when unitOp is Valve valve:
                    if (root.TryGetProperty("outletPressure", out var valveP))
                        valve.OutletPressure = valveP.GetDouble();
                    break;

                case UnitOperationType.HeatExchanger when unitOp is HeatExchanger hx:
                    if (root.TryGetProperty("hotSideOutletTemperature", out var hxHotT))
                        hx.HotSideOutletTemperature = hxHotT.GetDouble();
                    if (root.TryGetProperty("coldSideOutletTemperature", out var hxColdT))
                        hx.ColdSideOutletTemperature = hxColdT.GetDouble();
                    break;

                case UnitOperationType.Separator when unitOp is Vessel vessel:
                    if (root.TryGetProperty("pressure", out var vesselP))
                        vessel.FlashPressure = vesselP.GetDouble();
                    if (root.TryGetProperty("temperature", out var vesselT))
                        vessel.FlashTemperature = vesselT.GetDouble();
                    break;
            }

            _logger.LogDebug("Applied configuration parameters to {Name}", unitOp.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply some configuration parameters to {Name}", unitOp.Name);
        }
    }

    /// <summary>
    /// Gets the DWSIM graphic object type for visualization purposes.
    /// </summary>
    public ObjectType GetGraphicObjectType(UnitOperationType type)
    {
        return type switch
        {
            UnitOperationType.Mixer => ObjectType.NodeIn,
            UnitOperationType.Splitter => ObjectType.NodeOut,
            UnitOperationType.Separator => ObjectType.Vessel,
            UnitOperationType.Tank => ObjectType.Tank,
            UnitOperationType.Pipe => ObjectType.Pipe,
            UnitOperationType.Valve => ObjectType.Valve,
            UnitOperationType.Pump => ObjectType.Pump,
            UnitOperationType.Compressor => ObjectType.Compressor,
            UnitOperationType.Expander => ObjectType.Expander,
            UnitOperationType.Heater => ObjectType.Heater,
            UnitOperationType.Cooler => ObjectType.Cooler,
            UnitOperationType.HeatExchanger => ObjectType.HeatExchanger,
            UnitOperationType.ReactorConversion => ObjectType.RCT_Conversion,
            UnitOperationType.ReactorEquilibrium => ObjectType.RCT_Equilibrium,
            UnitOperationType.ReactorGibbs => ObjectType.RCT_Gibbs,
            UnitOperationType.ReactorCSTR => ObjectType.RCT_CSTR,
            UnitOperationType.ReactorPFR => ObjectType.RCT_PFR,
            UnitOperationType.DistillationColumn => ObjectType.DistillationColumn,
            UnitOperationType.AbsorptionColumn => ObjectType.AbsorptionColumn,
            UnitOperationType.ComponentSeparator => ObjectType.ComponentSeparator,
            UnitOperationType.OrificePlate => ObjectType.OrificePlate,
            UnitOperationType.Recycle => ObjectType.OT_Recycle,
            UnitOperationType.Adjust => ObjectType.OT_Adjust,
            UnitOperationType.Spec => ObjectType.OT_Spec,
            _ => ObjectType.Nenhum
        };
    }
}
