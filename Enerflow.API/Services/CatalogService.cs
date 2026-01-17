using Enerflow.Domain.Enums;

namespace Enerflow.API.Services;

/// <summary>
/// Service for providing catalog metadata about available compounds, property packages, and unit operations.
/// </summary>
public interface ICatalogService
{
    IEnumerable<CompoundInfo> GetCompounds(string? searchTerm = null);
    IEnumerable<PropertyPackageInfo> GetPropertyPackages();
    IEnumerable<FlashAlgorithmInfo> GetFlashAlgorithms();
    IEnumerable<UnitOperationInfo> GetUnitOperations();
}

public class CatalogService : ICatalogService
{
    private static readonly List<CompoundInfo> _compounds = new()
    {
        // Hydrocarbons
        new("Methane", "CH4", "74-82-8", "Alkane", "Simplest alkane, main component of natural gas"),
        new("Ethane", "C2H6", "74-84-0", "Alkane", "Second simplest alkane"),
        new("Propane", "C3H8", "74-98-6", "Alkane", "Used as fuel and refrigerant"),
        new("N-butane", "C4H10", "106-97-8", "Alkane", "Used in gasoline blending"),
        new("N-pentane", "C5H12", "109-66-0", "Alkane", "Used as solvent"),
        new("N-hexane", "C6H14", "110-54-3", "Alkane", "Industrial solvent"),
        new("N-heptane", "C7H16", "142-82-5", "Alkane", "Reference fuel for octane rating"),
        new("N-octane", "C8H18", "111-65-9", "Alkane", "Component of gasoline"),
        new("Benzene", "C6H6", "71-43-2", "Aromatic", "Basic aromatic compound"),
        new("Toluene", "C7H8", "108-88-3", "Aromatic", "Common aromatic solvent"),
        
        // Common gases
        new("Water", "H2O", "7732-18-5", "Inorganic", "Universal solvent"),
        new("Nitrogen", "N2", "7727-37-9", "Inorganic", "Inert gas, 78% of atmosphere"),
        new("Oxygen", "O2", "7782-44-7", "Inorganic", "Oxidizer, 21% of atmosphere"),
        new("Carbon dioxide", "CO2", "124-38-9", "Inorganic", "Greenhouse gas"),
        new("Carbon monoxide", "CO", "630-08-0", "Inorganic", "Toxic gas, reducing agent"),
        new("Hydrogen", "H2", "1333-74-0", "Inorganic", "Lightest element, fuel"),
        new("Hydrogen sulfide", "H2S", "7783-06-4", "Inorganic", "Toxic, sour gas"),
        new("Ammonia", "NH3", "7664-41-7", "Inorganic", "Fertilizer precursor"),
        new("Sulfur dioxide", "SO2", "7446-09-5", "Inorganic", "Industrial chemical"),
        
        // Alcohols
        new("Methanol", "CH4O", "67-56-1", "Alcohol", "Simplest alcohol"),
        new("Ethanol", "C2H6O", "64-17-5", "Alcohol", "Beverage and fuel alcohol"),
        new("1-propanol", "C3H8O", "71-23-8", "Alcohol", "Propyl alcohol"),
        new("2-propanol", "C3H8O", "67-63-0", "Alcohol", "Isopropyl alcohol"),
        
        // Other organics
        new("Acetone", "C3H6O", "67-64-1", "Ketone", "Common solvent"),
        new("Acetic acid", "C2H4O2", "64-19-7", "Carboxylic acid", "Vinegar component"),
        new("Ethylene", "C2H4", "74-85-1", "Alkene", "Polymer feedstock"),
        new("Propylene", "C3H6", "115-07-1", "Alkene", "Polypropylene feedstock"),
        
        // Refrigerants
        new("R134a", "C2H2F4", "811-97-2", "Refrigerant", "Common refrigerant"),
        new("R410a", "CH2F2/C2HF5", "N/A", "Refrigerant", "AC refrigerant blend")
    };

