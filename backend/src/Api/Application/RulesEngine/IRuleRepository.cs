using Api.Domain.Rules;
using Api.Domain.Leads;

namespace Api.Application.RulesEngine;

public interface IRuleRepository
{
    Task AddAsync(Rule rule, CancellationToken cancellationToken);
    Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Rule>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Rule>> GetActiveByTriggerAsync(string trigger, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lead>> GetLeadsSnapshotAsync(int take, CancellationToken cancellationToken);
    Task AddExecutionLogAsync(RuleExecutionLog log, CancellationToken cancellationToken);
    Task<RuleExecutionLog?> GetLastAppliedExecutionAsync(Guid ruleId, Guid entityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RuleExecutionLog>> GetExecutionLogsByRuleAsync(Guid ruleId, CancellationToken cancellationToken);
    Task AddRevisionAsync(RuleRevision revision, CancellationToken cancellationToken);
    Task<RuleRevision?> GetRevisionAsync(Guid ruleId, int? targetVersion, CancellationToken cancellationToken);
    Task ReplaceDefinitionChildrenAsync(Guid ruleId, IEnumerable<RuleCondition> conditions, IEnumerable<RuleAction> actions, CancellationToken cancellationToken);
    void Remove(Rule rule);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
