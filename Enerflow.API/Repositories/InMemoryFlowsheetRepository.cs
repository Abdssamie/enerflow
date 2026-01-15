using Enerflow.Domain.Entities;
using Enerflow.Domain.Interfaces;
using System.Collections.Concurrent;

namespace Enerflow.API.Repositories;

public class InMemoryFlowsheetRepository : IFlowsheetRepository
{
    private readonly ConcurrentDictionary<Guid, Flowsheet> _store = new();

    public Task<Flowsheet?> GetByIdAsync(Guid id)
    {
        _store.TryGetValue(id, out var flowsheet);
        return Task.FromResult(flowsheet);
    }

    public Task<IEnumerable<Flowsheet>> GetAllAsync()
    {
        return Task.FromResult(_store.Values.AsEnumerable());
    }

    public Task AddAsync(Flowsheet flowsheet)
    {
        _store.TryAdd(flowsheet.Id, flowsheet);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Flowsheet flowsheet)
    {
        _store[flowsheet.Id] = flowsheet;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
