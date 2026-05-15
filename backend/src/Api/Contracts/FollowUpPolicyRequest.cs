using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public sealed class FollowUpPolicyRequest
{
    public bool QuietHoursEnabled { get; init; }

    [Range(0, 23)]
    public int QuietHoursStartHourUtc { get; init; }

    [Range(0, 23)]
    public int QuietHoursEndHourUtc { get; init; }

    public IReadOnlyList<FollowUpPolicyRuleItem> Rules { get; init; } = Array.Empty<FollowUpPolicyRuleItem>();
}

public sealed class FollowUpPolicyRuleItem
{
    [Required, MaxLength(100)]
    public string StageName { get; init; } = "new";

    [Range(0, 100)]
    public int MinimumScore { get; init; }

    [Range(1, 720)]
    public int DelayHours { get; init; }
}