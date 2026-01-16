using Enerflow.Domain.Common;
using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities;

public class SimulationSession : Entity
{
    public string Name { get; private set; }
    public SimulationStatus Status { get; private set; }
    public Guid? DWSIMFlowsheetId { get; private set; } // The internal ID in the FlowsheetService
    public string? PropertyPackage { get; private set; }
    public List<string> Compounds { get; private set; } = new();

    // Concurrency Token for database optimistic locking later
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public SimulationSession(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        Name = name;
        Status = SimulationStatus.Created;
    }

    public void AssignFlowsheet(Guid flowsheetId)
    {
        DWSIMFlowsheetId = flowsheetId;
        Status = SimulationStatus.Loaded;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRunning()
    {
        Status = SimulationStatus.Running;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkConverged()
    {
        Status = SimulationStatus.Converged;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = SimulationStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
        // In a real app, we'd store the failure reason in a domain event or a separate field
    }

    public void ConfigureThermodynamics(string propertyPackage, IEnumerable<string> compounds)
    {
        if (Status == SimulationStatus.Running) 
            throw new InvalidOperationException("Cannot configure thermodynamics while simulation is running.");

        PropertyPackage = propertyPackage;
        Compounds = compounds.ToList();
        UpdatedAt = DateTime.UtcNow;
    }
}

public class SimulationRequest : Entity
{
    public double Temperature { get; set; }
    public double Pressure { get; set; }
    public double FlowRate { get; set; }

    public string PropertyPackage { get; set; }
    public List<string> Compounds { get; set; }

}

public class SimulationResponse : Entity
{
    public bool Success { get; set; }
}