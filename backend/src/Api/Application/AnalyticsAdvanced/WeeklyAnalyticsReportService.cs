using Api.Application.Dashboard;
using Api.Application.Security;
using Api.Contracts.Analytics;

namespace Api.Application.AnalyticsAdvanced;

public sealed class WeeklyAnalyticsReportService : IWeeklyAnalyticsReportService
{
    private const int WeeklyWindowDays = 7;

    private readonly IDashboardService _dashboardService;
    private readonly IAnalyticsAdvancedService _analyticsAdvancedService;
    private readonly IAdminAuditService _adminAuditService;

    public WeeklyAnalyticsReportService(
        IDashboardService dashboardService,
        IAnalyticsAdvancedService analyticsAdvancedService,
        IAdminAuditService adminAuditService)
    {
        _dashboardService = dashboardService;
        _analyticsAdvancedService = analyticsAdvancedService;
        _adminAuditService = adminAuditService;
    }

    public async Task<WeeklyAnalyticsReportResponse> GenerateAsync(CancellationToken cancellationToken)
    {
        var windowEndUtc = DateTime.UtcNow;
        var windowStartUtc = windowEndUtc.AddDays(-WeeklyWindowDays);

        var dashboardOverview = await _dashboardService.GetOverviewAsync(WeeklyWindowDays, cancellationToken);
        var advancedOverview = await _analyticsAdvancedService.GetOverviewAsync(
            new AnalyticsAdvancedQuery
            {
                StartDateUtc = windowStartUtc,
                EndDateUtc = windowEndUtc,
                GroupBy = "day"
            },
            cancellationToken);

        var response = new WeeklyAnalyticsReportResponse
        {
            GeneratedAtUtc = windowEndUtc,
            WindowStartUtc = windowStartUtc,
            WindowEndUtc = windowEndUtc,
            DashboardOverview = dashboardOverview,
            AdvancedOverview = advancedOverview
        };

        await _adminAuditService.RecordAsync(
            "analytics_weekly_report_generated",
            "analytics/advanced/reports/weekly",
            BuildAuditDetails(response),
            cancellationToken);

        return response;
    }

    private static string BuildAuditDetails(WeeklyAnalyticsReportResponse report)
    {
        return string.Join(';',
            $"WindowStartUtc={report.WindowStartUtc:O}",
            $"WindowEndUtc={report.WindowEndUtc:O}",
            $"TotalLeads={report.DashboardOverview.TotalLeads}",
            $"ConversionRate={report.DashboardOverview.ConversionRate}",
            $"PipelineValue={report.DashboardOverview.PipelineValue}",
            $"WonRevenue={report.AdvancedOverview.Revenue.WonRevenue}",
            $"WonCount={report.AdvancedOverview.Funnel.WonCount}");
    }
}
