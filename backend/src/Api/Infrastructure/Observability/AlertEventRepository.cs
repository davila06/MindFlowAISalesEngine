using Api.Application.Observability;
using Api.Domain.Observability;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Observability;

public sealed class AlertEventRepository : IAlertEventRepository
{
    private readonly LeadsDbContext _dbContext;

    public AlertEventRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(AlertEvent alertEvent, CancellationToken cancellationToken)
    {
        _dbContext.AlertEvents.Add(alertEvent);
        await Task.CompletedTask;
    }

    public async Task<AlertEvent?> GetLatestAsync(
        string endpointName,
        string metricName,
        CancellationToken cancellationToken)
    {
        return await _dbContext.AlertEvents
            .Where(x => x.EndpointName == endpointName && x.MetricName == metricName)
            .OrderByDescending(x => x.TriggeredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AlertEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.AlertEvents
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AlertEvent>> QueryAsync(
        string? endpointName,
        string? metricName,
        DateTime? startUtc,
        DateTime? endUtc,
        bool? notificationSent,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.AlertEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(endpointName))
        {
            query = query.Where(x => x.EndpointName == endpointName);
        }

        if (!string.IsNullOrWhiteSpace(metricName))
        {
            query = query.Where(x => x.MetricName == metricName);
        }

        if (startUtc.HasValue)
        {
            query = query.Where(x => x.TriggeredAtUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            query = query.Where(x => x.TriggeredAtUtc <= endUtc.Value);
        }

        if (notificationSent.HasValue)
        {
            query = query.Where(x => x.NotificationSent == notificationSent.Value);
        }

        var safePageSize = pageSize is > 0
            ? Math.Min(pageSize.Value, 200)
            : 100;
        var safePage = page is > 0 ? page.Value : 1;

        return await query
            .OrderByDescending(x => x.TriggeredAtUtc)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, int>> CountByStatusAsync(CancellationToken cancellationToken)
    {
        var items = await _dbContext.AlertEvents
            .AsNoTracking()
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return items.ToDictionary(
            x => string.IsNullOrWhiteSpace(x.Status) ? "open" : x.Status.ToLowerInvariant(),
            x => x.Count);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> PurgeAsync(DateTime olderThanUtc, CancellationToken cancellationToken)
    {
        var toDelete = await _dbContext.AlertEvents
            .Where(x => x.TriggeredAtUtc < olderThanUtc)
            .ToListAsync(cancellationToken);

        if (toDelete.Count == 0)
            return 0;

        _dbContext.AlertEvents.RemoveRange(toDelete);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return toDelete.Count;
    }
}