    private static readonly Dictionary<PropertyPackage, string> _propertyPackageDescriptions = new()
    {
        { PropertyPackage.PengRobinson, "Widely used cubic EOS for hydrocarbons and gases. Good for VLE up to moderate pressures." },
        { PropertyPackage.SoaveRedlichKwong, "Cubic EOS, good for non-polar components at low to moderate pressures." },
        { PropertyPackage.NRTL, "Activity coefficient model for highly non-ideal liquid mixtures. Excellent for polar systems." },
        { PropertyPackage.UNIQUAC, "Activity coefficient model based on molecular structure. Good for polar/non-polar mixtures." },
        { PropertyPackage.RaoultsLaw, "Ideal solution model. Only for nearly ideal mixtures at low pressures." },
        { PropertyPackage.SteamTables, "IAPWS-IF97 formulation for water/steam systems." },
        { PropertyPackage.IAPWS95, "High-accuracy formulation for water properties." }
    };

    private static readonly Dictionary<FlashAlgorithm, string> _flashAlgorithmDescriptions = new()
    {
        { FlashAlgorithm.NestedLoops, "Standard nested loops for VLE. Default choice for most systems." },
        { FlashAlgorithm.InsideOut, "Faster convergence for VLE. Good for distillation." },
        { FlashAlgorithm.InsideOut3Phase, "Three-phase VLE with Inside-Out method." },
        { FlashAlgorithm.GibbsMinimization3Phase, "Gibbs energy minimization for three-phase equilibrium." },
        { FlashAlgorithm.NestedLoops3Phase, "Nested loops for three-phase systems." },
        { FlashAlgorithm.SolidLiquidEquilibrium, "For systems with solid precipitation." },
        { FlashAlgorithm.ImmiscibleLLE, "For immiscible liquid-liquid systems." },
        { FlashAlgorithm.SimpleLLE, "Simple LLE for partially miscible systems." },
        { FlashAlgorithm.SVLLE, "Solid-vapor-liquid-liquid equilibrium." },
        { FlashAlgorithm.Universal, "Automatically selects appropriate algorithm." }
    };

