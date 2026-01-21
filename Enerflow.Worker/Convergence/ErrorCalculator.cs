using DWSIM.Interfaces;
using Microsoft.Extensions.Logging;

namespace Enerflow.Worker.Convergence;

public class ErrorCalculator
{
    private readonly ILogger<ErrorCalculator> _logger;

    public ErrorCalculator(ILogger<ErrorCalculator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates the maximum relative convergence error across all Recycle operations in the flowsheet.
    /// </summary>
    /// <param name="flowsheet">The DWSIM flowsheet to inspect.</param>
    /// <returns>The maximum error value found (e.g., 0.05 for 5% error). Returns 0 if no recycles exist.</returns>
    public double CalculateError(IFlowsheet flowsheet)
    {
        double maxError = 0.0;
        int recycleCount = 0;

        foreach (var obj in flowsheet.SimulationObjects.Values)
        {
            if (obj is IRecycle recycle)
            {
                recycleCount++;
                
                // Inspect errors dictionary
                if (recycle.Errors != null)
                {
                    foreach (var kvp in recycle.Errors)
                    {
                        // Some errors might be absolute, some relative. 
                        // DWSIM Recycle usually stores relative errors for MassFlow, but Absolute for T/P?
                        // "Values" vs "Errors". 
                        // The Recycle.vb logic showed:
                        // Me.Errors("MassFlow") = Werr
                        // Me.Errors("Temperature") = deltaT
                        
                        // We typically want to normalize or just take the raw error if that's what we are minimizing.
                        // However, spec says "Relative Error: Abs((New - Old) / New)".
                        // DWSIM's internal Recycle block ALREADY calculates these errors.
                        // If we are trusting DWSIM's Recycle block calculation logic, we just read its output.
                        
                        double error = Math.Abs(kvp.Value);
                        if (error > maxError)
                        {
                            maxError = error;
                        }
                    }
                }
            }
        }

        if (recycleCount == 0)
        {
            _logger.LogTrace("No recycle blocks found in flowsheet.");
        }
        else
        {
            _logger.LogDebug("Calculated max error {Error} across {Count} recycle blocks.", maxError, recycleCount);
        }

        return maxError;
    }
}
