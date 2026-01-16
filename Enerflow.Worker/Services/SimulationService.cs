using Enerflow.Domain.DTOs;
using Enerflow.Domain.Interfaces;
using DWSIM.Automation;
using DWSIM.GlobalSettings;
using DWSIM.Interfaces;
using DWSIM.Thermodynamics.Streams;
using Enerflow.Domain.Enums;
using DWSIMPropertyPackage = DWSIM.Thermodynamics.PropertyPackages;
using Microsoft.Extensions.Logging;

namespace Enerflow.Worker.Services;

/// <summary>
/// Implementation of ISimulationService that uses DWSIM automation API.
/// Maps Domain DTOs to DWSIM flowsheet objects and executes simulations.
/// </summary>
public class SimulationService : ISimulationService
{
    private readonly ILogger<SimulationService> _logger;
    private readonly UnitOperationFactory _unitOpFactory;

    private readonly Automation _automation;
    private IFlowsheet? _flowsheet;

    private readonly List<string> _errorMessages = [];
    private readonly List<string> _logMessages = [];

    // Maps DTO IDs to DWSIM object names for connection resolution
    private readonly Dictionary<Guid, string> _streamIdToName = new();
    private readonly Dictionary<Guid, string> _unitOpIdToName = new();

    public SimulationService(ILogger<SimulationService> logger, UnitOperationFactory unitOpFactory)
    {
        _logger = logger;
        _unitOpFactory = unitOpFactory;

        // CRITICAL: Set automation mode before any DWSIM operations
        Settings.AutomationMode = true;

        _automation = new Automation();
        _logger.LogInformation("DWSIM Automation initialized in headless mode");
    }

    public bool BuildFlowsheet(SimulationDefinitionDto definition)
    {
        try
        {
            _logger.LogInformation("Building flowsheet: {Name}", definition.Name);
            _errorMessages.Clear();
            _logMessages.Clear();
            _streamIdToName.Clear();
            _unitOpIdToName.Clear();

            // Create new flowsheet
            _flowsheet = _automation.CreateFlowsheet();

            // Set units system
            SetSystemOfUnits(definition.SystemOfUnits);

            // Add compounds
            foreach (var compound in definition.Compounds)
            {
                AddCompound(compound);
            }

            // Set property package (thermodynamic model)
            SetPropertyPackage(definition.PropertyPackage);

            // Create material streams
            foreach (var stream in definition.MaterialStreams)
            {
                CreateMaterialStream(stream);
            }

            // Create energy streams
            foreach (var stream in definition.EnergyStreams)
            {
                CreateEnergyStream(stream);
            }

            // Create unit operations
            foreach (var unitOp in definition.UnitOperations)
            {
                CreateUnitOperation(unitOp);
            }

            // Connect streams to unit operations
            foreach (var unitOp in definition.UnitOperations)
            {
                ConnectStreams(unitOp);
            }

            _logMessages.Add($"Flowsheet '{definition.Name}' built successfully");

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Flowsheet built: {Compounds} compounds, {Streams} streams, {UnitOps} unit operations",
                    definition.Compounds.Count,
                    definition.MaterialStreams.Count + definition.EnergyStreams.Count,
                    definition.UnitOperations.Count);
            }

