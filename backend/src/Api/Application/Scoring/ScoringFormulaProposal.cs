namespace Api.Application.Scoring;

public sealed class ScoringFormulaProposal
{
    public Guid ProposalId { get; init; } = Guid.NewGuid();
    public string Status { get; set; } = "pending";
    public string RequestedBy { get; init; } = string.Empty;
    public DateTime RequestedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? ApprovedAtUtc { get; set; }
    public string? ApprovedBy { get; set; }
    public LeadScoringFormula Formula { get; init; } = new();
}
