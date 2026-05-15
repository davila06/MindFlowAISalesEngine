using Api.Contracts;

namespace Api.Contracts.Analytics;

public sealed class WeeklyAnalyticsReportResponse
{
    public DateTime GeneratedAtUtc { get; init; }
    public DateTime WindowStartUtc { get; init; }
    public DateTime WindowEndUtc { get; init; }
    public DashboardOverviewResponse DashboardOverview { get; init; } = new();
    public AnalyticsAdvancedOverviewResponse AdvancedOverview { get; init; } = new();
}
