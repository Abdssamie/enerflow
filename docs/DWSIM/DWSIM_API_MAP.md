# DWSIM API Reference

**Version:** DWSIM Source (Live Scan from libs/dwsim_src)  
**Last Updated:** 2026-01-17  
**Purpose:** Authoritative API reference for DWSIM Automation

---

## 1. Automation Entry Point

### Automation3 Class
**Namespace:** `DWSIM.Automation`  
**File:** `libs/dwsim_src/DWSIM.Automation/Automation.cs`

**Key Methods:**

```csharp
// Flowsheet Creation & Loading
IFlowsheet CreateFlowsheet()
IFlowsheet LoadFlowsheet(string filepath)
IFlowsheet LoadFlowsheet(string filepath, Action<string, int> UIUpdateHandler)

// Calculation Methods
void CalculateFlowsheet(IFlowsheet flowsheet, ISimulationObject sender = null)
List<Exception> CalculateFlowsheet2(IFlowsheet flowsheet)
List<Exception> CalculateFlowsheet3(IFlowsheet flowsheet, int timeout_seconds)
List<Exception> CalculateFlowsheet4(IFlowsheet flowsheet)  // Recommended

// Saving
void SaveFlowsheet(IFlowsheet flowsheet, string filepath, bool compressed)
void SaveFlowsheet2(IFlowsheet flowsheet, string filepath)

// Resource Management
void ReleaseResources()
```

**Key Properties:**

```csharp
Dictionary<string, IPropertyPackage> AvailablePropertyPackages { get; }
Dictionary<string, ICompoundConstantProperties> AvailableCompounds { get; }
```

**Available Property Packages:**
- Peng-Robinson (PR), PR 1978, PRSV2-M, PRSV2-VL
- SRK, SRK Advanced, PR78 Advanced
- UNIFAC, UNIFAC-LL, Modified UNIFAC (Dortmund), Modified UNIFAC (NIST)
- Wilson, NRTL, UNIQUAC
- Chao-Seader, Grayson-Streed
- Raoult's Law
- Lee-Kesler-Plocker
- CoolProp, CoolProp (Incompressible Fluids/Mixtures)
- Steam Tables (IAPWS-IF97), Seawater IAPWS-08
- Ideal Electrolyte
- Black Oil
- GERG-2008
- PC-SAFT
- CAPE-OPEN

---

## 2. Core Interfaces

### IFlowsheet
**File:** `libs/dwsim_src/DWSIM.Interfaces/IFlowsheet.vb`

**Key Properties:**

```csharp
Dictionary<string, ISimulationObject> SimulationObjects { get; }
Dictionary<string, IGraphicObject> GraphicObjects { get; }
Dictionary<string, ICompoundConstantProperties> SelectedCompounds { get; }
Dictionary<string, IPropertyPackage> PropertyPackages { get; }
IFlowsheetOptions FlowsheetOptions { get; }
bool Solved { get; }
string ErrorMessage { get; }
```

**Key Methods:**

```csharp
// Object Management
ISimulationObject AddObject(ObjectType type, int x, int y, string tag)
ISimulationObject AddObject(ObjectType type, int x, int y, string id, string tag)
ISimulationObject GetObject(string name)
ISimulationObject GetFlowsheetSimulationObject(string tag)

// Compound Management
void AddCompound(string comp
// Property Package Management
void AddPropertyPackage(IPropertyPackage package)

// Connection Management
void ConnectObjects(IGraphicObject from, IGraphicObject to, int fromIdx, int toIdx)
void DisconnectObjects(IGraphicObject from, IGraphicObject to)

// Calculation
void RequestCalculation(ISimulationObject sender = null)
void RequestCalculation2(bool wait)
List<Exception> RequestCalculationAndWait()

// Cleanup
void ReleaseResources()
```

---

### ISimulationObject
**File:** `libs/dwsim_src/DWSIM.Interfaces/ISimulationObject.vb`

Base interface for all flowsheet objects (streams, unit operations, etc.)

**Key Properties:**

