using Enerflow.Domain.Common;
using Enerflow.Domain.Enums;
using Enerflow.Domain.ValueObjects;

namespace Enerflow.Domain.Entities;

public class UnitOperation : Entity
{
    public required string Tag { get; set; }
    public required UnitOperationType Type { get; set; }
    public string? Description { get; set; }
    public Coordinates Position { get; set; } = new(0, 0);

    // Parameters (Generic Key-Value for flexibility)
    // E.g. "OutletPressure" -> 100000 (for Valve), "Efficiency" -> 0.75 (for Compressor)
    public Dictionary<string, double> Parameters { get; set; } = new();
    
    // Connections are strictly managed via Streams pointing TO this unit, 
    // or we can map them here. DWSIM usually maps Unit -> Stream.
    // Let's store connection metadata here for easier graph traversal.
    public Dictionary<string, Guid> InputConnections { get; set; } = new(); // PortName -> StreamId
    public Dictionary<string, Guid> OutputConnections { get; set; } = new(); // PortName -> StreamId
}
