using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Enerflow.Domain.Common;
using Enerflow.Domain.Enums;

namespace Enerflow.Domain.DTOs;

public record SimulationExportDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    public required PropertyPackageType PropertyPackage { get; init; }
    public required FlashAlgorithm FlashAlgorithm { get; init; }
    public required SystemOfUnits SystemOfUnits { get; init; }

    [Length(0, 100, ErrorMessage = "Cannot import more than 100 compounds.")]
    public List<CompoundExportDto> Compounds { get; init; } = new();

    [Length(0, 500, ErrorMessage = "Cannot import more than 500 material streams.")]
    public List<MaterialStreamExportDto> MaterialStreams { get; init; } = new();

    [Length(0, 500, ErrorMessage = "Cannot import more than 500 energy streams.")]
    public List<EnergyStreamExportDto> EnergyStreams { get; init; } = new();

    [Length(0, 500, ErrorMessage = "Cannot import more than 500 unit operations.")]
    public List<UnitOperationExportDto> UnitOperations { get; init; } = new();
}

public record CompoundExportDto
{
    public Guid Id { get; init; }

    [Required]
    [StringLength(100)]
    public required string Name { get; init; }

    public JsonDocument? ConstantProperties { get; init; }
}

public record MaterialStreamExportDto
{
    public Guid Id { get; init; }

    [Required]
    [StringLength(100)]
    public required string Name { get; init; }

    public double Temperature { get; init; }
    public double Pressure { get; init; }
    public double MassFlow { get; init; }

    [Length(0, 50, ErrorMessage = "Composition cannot exceed 50 components.")]
    public Dictionary<string, double>? Composition { get; init; }
}

public record EnergyStreamExportDto
{
    public Guid Id { get; init; }

    [Required]
    [StringLength(100)]
    public required string Name { get; init; }

    public double EnergyFlow { get; init; }
}

public record UnitOperationExportDto
{
    public Guid Id { get; init; }

    [Required]
    [StringLength(100)]
    public required string Name { get; init; }

    public required UnitOperationType Type { get; init; }

    public List<Guid> InputStreamIds { get; init; } = new();
    public List<Guid> OutputStreamIds { get; init; } = new();

    public Guid SimulationId { get; set; } = IdGenerator.NextGuid();
    public JsonDocument? ConfigParams { get; init; }
}
