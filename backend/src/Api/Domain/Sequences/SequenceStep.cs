namespace Api.Domain.Sequences;

/// <summary>A single step in a sequence: action to take after a delay.</summary>
public class SequenceStep
{
    public Guid Id { get; private set; }
    public Guid SequenceId { get; private set; }
    /// <summary>1-based order within the sequence.</summary>
    public int Order { get; private set; }
    /// <summary>send_email | add_note | add_tag</summary>
    public string ActionType { get; private set; } = string.Empty;
    /// <summary>Template name for send_email; note text for add_note; tag value for add_tag.</summary>
    public string ActionValue { get; private set; } = string.Empty;
    /// <summary>Days to wait after the previous step (or enrollment) before executing.</summary>
    public int DelayDays { get; private set; }

    private SequenceStep() { }

    public static SequenceStep Create(Guid sequenceId, int order, string actionType, string actionValue, int delayDays)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionType);
        if (order < 1) throw new ArgumentOutOfRangeException(nameof(order), "Step order must be >= 1.");
        if (delayDays < 0) throw new ArgumentOutOfRangeException(nameof(delayDays), "Delay days must be >= 0.");

        return new SequenceStep
        {
            Id = Guid.NewGuid(),
            SequenceId = sequenceId,
            Order = order,
            ActionType = actionType,
            ActionValue = actionValue,
            DelayDays = delayDays
        };
    }
}
