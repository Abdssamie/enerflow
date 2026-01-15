namespace Enerflow.Domain.Enums;

public enum PropertyPackageType
{
    // Equations of State
    PengRobinson,
    SoaveRedlichKwong,
    PRSV2,
    PengRobinsonStryjekVera2,
    
    // Activity Coefficient Models
    NRTL,
    UNIQUAC,
    UNIFAC,
    ModifiedUNIFAC,
    ChaoSeader,
    GraysonStreed,
    
    // Special
    SteamTables,
    CoolProp,
    RaoultLaw,
    IAPWS97
}

public enum FlashAlgorithmType
{
    NestedLoops,
    InsideOut,
    GibbsMinimization,
    Simultaneous
}

public enum CompoundType
{
    Pure,
    Pseudocomponent,
    Hypothetical
}