```csharp
string Name { get; set; }
IGraphicObject GraphicObject { get; set; }
string ErrorMessage { get; set; }
bool Calculated { get; set; }
IPropertyPackage PropertyPackage { get; set; }
bool DebugMode { get; set; }
string DebugText { get; set; }
bool IsSource { get; }
bool IsSink { get; }
```

**Key Methods:**

```csharp
// Property Access
string[] GetProperties(PropertyType proptype)
object GetPropertyValue(string prop, IUnitsOfMeasure su)
bool SetPropertyValue(string prop, object propval, IUnitsOfMeasure su)

// Calculation
void Calculate(object args)
void Solve()
void Validate()

// Reporting
string GetReport(IUnitsOfMeasure su, CultureInfo ci, string numberformat)
List<Tuple<...>> GetStructuredReport()

// Connection Management
void ConnectFeedMaterialStream(ISimulationObject stream, int portnumber)
void ConnectProductMaterialStream(ISimulationObject stream, int portnumber)
void ConnectFeedEnergyStream(ISimulationObject stream, int portnumber)
void ConnectProductEnergyStream(ISimulationObject stream, int portnumber)
void ConnectEnergyStream(ISimulationObject stream)

// Port Information
List<string> GetConnectionPortsList()
List<IConnectionPortInfo> GetConnectionPortsInfo()
```

---

### IMaterialStream
**File:** `libs/dwsim_src/DWSIM.Interfaces/IMaterialStream.vb`

**Key Properties:**

```csharp
StreamSpec SpecType { get; set; }
FlowSpec DefinedFlow { get; set; }
CompositionBasis CompositionBasis { get; set; }
Dictionary<int, IPhase> Phases { get; }  // ReadOnly
Dictionary<string, double> InputComposition { get; set; }
ForcedPhase ForcePhase { get; set; }
bool AtEquilibrium { get; set; }
```

**Getter Methods:**

```csharp
double GetTemperature()      // K
double GetPressure()         // Pa
double GetMassFlow()         // kg/s
double GetMolarFlow()        // mol/s
double GetVolumetricFlow()   // m³/s
double GetMassEnthalpy()     // kJ/kg
double GetEnergyFlow()       // kW
double GetCompoundMassFlow(string name)
double GetCompoundMolarFlow(string name)
double GetCompoundMassConcentration(string name)
```

**Setter Methods:**

```csharp
void SetTemperature(double value)      // K
void SetPressure(double value)         // Pa
void SetMassFlow(double value)         // kg/s
void SetMolarFlow(double value)        // mol/s
void SetVolumetricFlow(double value)   // m³/s
void SetMassEnthalpy(double value)     // kJ/kg
```

**Composition Methods:**

```csharp
void SetOverallComposition(Array Vx)
void SetOverallMolarComposition(double[] Vx)
void SetOverallMassComposition(double[] Vx)
void SetPhaseComposition(double[] Vx, Phase phase)
double[] GetOverallComposition()
double[] GetOverallMassComposition()
double[] GetPhaseComposition(Phase phase)
```

**Phase Indices:**
- 0 = Mixture (overall)
- 1 = OverallLiquid
- 2 = Vapor
- 3 = Liquid1
- 4 = Liquid2
- 5 = Liquid3
- 6 = Aqueous
- 7 = Solid

---

### IEnergyStream
**File:** `libs/dwsim_src/DWSIM.Interfaces/IEnergyStream.vb`

```csharp
double GetEnergyFlow()           // kW
void SetEnergyFlow(double value) // kW
```

---

### IPhase
**File:** `libs/dwsim_src/DWSIM.Interfaces/IPhase.vb`

```csharp
string Name { get; set; }
Dictionary<string, ICompound> Compounds { get; }
IPhaseProperties Properties { get; }  // ReadOnly
```

---

### ICompound
**File:** `libs/dwsim_src/DWSIM.Interfaces/ICompound.vb`

