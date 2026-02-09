using Microsoft.Extensions.Logging;

namespace Enerflow.Worker.Convergence;

public class WegsteinAccelerator : IConvergenceAccelerator
{
    private readonly ILogger<WegsteinAccelerator> _logger;
    private double[]? _prevInput;
    private double[]? _prevOutput;

    public double MinLambda { get; set; } = -5.0;
    public double MaxLambda { get; set; } = 0.0;
    
    // Bounds for the calculated Q factor. 
    // Standard Wegstein bounds are often considered:
    // If slope s is such that we are oscillating, q becomes negative.
    // X_new = q*x + (1-q)*y
    // q = s / (s-1)
    
    public WegsteinAccelerator(ILogger<WegsteinAccelerator> logger)
    {
        _logger = logger;
    }

    public void Reset()
    {
        _prevInput = null;
        _prevOutput = null;
        _logger.LogTrace("Wegstein accelerator state reset.");
    }

    public double[] Accelerate(double[] currentValues, double[] newValues)
    {
        if (currentValues.Length != newValues.Length)
            throw new ArgumentException("Input and Output vectors must have the same length.");

        var nextValues = new double[currentValues.Length];
        
        // Initialize state if needed
        if (_prevInput == null || _prevInput.Length != currentValues.Length)
        {
            _prevInput = new double[currentValues.Length];
            _prevOutput = new double[currentValues.Length];
            
            // First step: Direct Substitution
            Array.Copy(currentValues, _prevInput, currentValues.Length);
            Array.Copy(newValues, _prevOutput, newValues.Length);
            Array.Copy(newValues, nextValues, newValues.Length);
            
            return nextValues;
        }

        for (int i = 0; i < currentValues.Length; i++)
        {
            var x = currentValues[i];
            var y = newValues[i];
            var xPrev = _prevInput[i];
            var yPrev = _prevOutput![i];

            // Denominator check to avoid div/0
            if (Math.Abs(x - xPrev) < 1e-9)
            {
                // No change in input? Direct substitution
                nextValues[i] = y;
            }
            else
            {
                // Calculate Slope s = (f(x) - f(x_prev)) / (x - x_prev)
                var s = (y - yPrev) / (x - xPrev);

                // Calculate Wegstein Q
                // q = s / (s - 1)
                double q;
                
                if (Math.Abs(s - 1) < 1e-9)
                {
                    // s approaches 1 -> instability. Fallback to direct sub or relaxation?
                    q = 0; // Direct substitution
                }
                else
                {
                    q = s / (s - 1.0);
                }

                // Bound Q
                if (q < MinLambda) q = MinLambda;
                if (q > MaxLambda) q = MaxLambda;

                // X_next = q * x + (1 - q) * y
                nextValues[i] = q * x + (1.0 - q) * y;
            }

            // Update history
            _prevInput[i] = x;
            _prevOutput[i] = y;
        }

        return nextValues;
    }
}
