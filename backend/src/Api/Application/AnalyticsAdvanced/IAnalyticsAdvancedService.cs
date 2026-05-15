using Api.Contracts.Analytics;

namespace Api.Application.AnalyticsAdvanced;

public interface IAnalyticsAdvancedService
{
    Task<AnalyticsAdvancedOverviewResponse> GetOverviewAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken);
    Task<FunnelKpiResponse> GetFunnelAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken);
    Task<RevenueKpiResponse> GetRevenueAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken);
    Task<VelocityKpiResponse> GetVelocityAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken);
    Task<SlaKpiResponse> GetSlaAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken);
    Task<OnboardingActivationKpiResponse> GetOnboardingActivationAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken);
    Task<ScopeMetricsResponse> GetScopeMetricsAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken);
    Task<PeriodOverPeriodComparisonResponse> GetPeriodOverPeriodAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken);
    Task<SegmentationResponse> GetSegmentationAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken);
}
