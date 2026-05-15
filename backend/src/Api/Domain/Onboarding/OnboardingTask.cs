namespace Api.Domain.Onboarding;

public class OnboardingTask
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string DependencyKeysSerialized { get; private set; } = string.Empty;
    public string Status { get; private set; } = OnboardingTaskStatus.Pending;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DueAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public IReadOnlyList<string> DependencyKeys => string.IsNullOrWhiteSpace(DependencyKeysSerialized)
        ? []
        : DependencyKeysSerialized.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private OnboardingTask() { }

    public OnboardingTask(Guid customerId, string key, string title, IEnumerable<string>? dependencyKeys = null, DateTime? dueAtUtc = null)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Key = key;
        Title = title;
        DependencyKeysSerialized = dependencyKeys is null ? string.Empty : string.Join('|', dependencyKeys);
        Status = OnboardingTaskStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc;
    }

    public void MarkCompleted()
    {
        Status = OnboardingTaskStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public bool HasPendingDependencies(IEnumerable<string> completedKeys)
    {
        var completed = completedKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return DependencyKeys.Any(x => !completed.Contains(x));
    }

    public bool IsOverdue(DateTime utcNow)
    {
        return Status != OnboardingTaskStatus.Completed && DueAtUtc.HasValue && DueAtUtc.Value < utcNow;
    }
}
