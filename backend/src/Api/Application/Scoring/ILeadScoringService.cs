using Api.Contracts;

namespace Api.Application.Scoring;

public interface ILeadScoringService
{
    Task<LeadScoreResponse?> ScoreLeadAsync(Guid leadId, CancellationToken cancellationToken);
    Task<LeadScoreResponse?> GetLeadScoreAsync(Guid leadId, CancellationToken cancellationToken);
    Task<ScoreRecalculationResponse> RecalculateScoresAsync(DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken cancellationToken);
    IReadOnlyList<ScoreRuleResponse> GetRules();
    Task<ScoringPriorityThresholdsResponse> GetPriorityThresholdsAsync(CancellationToken cancellationToken);
    Task<ScoringPriorityThresholdsResponse> UpdatePriorityThresholdsAsync(ScoringPriorityThresholdsRequest request, CancellationToken cancellationToken);
    Task<ScoringDriftResponse> GetScoreDriftAsync(ScoringDriftQueryRequest request, CancellationToken cancellationToken);
    Task<ScoringFormulaResponse> GetCurrentFormulaAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ScoringFormulaResponse>> GetFormulaVersionsAsync(CancellationToken cancellationToken);
    Task<ScoringFormulaProposalResponse> CreateFormulaProposalAsync(ScoringFormulaProposalRequest request, CancellationToken cancellationToken);
    Task<ScoringFormulaProposalResponse?> ApproveFormulaProposalAsync(Guid proposalId, string approvedBy, CancellationToken cancellationToken);
    Task<IReadOnlyList<ScoringFormulaProposalResponse>> GetFormulaProposalsAsync(CancellationToken cancellationToken);
    Task<ScoreExplainabilityResponse?> GetLeadExplainabilityAsync(Guid leadId, CancellationToken cancellationToken);
    Task<ScoringSimulationResponse> SimulateAsync(ScoringSimulationRequest request, CancellationToken cancellationToken);
    Task<ScoringConversionLoopResponse> GetConversionLoopAsync(CancellationToken cancellationToken);
}
