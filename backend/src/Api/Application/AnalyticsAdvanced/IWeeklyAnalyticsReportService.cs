using Api.Contracts.Analytics;

namespace Api.Application.AnalyticsAdvanced;

public interface IWeeklyAnalyticsReportService
{
    Task<WeeklyAnalyticsReportResponse> GenerateAsync(CancellationToken cancellationToken);
}
