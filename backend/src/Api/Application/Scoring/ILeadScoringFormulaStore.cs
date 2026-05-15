namespace Api.Application.Scoring;

public interface ILeadScoringFormulaStore
{
    Task<LeadScoringFormula> GetCurrentAsync(string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeadScoringFormula>> ListVersionsAsync(string tenantId, CancellationToken cancellationToken);
    Task<ScoringFormulaProposal> CreateProposalAsync(string tenantId, string requestedBy, LeadScoringFormula formula, CancellationToken cancellationToken);
    Task<ScoringFormulaProposal?> ApproveProposalAsync(string tenantId, Guid proposalId, string approvedBy, CancellationToken cancellationToken);
    Task<IReadOnlyList<ScoringFormulaProposal>> ListProposalsAsync(string tenantId, CancellationToken cancellationToken);
}