    private static readonly Dictionary<UnitOperationType, UnitOperationMetadata> _unitOpMetadata = new()
    {
        { UnitOperationType.Mixer, new("Mixer", "Combines multiple inlet streams into one outlet", 2, 10, 1, 1, "MVP") },
        { UnitOperationType.Splitter, new("Splitter", "Divides one inlet stream into multiple outlets", 1, 1, 2, 10, "MVP") },
        { UnitOperationType.Separator, new("Flash/Separator", "Separates phases at equilibrium conditions", 1, 1, 2, 3, "MVP") },
        { UnitOperationType.Tank, new("Storage Tank", "Accumulates material", 1, 1, 1, 1, "MVP") },
        { UnitOperationType.Pipe, new("Pipe Segment", "Models pressure drop and heat transfer in pipes", 1, 1, 1, 1, "MVP") },
        { UnitOperationType.Valve, new("Valve", "Reduces pressure (isenthalpic)", 1, 1, 1, 1, "MVP") },
        { UnitOperationType.Pump, new("Pump", "Increases liquid pressure", 1, 1, 1, 1, "MVP") },
        { UnitOperationType.Compressor, new("Compressor", "Increases gas pressure", 1, 1, 1, 1, "MVP") },
        { UnitOperationType.Expander, new("Expander", "Reduces gas pressure with work recovery", 1, 1, 1, 1, "MVP") },
        { UnitOperationType.Heater, new("Heater", "Adds heat to a stream", 1, 1, 1, 1, "MVP") },
        { UnitOperationType.Cooler, new("Cooler", "Removes heat from a stream", 1, 1, 1, 1, "MVP") },
        { UnitOperationType.HeatExchanger, new("Heat Exchanger", "Transfers heat between two streams", 2, 2, 2, 2, "MVP") },
        { UnitOperationType.ReactorConversion, new("Conversion Reactor", "Reaction with specified conversion", 1, 5, 1, 5, "Phase2") },
        { UnitOperationType.ReactorEquilibrium, new("Equilibrium Reactor", "Reaction at chemical equilibrium", 1, 5, 1, 5, "Phase2") },
        { UnitOperationType.ReactorGibbs, new("Gibbs Reactor", "Minimizes Gibbs energy", 1, 5, 1, 5, "Phase2") },
        { UnitOperationType.ReactorCSTR, new("CSTR", "Continuous stirred tank reactor", 1, 5, 1, 5, "Phase2") },
        { UnitOperationType.ReactorPFR, new("PFR", "Plug flow reactor", 1, 5, 1, 5, "Phase2") },
        { UnitOperationType.DistillationColumn, new("Distillation Column", "Multi-stage separation", 1, 3, 2, 10, "Phase2") },
        { UnitOperationType.AbsorptionColumn, new("Absorption Column", "Gas absorption into liquid", 2, 2, 2, 2, "Phase2") },
        { UnitOperationType.ComponentSeparator, new("Component Separator", "Separates specific components", 1, 1, 2, 10, "Phase2") },
        { UnitOperationType.OrificePlate, new("Orifice Plate", "Flow measurement device", 1, 1, 1, 1, "Phase2") },
        { UnitOperationType.Recycle, new("Recycle", "Handles recycle loops", 1, 1, 1, 1, "Phase2") },
        { UnitOperationType.Adjust, new("Adjust", "Controller to match a target", 0, 0, 0, 0, "Phase2") },
        { UnitOperationType.Spec, new("Spec", "Sets a specification constraint", 0, 0, 0, 0, "Phase2") }
    };

    public IEnumerable<CompoundInfo> GetCompounds(string? searchTerm = null)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return _compounds;

        var term = searchTerm.ToLowerInvariant();
        return _compounds.Where(c =>
            c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            c.Formula.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            c.CasNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            c.Category.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<PropertyPackageInfo> GetPropertyPackages()
    {
        return Enum.GetValues<PropertyPackage>()
            .Select(pp => new PropertyPackageInfo(
                pp.ToString(),
                _propertyPackageDescriptions.GetValueOrDefault(pp, "No description available")));
    }

    public IEnumerable<FlashAlgorithmInfo> GetFlashAlgorithms()
    {
        return Enum.GetValues<FlashAlgorithm>()
            .Select(fa => new FlashAlgorithmInfo(
                fa.ToString(),
                _flashAlgorithmDescriptions.GetValueOrDefault(fa, "No description available")));
    }

    public IEnumerable<UnitOperationInfo> GetUnitOperations()
    {
        return Enum.GetValues<UnitOperationType>()
            .Select(ut =>
            {
                var meta = _unitOpMetadata.GetValueOrDefault(ut, new(ut.ToString(), "No description", 1, 1, 1, 1, "Unknown"));
                return new UnitOperationInfo(
                    ut.ToString(),
                    meta.DisplayName,
                    meta.Description,
                    meta.MinInlets,
                    meta.MaxInlets,
                    meta.MinOutlets,
                    meta.MaxOutlets,
                    meta.Phase);
            });
    }
}

// DTOs for catalog responses
public record CompoundInfo(string Name, string Formula, string CasNumber, string Category, string Description);
public record PropertyPackageInfo(string Name, string Description);
public record FlashAlgorithmInfo(string Name, string Description);
public record UnitOperationInfo(string Type, string DisplayName, string Description, int MinInlets, int MaxInlets, int MinOutlets, int MaxOutlets, string Phase);
public record UnitOperationMetadata(string DisplayName, string Description, int MinInlets, int MaxInlets, int MinOutlets, int MaxOutlets, string Phase);
