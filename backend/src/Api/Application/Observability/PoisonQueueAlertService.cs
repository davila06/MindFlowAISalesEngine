using Api.Application.Email;
using Api.Domain.Observability;

namespace Api.Application.Observability;

public sealed class PoisonQueueAlertService : IPoisonQueueAlertService
{
    private const string MetricName = "PoisonQueueDepth";
    private static readonly TimeSpan CooldownWindow = TimeSpan.FromMinutes(20);
    private const decimal SignificantGrowthDelta = 2m;

    private readonly IAlertThresholdRepository _thresholdRepository;
    private readonly IAlertEventRepository _eventRepository;
    private readonly IEmailService _emailService;

    public PoisonQueueAlertService(
        IAlertThresholdRepository thresholdRepository,
        IAlertEventRepository eventRepository,
        IEmailService emailService)
    {
        _thresholdRepository = thresholdRepository;
        _eventRepository = eventRepository;
        _emailService = emailService;
    }

    public async Task NotifyGrowthAsync(string jobType, decimal queueDepth, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobType) || queueDepth <= 0)
        {
            return;
        }

        var endpointName = BuildEndpointName(jobType);
        var thresholds = await _thresholdRepository.GetActiveAsync(cancellationToken);
        var applicable = thresholds
            .Where(x => string.Equals(x.EndpointName, endpointName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (applicable.Count == 0)
        {
            return;
        }

        foreach (var threshold in applicable)
        {
            if (queueDepth <= threshold.MaxAverageLatencyMs)
            {
                continue;
            }

            var latest = await _eventRepository.GetLatestAsync(endpointName, MetricName, cancellationToken);
            if (latest is not null)
            {
                var growthDelta = queueDepth - latest.ObservedValue;
                if (growthDelta <= 0)
                {
                    continue;
                }

                var withinCooldown = DateTime.UtcNow - latest.TriggeredAtUtc <= CooldownWindow;
                if (withinCooldown && growthDelta < SignificantGrowthDelta)
                {
                    continue;
                }
            }

            var alertEvent = new AlertEvent(
                threshold.Id,
                endpointName,
                MetricName,
                queueDepth,
                threshold.MaxAverageLatencyMs,
                DateTime.UtcNow);

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
        }

        await _eventRepository.SaveChangesAsync(cancellationToken);
    }

    private static string BuildEndpointName(string jobType)
    {
        return $"poison-queue/{jobType.Trim().ToLowerInvariant()}";
    }
}