```csharp
string Name { get; set; }
double? MoleFraction { get; set; }
double? MassFraction { get; set; }
double? MolarFlow { get; set; }
double? MassFlow { get; set; }
double? VolumetricFlow { get; set; }
double? VolumetricFraction { get; set; }
double Kvalue { get; set; }
double? FugacityCoeff { get; set; }
double? ActivityCoeff { get; set; }
double? PartialVolume { get; set; }
double? PartialPressure { get; set; }
```

---

### IPhaseProperties
**File:/DWSIM/IPhaseProperties.cs` (reference file)

**All properties are `double?` and lowercase (VB.NET origin):**

```csharp
double? temperature           // K
double? pressure              // Pa
double? density               // kg/m³
double? enthalpy              // kJ/kg
double? entropy               // kJ/kg·K
double? molarflow             // mol/s
double? massflow              // kg/s
double? molarfraction         // 0-1 (vapor fraction for mixture)
double? massfraction          // 0-1
double? volumetric_flow       // m³/s
double? molecularWeight       // kg/kmol
double? heatCapacityCp        // kJ/kg·K
double? heatCapacityCv        //·K
double? viscosity             // Pa·s
double? thermalConductivity   // W/m·K
double? speedOfSound          // m/s
double? compressibilityFactor // Z
double? surfaceTension        // N/m
double? kinematic_viscosity   // m²/s
double? internal_energy       // kJ/kg
double? gibbs_free_energy     // kJ/kg
double? helmholtz_energy      // kJ/kg
```

---

### IGraphicObject
**File:** `libs/dwsim_src/DWSIM.Interfaces/IGraphicObject.vb`

```csharp
List<IConnectionPoint> InputConnectors { get; set; }
List<IConnectionPoint> OutputConnectors { get; set; }
IConnectionPoint EnergyConnector { get; set; }
ObjectType Obje; set; }
string Tag { get; set; }        // Display name
string Name { get; set; }       // Unique ID
ISimulationObject Owner { get; set; }
bool Calculated { get; set; }
```

---

### IConnectionPoint
**File:** `libs/dwsim_src/DWSIM.Interfaces/IGraphicObject.vb`

```csharp
bool IsAttached { get; set; }
string ConnectorName { get; set; }
ConType Type { get; set; }      // ConIn, ConOut, ConEn
ConDir Direction { get; set; }
bool Active { get; set; }
```

---

### IConnectionPortInfo
**File:** `libs/dwsim_src/DWSIM.Interfaces/IConnectionPortInfo.vb`

```csharp
string Name { get; set; }
int Index { get; set; }
bool IsConnected { get; set; }
bool IsInput { get; set; }
bool IsOutput { get; set; }
bool IsEnergyPort { get; set; }
ISimulationObject ConnectedObject { get; set; }
```

---

### IUnitOperation
**File:** `libs/dwsim_src/DWSIM.Interfaces/IUnitOperation.vb`

Minimal interface for unit operations:

```csharp
List<IDimension> Dimensions { get; set; }
string SelectedEquipmentType { get; set; }
List<string> EquipmentTypes { get; }  // ReadOnly
```

---

### IFlowsheetOptions
**File:** `libs/dwsim_src/DWSIM.Interfaces/IFlowsheetOptions.vb`

```csharp
IUnitsOfMeasure SelectedUnitSystem { get; set; }
string SimulationName { get; set; }
List<string> CustomCalculationOrder { get; set; }
bool ForceObjectSolving { get; set; }
bool SkipEquilibriumCalculationOnDefinedStreams { get; set; }
```

---

## 3. Key Enums

### ObjectType
**Namespace:** `DWSIM.Interfaces.Enums.GraphicObjects`  
**File:** `libs/dwsim_src/DWSIM.Interfaces/Enums.vb`

```csharp
// Streams
MaterialStream
EnergyStream

// Mixers/Splitters
NodeIn          // Mixer
NodeOut         // Splitter

// Pressure Changers
Pump
Compressor
Expander
Valve

// Heat Transfer
Heater
Cooler
HeatExchanger
Pipe

// Separators
Tank
Vessel          // Flash drum / Separator
ComponentSeparator
SolidSeparator
Filter

