public interface IPhaseProperties
{
  double? osmoticCoefficient { get; set; }

  double? freezingPointDepression { get; set; }

  double? freezingPoint { get; set; }

  double? ionicStrength { get; set; }

  double? pH { get; set; }

  double? dewTemperature { get; set; }

  double? dewPressure { get; set; }

  double? bubbleTemperature { get; set; }

  double? bubblePressure { get; set; }

  double? activity { get; set; }

  double? activityCoefficient { get; set; }

  double? compressibility { get; set; }

  double? compressibilityFactor { get; set; }

  double? density { get; set; }

  double? enthalpy { get; set; }

  double? entropy { get; set; }

  double? enthalpyF { get; set; }

  double? entropyF { get; set; }

  double? excessEnthalpy { get; set; }

  double? excessEntropy { get; set; }

  double? molarflow { get; set; }

  double? massflow { get; set; }

  double? molarfraction { get; set; }

  double? massfraction { get; set; }

  double? fugacity { get; set; }

  double? fugacityCoefficient { get; set; }

  double? heatCapacityCp { get; set; }

  double? heatCapacityCv { get; set; }

  double? jouleThomsonCoefficient { get; set; }

  double? logFugacityCoefficient { get; set; }

  double? molecularWeight { get; set; }

  double? pressure { get; set; }

  double? temperature { get; set; }

  double? speedOfSound { get; set; }

  double? thermalConductivity { get; set; }

  double? viscosity { get; set; }

  double? kinematic_viscosity { get; set; }

  double? volumetric_flow { get; set; }

  double? molar_enthalpy { get; set; }

  double? molar_entropy { get; set; }

  double? molar_enthalpyF { get; set; }

  double? molar_entropyF { get; set; }

  double? kvalue { get; set; }

  double? logKvalue { get; set; }

  double? surfaceTension { get; set; }

  double? internal_energy { get; set; }

  double? molar_internal_energy { get; set; }

  double? gibbs_free_energy { get; set; }

  double? molar_gibbs_free_energy { get; set; }

  double? helmholtz_energy { get; set; }

  double? molar_helmholtz_energy { get; set; }

  double? bulk_modulus { get; set; }

  double? isothermal_compressibility { get; set; }

  double? mean_ionic_acitivty_coefficient { get; set; }

  double? idealGasHeatCapacityCp { get; set; }

  double? idealGasHeatCapacityRatio { get; set; }

  double? CO2loading { get; set; }

  double? CO2partialpressure { get; set; }

  double? H2Sloading { get; set; }

  double? H2Spartialpressure { get; set; }

  double? particleSize_Mean { get; set; }

  double? particleSize_StdDev { get; set; }

  double? volumetricFraction { get; set; }
}
