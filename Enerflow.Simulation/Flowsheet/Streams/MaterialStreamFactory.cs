using Enerflow.Domain.DTOs;
using DWSIM.Thermodynamics.Streams;
using Microsoft.Extensions.Logging;
using Enerflow.Domain.Enums;

namespace Enerflow.Simulation.Flowsheet.Streams;

/// <summary>
/// Factory for creating and configuring DWSIM material streams.
/// </summary>
public class MaterialStreamFactory : IMaterialStreamFactory
{
    private readonly ILogger<MaterialStreamFactory> _logger;

    public MaterialStreamFactory(ILogger<MaterialStreamFactory> logger)
    {
        _logger = logger;
    }

    public MaterialStream CreateMaterialStream(MaterialStreamDto streamDto, SystemOfUnits systemOfUnits)
    {
        try
        {
            var stream = new MaterialStream(streamDto.Name, "");

            // Convert inputs to SI (Kelvin, Pascal, kg/s)
            double tempK = ConvertTemperatureToSI(streamDto.Temperature, systemOfUnits);
            double pressPa = ConvertPressureToSI(streamDto.Pressure, systemOfUnits);
            double massFlowKgS = ConvertMassFlowToSI(streamDto.MassFlow, systemOfUnits);

            // Set stream conditions (DWSIM always expects SI internally)
            stream.Phases[0].Properties.temperature = tempK;
            stream.Phases[0].Properties.pressure = pressPa;
            stream.Phases[0].Properties.massflow = massFlowKgS;

            // Set compositions
            foreach (var (compoundName, moleFraction) in streamDto.MolarCompositions)
            {
                if (stream.Phases[0].Compounds.ContainsKey(compoundName))
                {
                    stream.Phases[0].Compounds[compoundName].MoleFraction = moleFraction;
                }
            }

            _logger.LogDebug("Created material stream: {Name} (T={T}K, P={P}Pa, F={F}kg/s)",
                streamDto.Name, tempK, pressPa, massFlowKgS);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create material stream: {Name}", streamDto.Name);
            throw;
        }
    }

    private double ConvertTemperatureToSI(double value, SystemOfUnits units)
    {
        return units switch
        {
            SystemOfUnits.SI => value, // Kelvin
            SystemOfUnits.CGS => value + 273.15, // Celsius to Kelvin
            SystemOfUnits.English => (value - 32) * 5 / 9 + 273.15, // Fahrenheit to Kelvin
            _ => value // Default assume SI
        };
    }

    private double ConvertPressureToSI(double value, SystemOfUnits units)
    {
        return units switch
        {
            SystemOfUnits.SI => value, // Pascal
            SystemOfUnits.CGS => value * 100000, // Bar to Pascal (approx) or atm? Assume Bar for CGS/Metric
            SystemOfUnits.English => value * 6894.76, // PSI to Pascal
            _ => value
        };
    }

    private double ConvertMassFlowToSI(double value, SystemOfUnits units)
    {
        return units switch
        {
            SystemOfUnits.SI => value, // kg/s
            SystemOfUnits.CGS => value / 3600.0, // kg/h to kg/s (Engineering Metric)
            SystemOfUnits.English => value * 0.45359237 / 3600.0, // lb/h to kg/s
            _ => value
        };
    }
}
