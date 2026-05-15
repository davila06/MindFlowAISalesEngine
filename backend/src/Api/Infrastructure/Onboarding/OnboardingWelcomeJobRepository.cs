using Api.Application.Onboarding;
using Api.Domain.Onboarding;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Onboarding;

public class OnboardingWelcomeJobRepository : IOnboardingWelcomeJobRepository
{
    private readonly LeadsDbContext _context;

    public OnboardingWelcomeJobRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OnboardingWelcomeJob job, CancellationToken cancellationToken)
    {
        _context.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OnboardingWelcomeJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await _context.Set<OnboardingWelcomeJob>()
            .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
    }

    public async Task<OnboardingWelcomeJob?> GetLatestByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return await _context.Set<OnboardingWelcomeJob>()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.ScheduledAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OnboardingWelcomeJob>> GetScheduledDueAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        return await _context.Set<OnboardingWelcomeJob>()
            .Where(x => x.Status == OnboardingWelcomeJobStatus.Scheduled && x.DueAtUtc <= utcNow)
            .OrderBy(x => x.DueAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OnboardingWelcomeJob>> GetDeadLetterAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<OnboardingWelcomeJob>()
            .Where(x => x.Status == OnboardingWelcomeJobStatus.Failed)
            .OrderByDescending(x => x.ExecutedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OnboardingWelcomeJob>> GetPoisonQueueAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<OnboardingWelcomeJob>()
            .Where(x => x.Status == OnboardingWelcomeJobStatus.Poisoned)
            .OrderByDescending(x => x.ExecutedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountPoisonedAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<OnboardingWelcomeJob>()
            .CountAsync(x => x.Status == OnboardingWelcomeJobStatus.Poisoned, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}