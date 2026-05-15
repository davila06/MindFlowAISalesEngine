using Api.Contracts;
using Api.Contracts.Analytics;

namespace Api.Application.AnalyticsAdvanced;

public interface IAnalyticsCsvExportService
{
    string ExportDashboardOverviewCsv(DashboardOverviewResponse response);
    string ExportAdvancedOverviewCsv(AnalyticsAdvancedOverviewResponse response);
}