// Columns
ShortcutColumn
DistillationColumn
AbsorptionColumn

// Reactors
RCT_Conversion
RCT_Equilibrium
RCT_Gibbs
RCT_CSTR
RCT_PFR

// Special Operations
OT_Recycle
OT_EnergyRecycle
OT_Adjust
OT_Spec
OrificePlate

// Controllers
Controller_PID

// Custom
CustomUO
ExcelUO
FlowsheetUO
```

---

### StreamSpec
**File:** `libs/dwsim_src/DWSIM.Interfaces/Enums.vb`

```csharp
Temperature_and_Pressure = 0      // Default
Pressure_and_Enthalpy = 1
Pressure_and_Entropy = 2
Pressure_and_VaporFraction = 3
Temperature_and_VaporFraction = 4
Pressure_and_SolidFraction = 5
Volume_and_Temperature = 6
Volume_and_Enthalpy = 7
Volume_and_Entropy = 8
```

---

### FlowSpec
**File:** `libs/dwsim_src/DWSIM.Interfaces/Enums.vb`

```csharp
Mass = 0        // Default (kg/s)
Mole = 1        // mol/s
Volumetric = 2  // m³/s
```

---

### CompositionBasis
**File:** `libs/dwsim_src/DWSIM.Interfaces/Enums.vb`

```csharp
Molar_Fractions
Mass_Fractions
Volumetric_Fractions
Molar_Flows
Mass_Flows
Volumetric_Flows
DefaultBasis
```

---

## 4. Unit Operation CalcMode Enums

### Heater.CalculationMode
**e:** `libs/dwsim_src/DWSIM.UnitOperations/UnitOperations/Heater.vb`

```csharp
HeatAdded = 0
OutletTemperature = 1
EnergyStream = 2
OutletVaporFraction = 3
TemperatureChange = 4
```

**Properties:**
- `CalcMode` - **Set FIRST**
- `OutletTemperature` (K)
- `HeatDuty` / `DeltaQ` (kW)
- `TemperatureChange` / `DeltaT` (K)
- `PressureDrop` / `DeltaP` (Pa)
- `Efficiency` / `Eficiencia` (0-100%)

---

### Cooler.CalculationMode
**File:** `libs/dwsim_src/DWSIM.UnitOperations/UnitOperations/Cooler.vb`

```csharp
HeatRemoved = 0
OutletTemperature = 1
OutletVaporFraction = 2
TemperatureChange = 3
EnergyStream = 4
`erties:** Same as Heater

---

### Valve.CalculationMode
**File:** `libs/dwsim_src/DWSIM.UnitOperations/UnitOperations/Valve.vb`

```csharp
DeltaP = 0
OutletPressure = 1
Kv_Liquid = 2
Kv_Gas = 3
Kv_Steam = 4
Kv_General = 5
```

**Properties:**
- `CalcMode` - **Set FIRST**
- `OutletPressure` / `m_Pout` (Pa)
- `DeltaP` (Pa)
- `Kv` (valve coefficient)
- `OpeningPct` (0-100%)

---

### Compressor.CalculationMode
**File:** `libs/dwsim_src/DWSIM.UnitOperations/UnitOperations/Compressor.vb`

```csharp
OutletPressure = 0
Delta_P = 1
EnergyStream = 2
PowerRequired = 3
Head = 4
Curves = 5
PressureRatio = 6
```

**Properties:**
- `CalcMode` - **Set FIRST**
- `POut` / `OutletPressure` (Pa)
- `DeltaP` (Pa)
- `DeltaQ` / `Power` (kW)
- `AdiabaticEfficiency` (0-1)
- `PolytropicEfficiency` (0-1)
- `ProcessPath` (Adiabatic/Polytropic)

---

### Expander.CalculationMode
**File:** `libs/dwsim_src/DWSIM.UnitOperations/UnitOperations/Expander.vb`

```csharp
OutletPressure = 0
Delta_P = 1
PowerGenerated = 2
Head = 3
Curves = 4
PressureRatio = 5
```

**Properties:** Same structure as Compressor

---

### HeatExchanger.HeatExchangerCalcMode
**File:** `libs/dwsim_src/DWSIM.UnitOperations/UnitOperations/HeatExchanger.vb`

```csharp
CalcTempHotOut = 0
CalcTempColdOut = 1
CalcBothTemp = 2
CalcBothTemp_UA = 3
CalcArea = 4
ShellandTube_Rating = 5
ShellandTube_CalcFoulingFactor = 6
PinchPoint = 7
ThermalEfficiency = 8
OutletVaporFraction1 = 9
OutletVaporFraction2 = 10
```

**Properties:**
- `CalculationMode` - **Set FIRST**
- `OverallCoefficient` (W/m²·K)
- `Area` (m²)
- `TempHotOut` / `TempColdOut` (K)
- `HotSidePressureDrop` / `ColdSidePressureDrop` (Pa)
- `FlowDir` (CounterCurrent/CoCurrent)
- `Q` (kW)
- `LMTD_F`
- `ThermalEfficiency` (0-1)

---

### Mixer.PressureBehavior
**File:** `libs/dwsim_src/DWSIM.UnitOperations/UnitOperations/Mixer.vb`

```csharp
ximum
Minimum
```

---

### Splitter.OpMode
**File:** `libs/dwsim_src/DWSIM.UnitOperations/UnitOperations/Splitter.vb`

```csharp
SplitRatios
StreamMassFlowSpec
StreamMolarFlowSpec
```

**Properties:**
- `OperationMode`
- `Ratios` (List<double>) - **Set AFTER connections**

---

### Vessel.CalculationModes
**File:** `libs/dwsim_src/DWSIM.UnitOperations/UnitOperations/Vessel.vb`

```csharp
Legacy      // Specify T and P
Adiabatic   // Adiabatic flash (H and P)
```

**Properties:**
- `CalculationMode`
- `OverrideP` / `OverrideT` (bool)
- `FlashPressure` (Pa)
- `FlashTemperature` (K)

---

## 5. Critical Usage Patterns

### Creating a Flowsheet

```csharp
var automation = new Automation3();
var flowsheet = automation.CreateFlowsheet();
```

---

### Setting Unit System

```csharp
using DWSIM.SharedClasses.SystemsOfUnits;

