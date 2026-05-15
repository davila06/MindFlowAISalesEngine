namespace Api.Domain.FollowUp;

public sealed class FollowUpPolicyRule
{
    public string StageName { get; init; } = "new";
    public int MinimumScore { get; init; }
    public int DelayHours { get; init; }
}