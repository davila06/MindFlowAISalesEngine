namespace Api.Domain.Rules;

public class Rule
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Trigger { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }
    public string ConflictPolicy { get; private set; }
    public int? ExecutionStartHourUtc { get; private set; }
    public int? ExecutionEndHourUtc { get; private set; }
    public int CooldownMinutes { get; private set; }
    public bool AllowDestructiveActions { get; private set; }
    public int Version { get; private set; }
    public string Environment { get; private set; }
    public string ApprovalStatus { get; private set; }
    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public List<RuleCondition> Conditions { get; private set; }
    public List<RuleAction> Actions { get; private set; }

    private Rule()
    {
        Name = string.Empty;
        Trigger = string.Empty;
        ConflictPolicy = "first_wins";
        Environment = "dev";
        ApprovalStatus = "approved";
        Conditions = [];
        Actions = [];
    }

    public Rule(
        string name,
        string trigger,
        bool isActive,
        int priority,
        string conflictPolicy,
        int? executionStartHourUtc,
        int? executionEndHourUtc,
        int cooldownMinutes,
        bool allowDestructiveActions,
        string environment,
        string approvalStatus,
        IEnumerable<RuleCondition> conditions,
        IEnumerable<RuleAction> actions)
    {
        Id = Guid.NewGuid();
        Name = name;
        Trigger = trigger;
        IsActive = isActive;
        Priority = priority;
        ConflictPolicy = conflictPolicy;
        ExecutionStartHourUtc = executionStartHourUtc;
        ExecutionEndHourUtc = executionEndHourUtc;
        CooldownMinutes = cooldownMinutes;
        AllowDestructiveActions = allowDestructiveActions;
        Version = 1;
        Environment = environment;
        ApprovalStatus = approvalStatus;
        ApprovedBy = approvalStatus == "approved" ? "system" : null;
        ApprovedAtUtc = approvalStatus == "approved" ? DateTime.UtcNow : null;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        Conditions = conditions.ToList();
        Actions = actions.ToList();
    }

    public void Update(
        string name,
        string trigger,
        bool isActive,
        int priority,
        string conflictPolicy,
        int? executionStartHourUtc,
        int? executionEndHourUtc,
        int cooldownMinutes,
        bool allowDestructiveActions,
        string environment,
        string approvalStatus,
        IEnumerable<RuleCondition> conditions,
        IEnumerable<RuleAction> actions)
    {
        UpdateMetadata(
            name,
            trigger,
            isActive,
            priority,
            conflictPolicy,
            executionStartHourUtc,
            executionEndHourUtc,
            cooldownMinutes,
            allowDestructiveActions,
            environment,
            approvalStatus);
        ReplaceDefinitionChildren(conditions, actions);
    }

    public void UpdateMetadata(
        string name,
        string trigger,
        bool isActive,
        int priority,
        string conflictPolicy,
        int? executionStartHourUtc,
        int? executionEndHourUtc,
        int cooldownMinutes,
        bool allowDestructiveActions,
        string environment,
        string approvalStatus)
    {
        Name = name;
        Trigger = trigger;
        IsActive = isActive;
        Priority = priority;
        ConflictPolicy = conflictPolicy;
        ExecutionStartHourUtc = executionStartHourUtc;
        ExecutionEndHourUtc = executionEndHourUtc;
        CooldownMinutes = cooldownMinutes;
        AllowDestructiveActions = allowDestructiveActions;
        Environment = environment;
        ApprovalStatus = approvalStatus;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Promote(string targetEnvironment, string approvedBy)
    {
        Environment = targetEnvironment;
        ApprovalStatus = "approved";
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTime.UtcNow;
        Version += 1;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Restore(
        string name,
        string trigger,
        bool isActive,
        int priority,
        string conflictPolicy,
        int? executionStartHourUtc,
        int? executionEndHourUtc,
        int cooldownMinutes,
        bool allowDestructiveActions,
        string environment,
        string approvalStatus,
        string? approvedBy,
        DateTime? approvedAtUtc,
        IEnumerable<RuleCondition> conditions,
        IEnumerable<RuleAction> actions)
    {
        Name = name;
        Trigger = trigger;
        IsActive = isActive;
        Priority = priority;
        ConflictPolicy = conflictPolicy;
        ExecutionStartHourUtc = executionStartHourUtc;
        ExecutionEndHourUtc = executionEndHourUtc;
        CooldownMinutes = cooldownMinutes;
        AllowDestructiveActions = allowDestructiveActions;
        Environment = environment;
        ApprovalStatus = approvalStatus;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = approvedAtUtc;
        ReplaceConditions(conditions);
        ReplaceActions(actions);
        Version += 1;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceDefinitionChildren(IEnumerable<RuleCondition> conditions, IEnumerable<RuleAction> actions)
    {
        ReplaceConditions(conditions);
        ReplaceActions(actions);
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void ReplaceConditions(IEnumerable<RuleCondition> conditions)
    {
        var replacementConditions = conditions.ToList();
        var sharedCount = Math.Min(Conditions.Count, replacementConditions.Count);

        for (var index = 0; index < sharedCount; index++)
        {
            var currentCondition = Conditions[index];
            var replacementCondition = replacementConditions[index];
            currentCondition.Update(replacementCondition.Field, replacementCondition.Operator, replacementCondition.Value);
        }

        if (Conditions.Count > replacementConditions.Count)
        {
            Conditions.RemoveRange(replacementConditions.Count, Conditions.Count - replacementConditions.Count);
        }

        if (replacementConditions.Count > sharedCount)
        {
            Conditions.AddRange(replacementConditions.Skip(sharedCount));
        }
    }

    private void ReplaceActions(IEnumerable<RuleAction> actions)
    {
        var replacementActions = actions.ToList();
        var sharedCount = Math.Min(Actions.Count, replacementActions.Count);

        for (var index = 0; index < sharedCount; index++)
        {
            var currentAction = Actions[index];
            var replacementAction = replacementActions[index];
            currentAction.Update(replacementAction.Type, replacementAction.Value);
        }

        if (Actions.Count > replacementActions.Count)
        {
            Actions.RemoveRange(replacementActions.Count, Actions.Count - replacementActions.Count);
        }

        if (replacementActions.Count > sharedCount)
        {
            Actions.AddRange(replacementActions.Skip(sharedCount));
        }
    }
}
