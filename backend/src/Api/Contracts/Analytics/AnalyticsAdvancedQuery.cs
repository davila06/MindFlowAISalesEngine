namespace Api.Contracts.Analytics;

public class AnalyticsAdvancedQuery
{
    public DateTime? StartDateUtc { get; init; }
    public DateTime? EndDateUtc { get; init; }
    public string GroupBy { get; init; } = "day";
    public string? Stage { get; init; }
    public string? Source { get; init; }
    public string? Tenant { get; init; }
}
