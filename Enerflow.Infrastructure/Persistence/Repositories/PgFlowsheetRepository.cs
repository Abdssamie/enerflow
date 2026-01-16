using Enerflow.Domain.Entities;
using Enerflow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Enerflow.Infrastructure.Persistence.Repositories;

public class PgFlowsheetRepository : IFlowsheetRepository
{
    private readonly FlowsheetDbContext _context;

    public PgFlowsheetRepository(FlowsheetDbContext context)
    {
        _context = context;
    }

    public async Task<Flowsheet?> GetByIdAsync(Guid id)
    {
        return await _context.Flowsheets
            .Include(f => f.UnitOperations)
            .Include(f => f.MaterialStreams)
            .Include(f => f.EnergyStreams)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<Flowsheet>> GetAllAsync()
    {
        // For list view, we might not want to fetch all children, but strictly following interface for now.
        // In a real app, we'd have a specific DTO or a GetSummaries method.
        return await _context.Flowsheets.AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(Flowsheet flowsheet)
    {
        await _context.Flowsheets.AddAsync(flowsheet);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Flowsheet flowsheet)
    {
        _context.Flowsheets.Update(flowsheet);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var flowsheet = await _context.Flowsheets.FindAsync(id);
        if (flowsheet != null)
        {
            _context.Flowsheets.Remove(flowsheet);
            await _context.SaveChangesAsync();
        }
    }
}
