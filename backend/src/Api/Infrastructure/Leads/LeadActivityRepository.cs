using Api.Application.Leads;
using Api.Domain.Leads;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Leads;

public class LeadActivityRepository : ILeadActivityRepository
{
    private readonly LeadsDbContext _context;

    public LeadActivityRepository(LeadsDbContext context) => _context = context;

    public async Task AddAsync(LeadActivity activity, CancellationToken cancellationToken)
    {
        _context.LeadActivities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<LeadActivity> activities, CancellationToken cancellationToken)
    {
        _context.LeadActivities.AddRange(activities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LeadActivity>> GetByLeadAsync(
        Guid leadId,
        int page,
        int pageSize,
        string? typeFilter,
        CancellationToken cancellationToken)
    {
        var query = _context.LeadActivities.Where(a => a.LeadId == leadId);
        if (!string.IsNullOrWhiteSpace(typeFilter))
            query = query.Where(a => a.ActivityType == typeFilter);

        return await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByLeadAsync(Guid leadId, CancellationToken cancellationToken)
        => await _context.LeadActivities.CountAsync(a => a.LeadId == leadId, cancellationToken);
}
