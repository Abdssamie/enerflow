# DWSIM API Context Map

**Version:** DWSIM Source (Live Scan)
**Generated:** 2026-01-17

This map provides the *authoritative* API surface for `DWSIM.Automation`, `DWSIM.Interfaces`, and key Enums, derived directly from the `libs/dwsim_src` source code.

## 1. Automation Entry Point
**Namespace:** `DWSIM.Automation`
**Class:** `Automation3` (Recommended over `Automation` or `Automation2`)
**File:** `libs/dwsim_src/DWSIM.Automation/Automation.cs`

### Key Methods
```csharp
// Loading & Creating
public IFlowsheet CreateFlowsheet()
public IFlowsheet LoadFlowsheet2(string filepath) // Returns IFlowsheet directly

// Calculation
public void CalculateFlowsheet2(IFlowsheet flowsheet) // Void, standard calculation
public List<Exception> CalculateFlowsheet4(IFlowsheet flowsheet) // Returns errors, recommended

// Saving
public void SaveFlowsheet2(IFlowsheet flowsheet, string filepath)

// Management
public Dictionary<String, IPropertyPackage> AvailablePropertyPackages { get; }
public Dictionary<String, ICompoundConstantProperties> AvailableCompounds { get; }
public void ReleaseResources()
```

## 2. Core Interfaces
**Namespace:** `DWSIM.Interfaces`

### `IFlowsheet`
**File:** `libs/dwsim_src/DWSIM.Interfaces/IFlowsheet.vb`

**Key Properties:**
*   `SimulationObjects`: `Dictionary<String, ISimulationObject>`
*   `AvailablePropertyPackages`: `Dictionary<String, IPropertyPackage>`
*   `SelectedCompounds`: `Dictionary<String, ICompoundConstantProperties>`
*   `MasterUnitOp`: `ISimulationObject` (The flowsheet itself as a unit op)

**Key Methods:**
*   `AddObject(ObjectType t, int x, int y, string tag)`: Adds a new object.
*   `GetObject(string name)`: Retrieves an object by tag.
*   `RequestCalculation(ISimulationObject sender = null)`: Triggers async calculation (use Automation methods for sync wait).
*   `AddCompoundsToMaterialStream(IMaterialStream stream)`: Essential when creating new streams.
*   `ConnectObjects(IGraphicObject from, IGraphicObject to, int fromIdx, int toIdx)`: Connects ports.

### `IMaterialStream`
**File:** `libs/dwsim_src/DWSIM.Interfaces/IMaterialStream.vb`

**Key Properties:**
*   `SpecType`: `StreamSpec` (e.g., Temperature_Pressure, Pressure_Enthalpy)
*   `InputComposition`: `Dictionary<string, double>`
*   `Phases`: `Dictionary<int, IPhase>`
*   `GetPhase(string phaseName)`: (Method) Retrieves phase properties.

## 3. Essential Enums
**Namespace:** `DWSIM.Interfaces.Enums.GraphicObjects`
**File:** `libs/dwsim_src/DWSIM.Interfaces/Enums.vb`

### `ObjectType`
Used in `flowsheet.AddObject(ObjectType, ...)`

*   `MaterialStream`
*   `EnergyStream`
*   `Valve`
*   `Pump`
*   `Compressor`
*   `Heater`
*   `Cooler`
*   `Pipe`
*   `Reactor_CSTR` (Check exact spelling in source if needed)
*   `DestillationColumn` (Check exact spelling)

## 4. Usage Patterns & Pitfalls

### Creating a Stream
```csharp
// 1. Create the object
var stream = flowsheet.AddObject(ObjectType.MaterialStream, 100, 100, "Stream1") as IMaterialStream;
// 2. Initialize compounds (CRITICAL STEP)
flowsheet.AddCompoundsToMaterialStream(stream);
// 3. Set properties
stream.SpecType = StreamSpec.Temperature_Pressure;
```

### Running a Calculation
**DO NOT** use `flowsheet.CalculateFlowsheet2()` directly if you want error handling or timeout control.
**DO** use `automation.CalculateFlowsheet4(flowsheet)`.

## 5. File System Locations (For `grep` verification)
*   `Automation.cs`: `libs/dwsim_src/DWSIM.Automation/Automation.cs`
*   `IFlowsheet.vb`: `libs/dwsim_src/DWSIM.Interfaces/IFlowsheet.vb`
*   `Enums.vb`: `libs/dwsim_src/DWSIM.Interfaces/Enums.vb`
