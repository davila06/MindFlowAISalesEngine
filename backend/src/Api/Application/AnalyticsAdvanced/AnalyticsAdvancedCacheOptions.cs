namespace Api.Application.AnalyticsAdvanced;

public sealed class AnalyticsAdvancedCacheOptions
{
    public bool Enabled { get; init; } = true;
    public int SnapshotTtlSeconds { get; init; } = 60;
}
