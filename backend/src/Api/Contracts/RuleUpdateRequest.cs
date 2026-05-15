using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class RuleUpdateRequest
{
    [Required]
    [MaxLength(160)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Trigger { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;

    [Range(1, 1000)]
    public int Priority { get; init; } = 100;

    [MaxLength(20)]
    public string ConflictPolicy { get; init; } = "first_wins";

    [Range(0, 23)]
    public int? ExecutionStartHourUtc { get; init; }

    [Range(0, 23)]
    public int? ExecutionEndHourUtc { get; init; }

    [Range(0, 10080)]
    public int CooldownMinutes { get; init; }

    public bool AllowDestructiveActions { get; init; }

    [MaxLength(12)]
    public string Environment { get; init; } = "dev";

    [MaxLength(20)]
    public string ApprovalStatus { get; init; } = "approved";

    [MinLength(1)]
    public List<RuleConditionRequest> Conditions { get; init; } = [];

    [MinLength(1)]
    public List<RuleActionRequest> Actions { get; init; } = [];
}
