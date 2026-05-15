using Api.Application.Common.Interfaces;
using Api.Domain.Leads;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

public class LeadRepository : ILeadRepository
{
    private readonly LeadsDbContext _dbContext;

    public LeadRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Lead lead, CancellationToken cancellationToken)
    {
        await _dbContext.Leads.AddAsync(lead, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Lead?> GetByIdAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return _dbContext.Leads.FirstOrDefaultAsync(x => x.Id == leadId, cancellationToken);
    }

    public async Task<IReadOnlyList<Lead>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Leads
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Lead>> ListByCreatedRangeAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Leads.AsQueryable();

        if (startDateUtc.HasValue)
        {
            query = query.Where(lead => lead.CreatedAtUtc >= startDateUtc.Value);
        }

        if (endDateUtc.HasValue)
        {
            query = query.Where(lead => lead.CreatedAtUtc <= endDateUtc.Value);
        }

        return await query
            .OrderByDescending(lead => lead.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return _dbContext.Leads.AnyAsync(x => x.Id == leadId, cancellationToken);
    }

    public async Task DeleteAsync(Lead lead, CancellationToken cancellationToken)
    {
        _dbContext.Leads.Remove(lead);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
