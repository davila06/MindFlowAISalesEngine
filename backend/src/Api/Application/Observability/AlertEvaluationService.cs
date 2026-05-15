using Api.Application.AnalyticsAdvanced;
using Api.Application.Email;
using Api.Domain.Observability;

namespace Api.Application.Observability;

public sealed class AlertEvaluationService : IAlertEvaluationService
{
    private static readonly TimeSpan CooldownWindow = TimeSpan.FromMinutes(20);
    private const decimal SignificantGrowthDelta = 2m;
    private readonly IAlertThresholdRepository _thresholdRepository;
    private readonly IAlertEventRepository _eventRepository;
    private readonly IEmailService _emailService;
    private static readonly HttpClient WebhookHttpClient = new();

    public AlertEvaluationService(
        IAlertThresholdRepository thresholdRepository,
        IAlertEventRepository eventRepository,
        IEmailService emailService)
    {
        _thresholdRepository = thresholdRepository;
        _eventRepository = eventRepository;
        _emailService = emailService;
    }

    public async Task EvaluateAndNotifyAsync(AnalyticsObservabilitySnapshot snapshot, CancellationToken cancellationToken)
    {
        var activeThresholds = await _thresholdRepository.GetActiveAsync(cancellationToken);
        if (!activeThresholds.Any())
        {
            return;
        }

        var endpointMetrics = snapshot.Endpoints.ToDictionary(x => x.Endpoint, StringComparer.OrdinalIgnoreCase);

        foreach (var threshold in activeThresholds)
        {
            if (!endpointMetrics.TryGetValue(threshold.EndpointName, out var metric))
            {
                continue;
            }

            var events = BuildEvents(threshold, metric, snapshot.GeneratedAtUtc);
            foreach (var alertEvent in events)
            {
                var latest = await _eventRepository.GetLatestAsync(
                    alertEvent.EndpointName,
                    alertEvent.MetricName,
                    cancellationToken);

                if (latest is not null)
                {
                    var growthDelta = alertEvent.ObservedValue - latest.ObservedValue;
                    if (growthDelta <= 0)
                    {
                        continue;
                    }

                    var withinCooldown = alertEvent.TriggeredAtUtc - latest.TriggeredAtUtc <= CooldownWindow;
                    if (withinCooldown && growthDelta < SignificantGrowthDelta)
                    {
                        continue;
                    }
                }

                await _eventRepository.AddAsync(alertEvent, cancellationToken);

                var sent = await _emailService.SendAnalyticsDegradationAlertAsync(
                    threshold.NotificationEmail,
                    alertEvent.EndpointName,
                    alertEvent.MetricName,
                    alertEvent.ObservedValue,
                    alertEvent.ThresholdValue,
                    alertEvent.TriggeredAtUtc,
                    cancellationToken);

                if (sent)
                {
                    alertEvent.MarkNotificationSent();
                }

                if (!string.IsNullOrWhiteSpace(threshold.WebhookUrl))
                {
                    await TrySendWebhookAsync(threshold.WebhookUrl, alertEvent, cancellationToken);
                }
            }
        }

        await _eventRepository.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<AlertEvent> BuildEvents(
        AlertThreshold threshold,
        AnalyticsEndpointMetricSnapshot metric,
        DateTime triggeredAtUtc)
    {
        var events = new List<AlertEvent>();

        var errorRatePercent = metric.RequestCount == 0
            ? 0m
            : Math.Round((decimal)metric.ErrorCount * 100m / metric.RequestCount, 2);

        if (errorRatePercent > threshold.MaxErrorRatePercent)
        {
            events.Add(new AlertEvent(
                threshold.Id,
                threshold.EndpointName,
                "ErrorRatePercent",
                errorRatePercent,
                threshold.MaxErrorRatePercent,
                triggeredAtUtc));
        }

        if (metric.AverageLatencyMs > threshold.MaxAverageLatencyMs)
        {
            events.Add(new AlertEvent(
                threshold.Id,
                threshold.EndpointName,
                "AverageLatencyMs",
                metric.AverageLatencyMs,
                threshold.MaxAverageLatencyMs,
                triggeredAtUtc));
        }

        return events;
    }

    private static async Task TrySendWebhookAsync(string webhookUrl, AlertEvent alertEvent, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                alertEvent.Id,
                alertEvent.ThresholdId,
                alertEvent.EndpointName,
                alertEvent.MetricName,
                alertEvent.ObservedValue,
                alertEvent.ThresholdValue,
                alertEvent.TriggeredAtUtc
            };

            using var response = await WebhookHttpClient.PostAsJsonAsync(webhookUrl, payload, cancellationToken);
            _ = response.IsSuccessStatusCode;
        }
        catch
        {
            // Webhook failures are non-blocking; alerts remain persisted and email notification still applies.
        }
    }
}
