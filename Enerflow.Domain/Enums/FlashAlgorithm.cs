namespace Enerflow.Domain.Enums;

/// <summary>
/// Flash algorithms for phase equilibrium calculations in DWSIM.
/// </summary>
public enum FlashAlgorithm
{
    /// <summary>
    /// Standard nested loops algorithm for VLE. Default for most systems.
    /// </summary>
    NestedLoops,

    /// <summary>
    /// Inside-Out algorithm for faster convergence in VLE systems.
    /// </summary>
    InsideOut,

    /// <summary>
    /// Three-phase Inside-Out algorithm for VLLE systems.
    /// </summary>
    InsideOut3Phase,

    /// <summary>
    /// Gibbs minimization for three-phase equilibrium.
    /// </summary>
    GibbsMinimization3Phase,

    /// <summary>
    /// Three-phase nested loops algorithm.
    /// </summary>
    NestedLoops3Phase,

    /// <summary>
    /// Solid-liquid equilibrium algorithm.
    /// </summary>
    SolidLiquidEquilibrium,

    /// <summary>
    /// Algorithm for immiscible liquid-liquid systems.
    /// </summary>
    ImmiscibleLLE,

    /// <summary>
    /// Simple liquid-liquid equilibrium algorithm.
    /// </summary>
    SimpleLLE,

    /// <summary>
    /// Solid-vapor-liquid-liquid equilibrium algorithm.
    /// </summary>
    SVLLE,

    /// <summary>
    /// Universal flash - automatically selects appropriate algorithm.
    /// </summary>
    Universal
}
