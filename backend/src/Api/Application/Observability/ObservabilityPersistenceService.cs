using Api.Application.AnalyticsAdvanced;
using Api.Domain.Observability;

namespace Api.Application.Observability;

public sealed class ObservabilityPersistenceService : IObservabilityPersistenceService
{
    private readonly IAnalyticsObservabilityService _observabilityService;
    private readonly IObservabilitySnapshotRepository _repository;
    private readonly IAlertEvaluationService _alertEvaluationService;

    public ObservabilityPersistenceService(
        IAnalyticsObservabilityService observabilityService,
        IObservabilitySnapshotRepository repository,
        IAlertEvaluationService alertEvaluationService)
    {
        _observabilityService = observabilityService;
        _repository = repository;
        _alertEvaluationService = alertEvaluationService;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = _observabilityService.GetSnapshot();

        if (!snapshot.Endpoints.Any())
        {
            return;
        }

        var recordedAt = snapshot.GeneratedAtUtc;

        var records = snapshot.Endpoints
            .Select(e => new ObservabilityMetricRecord(
                e.Endpoint,
                e.RequestCount,
                e.SuccessCount,
                e.ErrorCount,
                e.AverageLatencyMs,
                recordedAt))
            .ToList();

        await _repository.SaveAsync(records, cancellationToken);
        await _alertEvaluationService.EvaluateAndNotifyAsync(snapshot, cancellationToken);
    }
}
