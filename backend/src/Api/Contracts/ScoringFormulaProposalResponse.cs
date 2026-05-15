namespace Api.Contracts;

public class ScoringFormulaProposalResponse
{
    public Guid ProposalId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;
    public DateTime RequestedAtUtc { get; init; }
    public DateTime? ApprovedAtUtc { get; init; }
    public string? ApprovedBy { get; init; }
    public ScoringFormulaResponse Formula { get; init; } = new();
}
