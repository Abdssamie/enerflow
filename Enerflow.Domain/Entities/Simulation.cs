using System.Text.Json;
using Enerflow.Domain.Entities.Streams;
using Enerflow.Domain.Entities.UnitOperations;
using Enerflow.Domain.Enums;
using Enerflow.Domain.Exceptions;

namespace Enerflow.Domain.Entities;

public class Simulation
{
    public Guid Id { get; set; } = Common.IdGenerator.NextGuid();
    public required string Name { get; set; }
    public required PropertyPackageType PropertyPackage { get; set; }
    public required FlashAlgorithm FlashAlgorithm { get; set; }
    public required SystemOfUnits SystemOfUnits { get; set; }

    // Execution state
    public SimulationStatus Status { get; set; } = SimulationStatus.Created;
    public string? ErrorMessage { get; set; }

    // Results stored as JSON blob (for quick retrieval)
    public JsonDocument? ResultJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<Compound> Compounds { get; set; } = new List<Compound>();
    public ICollection<MaterialStream> MaterialStreams { get; set; } = new List<MaterialStream>();
    public ICollection<EnergyStream> EnergyStreams { get; set; } = new List<EnergyStream>();
    public ICollection<UnitOperationObject> UnitOperations { get; set; } = new List<UnitOperationObject>();

    public void Validate()
    {
        if (Compounds.Count == 0)
            throw new InvalidOperationException("Simulation must have at least one compound.");
            
        // Note: Logic says "Must have at least one object", but usually streams are added first.
        // However, for a runnable simulation, we generally expect units.
        // I'll make it a warning or skip strict check if simple validation is required,
        // but the prompt says: "Validation: Must have at least one object."
        if (UnitOperations.Count == 0)
             throw new InvalidOperationException("Simulation must have at least one unit operation.");
    }

    /// <summary>
    /// Returns a list of UnitOperationObjects sorted by calculation order using a topological sort.
    /// Cycle detection is implemented; loops must be broken by a Recycle unit.
    /// </summary>
    public List<UnitOperationObject> GetTopologicalOrder()
    {
        if (UnitOperations.Count == 0)
            return [];

        // 1. Build Adjacency Graph
        // Node: UnitOperationObject
        // Edge: Producer -> Consumer (Stream connection)
        
        // Map StreamId -> Producer Unit
        var streamProducers = new Dictionary<Guid, UnitOperationObject>();
        foreach (var unit in UnitOperations)
        {
            foreach (var outId in unit.OutputStreamIds)
            {
                streamProducers[outId] = unit;
            }
        }

        var graph = new Dictionary<UnitOperationObject, List<UnitOperationObject>>();
        var inDegree = new Dictionary<UnitOperationObject, int>();

        // Initialize graph
        foreach (var unit in UnitOperations)
        {
            graph[unit] = new List<UnitOperationObject>();
            inDegree[unit] = 0;
        }

        // Populate edges
        foreach (var consumer in UnitOperations)
        {
            foreach (var inId in consumer.InputStreamIds)
            {
                if (streamProducers.TryGetValue(inId, out var producer))
                {
                    // CRITICAL: Break loops at Recycle units.
                    // If the producer is a Recycle unit, we treat this connection as "torn".
                    // It does not contribute to the dependency order for the initial pass.
                    if (producer is RecycleObject)
                    {
                        continue;
                    }

                    graph[producer].Add(consumer);
                    inDegree[consumer]++;
                }
            }
        }

        // 2. Kahn's Algorithm
        var queue = new Queue<UnitOperationObject>();
        foreach (var unit in UnitOperations)
        {
            if (inDegree[unit] == 0)
            {
                queue.Enqueue(unit);
            }
        }

        var sortedList = new List<UnitOperationObject>();

        while (queue.Count > 0)
        {
            var u = queue.Dequeue();
            sortedList.Add(u);

            if (graph.TryGetValue(u, out var neighbors))
            {
                foreach (var v in neighbors)
                {
                    inDegree[v]--;
                    if (inDegree[v] == 0)
                    {
                        queue.Enqueue(v);
                    }
                }
            }
        }

        // 3. Cycle Detection
        if (sortedList.Count != UnitOperations.Count)
        {
            throw new InvalidTopologyException("Cycle detected in simulation topology. Loops must be broken by a Recycle unit.");
        }

        return sortedList;
    }
}
