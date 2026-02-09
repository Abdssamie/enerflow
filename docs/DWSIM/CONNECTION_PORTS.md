# DWSIM Connection Port Indices

**Purpose:** Port index reference for `flowsheet.ConnectObjects()` calls  
**Last Updated:** 2026-01-17

This document provides the authoritative port index mapping for connecting objects in DWSIM flowsheets.

---

## Connection Syntax

```csharp
flowsheet.ConnectObjects(
    fromObject.GraphicObject,
    toObject.GraphicObject,
    fromPortIndex,  // Output port on source object
    toPortIndex     // Input port on destination object
);
```

---

## Port Index Reference

### MaterialStream

**Input Ports:**
- Index 0: "Input" - From upstream unit/stream

**Output Ports:**
- Index 0: "Output" - To downstream unit/stream

**Energy Port:** Inactive (not used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/MaterialStream.vb`

---

### EnergyStream

**Input Ports:**
- Index 0: "Input" - Energy input

**Output Ports:**
- Index 0: "Output" - Energy output

**Energy Port:** Present but inactive (not typically used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/EnergyStream.vb`

---

### Mixer (ObjectType.NodeIn)

**Input Ports:**
- Index 0: "Inlet Stream 1" - First inlet
- Index 1: "Inlet Stream 2" - Second inlet
- Index 2: "Inlet Stream 3" - Third inlet
- Index 3: "Inlet Stream 4" - Fourth inlet
- Index 4: "Inlet Stream 5" - Fifth inlet
- Index 5: "Inlet Stream 6" - Sixth inlet

**Output Ports:**
- Index 0: "Mixed Stream" - Mixed outlet

**Energy Port:** Inactive (not used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/Mixer.vb`

---

### Splitter (ObjectType.NodeOut)

**Input Ports:**
- Index 0: "Inlet" - Single inlet

**Output Ports:**
- Index 0: "Outlet 1" - First outlet
- Index 1: "Outlet 2" - Second outlet
- Index 2: "Outlet 3" - Third outlet

**Energy Port:** Inactive (not used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/Splitter.vb`

---

### Heater

**Input Ports:**
- Index 0: "Inlet" - Material stream inlet
- Index 1: "Energy Stream (Primary)" - Energy stream input

**Output Ports:**
- Index 0: "Outlet" - Material stream outlet

**Energy Port:** Active - "Energy Stream (Secondary)" - Alternative energy connection

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/Heater.vb`

---

### Cooler

**Input Ports:**
- Index 0: "Inlet" - Material stream inlet
- Index 1: "Energy Stream (Secondary)" - Energy stream input (top)

**Output Ports:**
- Index 0: "Outlet" - Material stream outlet

**Energy Port:** Active - "Energy Stream (Primary)" - Energy connection (bottom)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/Cooler.vb`

---

### Valve

**Input Ports:**
- Index 0: "Inlet" - Inlet

**Output Ports:**
- Index 0: "Outlet" - Outlet

**Energy Port:** Inactive (not used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/Valve.vb`

---

### Compressor

**Input Ports:**
- Index 0: "Inlet" - Material stream inlet
- Index 1: "Energy Stream" - Energy stream input

**Output Ports:**
- Index 0: "Outlet" - Material stream outlet

**Energy Port:** Inactive (not used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/Compressor.vb`

---

### Expander (TurbineGraphic)

**Input Ports:**
- Index 0: "Inlet" - Material stream inlet

**Output Ports:**
- Index 0: "Outlet" - Material stream outlet

**Energy Port:** Active - "Energy Stream" - Energy stream output

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/Expander.vb`

---

### Pump

**Input Ports:**
- Index 0: "Inlet" - Material stream inlet
- Index 1: "Energy Stream" - Energy stream input

**Output Ports:**
- Index 0: "Outlet" - Material stream outlet

**Energy Port:** Inactive (not used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/Pump.vb`

---

### HeatExchanger

**Input Ports:**
- Index 0: "Inlet Stream 1" - Hot side inlet
- Index 1: "Inlet Stream 2" - Cold side inlet

**Output Ports:**
- Index 0: "Outlet Stream 1" - Hot side outlet
- Index 1: "Outlet Stream 2" - Cold side outlet

**Energy Port:** Inactive (not used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/HeatExchanger.vb`

---

### Vessel (FlashDrum, Separator)

**Input Ports:**
- Index 0: "Inlet Stream #0" - First feed
- Index 1: "Inlet Stream #1" - Second feed
- Index 2: "Inlet Stream #2" - Third feed
- Index 3: "Inlet Stream #3" - Fourth feed
- Index 4: "Inlet Stream #4" - Fifth feed
- Index 5: "Inlet Stream #5" - Sixth feed
- Index 6: "Energy Stream" - Energy input

**Output Ports:**
- Index 0: "Vapor Outlet" - Vapor product
- Index 1: "Light Liquid Outlet" - Light liquid product
- Index 2: "Heavy Liquid OHeavy liquid product
- Index 3: "Relief Valve Outlet" - Relief outlet

**Energy Port:** Inactive (not used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/SeparatorVessel.vb`

---

### Pipe Segment

**Input Ports:**
- Index 0: "Inlet" - Inlet

**Output Ports:**
- Index 0: "Outlet" - Outlet

**Energy Port:** Active - "Energy Stream" - Heat loss/gain

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/PipeSegment.vb`

---

### Conversion Reactor (also Equilibrium, Gibbs)

**Input Ports:**
- Index 0: "Inlet" - Material stream inlet
- Index 1: "Energy Stream" - Legacy, not in use

**Output Podex 0: "Vapor Outlet" - Vapor product
- Index 1: "Liquid Outlet" - Liquid product
- Index 2: "Energy Stream" - Energy output

**Energy Port:** Inactive (not used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/ConversionReactor.vb`

---

### Recycle (ObjectType.OT_Recycle)

**Input Ports:**
- Index 0: "Inlet" - Recycle inlet

**Output Ports:**
- Index 0: "Outlet" - Recycle outlet

**Energy Port:** Inactive (not used)

**Source:** `libs/dwsim_src/DWSIM.Drawing.SkiaSharp/GraphicObjects/Shapes/Recycle.vb`

---

## Common Connection Patterns

### Stream to Unit Operation

```csharp
// Material stream output to unit input port 0
flowsheet.ConnectObjects(
    materialStream.GraphicObject,
    heater.GraphicObject,
    0,  // stream output
    0   // heater inlet
);
```

### Unit Operation to Stream

```csharp
// Unit output port 0 to material stream input
flowsheet.ConnectObjects(
    heater.GraphicObject,
    materialStream.GraphicObject,
    0,  // heater outlet
    0   // stream input
);
```

### Energy Stream to Unit Operation

```csharp
// Energy stream to heater energy input port 1
flowsheet.ConnectObjects(
    energyStream.GraphicObject,
    heater.GraphicObject,
    0,  // energy stream output
    1   // heater energy input
);
```

### Mixer with Multiple Inlets

```csharp
// Connect three streams to mixer
flowsheet.ConnectObjects(stream1.GraphicObject, mixer.GraphicObject, 0, 0);
flowsheet.ConnectObjects(stream2.GraphicObject, mixer.GraphicObject, 0, 1);
flowsheet.ConnectObjects(stream3.GraphicObject, mixer.GraphicObject, 0, 2);

// Connect mixer output
flowsheet.ConnectObjects(mixer.GraphicObject, outletStream.GraphicObject, 0, 0);
```

### Splitter with Multiple Outlets

```csharp
// Connect inlet to splitter
flowsheet.ConnectObjects(inletStream.GraphicObject, splitter.GraphicObject, 0, 0);

// Connect splitter to three outlet streams
flowsheet.ConnectObjects(splitter.GraphicObject, stream1.GraphicObject, 0, 0);
flowsheet.ConnectObjects(splitter.GraphicObject, stream2.GraphicObject, 1, 0);
flowsheet.ConnectObjects(splitter.GraphicObject, stream3.GraphicObject, 2, 0);
```

### Heat Exchanger

```csharp
// Hot side: inlet to port 0, outlet from port 0
flowsheet.ConnectObjects(hotInlet.GraphicObject, hx.GraphicObject, 0, 0);
flowsheet.ConnectObjects(hx.GraphicObject, hotOutlet.GraphicObject, 0, 0);

// Cold side: inlet to port 1, outlet from port 1
flowsheet.ConnectObjects(coldInlet.GraphicObject, hx.GraphicObject, 0, 1);
flowsheet.ConnecGraphicObject, coldOutlet.GraphicObject, 1, 0);
```

---

## Verification

To verify port indices for a specific unit operation, use:

```csharp
var portInfo = unitOperation.GetConnectionPortsInfo();
foreach (var port in portInfo)
{
    Console.WriteLine($"{port.Name}: Index={port.Index}, IsInput={port.IsInput}, IsOutput={port.IsOutput}");
}
```

---

## Related Documentation

- **DWSIM API Reference:** See `DWSIM_API_MAP.md`
- **Phase Properties:** See `IPhaseProperties.cs`
