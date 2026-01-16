using DWSIM.Interfaces;
using DWSIM.SharedClasses;
using Enerflow.Domain.ValueObjects;

namespace Enerflow.API.Services;

public interface IFlowsheetService
{
    Guid LoadFlowsheet(string filePath);
    IFlowsheet GetFlowsheet(Guid id);
    IEnumerable<Guid> GetLoadedFlowsheetIds();
    void UnloadFlowsheet(Guid id);
    
    StreamState GetStreamState(Guid flowsheetId, string streamTag);
    void UpdateStreamState(Guid flowsheetId, string streamTag, StreamState state);
    void Solve(Guid flowsheetId);
}

public class FlowsheetService : IFlowsheetService
{
    private readonly IDWSIMService _dwsimService;
    private readonly Dictionary<Guid, IFlowsheet> _flowsheets = new();

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
        _flowsheets[id] = flowsheet;
        
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
        _flowsheets.Remove(id);
    }

    public StreamState GetStreamState(Guid flowsheetId, string streamTag)
    {
        var flowsheet = GetFlowsheet(flowsheetId);
        var obj = flowsheet.GetObject(streamTag);
        
        if (obj is IMaterialStream stream)
        {
            // Note: Assuming DWSIM returns values in SI (K, Pa, kg/s, kJ/kg)
            return StreamState.Create(
                stream.GetTemperature(),
                stream.GetPressure(),
                stream.GetMassFlow(),
                stream.GetMolarFlow(),
                stream.GetMassEnthalpy(), 
                stream.GetOverallComposition()
            );
        }
        
        throw new InvalidOperationException($"Object with tag {streamTag} is not a material stream.");
    }

    public void UpdateStreamState(Guid flowsheetId, string streamTag, StreamState state)
    {
        var flowsheet = GetFlowsheet(flowsheetId);
        var obj = flowsheet.GetObject(streamTag);
        
        if (obj is IMaterialStream stream)
        {
            // Set T, P, Flow directly (SI units assumed by DWSIM setters)
            stream.SetTemperature(state.Temperature);
            stream.SetPressure(state.Pressure);
            stream.SetMassFlow(state.MassFlow);
            
            // Set Composition if provided
            if (state.MoleFractions != null && state.MoleFractions.Length > 0)
            {
                stream.SetOverallComposition(state.MoleFractions);
            }
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
