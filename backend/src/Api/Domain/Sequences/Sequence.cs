namespace Api.Domain.Sequences;

/// <summary>Sales sequence (cadence) definition containing ordered steps.</summary>
public class Sequence
{
    public static class StepActionTypes
    {
        public const string SendEmail   = "send_email";
        public const string AddNote     = "add_note";
        public const string AddTag      = "add_tag";
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private readonly List<SequenceStep> _steps = [];
    public IReadOnlyList<SequenceStep> Steps => _steps.AsReadOnly();

    private Sequence() { }

    public static Sequence Create(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Sequence
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(string name, string? description, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = description?.Trim();
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public SequenceStep AddStep(int order, string actionType, string actionValue, int delayDays)
    {
        var step = SequenceStep.Create(Id, order, actionType, actionValue, delayDays);
        _steps.Add(step);
        UpdatedAtUtc = DateTime.UtcNow;
        return step;
    }

    public void SetSteps(IEnumerable<SequenceStep> steps)
    {
        _steps.Clear();
        _steps.AddRange(steps);
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
