namespace Api.Contracts;

public class ProposalKpiResponse
{
    public int TotalProposals { get; init; }
    public int SignedProposals { get; init; }
    public decimal ProposalToWonRate { get; init; }
    public int TrackedProposals { get; init; }
    public decimal AverageViewsPerProposal { get; init; }
}
