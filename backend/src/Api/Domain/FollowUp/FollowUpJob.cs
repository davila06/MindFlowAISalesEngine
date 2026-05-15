namespace Api.Domain.FollowUp;

/// <summary>
/// Represents a scheduled follow-up job for a lead.
/// Lifecycle: Scheduled → Sent | Cancelled | Poisoned.
/// </summary>
public class FollowUpJob
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public string? ToEmail { get; private set; }
    public string Status { get; private set; } = FollowUpJobStatus.Scheduled;
    public DateTime ScheduledAtUtc { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public DateTime? ExecutedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public string? CancelReason { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int AttemptNumber { get; private set; }

    private FollowUpJob() { }

    public FollowUpJob(Guid leadId, string? toEmail, DateTime dueAtUtc, int attemptNumber = 1)
    {
        Id = Guid.NewGuid();
        LeadId = leadId;
        ToEmail = toEmail;
        Status = FollowUpJobStatus.Scheduled;
        ScheduledAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc;
        AttemptNumber = attemptNumber;
    }

    public void MarkSent()
    {
        Status = FollowUpJobStatus.Sent;
        ExecutedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = FollowUpJobStatus.Failed;
        ExecutedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void ScheduleRetry(DateTime dueAtUtc, string errorMessage)
    {
        Status = FollowUpJobStatus.Scheduled;
        AttemptNumber += 1;
        ScheduledAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc;
        ExecutedAtUtc = null;
        ErrorMessage = errorMessage;
    }

    public void MarkPoisoned(string errorMessage)
    {
        Status = FollowUpJobStatus.Poisoned;
        ExecutedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void Cancel(string reason)
    {
        if (Status == FollowUpJobStatus.Scheduled)
        {
            Status = FollowUpJobStatus.Cancelled;
            CancelledAtUtc = DateTime.UtcNow;
            CancelReason = reason;
        }
    }

    public bool IsDue(DateTime utcNow) =>
        Status == FollowUpJobStatus.Scheduled && utcNow >= DueAtUtc;

    public bool CanBeCancelled => Status == FollowUpJobStatus.Scheduled;
}
