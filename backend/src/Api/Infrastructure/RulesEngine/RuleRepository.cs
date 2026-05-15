using Api.Application.RulesEngine;
using Api.Domain.Leads;
using Api.Domain.Rules;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.RulesEngine;

public class RuleRepository : IRuleRepository
{
    private readonly LeadsDbContext _context;

    public RuleRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Rule rule, CancellationToken cancellationToken)
    {
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Rule?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Rules
            .AsSplitQuery()
            .Include(x => x.Conditions)
            .Include(x => x.Actions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Rule>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Rules
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Conditions)
            .Include(x => x.Actions)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Rule>> GetActiveByTriggerAsync(string trigger, CancellationToken cancellationToken)
    {
        return await _context.Rules
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Conditions)
            .Include(x => x.Actions)
            .Where(x => x.IsActive && x.Trigger == trigger)
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lead>> GetLeadsSnapshotAsync(int take, CancellationToken cancellationToken)
    {
        return await _context.Leads
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddExecutionLogAsync(RuleExecutionLog log, CancellationToken cancellationToken)
    {
        _context.RuleExecutionLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RuleExecutionLog?> GetLastAppliedExecutionAsync(Guid ruleId, Guid entityId, CancellationToken cancellationToken)
    {
        return await _context.RuleExecutionLogs
            .AsNoTracking()
            .Where(x => x.RuleId == ruleId && x.EntityId == entityId && x.Applied)
            .OrderByDescending(x => x.ExecutedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RuleExecutionLog>> GetExecutionLogsByRuleAsync(Guid ruleId, CancellationToken cancellationToken)
    {
        return await _context.RuleExecutionLogs
            .AsNoTracking()
            .Where(x => x.RuleId == ruleId)
            .OrderByDescending(x => x.ExecutedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRevisionAsync(RuleRevision revision, CancellationToken cancellationToken)
    {
        _context.RuleRevisions.Add(revision);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RuleRevision?> GetRevisionAsync(Guid ruleId, int? targetVersion, CancellationToken cancellationToken)
    {
        var query = _context.RuleRevisions
            .AsNoTracking()
            .Where(x => x.RuleId == ruleId);

        if (targetVersion.HasValue)
        {
            query = query.Where(x => x.Version == targetVersion.Value);
        }

        return await query
            .OrderByDescending(x => x.Version)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ReplaceDefinitionChildrenAsync(
        Guid ruleId,
        IEnumerable<RuleCondition> conditions,
        IEnumerable<RuleAction> actions,
        CancellationToken cancellationToken)
    {
        var existingConditions = await _context.RuleConditions
            .Where(x => x.RuleId == ruleId)
            .ToListAsync(cancellationToken);

        var existingActions = await _context.RuleActions
            .Where(x => x.RuleId == ruleId)
            .ToListAsync(cancellationToken);

        if (existingConditions.Count > 0)
        {
            _context.RuleConditions.RemoveRange(existingConditions);
        }

        if (existingActions.Count > 0)
        {
            _context.RuleActions.RemoveRange(existingActions);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var conditionRows = conditions.ToList();
        var actionRows = actions.ToList();

        foreach (var condition in conditionRows)
        {
            _context.Entry(condition).Property(x => x.RuleId).CurrentValue = ruleId;
        }

        foreach (var action in actionRows)
        {
            _context.Entry(action).Property(x => x.RuleId).CurrentValue = ruleId;
        }

        _context.RuleConditions.AddRange(conditionRows);
        _context.RuleActions.AddRange(actionRows);
    }

    public void Remove(Rule rule)
    {
        _context.Rules.Remove(rule);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
