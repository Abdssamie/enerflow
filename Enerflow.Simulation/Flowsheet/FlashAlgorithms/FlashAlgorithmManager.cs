using Enerflow.Domain.Enums;
using DWSIM.Interfaces;
using DWSIM.Thermodynamics.PropertyPackages.Auxiliary.FlashAlgorithms;
using Microsoft.Extensions.Logging;
using EnerflowFlashAlgorithm = Enerflow.Domain.Enums.FlashAlgorithm;

namespace Enerflow.Simulation.Flowsheet.FlashAlgorithms;

/// <summary>
/// Manages flash algorithm creation for DWSIM property packages.
/// </summary>
public class FlashAlgorithmManager : IFlashAlgorithmManager
{
    private readonly ILogger<FlashAlgorithmManager> _logger;

    public FlashAlgorithmManager(ILogger<FlashAlgorithmManager> logger)
    {
        _logger = logger;
    }

    public IFlashAlgorithm CreateFlashAlgorithm(EnerflowFlashAlgorithm algorithmType)
    {
        IFlashAlgorithm algorithm = algorithmType switch
        {
            EnerflowFlashAlgorithm.NestedLoops => new NestedLoops(),
            EnerflowFlashAlgorithm.InsideOut => new BostonBrittInsideOut(),
            EnerflowFlashAlgorithm.InsideOut3Phase => new BostonFournierInsideOut3P(),
            EnerflowFlashAlgorithm.GibbsMinimization3Phase => new GibbsMinimization3P(),
            EnerflowFlashAlgorithm.NestedLoops3Phase => new NestedLoops3PV3(),
            EnerflowFlashAlgorithm.SolidLiquidEquilibrium => new NestedLoopsSLE(),
            EnerflowFlashAlgorithm.ImmiscibleLLE => new NestedLoopsImmiscible(),
            EnerflowFlashAlgorithm.SimpleLLE => new SimpleLLE(),
            EnerflowFlashAlgorithm.SVLLE => new NestedLoopsSVLLE(),
            EnerflowFlashAlgorithm.Universal => new UniversalFlash(),
            _ => new NestedLoops() // Fallback to default
        };

        _logger.LogDebug("Created flash algorithm: {AlgorithmType}", algorithmType);
        return algorithm;
    }
}
