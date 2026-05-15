namespace Api.Domain.Rules;

public class RuleExecutionLog
{
    public Guid Id { get; private set; }
    public Guid RuleId { get; private set; }
    public string Trigger { get; private set; }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public bool Matched { get; private set; }
    public bool Applied { get; private set; }
    public int ActionsAppliedCount { get; private set; }
    public string? SkippedReason { get; private set; }
    public decimal DurationMs { get; private set; }
    public DateTime ExecutedAtUtc { get; private set; }

    private RuleExecutionLog()
    {
        Trigger = string.Empty;
        EntityType = string.Empty;
    }

    public RuleExecutionLog(
        Guid ruleId,
        string trigger,
        string entityType,
        Guid entityId,
        bool matched,
        bool applied,
        int actionsAppliedCount,
        string? skippedReason,
        decimal durationMs)
    {
        Id = Guid.NewGuid();
        RuleId = ruleId;
        Trigger = trigger;
        EntityType = entityType;
        EntityId = entityId;
        Matched = matched;
        Applied = applied;
        ActionsAppliedCount = actionsAppliedCount;
        SkippedReason = skippedReason;
        DurationMs = durationMs;
        ExecutedAtUtc = DateTime.UtcNow;
    }
}
