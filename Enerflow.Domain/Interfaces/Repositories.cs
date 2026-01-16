using Enerflow.Domain.Entities;

namespace Enerflow.Domain.Interfaces;

public interface IFlowsheetRepository
{
    Task<Flowsheet?> GetByIdAsync(Guid id);
    Task<IEnumerable<Flowsheet>> GetAllAsync();
    Task AddAsync(Flowsheet flowsheet);
    Task UpdateAsync(Flowsheet flowsheet);
    Task DeleteAsync(Guid id);
}

public interface IUnitOperationRepository
{
    // Might not be needed if accessed via Flowsheet Aggregate, 
    // but useful for quick lookups if DB is relational.
}