flowsheet.FlowsheetOptions.SelectedUnitSystem = new SI();
// or new CGS(), new English()
```

---

### Adding Compounds

```csharp
flowsheet.AddCompound("Water");
flowsheet.AddCompound("Methane");
flowsheet.AddCompound("Ethane");
```

---

### Creating Material Streams

```csharp
using DWSIM.Thermodynamics.Streams;

// Create stream via AddObject
var stream = flowsheet.AddObject(ObjectType.MaterialStream, 0, 0, "Stream1") 
    as MaterialStream;

// DO NOT call AddCompoundsToMaterialStream() - causes duplicate key exception
// Compounds are added automatically by AddObject()

// Configure stream properties
stream.Phases[0].Properties.temperature = 298.15;  // K
stream.Phases[0].Properties.pressure = 101325;      // Pa
stream.Phases[0].Properties.massflow = 1.0;         // kg/s

// Set composition
stream.Phases[0].Compounds["Water"].MoleFraction = 0.5;
stream.Phases[0].Compounds["Methane"].MoleFraction = 0.5;

// Set spec type (default is Temperature_and_Pressure)
stream.SpecType = StreamSpec.Temperature_and_Pressure;
```

---

### Creating Unit Operations

```csharp
using DWSIM.UnitOperations.UnitOperations;

var heater = flowsheet.AddObject(ObjectType.Heater, 0, 0, "Heater1") as Heater;

// CRITICAL: Set CalcMode FIRST
heater.CalcMode = Heater.CalculationMode.OutletTemperature;
heater.OutletTemperature = 350.0;  // K
heater.Efficiency = 95.0;          // %
heater.PressureDrop = 0.0;         // Pa
```

---

### Connecting Objects

```csharp
// Stream Output (port 0) to Unit Input (port i)
flowsheet.ConnectObjects(
    streamObj.GraphicObject, 
    unitObj.GraphicObject, 
    0,  // stream output port
    i   // unit input port
);

