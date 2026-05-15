namespace Api.Contracts;

public class ScoringDriftResponse
{
    public int CurrentSampleCount { get; init; }
    public int BaselineSampleCount { get; init; }
    public decimal CurrentAverageScore { get; init; }
    public decimal BaselineAverageScore { get; init; }
    public decimal AverageScoreDeltaPercent { get; init; }
    public decimal CurrentHighPriorityRatePercent { get; init; }
    public decimal BaselineHighPriorityRatePercent { get; init; }
    public decimal HighPriorityRateDeltaPercent { get; init; }
    public decimal DriftThresholdPercent { get; init; }
    public bool HasDrift { get; init; }
    public IReadOnlyList<string> DriftSignals { get; init; } = [];
}