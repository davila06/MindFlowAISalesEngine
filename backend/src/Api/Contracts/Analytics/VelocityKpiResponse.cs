namespace Api.Contracts.Analytics;

public class VelocityKpiResponse
{
    public decimal AverageHoursToQualified { get; init; }
    public decimal AverageHoursToProposal { get; init; }
    public decimal AverageHoursToWon { get; init; }
}
