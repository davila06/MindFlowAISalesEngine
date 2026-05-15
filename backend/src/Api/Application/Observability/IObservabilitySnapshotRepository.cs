using Api.Domain.Observability;

namespace Api.Application.Observability;

public interface IObservabilitySnapshotRepository
{
    Task SaveAsync(IEnumerable<ObservabilityMetricRecord> records, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ObservabilityMetricRecord>> QueryAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        string? endpointName,
        CancellationToken cancellationToken = default);
}
