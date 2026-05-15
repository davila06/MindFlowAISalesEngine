namespace Api.Contracts;

public class RuleMetricsResponse
{
    public Guid RuleId { get; init; }
    public int TotalExecutions { get; init; }
    public int MatchedExecutions { get; init; }
    public int AppliedExecutions { get; init; }
    public decimal MatchRatePercent { get; init; }
    public decimal ApplyRatePercent { get; init; }
    public decimal AverageDurationMs { get; init; }
    public DateTime? LastExecutedAtUtc { get; init; }
}
