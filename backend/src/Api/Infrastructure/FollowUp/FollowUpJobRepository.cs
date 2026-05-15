using Api.Application.FollowUp;
using Api.Domain.FollowUp;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.FollowUp;

public class FollowUpJobRepository : IFollowUpJobRepository
{
    private readonly LeadsDbContext _context;

    public FollowUpJobRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FollowUpJob job, CancellationToken cancellationToken)
    {
        _context.FollowUpJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FollowUpJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.FollowUpJobs
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<FollowUpJob>> GetScheduledDueAsync(
        DateTime utcNow, CancellationToken cancellationToken)
    {
        return await _context.FollowUpJobs
            .Where(j => j.Status == FollowUpJobStatus.Scheduled && j.DueAtUtc <= utcNow)
            .OrderBy(j => j.DueAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FollowUpJob>> GetByLeadIdAsync(
        Guid leadId, CancellationToken cancellationToken)
    {
        return await _context.FollowUpJobs
            .Where(j => j.LeadId == leadId)
            .OrderByDescending(j => j.ScheduledAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FollowUpJob>> GetDeadLetterAsync(CancellationToken cancellationToken)
    {
        return await _context.FollowUpJobs
            .Where(j => j.Status == FollowUpJobStatus.Failed)
            .OrderByDescending(j => j.ExecutedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FollowUpJob>> GetPoisonQueueAsync(CancellationToken cancellationToken)
    {
        return await _context.FollowUpJobs
            .Where(j => j.Status == FollowUpJobStatus.Poisoned)
            .OrderByDescending(j => j.ExecutedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountPoisonedAsync(CancellationToken cancellationToken)
    {
        return await _context.FollowUpJobs
            .CountAsync(j => j.Status == FollowUpJobStatus.Poisoned, cancellationToken);
    }

    public async Task<IReadOnlyList<FollowUpJob>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.FollowUpJobs
            .OrderByDescending(j => j.ScheduledAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
