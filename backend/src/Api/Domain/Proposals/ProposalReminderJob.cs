namespace Api.Domain.Proposals;

public class ProposalReminderJob
{
    public Guid Id { get; private set; }
    public Guid ProposalId { get; private set; }
    public Guid LeadId { get; private set; }
    public string? ToEmail { get; private set; }
    public string Status { get; private set; } = ProposalReminderStatus.Scheduled;
    public int AttemptNumber { get; private set; }
    public DateTime ScheduledAtUtc { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public DateTime? ExecutedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }

    private ProposalReminderJob() { }

    public ProposalReminderJob(Guid proposalId, Guid leadId, string? toEmail, DateTime dueAtUtc, int attemptNumber = 1)
    {
        Id = Guid.NewGuid();
        ProposalId = proposalId;
        LeadId = leadId;
        ToEmail = toEmail;
        Status = ProposalReminderStatus.Scheduled;
        AttemptNumber = attemptNumber;
        ScheduledAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc;
    }

    public bool IsDue(DateTime utcNow) =>
        Status == ProposalReminderStatus.Scheduled && utcNow >= DueAtUtc;

    public void MarkSent()
    {
        Status = ProposalReminderStatus.Sent;
        ExecutedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = ProposalReminderStatus.Failed;
        ExecutedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void ScheduleRetry(DateTime dueAtUtc, string errorMessage)
    {
        Status = ProposalReminderStatus.Scheduled;
        AttemptNumber += 1;
        ScheduledAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc;
        ExecutedAtUtc = null;
        ErrorMessage = errorMessage;
    }

    public void MarkPoisoned(string errorMessage)
    {
        Status = ProposalReminderStatus.Poisoned;
        ExecutedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void ForceDue()
    {
        DueAtUtc = DateTime.UtcNow.AddSeconds(-1);
    }

    public void Requeue(DateTime dueAtUtc)
    {
        Status = ProposalReminderStatus.Scheduled;
        AttemptNumber += 1;
        ScheduledAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc;
        ExecutedAtUtc = null;
        ErrorMessage = null;
    }
}
