namespace Api.Contracts;

public class RuleResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Trigger { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int Priority { get; init; }
    public string ConflictPolicy { get; init; } = string.Empty;
    public int? ExecutionStartHourUtc { get; init; }
    public int? ExecutionEndHourUtc { get; init; }
    public int CooldownMinutes { get; init; }
    public bool AllowDestructiveActions { get; init; }
    public int Version { get; init; }
    public string Environment { get; init; } = string.Empty;
    public string ApprovalStatus { get; init; } = string.Empty;
    public string? ApprovedBy { get; init; }
    public DateTime? ApprovedAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public List<RuleConditionResponse> Conditions { get; init; } = [];
    public List<RuleActionResponse> Actions { get; init; } = [];
}
