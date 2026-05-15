namespace Api.Contracts.Analytics;

public class AnalyticsAdvancedOverviewResponse
{
    public FunnelKpiResponse Funnel { get; init; } = new();
    public RevenueKpiResponse Revenue { get; init; } = new();
    public VelocityKpiResponse Velocity { get; init; } = new();
    public SlaKpiResponse Sla { get; init; } = new();
    public OnboardingActivationKpiResponse OnboardingActivation { get; init; } = new();
}