// Unit Output (port i) to Stream Input (port 0)
flowsheet.ConnectObjects(
    unitObj.GraphicObject, 
    streamObj.GraphicObject, 
    i,  // unit output port
    0   // stream input port
);
```

**See `CONNECTION_PORTS.md` for port index reference.**

---

### Solving

```csharp
// Recommended: Returns exceptions
var errors = flowsheet.RequestCalculationAndWait();

// Check convergence
if (!flowsheet.Solved)
{
    Console.WriteLine("Error: " + flowsheet.ErrorMessage);
}

// Check individual objects
foreach (v flowsheet.SimulationObjects.Values)
{
    if (!string.IsNullOrEmpty(obj.ErrorMessage))
    {
        Console.WriteLine(obj.Name + ": " + obj.ErrorMessage);
    }
}
```

---

### Collecting Results

```csharp
// Material Stream Results
var stream = flowsheet.GetObject("Stream1") as MaterialStream;
double T = stream.Phases[0].Properties.temperature;    // K
double P = stream.Phases[0].Properties.pressure;       // Pa
double W = stream.Phases[0].Properties.massflow;       // kg/s
double VF = stream.Phases[0].Properties.molarfraction;action

// Compound Results
double moleFrac = stream.Phases[0].Compounds["Water"].MoleFraction;

// Unit Operation Results
var heater = flowsheet.GetObject("Heater1") as Heater;
double duty = heater.DeltaQ;  // kW
bool calculated = heater.Calculated;
string error = heater.ErrorMessage;
```

---

### Cleanup

```csharp
automation.ReleaseResources();
```

---

## 6. Common Pitfalls

### DO NOT call AddCompoundsToMaterialStream()
This causes duplicate key exceptions. Compounds are added automatically by `AddObject()`.

### DO NOT forget to set CalcMode first
Always set `CalcMode` before setting mode-specific properties.

```csharp
// WRONG
heater.OutletTemperature = 350.0;
heater.CalcMode = Heater.CalculationMode.OutletTemperature;

// CORRECT
heater.CalcMode = Heater.CalculationMode.OutletTemperature;
heater.OutletTemperature = 350.0;
```

### DO NOT use CalculateFlowsheet2() for error handling
Use `RequestCalculationAndWait()` which returns exceptions.

### DO NOT assume calculation succeeded
Always check `flowsheet.Solved` and `obj.ErrorMessage`.

### DO NOT forget unit conversions
DWSIM uses SI units internally:
- Temperature: K (not °C)
- Pressure: Pa (not bar or psi)
- Mass flow: kg/s
- Energy: kW
- Efficiency: 0-100% (not 0-1)

### DO NOT set Splitter ratios before connections
Splitter ratios must be set AFTER connections are made.

---

## 7. Source File Locations

For verification via grep:

- **Automation:** `libs/dwsim_src/DWSIM.Automation/Automation.cs`
- **FlowsheetBase:** `libs/dwsim_src/DWSIM.FlowsheetBase/FlowsheetBase.vb`
- **Interfaces:** `libs/dwsim_src/DWSIM.Interfaces/*.vb`
- **Enums:** `libs/dwsim_src/DWSIM.Interfaces/Enums.vb`
- **Unit Operations:** `libs/dwsim_src/DWSIM.UnitOperations/UnitOperations/*.vb`
- **Material Stream:** `libs/dwsim_src/DWSIM.Thermodynamics/MaterialStream/MaterialStream.vb`
- **Energy Stream:** `libs/dwsim_src/DWSIM.UnitOperations/Streams/Evb`
- **Graphic Objects:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/*.vb`

---

## 8. Related Documentation

- **Connection Port Indices:** See `CONNECTION_PORTS.md`
- **Phase Properties Reference:** See `IPhaseProperties.cs`
