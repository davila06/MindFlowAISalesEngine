using Api.Domain.Observability;

namespace Api.Application.Observability;

public interface IAlertEventRepository
{
    Task AddAsync(AlertEvent alertEvent, CancellationToken cancellationToken);

    Task<AlertEvent?> GetLatestAsync(
        string endpointName,
        string metricName,
        CancellationToken cancellationToken);

    Task<AlertEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<AlertEvent>> QueryAsync(
        string? endpointName,
        string? metricName,
        DateTime? startUtc,
        DateTime? endUtc,
        bool? notificationSent,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, int>> CountByStatusAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<int> PurgeAsync(DateTime olderThanUtc, CancellationToken cancellationToken);
}
