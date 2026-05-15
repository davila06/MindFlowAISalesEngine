namespace Api.Contracts;

public class OnboardingOverviewResponse
{
    public int TotalCustomers { get; init; }
    public int AtRiskCustomers { get; init; }
    public int ChurnRiskCustomers { get; init; }
    public int OverdueTasks { get; init; }
    public decimal EarlyActivationRatePercent { get; init; }
    public decimal AverageHealthScore { get; init; }
}
