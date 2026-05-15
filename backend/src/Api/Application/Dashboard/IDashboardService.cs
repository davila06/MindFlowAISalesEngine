using Api.Contracts;

namespace Api.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardOverviewResponse> GetOverviewAsync(int days, CancellationToken cancellationToken);
    Task<DataQualityOverviewResponse> GetDataQualityOverviewAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DataAnomalyEventResponse>> GetDataAnomalyEventsAsync(string? eventType, DateTime? startUtc, DateTime? endUtc, CancellationToken cancellationToken);

    /// <summary>QA-17: Generate automated weekly quality health report.</summary>
    Task<QaHealthReportResponse> GetQaHealthReportAsync(int windowDays, CancellationToken cancellationToken);
}
