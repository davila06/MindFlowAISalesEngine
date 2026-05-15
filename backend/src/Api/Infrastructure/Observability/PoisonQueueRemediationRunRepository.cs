using Api.Application.Observability;
using Api.Domain.Observability;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Observability;

public sealed class PoisonQueueRemediationRunRepository : IPoisonQueueRemediationRunRepository
{
    private readonly LeadsDbContext _dbContext;

    public PoisonQueueRemediationRunRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PoisonQueueRemediationRun run, CancellationToken cancellationToken)
    {
        _dbContext.Set<PoisonQueueRemediationRun>().Add(run);
        await Task.CompletedTask;
    }

    public async Task<PoisonQueueRemediationRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<PoisonQueueRemediationRun>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PoisonQueueRemediationRun>> QueryAsync(
        string? jobType,
        string? outcome,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<PoisonQueueRemediationRun>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(jobType))
        {
            var normalized = jobType.Trim().ToLowerInvariant();
            query = query.Where(x => x.JobType == normalized);
        }

        if (!string.IsNullOrWhiteSpace(outcome))
        {
            var normalized = outcome.Trim().ToLowerInvariant();
            query = query.Where(x => x.Outcome == normalized);
        }

        if (startUtc.HasValue)
        {
            query = query.Where(x => x.ExecutedAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            query = query.Where(x => x.ExecutedAtUtc <= endUtc.Value);
        }

        return await query
            .OrderByDescending(x => x.ExecutedAtUtc)
            .Take(2000)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
