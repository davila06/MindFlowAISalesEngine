using Api.Application.Common.Interfaces;
using Api.Domain.Leads;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

public sealed class LeadAuditSnapshotRepository : ILeadAuditSnapshotRepository
{
    private readonly LeadsDbContext _dbContext;

    public LeadAuditSnapshotRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(LeadAuditSnapshot snapshot, CancellationToken cancellationToken)
    {
        await _dbContext.Set<LeadAuditSnapshot>().AddAsync(snapshot, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeadAuditSnapshot>> ListByLeadAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<LeadAuditSnapshot>()
            .Where(x => x.LeadId == leadId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByEventTypePrefixAsync(string eventTypePrefix, CancellationToken cancellationToken)
    {
        return _dbContext.Set<LeadAuditSnapshot>()
            .CountAsync(x => x.EventType.StartsWith(eventTypePrefix), cancellationToken);
    }

    public async Task<IReadOnlyList<LeadAuditSnapshot>> QueryByEventTypePrefixAsync(
        string eventTypePrefix,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<LeadAuditSnapshot>()
            .Where(x => x.EventType.StartsWith(eventTypePrefix));

        if (startUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= endUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
