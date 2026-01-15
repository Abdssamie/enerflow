using Enerflow.Domain.Entities;

namespace Enerflow.Domain.Interfaces;

public interface ISimulationRepository
{
    Task<SimulationSession?> GetByIdAsync(Guid id);
    Task<IEnumerable<SimulationSession>> GetAllAsync();
    Task AddAsync(SimulationSession session);
    Task UpdateAsync(SimulationSession session);
    Task DeleteAsync(Guid id);
}
