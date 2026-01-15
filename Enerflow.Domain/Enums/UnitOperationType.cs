namespace Enerflow.Domain.Enums;

public enum UnitOperationType
{
    // MVP
    Mixer,
    Splitter,
    Separator,
    Tank,
    Pipe,
    Valve,
    Pump,
    Compressor,
    Expander,
    Heater,
    Cooler,
    HeatExchanger,
    
    // Phase 2
    ReactorConversion,
    ReactorEquilibrium,
    ReactorGibbs,
    ReactorCSTR,
    ReactorPFR,
    DistillationColumn,
    AbsorptionColumn,
    ComponentSeparator,
    OrificePlate,
    Recycle,
    Adjust,
    Spec
}
