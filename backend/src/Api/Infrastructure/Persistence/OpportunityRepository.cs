using Api.Application.Common.Interfaces;
using Api.Domain.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

public class OpportunityRepository : IOpportunityRepository
{
    private readonly LeadsDbContext _dbContext;

    public OpportunityRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Opportunity opportunity, CancellationToken cancellationToken)
    {
        await _dbContext.Opportunities.AddAsync(opportunity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Opportunity?> GetByIdAsync(Guid opportunityId, CancellationToken cancellationToken)
    {
        return _dbContext.Opportunities.FirstOrDefaultAsync(x => x.Id == opportunityId, cancellationToken);
    }

    public async Task<IReadOnlyList<Opportunity>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Opportunities
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}