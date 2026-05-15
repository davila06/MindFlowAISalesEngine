using Api.Application.Observability;
using Api.Domain.Observability;

namespace Api.Application.Email;

public sealed class EmailDispatchService : IEmailDispatchService
{
    private const int MaxAutoRetryAttempts = 3;
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMinutes(2);

    private readonly IEmailDispatchJobRepository _dispatchJobRepository;
    private readonly IEmailLogRepository _emailLogRepository;
    private readonly ISmtpSettingsRepository _smtpSettingsRepository;
    private readonly IEmailSender _emailSender;
    private readonly IAlertThresholdRepository _alertThresholdRepository;
    private readonly IAlertEventRepository _alertEventRepository;

    public EmailDispatchService(
        IEmailDispatchJobRepository dispatchJobRepository,
        IEmailLogRepository emailLogRepository,
        ISmtpSettingsRepository smtpSettingsRepository,
        IEmailSender emailSender,
        IAlertThresholdRepository alertThresholdRepository,
        IAlertEventRepository alertEventRepository)
    {
        _dispatchJobRepository = dispatchJobRepository;
        _emailLogRepository = emailLogRepository;
        _smtpSettingsRepository = smtpSettingsRepository;
        _emailSender = emailSender;
        _alertThresholdRepository = alertThresholdRepository;
        _alertEventRepository = alertEventRepository;
    }

    public async Task<int> ExecuteDueAsync(CancellationToken cancellationToken)
    {
        var settings = await _smtpSettingsRepository.GetActiveAsync(cancellationToken);
        if (settings is null)
        {
            return 0;
        }

        var jobs = await _dispatchJobRepository.GetDueAsync(DateTime.UtcNow, cancellationToken);
        var executed = 0;

        foreach (var job in jobs)
        {
            var log = string.IsNullOrWhiteSpace(job.CorrelationId)
                ? null
                : await _emailLogRepository.GetByCorrelationIdAsync(job.CorrelationId, cancellationToken);

            try
            {
                if (string.Equals(settings.ProviderType, Api.Domain.Email.SmtpSettings.WebhookProviderType, StringComparison.OrdinalIgnoreCase))
                {
                    job.MarkSent();
                    log?.UpdateDelivery("Sent", true, null);
                }
                else
                {
                    await _emailSender.SendAsync(
                        settings.Host,
                        settings.Port,
                        settings.Username,
                        settings.Password,
                        settings.EnableSsl,
                        settings.FromEmail,
                        settings.FromName ?? "MindFlow",
                        job.ToEmail ?? string.Empty,
                        job.Subject,
                        job.BodyHtml,
                        job.AttachmentBytes,
                        job.AttachmentFileName,
                        job.AttachmentContentType,
                        cancellationToken);

                    job.MarkSent();
                    log?.UpdateDelivery("Sent", true, null);
                }
            }
            catch (Exception ex)
            {
                job.MarkFailed(ex.Message);
                log?.UpdateDelivery("Failed", false, ex.Message);

                if (job.AttemptCount < MaxAutoRetryAttempts)
                {
                    var dueAtUtc = DateTime.UtcNow.Add(GetRetryDelay(job.AttemptCount));
                    job.Requeue(dueAtUtc);
                }
            }

            executed++;
        }

        await _dispatchJobRepository.SaveChangesAsync(cancellationToken);
        await _emailLogRepository.SaveChangesAsync(cancellationToken);
        await EvaluateAlertsAsync(cancellationToken);
        return executed;
    }

    public async Task RetryAsync(Guid emailLogId, CancellationToken cancellationToken)
    {
        var log = await _emailLogRepository.GetByIdAsync(emailLogId, cancellationToken);
        if (log is null || string.IsNullOrWhiteSpace(log.CorrelationId))
        {
            return;
        }

        var job = await _dispatchJobRepository.GetByCorrelationIdAsync(log.CorrelationId, cancellationToken);
        if (job is null)
        {
            return;
        }

        job.Requeue(DateTime.UtcNow);
        await _dispatchJobRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmailKpiSnapshot> GetKpisAsync(CancellationToken cancellationToken)
    {
        var logs = await _emailLogRepository.GetAllAsync(cancellationToken);
        var jobs = await _dispatchJobRepository.GetAllAsync(cancellationToken);
        var total = logs.Count;
        var sent = logs.Count(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
        var failed = logs.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));
        var queued = logs.Count(x => string.Equals(x.Status, "Queued", StringComparison.OrdinalIgnoreCase));
        var bounced = failed;

        var byChannel = jobs
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ProviderType)
                ? Api.Domain.Email.SmtpSettings.SmtpProviderType
                : x.ProviderType,
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var provider = group.Key.ToLowerInvariant();
                var channelTotal = group.Count();
                var channelQueued = group.Count(x => string.Equals(x.Status, "Queued", StringComparison.OrdinalIgnoreCase));
                var channelSent = group.Count(x => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
                var channelFailed = group.Count(x => string.Equals(x.Status, "Failed", StringComparison.OrdinalIgnoreCase));

                return new EmailChannelKpiSnapshot
                {
                    Channel = provider,
                    TotalCount = channelTotal,
                    SentCount = channelSent,
                    FailedCount = channelFailed,
                    QueuedCount = channelQueued,
                    BouncedCount = channelFailed,
                    ErrorRatePercent = channelTotal == 0
                        ? 0
                        : decimal.Round((channelFailed * 100m) / channelTotal, 2)
                };
            })
            .OrderBy(x => x.Channel)
            .ToArray();

        return new EmailKpiSnapshot
        {
            TotalCount = total,
            SentCount = sent,
            FailedCount = failed,
            QueuedCount = queued,
            BouncedCount = bounced,
            ByChannel = byChannel,
            ErrorRatePercent = total == 0 ? 0 : decimal.Round((failed * 100m) / total, 2)
        };
    }

    private static TimeSpan GetRetryDelay(int attemptNumber)
    {
        var boundedAttempt = Math.Max(1, attemptNumber);
        var multiplier = Math.Pow(2, boundedAttempt - 1);
        return TimeSpan.FromMinutes(RetryBaseDelay.TotalMinutes * multiplier);
    }

    private async Task EvaluateAlertsAsync(CancellationToken cancellationToken)
    {
        var kpis = await GetKpisAsync(cancellationToken);
        var thresholds = await _alertThresholdRepository.GetActiveAsync(cancellationToken);
        foreach (var threshold in thresholds.Where(x => string.Equals(x.EndpointName, "email.delivery", StringComparison.OrdinalIgnoreCase)))
        {
            if (kpis.ErrorRatePercent <= threshold.MaxErrorRatePercent)
            {
                continue;
            }

            await _alertEventRepository.AddAsync(
                new AlertEvent(
                    threshold.Id,
                    threshold.EndpointName,
                    "ErrorRatePercent",
                    kpis.ErrorRatePercent,
                    threshold.MaxErrorRatePercent,
                    DateTime.UtcNow),
                cancellationToken);
        }

        await _alertEventRepository.SaveChangesAsync(cancellationToken);
    }
}