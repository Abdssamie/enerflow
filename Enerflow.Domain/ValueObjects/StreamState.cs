namespace Enerflow.Domain.ValueObjects;

public record StreamState
{
    public double Temperature { get; init; } // Kelvin
    public double Pressure { get; init; }    // Pascal
    public double MassFlow { get; init; }    // kg/s
    public double MolarFlow { get; init; }   // mol/s
    public double Enthalpy { get; init; }    // kJ/kg
    public double[] MoleFractions { get; init; }

    private StreamState(double temperature, double pressure, double massFlow, double molarFlow, double enthalpy, double[] moleFractions)
    {
        if (temperature <= 0) throw new ArgumentException("Temperature must be greater than 0 K.");
        if (pressure <= 0) throw new ArgumentException("Pressure must be greater than 0 Pa.");
        if (massFlow < 0) throw new ArgumentException("Mass Flow cannot be negative.");

        Temperature = temperature;
        Pressure = pressure;
        MassFlow = massFlow;
        MolarFlow = molarFlow;
        Enthalpy = enthalpy;
        MoleFractions = moleFractions ?? Array.Empty<double>();
    }

    public static StreamState Create(double temperature, double pressure, double massFlow, double molarFlow, double enthalpy, double[] moleFractions)
    {
        return new StreamState(temperature, pressure, massFlow, molarFlow, enthalpy, moleFractions);
    }
}
