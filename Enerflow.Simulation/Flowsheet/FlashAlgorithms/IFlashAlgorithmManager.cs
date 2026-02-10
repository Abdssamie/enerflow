using Enerflow.Domain.Enums;
using DWSIM.Interfaces;
using EnerflowFlashAlgorithm = Enerflow.Domain.Enums.FlashAlgorithm;

namespace Enerflow.Simulation.Flowsheet.FlashAlgorithms;

/// <summary>
/// Manages flash algorithm creation for DWSIM property packages.
/// </summary>
public interface IFlashAlgorithmManager
{
   /// <summary>
   /// Creates a DWSIM flash algorithm instance from the Enerflow enum.
   /// </summary>
   IFlashAlgorithm CreateFlashAlgorithm(EnerflowFlashAlgorithm algorithmType);

   /// <summary>
   /// Sets the flash algorithm for the property package.
   /// </summary>
   void SetFlashAlgorithm(IPropertyPackage package, IFlashAlgorithm flashAlgorithm);
}
