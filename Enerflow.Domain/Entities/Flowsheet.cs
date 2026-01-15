using Enerflow.Domain.Common;
using Enerflow.Domain.ValueObjects;

namespace Enerflow.Domain.Entities;

public class Flowsheet : Entity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    
    public ThermodynamicsConfiguration? DefaultPropertyPackage { get; set; }
    public List<Compound> Compounds { get; set; } = new();

    // Aggregates
    public List<UnitOperation> UnitOperations { get; set; } = new();
    public List<MaterialStream> MaterialStreams { get; set; } = new();
    public List<EnergyStream> EnergyStreams { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Logic
    public void AddUnitOperation(UnitOperation unit)
    {
        if (UnitOperations.Any(u => u.Tag == unit.Tag))
            throw new ArgumentException($"Unit with tag {unit.Tag} already exists.");
        UnitOperations.Add(unit);
    }

    public void AddMaterialStream(MaterialStream stream)
    {
        if (MaterialStreams.Any(s => s.Tag == stream.Tag))
            throw new ArgumentException($"Stream with tag {stream.Tag} already exists.");
        MaterialStreams.Add(stream);
    }
}
