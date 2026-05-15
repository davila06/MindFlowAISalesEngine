using Api.Application.Observability;
using Api.Domain.Observability;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Observability;

public sealed class ObservabilitySnapshotRepository : IObservabilitySnapshotRepository
{
    private readonly LeadsDbContext _dbContext;

    public ObservabilitySnapshotRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(IEnumerable<ObservabilityMetricRecord> records, CancellationToken cancellationToken = default)
    {
        _dbContext.ObservabilityMetricRecords.AddRange(records);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ObservabilityMetricRecord>> QueryAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        string? endpointName,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ObservabilityMetricRecords.AsNoTracking();

        if (startUtc.HasValue)
        {
            query = query.Where(r => r.RecordedAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            query = query.Where(r => r.RecordedAtUtc <= endUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(endpointName))
        {
            query = query.Where(r => r.EndpointName == endpointName);
        }

        return await query
            .OrderByDescending(r => r.RecordedAtUtc)
            .Take(1000)
            .ToListAsync(cancellationToken);
    }
}
