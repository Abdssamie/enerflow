using Enerflow.Domain.DTOs;
using Enerflow.Domain.Entities;
using Enerflow.Domain.Enums;
using Enerflow.Domain.Common;

namespace Enerflow.Domain.Extensions;

public static class SimulationMappingExtensions
{
    public static SimulationJob ToSimulationJob(this Simulation simulation)
    {
        return new SimulationJob
        {
            JobId = IdGenerator.NextGuid(),
            SimulationId = simulation.Id,
            Definition = simulation.ToSimulationDefinitionDto()
        };
    }

    public static SimulationDefinitionDto ToSimulationDefinitionDto(this Simulation simulation)
    {
        // Parse enums
        var propertyPackage = Enum.TryParse<PropertyPackage>(simulation.ThermoPackage, out var pp)
            ? pp
            : PropertyPackage.PengRobinson; // Default or fallback

        var flashAlgorithm = Enum.TryParse<FlashAlgorithm>(simulation.FlashAlgorithm, out var fa)
            ? fa
            : FlashAlgorithm.NestedLoops; // Default

        var systemOfUnits = Enum.TryParse<SystemOfUnits>(simulation.SystemOfUnits, out var sou)
            ? sou
            : SystemOfUnits.SI;

        return new SimulationDefinitionDto
        {
            Name = simulation.Name,
            PropertyPackage = propertyPackage,
            FlashAlgorithm = flashAlgorithm,
            SystemOfUnits = systemOfUnits,
            Compounds = simulation.Compounds?.Select(c => c.ToCompoundDto()).ToList() ?? new(),
            MaterialStreams = simulation.MaterialStreams?.Select(s => s.ToMaterialStreamDto()).ToList() ?? new(),
            EnergyStreams = simulation.EnergyStreams?.Select(s => s.ToEnergyStreamDto()).ToList() ?? new(),
            UnitOperations = simulation.UnitOperations?.Select(u => u.ToUnitOperationDto()).ToList() ?? new()
        };
    }

    public static CompoundDto ToCompoundDto(this Compound compound)
    {
        return new CompoundDto(compound.Id, compound.Name, compound.ConstantProperties);
    }

    public static MaterialStreamDto ToMaterialStreamDto(this MaterialStream stream)
    {
        return new MaterialStreamDto
        {
            Id = stream.Id,
            Name = stream.Name,
            Temperature = stream.Temperature,
            Pressure = stream.Pressure,
            MassFlow = stream.MassFlow,
            MolarCompositions = stream.MolarCompositions ?? new()
        };
    }

    public static EnergyStreamDto ToEnergyStreamDto(this EnergyStream stream)
    {
        return new EnergyStreamDto
        {
            Id = stream.Id,
            Name = stream.Name,
            EnergyFlow = stream.EnergyFlow
        };
    }

    public static UnitOperationDto ToUnitOperationDto(this UnitOperation unit)
    {
        var type = Enum.TryParse<UnitOperationType>(unit.Type, out var uot)
            ? uot
            : UnitOperationType.Mixer; // Default? Or maybe throw if unknown.

        return new UnitOperationDto
        {
            Id = unit.Id,
            Name = unit.Name,
            Type = type,
            InputStreamIds = unit.InputStreamIds ?? new(),
            OutputStreamIds = unit.OutputStreamIds ?? new(),
            ConfigParams = unit.ConfigParams
        };
    }
}
