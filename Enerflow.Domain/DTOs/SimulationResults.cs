namespace Enerflow.Domain.DTOs;

/// <summary>
/// Strongly-typed results from a simulation execution.
/// Replaces the weakly-typed Dictionary&lt;string, Dictionary&lt;string, object&gt;&gt; approach.
/// </summary>
public record SimulationResultsDto
{
    public required Dictionary<string, MaterialStreamResultDto> MaterialStreams { get; init; }
    public required Dictionary<string, UnitOperationResultDto> UnitOperations { get; init; }
    public required List<string> Warnings { get; init; }
}

/// <summary>
/// Results for a material stream after simulation.
/// All values are in SI units (K, Pa, kg/s).
/// </summary>
public record MaterialStreamResultDto
{
    public required string Name { get; init; }

    /// <summary>Temperature in Kelvin</summary>
    public double Temperature { get; init; }

    /// <summary>Pressure in Pascal</summary>
    public double Pressure { get; init; }

    /// <summary>Mass flow rate in kg/s</summary>
    public double MassFlow { get; init; }

    /// <summary>Molar flow rate in mol/s</summary>
    public double MolarFlow { get; init; }

    /// <summary>Volumetric flow rate in m³/s</summary>
    public double VolumetricFlow { get; init; }

    /// <summary>Enthalpy in J/mol</summary>
    public double Enthalpy { get; init; }

    /// <summary>Molar compositions (compound name -> mole fraction)</summary>
    public required Dictionary<string, double> MolarCompositions { get; init; }
}

/// <summary>
/// Results for a unit operation after simulation.
/// </summary>
public record UnitOperationResultDto
{
    public required string Name { get; init; }

    /// <summary>Whether the unit operation calculation succeeded</summary>
    public bool Calculated { get; init; }

    /// <summary>Any error message from the unit operation</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Additional unit-specific properties (e.g., duty for heaters)</summary>
    public Dictionary<string, double>? AdditionalProperties { get; init; }
}
