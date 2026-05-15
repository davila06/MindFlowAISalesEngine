namespace Api.Application.AnalyticsAdvanced;

public sealed class WeeklyAnalyticsReportOptions
{
    public bool Enabled { get; set; } = true;
    public bool RunOnStartup { get; set; }
    public int IntervalMinutes { get; set; } = 7 * 24 * 60;
}
