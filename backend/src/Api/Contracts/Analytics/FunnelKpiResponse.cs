namespace Api.Contracts.Analytics;

public class FunnelKpiResponse
{
    public int NewCount { get; init; }
    public int QualifiedCount { get; init; }
    public int ProposalCount { get; init; }
    public int WonCount { get; init; }
    public decimal NewToQualifiedRate { get; init; }
    public decimal QualifiedToProposalRate { get; init; }
    public decimal ProposalToWonRate { get; init; }
}
