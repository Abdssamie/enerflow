using System.Collections.Concurrent;
using DWSIM.Interfaces;
using DWSIM.SharedClasses;

namespace Enerflow.API.Services;

public record StreamProperties(
    string Tag,
    double Temperature, // K
    double Pressure,    // Pa
    double MassFlow,    // kg/s
    double MolarFlow,   // mol/s
    double VapourFraction,
    double Enthalpy     // kJ/kg
);

public interface IFlowsheetService
{
    Guid LoadFlowsheet(string filePath);
    IFlowsheet GetFlowsheet(Guid id);
    IEnumerable<Guid> GetLoadedFlowsheetIds();
    void UnloadFlowsheet(Guid id);
    
    StreamProperties GetStreamProperties(Guid flowsheetId, string streamTag);
    void UpdateStreamProperties(Guid flowsheetId, string streamTag, double? temperature, double? pressure, double? massFlow);
    void Solve(Guid flowsheetId);
}

public class FlowsheetService : IFlowsheetService
{
    private readonly IDWSIMService _dwsimService;
    private readonly ConcurrentDictionary<Guid, IFlowsheet> _flowsheets = new();

    public FlowsheetService(IDWSIMService dwsimService)
    {
        _dwsimService = dwsimService;
    }

    public Guid LoadFlowsheet(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
            throw new FileNotFoundException("Flowsheet file not found.", filePath);

        var interf = _dwsimService.GetAutomationManager();
        var flowsheet = interf.LoadFlowsheet(filePath);
        
        var id = Guid.NewGuid();
        _flowsheets.TryAdd(id, flowsheet);
        
        return id;
    }

    public IFlowsheet GetFlowsheet(Guid id)
    {
        if (_flowsheets.TryGetValue(id, out var flowsheet))
            return flowsheet;
            
        throw new KeyNotFoundException($"Flowsheet with ID {id} not found.");
    }

    public IEnumerable<Guid> GetLoadedFlowsheetIds()
    {
        return _flowsheets.Keys;
    }

    public void UnloadFlowsheet(Guid id)
    {
        _flowsheets.TryRemove(id, out _);
    }

    public StreamProperties GetStreamProperties(Guid flowsheetId, string streamTag)
    {
        var flowsheet = GetFlowsheet(flowsheetId);
        var obj = flowsheet.GetObject(streamTag);
        
        if (obj is IMaterialStream stream)
        {
            return new StreamProperties(
                ((ISimulationObject)stream).GraphicObject.Tag,
                stream.GetTemperature(),
                stream.GetPressure(),
                stream.GetMassFlow(),
                stream.GetMolarFlow(),
                (double)((dynamic)stream).GetVaporPhaseMoleFraction(),
                stream.GetMassEnthalpy()
            );
        }
        
        throw new InvalidOperationException($"Object with tag {streamTag} is not a material stream.");
    }

    public void UpdateStreamProperties(Guid flowsheetId, string streamTag, double? temperature, double? pressure, double? massFlow)
    {
        var flowsheet = GetFlowsheet(flowsheetId);
        var obj = flowsheet.GetObject(streamTag);
        
        if (obj is IMaterialStream stream)
        {
            if (temperature.HasValue) stream.SetTemperature(temperature.Value);
            if (pressure.HasValue) stream.SetPressure(pressure.Value);
            if (massFlow.HasValue) stream.SetMassFlow(massFlow.Value);
            return;
        }
        
        throw new InvalidOperationException($"Object with tag {streamTag} is not a material stream.");
    }

    public void Solve(Guid flowsheetId)
    {
        var flowsheet = GetFlowsheet(flowsheetId);
        var interf = _dwsimService.GetAutomationManager();
        
        var errors = interf.CalculateFlowsheet2(flowsheet);
        
        if (errors.Count > 0)
        {
            throw new Exception($"Simulation failed to converge: {string.Join(", ", errors)}");
        }
    }
}
