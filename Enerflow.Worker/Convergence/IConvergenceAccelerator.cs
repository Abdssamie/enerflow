namespace Enerflow.Worker.Convergence;

/// <summary>
/// Defines a strategy for accelerating the convergence of iterative calculations.
/// </summary>
public interface IConvergenceAccelerator
{
    /// <summary>
    /// Calculates the next set of assumed values based on current input and calculated output.
    /// </summary>
    /// <param name="currentValues">The values assumed at the start of the current iteration (X_n).</param>
    /// <param name="newValues">The values calculated by the iteration function (F(X_n)).</param>
    /// <returns>The accelerated next guess (X_{n+1}).</returns>
    double[] Accelerate(double[] currentValues, double[] newValues);

    /// <summary>
    /// Resets any internal state, effectively restarting the acceleration history.
    /// </summary>
    void Reset();
}
