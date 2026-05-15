namespace Api.Contracts.Analytics;

public class SlaKpiResponse
{
    public decimal AssignmentWithinSlaRate { get; init; }
    public decimal FirstResponseWithinSlaRate { get; init; }
    public int SlaBreaches { get; init; }
}
