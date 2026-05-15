using Api.Application.AnalyticsAdvanced;
using Api.Contracts.Analytics;
using Api.Domain.Pipeline;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.AnalyticsAdvanced;

public sealed class AnalyticsAdvancedDataRepository : IAnalyticsAdvancedDataRepository
{
    private readonly LeadsDbContext _dbContext;

    public AnalyticsAdvancedDataRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AnalyticsAdvancedDataSnapshot> LoadSnapshotAsync(
        AnalyticsAdvancedQuery query,
        CancellationToken cancellationToken)
    {
        var (startUtc, endUtc) = ResolveWindow(query);

        var leadsQuery = _dbContext.Leads
            .AsNoTracking()
            .AsQueryable();
        leadsQuery = leadsQuery.Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc <= endUtc);

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            var source = query.Source.Trim().ToLowerInvariant();
            leadsQuery = leadsQuery.Where(x => x.Source.ToLower() == source);
        }

        var leads = await leadsQuery.ToListAsync(cancellationToken);
        if (leads.Count == 0)
        {
            return new AnalyticsAdvancedDataSnapshot();
        }

        var leadIds = leads.Select(x => x.Id).ToList();

        var opportunitiesQuery = _dbContext.Opportunities
            .AsNoTracking()
            .Where(x => leadIds.Contains(x.LeadId));
        if (!string.IsNullOrWhiteSpace(query.Stage) && TryResolveStageId(query.Stage, out var stageId))
        {
            opportunitiesQuery = opportunitiesQuery.Where(x => x.StageId == stageId);
        }

        var opportunities = await opportunitiesQuery.ToListAsync(cancellationToken);
        var opportunityIds = opportunities.Select(x => x.Id).ToList();

        var history = opportunityIds.Count == 0
            ? []
            : await _dbContext.OpportunityStageHistories
                .AsNoTracking()
                .Where(x => opportunityIds.Contains(x.OpportunityId))
                .ToListAsync(cancellationToken);

        var assignments = await _dbContext.LeadAssignments
            .AsNoTracking()
            .Where(x => leadIds.Contains(x.LeadId))
            .ToListAsync(cancellationToken);

        var assignedUserIds = assignments
            .Select(x => x.UserId)
            .Distinct()
            .ToList();

        var assignmentUsers = assignedUserIds.Count == 0
            ? []
            : await _dbContext.AssignmentUsers
                .AsNoTracking()
                .Where(x => assignedUserIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

        var customers = await _dbContext.Customers
            .AsNoTracking()
            .Where(x => leadIds.Contains(x.LeadId))
            .ToListAsync(cancellationToken);

        var companies = await _dbContext.Companies
            .AsNoTracking()
            .Where(x => leadIds.Contains(x.LeadId))
            .ToListAsync(cancellationToken);

        return new AnalyticsAdvancedDataSnapshot
        {
            Leads = leads,
            Opportunities = opportunities,
            StageHistory = history,
            Assignments = assignments,
            AssignmentUsers = assignmentUsers,
            Companies = companies,
            Customers = customers
        };
    }

    private static (DateTime startUtc, DateTime endUtc) ResolveWindow(AnalyticsAdvancedQuery query)
    {
        var endUtc = query.EndDateUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        var startUtc = query.StartDateUtc?.ToUniversalTime() ?? endUtc.AddDays(-30);

        if (startUtc > endUtc)
        {
            (startUtc, endUtc) = (endUtc, startUtc);
        }

        return (startUtc, endUtc);
    }

    private static bool TryResolveStageId(string stageName, out Guid stageId)
    {
        var normalized = stageName.Trim().ToLowerInvariant();
        var stage = DefaultPipelineStages.All.FirstOrDefault(x => x.Name == normalized);
        if (stage is null)
        {
            stageId = Guid.Empty;
            return false;
        }

        stageId = stage.Id;
        return true;
    }
}
