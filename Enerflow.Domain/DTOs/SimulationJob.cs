using System.Text.Json;
using Enerflow.Domain.Enums;

namespace Enerflow.Domain.DTOs;

// --- Worker Job DTOs ---

public record SimulationJob
{
    public Guid JobId { get; init; } = Common.IdGenerator.NextGuid();
    public required Guid SimulationId { get; init; }

    // The complete definition required to build/solve the flowsheet
    public required SimulationDefinitionDto Definition { get; init; }
}

public record SimulationDefinitionDto
{
    public required string Name { get; init; }
    public required PropertyPackageType PropertyPackageType { get; init; }
    public required FlashAlgorithm FlashAlgorithm { get; init; }
    public required SystemOfUnits SystemOfUnits { get; init; }

    public List<CompoundDto> Compounds { get; init; } = [];
    public List<MaterialStreamDto> MaterialStreams { get; init; } = [];
    public List<EnergyStreamDto> EnergyStreams { get; init; } = [];
    public List<UnitOperationDto> UnitOperations { get; init; } = [];
}

/// <summary>
/// DTO representing a chemical compound in the simulation.
/// Name must match name expect by the DWSIM compound database for correct property retrieval.
/// </summary>
public record CompoundDto(Guid Id, string Name, JsonDocument? ConstantProperties);

public record MaterialStreamDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public double Temperature { get; init; }
    public double Pressure { get; init; }
    public double MassFlow { get; init; }
    public Dictionary<string, double> MolarCompositions { get; init; } = [];
}

public record EnergyStreamDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public double EnergyFlow { get; init; }
}

public record UnitOperationDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required UnitOperationType Type { get; init; }
    public List<Guid> InputStreamIds { get; init; } = [];
    public List<Guid> OutputStreamIds { get; init; } = [];
    public JsonDocument? ConfigParams { get; init; }
}
