namespace Api.Contracts.Analytics;

public class OnboardingActivationKpiResponse
{
    public int NewCustomers { get; init; }
    public int ActivatedCustomers { get; init; }
    public decimal ActivationRate { get; init; }
    public decimal AverageHoursToFirstActivation { get; init; }
}
