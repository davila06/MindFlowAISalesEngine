namespace Api.Contracts;

public class RuleDryRunResponse
{
    public Guid RuleId { get; init; }
    public string Trigger { get; init; } = string.Empty;
    public int TotalEvaluated { get; init; }
    public int MatchedCount { get; init; }
    public int AppliedCount { get; init; }
    public List<Guid> SampleEntityIds { get; init; } = [];
    public string? Notes { get; init; }
}
