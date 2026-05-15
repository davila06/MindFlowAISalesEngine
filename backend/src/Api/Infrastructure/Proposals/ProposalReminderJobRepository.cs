using Api.Application.Proposals;
using Api.Domain.Proposals;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Proposals;

public class ProposalReminderJobRepository : IProposalReminderJobRepository
{
    private readonly LeadsDbContext _context;

    public ProposalReminderJobRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ProposalReminderJob job, CancellationToken cancellationToken)
    {
        _context.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProposalReminderJob?> GetByProposalIdAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        return await _context.Set<ProposalReminderJob>()
            .Where(x => x.ProposalId == proposalId)
            .OrderByDescending(x => x.ScheduledAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProposalReminderJob>> GetScheduledDueAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        return await _context.Set<ProposalReminderJob>()
            .Where(x => x.Status == ProposalReminderStatus.Scheduled && x.DueAtUtc <= utcNow)
            .OrderBy(x => x.DueAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProposalReminderJob>> GetDeadLetterAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<ProposalReminderJob>()
            .Where(x => x.Status == ProposalReminderStatus.Failed)
            .OrderByDescending(x => x.ExecutedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProposalReminderJob>> GetPoisonQueueAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<ProposalReminderJob>()
            .Where(x => x.Status == ProposalReminderStatus.Poisoned)
            .OrderByDescending(x => x.ExecutedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountPoisonedAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<ProposalReminderJob>()
            .CountAsync(x => x.Status == ProposalReminderStatus.Poisoned, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
