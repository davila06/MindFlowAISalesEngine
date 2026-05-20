namespace Api.Domain.Sequences;

/// <summary>
/// Tracks one lead's progress through a sequence.
/// Status lifecycle: active → completed | exited
/// </summary>
public class SequenceEnrollment
{
    public static class Statuses
    {
        public const string Active    = "active";
        public const string Completed = "completed";
        public const string Exited    = "exited";
        public const string Paused    = "paused";
    }

    public static class ExitReasons
    {
        public const string Replied      = "replied";
        public const string StageChanged = "stage_changed";
        public const string Unsubscribed = "unsubscribed";
        public const string Manual       = "manual";
        public const string Completed    = "completed";
    }

    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public Guid SequenceId { get; private set; }
    public string Status { get; private set; } = Statuses.Active;
    /// <summary>The step order that will be executed next (0 = not started yet).</summary>
    public int NextStepOrder { get; private set; }
    /// <summary>When the next step becomes eligible for execution.</summary>
    public DateTime NextStepDueAtUtc { get; private set; }
    public DateTime EnrolledAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? ExitedAtUtc { get; private set; }
    public string? ExitReason { get; private set; }

    private SequenceEnrollment() { }

    public static SequenceEnrollment Create(Guid leadId, Guid sequenceId)
    {
        return new SequenceEnrollment
        {
            Id = Guid.NewGuid(),
            LeadId = leadId,
            SequenceId = sequenceId,
            Status = Statuses.Active,
            NextStepOrder = 1,
            NextStepDueAtUtc = DateTime.UtcNow,
            EnrolledAtUtc = DateTime.UtcNow
        };
    }

    public void AdvanceToNextStep(int nextOrder, int delayDays)
    {
        NextStepOrder = nextOrder;
        NextStepDueAtUtc = DateTime.UtcNow.AddDays(delayDays);
    }

    public void Complete()
    {
        Status = Statuses.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        ExitReason = ExitReasons.Completed;
    }

    public void Exit(string reason)
    {
        Status = Statuses.Exited;
        ExitedAtUtc = DateTime.UtcNow;
        ExitReason = reason;
    }

    public void Pause() => Status = Statuses.Paused;
    public void Resume()
    {
        if (Status == Statuses.Paused)
            Status = Statuses.Active;
    }
}
