namespace Api.Contracts.Analytics;

public sealed class PeriodOverPeriodComparisonResponse
{
    public DateTime CurrentStartUtc { get; init; }
    public DateTime CurrentEndUtc { get; init; }
    public DateTime PreviousStartUtc { get; init; }
    public DateTime PreviousEndUtc { get; init; }
    public AnalyticsAdvancedOverviewResponse Current { get; init; } = new();
    public AnalyticsAdvancedOverviewResponse Previous { get; init; } = new();
    public PeriodOverPeriodDeltaResponse Delta { get; init; } = new();
}

public sealed class PeriodOverPeriodDeltaResponse
{
    public int WonCountDelta { get; init; }
    public decimal WonRevenueDelta { get; init; }
    public decimal PipelineRevenueDelta { get; init; }
    public decimal ProposalToWonRateDelta { get; init; }
}