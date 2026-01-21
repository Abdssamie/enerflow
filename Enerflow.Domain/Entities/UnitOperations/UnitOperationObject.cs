using System.Text.Json;
using Enerflow.Domain.Enums;

namespace Enerflow.Domain.Entities.UnitOperations;

public abstract class UnitOperationObject : SimulationObject
{
    public abstract UnitOperationType Type { get; }
    public List<Guid> InputStreamIds { get; set; } = [];
    public List<Guid> OutputStreamIds { get; set; } = [];
    
    public JsonDocument? ConfigParams { get; init; }

    public override void Validate()
    {
        if (InputStreamIds == null)
            throw new ArgumentNullException(nameof(InputStreamIds));
        if (OutputStreamIds == null)
            throw new ArgumentNullException(nameof(OutputStreamIds));
    }
}
