using Api.Application.Onboarding;
using Api.Domain.Onboarding;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Onboarding;

public class OnboardingTaskRepository : IOnboardingTaskRepository
{
    private readonly LeadsDbContext _context;

    public OnboardingTaskRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<OnboardingTask> tasks, CancellationToken cancellationToken)
    {
        _context.AddRange(tasks);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OnboardingTask?> GetByIdAsync(Guid taskId, CancellationToken cancellationToken)
    {
        return await _context.Set<OnboardingTask>()
            .FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
    }

    public async Task<IReadOnlyList<OnboardingTask>> ListByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return await _context.Set<OnboardingTask>()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OnboardingTask>> ListAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<OnboardingTask>()
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
