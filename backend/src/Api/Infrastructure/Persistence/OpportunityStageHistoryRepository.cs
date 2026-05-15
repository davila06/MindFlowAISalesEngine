using Api.Application.Common.Interfaces;
using Api.Domain.Pipeline;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

public class OpportunityStageHistoryRepository : IOpportunityStageHistoryRepository
{
    private readonly LeadsDbContext _dbContext;

    public OpportunityStageHistoryRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OpportunityStageHistory history, CancellationToken cancellationToken)
    {
        await _dbContext.OpportunityStageHistories.AddAsync(history, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpportunityStageHistory>> ListByOpportunityAsync(Guid opportunityId, CancellationToken cancellationToken)
    {
        return await _dbContext.OpportunityStageHistories
            .Where(x => x.OpportunityId == opportunityId)
            .OrderByDescending(x => x.ChangedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OpportunityStageHistory>> ListByChangedRangeAsync(DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken cancellationToken)
    {
        var query = _dbContext.OpportunityStageHistories.AsQueryable();

        if (startDateUtc.HasValue)
        {
            query = query.Where(x => x.ChangedAtUtc >= startDateUtc.Value);
        }

        if (endDateUtc.HasValue)
        {
            query = query.Where(x => x.ChangedAtUtc <= endDateUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.ChangedAtUtc)
            .ToListAsync(cancellationToken);
    }
}