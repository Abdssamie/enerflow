using System.Text.Json;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Entities;
using Enerflow.Domain.Entities.Streams;
using Enerflow.Domain.Entities.UnitOperations;
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
        return new SimulationDefinitionDto
        {
            Name = simulation.Name,
            PropertyPackageType = simulation.ThermoPackage,
            FlashAlgorithm = simulation.FlashAlgorithm,
            SystemOfUnits = simulation.SystemOfUnits,
            Compounds = simulation.Compounds.Select(c => c.ToCompoundDto()).ToList(),
            MaterialStreams = simulation.MaterialStreams.Select(s => s.ToMaterialStreamDto()).ToList(),
            EnergyStreams = simulation.EnergyStreams.Select(s => s.ToEnergyStreamDto()).ToList(),
            UnitOperations = simulation.UnitOperations.Select(u => u.ToUnitOperationDto()).ToList()
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
            MolarCompositions = stream.Composition
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

    public static UnitOperationDto ToUnitOperationDto(this UnitOperationObject unit)
    {
        return new UnitOperationDto
        {
            Id = unit.Id,
            Name = unit.Name,
            Type = unit.Type,
            InputStreamIds = unit.InputStreamIds,
            OutputStreamIds = unit.OutputStreamIds,
            ConfigParams = JsonSerializer.SerializeToDocument(unit, unit.GetType())
        };
    }
}
