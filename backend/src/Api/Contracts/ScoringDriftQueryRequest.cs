namespace Api.Contracts;

public class ScoringDriftQueryRequest
{
    public int CurrentSampleSize { get; init; } = 30;
    public int BaselineSampleSize { get; init; } = 30;
    public decimal DriftThresholdPercent { get; init; } = 20m;
}