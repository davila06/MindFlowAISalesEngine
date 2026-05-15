using Api.Application.Email;
using Api.Application.Common.Interfaces;
using Api.Application.Observability;
using Api.Domain.FollowUp;
using Microsoft.Extensions.Logging;

namespace Api.Application.FollowUp;

public class FollowUpService : IFollowUpService
{
    private static readonly TimeSpan FollowUpDelay = TimeSpan.FromHours(48);
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMinutes(15);
    private const int MaxRetryAttempts = 3;

    private readonly IFollowUpJobRepository _repository;
    private readonly IFollowUpPolicyRepository _policyRepository;
    private readonly IEmailStopListRepository _emailStopListRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IEmailService _emailService;
    private readonly IPoisonQueueAlertService _poisonQueueAlertService;
    private readonly ILogger<FollowUpService> _logger;

    public FollowUpService(
        IFollowUpJobRepository repository,
        IFollowUpPolicyRepository policyRepository,
        IEmailStopListRepository emailStopListRepository,
        ILeadRepository leadRepository,
        IEmailService emailService,
        IPoisonQueueAlertService poisonQueueAlertService,
        ILogger<FollowUpService> logger)
    {
        _repository = repository;
        _policyRepository = policyRepository;
        _emailStopListRepository = emailStopListRepository;
        _leadRepository = leadRepository;
        _emailService = emailService;
        _poisonQueueAlertService = poisonQueueAlertService;
        _logger = logger;
    }

    public async Task ScheduleAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(toEmail)
            && await _emailStopListRepository.ExistsAsync(toEmail, cancellationToken))
        {
            _logger.LogInformation("Follow-up scheduling skipped for suppressed recipient {ToEmail}", toEmail);
            return;
        }

        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        var dueAt = DateTime.UtcNow.Add(FollowUpDelay);
        var policy = await _policyRepository.GetAsync(cancellationToken);
        if (lead is not null && policy is not null)
        {
            var rule = policy.GetRules()
                .Where(x => string.Equals(x.StageName, "new", StringComparison.OrdinalIgnoreCase) && lead.Score >= x.MinimumScore)
                .OrderByDescending(x => x.MinimumScore)
                .FirstOrDefault();

            if (rule is not null)
            {
                dueAt = DateTime.UtcNow.AddHours(rule.DelayHours);
            }

            dueAt = ApplyQuietHours(dueAt, policy);
        }

        var job = new FollowUpJob(leadId, toEmail, dueAt, attemptNumber: 1);

        await _repository.AddAsync(job, cancellationToken);

        _logger.LogInformation(
            "Follow-up job {JobId} scheduled for lead {LeadId}, due at {DueAt:O}",
            job.Id,
            leadId,
            dueAt);
    }

    public async Task CancelByLeadAsync(Guid leadId, string reason, CancellationToken cancellationToken)
    {
        var jobs = await _repository.GetByLeadIdAsync(leadId, cancellationToken);

        var cancelled = 0;
        foreach (var job in jobs.Where(j => j.CanBeCancelled))
        {
            job.Cancel(reason);
            cancelled++;
        }

        if (cancelled > 0)
        {
            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Cancelled {Count} follow-up job(s) for lead {LeadId}. Reason: {Reason}",
                cancelled, leadId, reason);
        }
    }

    public async Task CancelAsync(Guid jobId, string reason, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(jobId, cancellationToken);

        if (job is null)
        {
            _logger.LogWarning("Follow-up job {JobId} not found for cancellation", jobId);
            return;
        }

        job.Cancel(reason);
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Follow-up job {JobId} cancelled. Reason: {Reason}",
            jobId, reason);
    }

    public async Task RequeueAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Follow-up job {JobId} not found for requeue", jobId);
            return;
        }

        if (!string.Equals(job.Status, FollowUpJobStatus.Failed, StringComparison.Ordinal)
            && !string.Equals(job.Status, FollowUpJobStatus.Poisoned, StringComparison.Ordinal))
        {
            _logger.LogWarning("Follow-up job {JobId} is not in dead-letter state", jobId);
            return;
        }

        var retry = new FollowUpJob(job.LeadId, job.ToEmail, DateTime.UtcNow, job.AttemptNumber + 1);
        await _repository.AddAsync(retry, cancellationToken);

        _logger.LogInformation(
            "Follow-up job {JobId} requeued as {RetryJobId} with attempt {AttemptNumber}",
            jobId,
            retry.Id,
            retry.AttemptNumber);
    }

    public async Task ExecuteDueJobsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dueJobs = await _repository.GetScheduledDueAsync(now, cancellationToken);

        if (dueJobs.Count == 0)
        {
            _logger.LogDebug("No due follow-up jobs at {UtcNow:O}", now);
            return;
        }

        _logger.LogInformation("Executing {Count} due follow-up job(s)", dueJobs.Count);

        foreach (var job in dueJobs)
        {
            try
            {
                await _emailService.SendLeadFollowUpAsync(job.LeadId, job.ToEmail, cancellationToken);
                job.MarkSent();

                _logger.LogInformation(
                    "Follow-up job {JobId} executed for lead {LeadId} → email sent to {ToEmail}",
                    job.Id, job.LeadId, job.ToEmail);
            }
            catch (Exception ex)
            {
                if (job.AttemptNumber >= MaxRetryAttempts)
                {
                    job.MarkPoisoned(ex.Message);

                    var queueDepth = await _repository.CountPoisonedAsync(cancellationToken) + 1;
                    await _poisonQueueAlertService.NotifyGrowthAsync("followup", queueDepth, cancellationToken);

                    _logger.LogError(ex,
                        "Follow-up job {JobId} moved to poison queue for lead {LeadId} after attempt {AttemptNumber}",
                        job.Id, job.LeadId, job.AttemptNumber);
                }
                else
                {
                    var nextAttempt = job.AttemptNumber + 1;
                    var nextDueAt = now.Add(GetRetryDelay(nextAttempt));
                    job.ScheduleRetry(nextDueAt, ex.Message);

                    _logger.LogWarning(ex,
                        "Follow-up job {JobId} scheduled retry attempt {AttemptNumber} for lead {LeadId} at {DueAt:O}",
                        job.Id, nextAttempt, job.LeadId, nextDueAt);
                }
            }

            await _repository.SaveChangesAsync(cancellationToken);
        }
    }

    private static TimeSpan GetRetryDelay(int attemptNumber)
    {
        return TimeSpan.FromMinutes(RetryBaseDelay.TotalMinutes * attemptNumber);
    }

    private static DateTime ApplyQuietHours(DateTime dueAtUtc, FollowUpPolicySettings policy)
    {
        if (!policy.QuietHoursEnabled)
        {
            return dueAtUtc;
        }

        var hour = dueAtUtc.Hour;
        var start = policy.QuietHoursStartHourUtc;
        var end = policy.QuietHoursEndHourUtc;

        var isWithin = start < end
            ? hour >= start && hour < end
            : hour >= start || hour < end;

        if (!isWithin)
        {
            return dueAtUtc;
        }

        var adjusted = new DateTime(
            dueAtUtc.Year,
            dueAtUtc.Month,
            dueAtUtc.Day,
            end,
            dueAtUtc.Minute,
            dueAtUtc.Second,
            dueAtUtc.Kind);

        if (adjusted <= dueAtUtc)
        {
            adjusted = adjusted.AddDays(1);
        }

        return adjusted;
    }
}
