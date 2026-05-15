using Api.Contracts;

namespace Api.Application.RulesEngine;

public interface IRuleService
{
    Task<RuleResponse> CreateAsync(RuleCreateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<RuleResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<RuleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<RuleResponse?> UpdateAsync(Guid id, RuleUpdateRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<RuleResponse?> PromoteAsync(Guid id, RulePromotionRequest request, CancellationToken cancellationToken);
    Task<RuleDriftSummaryResponse> GetDriftSummaryAsync(CancellationToken cancellationToken);
    Task<RuleDryRunResponse?> DryRunAsync(Guid id, CancellationToken cancellationToken);
    Task<RuleMetricsResponse?> GetMetricsAsync(Guid id, CancellationToken cancellationToken);
    Task<RuleResponse?> RollbackAsync(Guid id, int? targetVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<RuleTemplateResponse>> GetTemplatesAsync(CancellationToken cancellationToken);
    Task<RuleFixtureTestResponse?> TestFixtureAsync(RuleFixtureTestRequest request, CancellationToken cancellationToken);
}
