namespace Api.Domain.Onboarding;

public class OnboardingWelcomeJob
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid LeadId { get; private set; }
    public string? ToEmail { get; private set; }
    public string Status { get; private set; } = OnboardingWelcomeJobStatus.Scheduled;
    public int AttemptNumber { get; private set; }
    public DateTime ScheduledAtUtc { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public DateTime? ExecutedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }

    private OnboardingWelcomeJob() { }

    public OnboardingWelcomeJob(Guid customerId, Guid leadId, string? toEmail, DateTime dueAtUtc, int attemptNumber = 1)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        LeadId = leadId;
        ToEmail = toEmail;
        Status = OnboardingWelcomeJobStatus.Scheduled;
        AttemptNumber = attemptNumber;
        ScheduledAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc;
    }

    public bool IsDue(DateTime utcNow) =>
        Status == OnboardingWelcomeJobStatus.Scheduled && utcNow >= DueAtUtc;

    public void MarkSent()
    {
        Status = OnboardingWelcomeJobStatus.Sent;
        ExecutedAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = OnboardingWelcomeJobStatus.Failed;
        ExecutedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void MarkPoisoned(string errorMessage)
    {
        Status = OnboardingWelcomeJobStatus.Poisoned;
        ExecutedAtUtc = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    public void ScheduleRetry(DateTime dueAtUtc, string errorMessage)
    {
        Status = OnboardingWelcomeJobStatus.Scheduled;
        AttemptNumber += 1;
        ScheduledAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc;
        ExecutedAtUtc = null;
        ErrorMessage = errorMessage;
    }

    public void ForceDue()
    {
        DueAtUtc = DateTime.UtcNow.AddSeconds(-1);
    }

    public void Requeue(DateTime dueAtUtc)
    {
        Status = OnboardingWelcomeJobStatus.Scheduled;
        AttemptNumber += 1;
        ScheduledAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc;
        ExecutedAtUtc = null;
        ErrorMessage = null;
    }
}