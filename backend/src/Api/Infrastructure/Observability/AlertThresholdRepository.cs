using Api.Application.Observability;
using Api.Domain.Observability;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Observability;

public sealed class AlertThresholdRepository : IAlertThresholdRepository
{
    private readonly LeadsDbContext _dbContext;

    public AlertThresholdRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AlertThreshold threshold, CancellationToken cancellationToken)
    {
        _dbContext.AlertThresholds.Add(threshold);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AlertThreshold?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.AlertThresholds.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AlertThreshold>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.AlertThresholds
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertThreshold>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.AlertThresholds
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.AlertThresholds
            .AsNoTracking()
            .CountAsync(x => x.IsActive, cancellationToken);
    }

    public void Remove(AlertThreshold threshold)
    {
        _dbContext.AlertThresholds.Remove(threshold);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