            return true;
        }
        catch (Exception ex)
        {
            _errorMessages.Add($"Failed to build flowsheet: {ex.Message}");
            _logger.LogError(ex, "Failed to build flowsheet: {Name}", definition.Name);
            return false;
        }
    }

    public bool Solve()
    {
        if (_flowsheet == null)
        {
            _errorMessages.Add("Cannot solve: flowsheet not initialized");
            return false;
        }

        try
        {
            _logger.LogInformation("Solving flowsheet...");

            // Use the correct DWSIM API method for solving
            _flowsheet.RequestCalculation();

            // Check for calculation errors
            var hasErrors = false;
            foreach (var obj in _flowsheet.SimulationObjects.Values)
            {
                if (!string.IsNullOrEmpty(obj.ErrorMessage))
                {
                    _errorMessages.Add($"{obj.Name}: {obj.ErrorMessage}");
                    hasErrors = true;
                }
            }

            if (hasErrors)
            {
                _logger.LogWarning("Flowsheet solved with errors");
            }
            else
            {
                _logMessages.Add("Flowsheet solved successfully");
                _logger.LogInformation("Flowsheet solved successfully");
            }

            return !hasErrors;
        }
        catch (Exception ex)
        {
            _errorMessages.Add($"Solver error: {ex.Message}");
            _logger.LogError(ex, "Failed to solve flowsheet");
            return false;
        }
    }

    public Dictionary<string, Dictionary<string, object>> CollectResults()
    {
        var results = new Dictionary<string, Dictionary<string, object>>();

        if (_flowsheet == null)
        {
            _logger.LogWarning("Cannot collect results: flowsheet not initialized");
            return results;
        }

        try
        {
            foreach (var obj in _flowsheet.SimulationObjects.Values)
            {
                var objResults = new Dictionary<string, object>();

                if (obj is MaterialStream ms)
                {
                    var phase0 = ms.Phases[0];
                    objResults["temperature"] = phase0.Properties.temperature ?? 0;
                    objResults["pressure"] = phase0.Properties.pressure ?? 0;
                    objResults["massFlow"] = phase0.Properties.massflow ?? 0;
                    objResults["molarFlow"] = phase0.Properties.molarflow ?? 0;
                    objResults["volumetricFlow"] = phase0.Properties.volumetric_flow ?? 0;
                    objResults["enthalpy"] = phase0.Properties.enthalpy ?? 0;

                    // Collect phase compositions
                    var compositions = new Dictionary<string, double>();
                    foreach (var compound in phase0.Compounds)
                    {
                        compositions[compound.Key] = compound.Value.MoleFraction ?? 0;
                    }

                    objResults["molarCompositions"] = compositions;
                }
                else
                {
                    // For unit operations, collect basic info
                    objResults["calculated"] = obj.Calculated;
                }

                results[obj.Name] = objResults;
            }

            _logger.LogInformation("Collected results for {Count} simulation objects", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting results");
            _errorMessages.Add($"Error collecting results: {ex.Message}");
        }

        return results;
    }

    public IReadOnlyList<string> GetErrorMessages() => _errorMessages.AsReadOnly();

    public IReadOnlyList<string> GetLogMessages() => _logMessages.AsReadOnly();

    private void SetSystemOfUnits(string systemOfUnits)
    {
        if (_flowsheet == null) return;

        try
        {
            // DWSIM uses specific unit system names
            var units = systemOfUnits.ToUpperInvariant() switch
            {
                "SI" => _flowsheet.AvailableSystemsOfUnits.FirstOrDefault(u => u.Name.Contains("SI")),
                "CGS" => _flowsheet.AvailableSystemsOfUnits.FirstOrDefault(u => u.Name.Contains("CGS")),
                "ENGLISH" or "IMPERIAL" => _flowsheet.AvailableSystemsOfUnits.FirstOrDefault(u =>
                    u.Name.Contains("English")),
                _ => _flowsheet.AvailableSystemsOfUnits.FirstOrDefault()
            };

            if (units != null)
            {
                _flowsheet.FlowsheetOptions.SelectedUnitSystem = units;
                _logger.LogDebug("Set system of units to: {Units}", units.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set system of units: {Units}", systemOfUnits);
        }
    }

    private void SetPropertyPackage(PropertyPackage thermoPackage)
    {
        if (_flowsheet == null) return;

        try
        {
            IPropertyPackage? pp = thermoPackage switch
            {
                PropertyPackage.PengRobinson => new DWSIMPropertyPackage.PengRobinsonPropertyPackage(),
                PropertyPackage.SoaveRedlichKwong => new DWSIMPropertyPackage.SRKPropertyPackage(),
                PropertyPackage.NRTL => new DWSIMPropertyPackage.NRTLPropertyPackage(),
                PropertyPackage.UNIQUAC => new DWSIMPropertyPackage.UNIQUACPropertyPackage(),
                PropertyPackage.RaoultsLaw => new DWSIMPropertyPackage.RaoultPropertyPackage(),
                PropertyPackage.SteamTables => new DWSIMPropertyPackage.SteamTablesPropertyPackage(),
                _ => new DWSIMPropertyPackage.PengRobinsonPropertyPackage()
            };

            _flowsheet.AddPropertyPackage(pp);
            _logger.LogDebug("Set property package to: {Package}", thermoPackage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set property package: {Package}", thermoPackage);
        }
    }

    private void AddCompound(CompoundDto compound)
    {
        if (_flowsheet == null) return;

        try
        {
            // DWSIM compounds are identified by name
            _flowsheet.AddCompound(compound.Name);
            _logger.LogDebug("Added compound: {Name}", compound.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add compound: {Name}", compound.Name);
            _errorMessages.Add($"Failed to add compound '{compound.Name}': {ex.Message}");
        }
    }

    private void CreateMaterialStream(MaterialStreamDto streamDto)
    {
        if (_flowsheet == null) return;

        try
        {
            var stream = new MaterialStream(streamDto.Name, "");

            // Set stream conditions
            stream.Phases[0].Properties.temperature = streamDto.Temperature;
            stream.Phases[0].Properties.pressure = streamDto.Pressure;
            stream.Phases[0].Properties.massflow = streamDto.MassFlow;

            // Set compositions
            foreach (var (compoundName, moleFraction) in streamDto.MolarCompositions)
            {
                if (stream.Phases[0].Compounds.ContainsKey(compoundName))
                {
                    stream.Phases[0].Compounds[compoundName].MoleFraction = moleFraction;
                }
            }

            _flowsheet.AddSimulationObject(stream);
            _streamIdToName[streamDto.Id] = streamDto.Name;

            _logger.LogDebug("Created material stream: {Name}", streamDto.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create material stream: {Name}", streamDto.Name);
            _errorMessages.Add($"Failed to create stream '{streamDto.Name}': {ex.Message}");
        }
    }

    private void CreateEnergyStream(EnergyStreamDto streamDto)
    {
        if (_flowsheet == null) return;

        try
        {
            var stream = new DWSIM.UnitOperations.Streams.EnergyStream(streamDto.Name, "");
            stream.EnergyFlow = streamDto.EnergyFlow;

            _flowsheet.AddSimulationObject(stream);
            _streamIdToName[streamDto.Id] = streamDto.Name;

            _logger.LogDebug("Created energy stream: {Name}", streamDto.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create energy stream: {Name}", streamDto.Name);
            _errorMessages.Add($"Failed to create energy stream '{streamDto.Name}': {ex.Message}");
        }
    }

    private void CreateUnitOperation(UnitOperationDto unitOpDto)
    {
        if (_flowsheet == null) return;

        try
        {
            var unitOp = _unitOpFactory.CreateUnitOperation(unitOpDto.Type, unitOpDto.Name, unitOpDto.ConfigParams);

            if (unitOp != null)
            {
                _flowsheet.AddSimulationObject(unitOp);
                _unitOpIdToName[unitOpDto.Id] = unitOpDto.Name;
                _logger.LogDebug("Created unit operation: {Type} named {Name}", unitOpDto.Type, unitOpDto.Name);
            }
            else
            {
                _errorMessages.Add($"Unsupported unit operation type: {unitOpDto.Type}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create unit operation: {Name}", unitOpDto.Name);
            _errorMessages.Add($"Failed to create unit operation '{unitOpDto.Name}': {ex.Message}");
        }
    }

    private void ConnectStreams(UnitOperationDto unitOpDto)
    {
        if (_flowsheet == null) return;

        if (!_unitOpIdToName.TryGetValue(unitOpDto.Id, out var unitOpName))
        {
            if (_logger.IsEnabled(LogLevel.Warning)) _logger.LogWarning("Unit operation not found for connection: {Id}", unitOpDto.Id);
            return;
        }

        // Get the unit operation from the simulation objects
        if (!_flowsheet.SimulationObjects.TryGetValue(unitOpName, out var unitOpObj))
        {
            if (_logger.IsEnabled(LogLevel.Warning)) _logger.LogWarning("Unit operation '{Name}' not found in flowsheet", unitOpName);
            return;
        }

        try
        {
            // Connect input streams
            for (int i = 0; i < unitOpDto.InputStreamIds.Count; i++)
            {
                var streamId = unitOpDto.InputStreamIds[i];
                if (!_streamIdToName.TryGetValue(streamId, out var streamName)) continue;
                if (!_flowsheet.SimulationObjects.TryGetValue(streamName, out var streamObj)) continue;
                _flowsheet.ConnectObjects(streamObj.GraphicObject, unitOpObj.GraphicObject, i, 0);
                if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("Connected input stream {Stream} to {UnitOp}", streamName, unitOpName);
            }

            // Connect output streams
            for (int i = 0; i < unitOpDto.OutputStreamIds.Count; i++)
            {
                var streamId = unitOpDto.OutputStreamIds[i];
                if (!_streamIdToName.TryGetValue(streamId, out var streamName)) continue;
                if (!_flowsheet.SimulationObjects.TryGetValue(streamName, out var streamObj)) continue;
                _flowsheet.ConnectObjects(unitOpObj.GraphicObject, streamObj.GraphicObject, 0, i);
                if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("Connected output stream {Stream} from {UnitOp}", streamName, unitOpName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect streams for unit operation: {Name}", unitOpName);
            _errorMessages.Add($"Failed to connect streams for '{unitOpName}': {ex.Message}");
        }
    }

    public void Dispose()
    {
        _flowsheet = null;
        _logger.LogInformation("SimulationService disposed");
    }
}