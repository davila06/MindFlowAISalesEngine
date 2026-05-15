namespace Api.Contracts;

public sealed class FollowUpPolicyResponse
{
    public bool QuietHoursEnabled { get; init; }
    public int QuietHoursStartHourUtc { get; init; }
    public int QuietHoursEndHourUtc { get; init; }
    public IReadOnlyList<FollowUpPolicyRuleItem> Rules { get; init; } = Array.Empty<FollowUpPolicyRuleItem>();
}