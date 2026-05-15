using Api.Application.AnalyticsAdvanced;

namespace Api.Application.Observability;

public interface IAlertEvaluationService
{
    Task EvaluateAndNotifyAsync(AnalyticsObservabilitySnapshot snapshot, CancellationToken cancellationToken);
}
