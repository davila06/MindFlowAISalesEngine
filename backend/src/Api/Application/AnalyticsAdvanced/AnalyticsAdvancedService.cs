using Api.Contracts.Analytics;
using Api.Application.Common.Interfaces;
using Api.Domain.Pipeline;

namespace Api.Application.AnalyticsAdvanced;

public sealed class AnalyticsAdvancedService : IAnalyticsAdvancedService
{
    private readonly IAnalyticsAdvancedDataRepository _repository;
    private readonly ITenantContext _tenantContext;

    public AnalyticsAdvancedService(IAnalyticsAdvancedDataRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<AnalyticsAdvancedOverviewResponse> GetOverviewAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadSnapshotAsync(query, cancellationToken);
        return BuildOverview(snapshot);
    }

    public async Task<FunnelKpiResponse> GetFunnelAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadSnapshotAsync(query, cancellationToken);
        return BuildFunnel(snapshot);
    }

    public async Task<RevenueKpiResponse> GetRevenueAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadSnapshotAsync(query, cancellationToken);
        return BuildRevenue(snapshot);
    }

    public async Task<VelocityKpiResponse> GetVelocityAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadSnapshotAsync(query, cancellationToken);
        return BuildVelocity(snapshot);
    }

    public async Task<SlaKpiResponse> GetSlaAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadSnapshotAsync(query, cancellationToken);
        return BuildSla(snapshot);
    }

    public async Task<OnboardingActivationKpiResponse> GetOnboardingActivationAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadSnapshotAsync(query, cancellationToken);
        return BuildOnboardingActivation(snapshot);
    }

    public async Task<ScopeMetricsResponse> GetScopeMetricsAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadSnapshotAsync(query, cancellationToken);

        var latestAssignmentByLead = snapshot.Assignments
            .GroupBy(x => x.LeadId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(a => a.AssignedAtUtc).First());

        var wonOpportunities = snapshot.Opportunities
            .Where(x => x.StageId == DefaultPipelineStages.Won.Id)
            .ToList();

        var opportunitiesByLead = snapshot.Opportunities
            .GroupBy(x => x.LeadId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var wonLeadIds = wonOpportunities
            .Select(x => x.LeadId)
            .ToHashSet();

        var usersById = snapshot.AssignmentUsers.ToDictionary(x => x.Id, x => x);

        var sellerMetrics = latestAssignmentByLead.Values
            .GroupBy(x => x.UserId)
            .Select(group =>
            {
                var assignedLeadIds = group.Select(x => x.LeadId).Distinct().ToList();
                var wonLeadsCount = assignedLeadIds.Count(wonLeadIds.Contains);
                var pipelineRevenue = assignedLeadIds
                    .SelectMany(leadId => opportunitiesByLead.TryGetValue(leadId, out var leadOpps) ? leadOpps : [])
                    .Sum(x => x.Value);
                var wonRevenue = assignedLeadIds
                    .SelectMany(leadId => opportunitiesByLead.TryGetValue(leadId, out var leadOpps) ? leadOpps : [])
                    .Where(x => x.StageId == DefaultPipelineStages.Won.Id)
                    .Sum(x => x.Value);

                var user = usersById.TryGetValue(group.Key, out var assignmentUser)
                    ? assignmentUser
                    : null;

                return new SellerScopeMetricResponse
                {
                    UserId = group.Key,
                    FullName = user?.FullName ?? "Unknown",
                    Email = user?.Email ?? string.Empty,
                    AssignedLeadsCount = assignedLeadIds.Count,
                    WonLeadsCount = wonLeadsCount,
                    ConversionRate = Percentage(wonLeadsCount, assignedLeadIds.Count),
                    PipelineRevenue = pipelineRevenue,
                    WonRevenue = wonRevenue
                };
            })
            .OrderByDescending(x => x.WonRevenue)
            .ThenByDescending(x => x.AssignedLeadsCount)
            .ToList();

        var teamMetrics = latestAssignmentByLead.Values
            .GroupBy(ResolveTeamKey)
            .Select(group =>
            {
                var assignedLeadIds = group.Select(x => x.LeadId).Distinct().ToList();
                var wonLeadsCount = assignedLeadIds.Count(wonLeadIds.Contains);
                var pipelineRevenue = assignedLeadIds
                    .SelectMany(leadId => opportunitiesByLead.TryGetValue(leadId, out var leadOpps) ? leadOpps : [])
                    .Sum(x => x.Value);
                var wonRevenue = assignedLeadIds
                    .SelectMany(leadId => opportunitiesByLead.TryGetValue(leadId, out var leadOpps) ? leadOpps : [])
                    .Where(x => x.StageId == DefaultPipelineStages.Won.Id)
                    .Sum(x => x.Value);

                return new TeamScopeMetricResponse
                {
                    TeamKey = group.Key,
                    AssignedLeadsCount = assignedLeadIds.Count,
                    WonLeadsCount = wonLeadsCount,
                    ConversionRate = Percentage(wonLeadsCount, assignedLeadIds.Count),
                    PipelineRevenue = pipelineRevenue,
                    WonRevenue = wonRevenue
                };
            })
            .OrderByDescending(x => x.WonRevenue)
            .ThenByDescending(x => x.AssignedLeadsCount)
            .ToList();

        var tenantTotalLeads = snapshot.Leads.Count;
        var tenantAssignedLeads = latestAssignmentByLead.Count;
        var tenantWonLeads = snapshot.Leads.Count(x => wonLeadIds.Contains(x.Id));
        var tenantPipelineRevenue = snapshot.Opportunities.Sum(x => x.Value);
        var tenantWonRevenue = wonOpportunities.Sum(x => x.Value);

        return new ScopeMetricsResponse
        {
            Tenant = new TenantScopeMetricResponse
            {
                TenantId = _tenantContext.TenantId,
                TotalLeads = tenantTotalLeads,
                AssignedLeadsCount = tenantAssignedLeads,
                WonLeadsCount = tenantWonLeads,
                ConversionRate = Percentage(tenantWonLeads, tenantTotalLeads),
                PipelineRevenue = tenantPipelineRevenue,
                WonRevenue = tenantWonRevenue
            },
            Sellers = sellerMetrics,
            Teams = teamMetrics
        };
    }

    public async Task<PeriodOverPeriodComparisonResponse> GetPeriodOverPeriodAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var (currentStartUtc, currentEndUtc) = ResolveWindow(query);
        var duration = currentEndUtc - currentStartUtc;
        if (duration <= TimeSpan.Zero)
        {
            duration = TimeSpan.FromDays(1);
        }

        var previousEndUtc = currentStartUtc.AddTicks(-1);
        var previousStartUtc = previousEndUtc - duration;

        var currentQuery = new AnalyticsAdvancedQuery
        {
            StartDateUtc = currentStartUtc,
            EndDateUtc = currentEndUtc,
            GroupBy = query.GroupBy,
            Stage = query.Stage,
            Source = query.Source,
            Tenant = query.Tenant
        };

        var previousQuery = new AnalyticsAdvancedQuery
        {
            StartDateUtc = previousStartUtc,
            EndDateUtc = previousEndUtc,
            GroupBy = query.GroupBy,
            Stage = query.Stage,
            Source = query.Source,
            Tenant = query.Tenant
        };

        var currentSnapshot = await _repository.LoadSnapshotAsync(currentQuery, cancellationToken);
        var previousSnapshot = await _repository.LoadSnapshotAsync(previousQuery, cancellationToken);

        var current = BuildOverview(currentSnapshot);
        var previous = BuildOverview(previousSnapshot);

        return new PeriodOverPeriodComparisonResponse
        {
            CurrentStartUtc = currentStartUtc,
            CurrentEndUtc = currentEndUtc,
            PreviousStartUtc = previousStartUtc,
            PreviousEndUtc = previousEndUtc,
            Current = current,
            Previous = previous,
            Delta = new PeriodOverPeriodDeltaResponse
            {
                WonCountDelta = current.Funnel.WonCount - previous.Funnel.WonCount,
                WonRevenueDelta = current.Revenue.WonRevenue - previous.Revenue.WonRevenue,
                PipelineRevenueDelta = current.Revenue.PipelineRevenue - previous.Revenue.PipelineRevenue,
                ProposalToWonRateDelta = current.Funnel.ProposalToWonRate - previous.Funnel.ProposalToWonRate
            }
        };
    }

    public async Task<SegmentationResponse> GetSegmentationAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var snapshot = await _repository.LoadSnapshotAsync(query, cancellationToken);

        var opportunitiesByLead = snapshot.Opportunities
            .GroupBy(x => x.LeadId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var companyByLead = snapshot.Companies
            .GroupBy(x => x.LeadId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(c => c.CreatedAtUtc).First());

        var bySource = BuildSegments(
            snapshot.Leads,
            lead => NormalizeSegmentKey(lead.Source),
            opportunitiesByLead);

        var byCampaign = BuildSegments(
            snapshot.Leads,
            lead => NormalizeSegmentKey(lead.Campaign),
            opportunitiesByLead);

        var byIndustry = BuildSegments(
            snapshot.Leads,
            lead => companyByLead.TryGetValue(lead.Id, out var company)
                ? NormalizeSegmentKey(company.Industry)
                : "unknown",
            opportunitiesByLead);

        return new SegmentationResponse
        {
            BySource = bySource,
            ByCampaign = byCampaign,
            ByIndustry = byIndustry
        };
    }

    private static AnalyticsAdvancedOverviewResponse BuildOverview(AnalyticsAdvancedDataSnapshot snapshot)
    {
        return new AnalyticsAdvancedOverviewResponse
        {
            Funnel = BuildFunnel(snapshot),
            Revenue = BuildRevenue(snapshot),
            Velocity = BuildVelocity(snapshot),
            Sla = BuildSla(snapshot),
            OnboardingActivation = BuildOnboardingActivation(snapshot)
        };
    }

    private static FunnelKpiResponse BuildFunnel(AnalyticsAdvancedDataSnapshot snapshot)
    {
        var reachedQualified = snapshot.StageHistory
            .Where(x => x.ToStageId == DefaultPipelineStages.Qualified.Id)
            .Select(x => x.OpportunityId)
            .ToHashSet();

        var reachedProposal = snapshot.StageHistory
            .Where(x => x.ToStageId == DefaultPipelineStages.Proposal.Id)
            .Select(x => x.OpportunityId)
            .ToHashSet();

        var reachedWon = snapshot.StageHistory
            .Where(x => x.ToStageId == DefaultPipelineStages.Won.Id)
            .Select(x => x.OpportunityId)
            .ToHashSet();

        var newCount = snapshot.Opportunities.Count;
        var qualifiedCount = snapshot.Opportunities.Count(x =>
            x.StageId == DefaultPipelineStages.Qualified.Id ||
            x.StageId == DefaultPipelineStages.Proposal.Id ||
            x.StageId == DefaultPipelineStages.Won.Id ||
            reachedQualified.Contains(x.Id));

        var proposalCount = snapshot.Opportunities.Count(x =>
            x.StageId == DefaultPipelineStages.Proposal.Id ||
            x.StageId == DefaultPipelineStages.Won.Id ||
            reachedProposal.Contains(x.Id));

        var wonCount = snapshot.Opportunities.Count(x =>
            x.StageId == DefaultPipelineStages.Won.Id ||
            reachedWon.Contains(x.Id));

        return new FunnelKpiResponse
        {
            NewCount = newCount,
            QualifiedCount = qualifiedCount,
            ProposalCount = proposalCount,
            WonCount = wonCount,
            NewToQualifiedRate = Percentage(qualifiedCount, newCount),
            QualifiedToProposalRate = Percentage(proposalCount, qualifiedCount),
            ProposalToWonRate = Percentage(wonCount, proposalCount)
        };
    }

    private static RevenueKpiResponse BuildRevenue(AnalyticsAdvancedDataSnapshot snapshot)
    {
        var wonDeals = snapshot.Opportunities.Where(x => x.StageId == DefaultPipelineStages.Won.Id).ToList();
        var wonRevenue = wonDeals.Sum(x => x.Value);
        var pipelineRevenue = snapshot.Opportunities.Sum(x => x.Value);

        return new RevenueKpiResponse
        {
            WonRevenue = wonRevenue,
            PipelineRevenue = pipelineRevenue,
            AverageDealSize = wonDeals.Count == 0 ? 0m : Math.Round(wonRevenue / wonDeals.Count, 2),
            Currency = "USD"
        };
    }

    private static VelocityKpiResponse BuildVelocity(AnalyticsAdvancedDataSnapshot snapshot)
    {
        var leadById = snapshot.Leads.ToDictionary(x => x.Id, x => x);
        var opportunityById = snapshot.Opportunities.ToDictionary(x => x.Id, x => x);

        decimal ComputeAverageHours(Guid targetStageId)
        {
            var durations = snapshot.StageHistory
                .Where(x => x.ToStageId == targetStageId)
                .GroupBy(x => x.OpportunityId)
                .Select(x => new { OpportunityId = x.Key, FirstReachedAtUtc = x.Min(h => h.ChangedAtUtc) })
                .Where(x => opportunityById.ContainsKey(x.OpportunityId))
                .Select(x => new
                {
                    Opportunity = opportunityById[x.OpportunityId],
                    x.FirstReachedAtUtc
                })
                .Where(x => leadById.ContainsKey(x.Opportunity.LeadId))
                .Select(x => (x.FirstReachedAtUtc - leadById[x.Opportunity.LeadId].CreatedAtUtc).TotalHours)
                .Where(x => x >= 0)
                .ToList();

            return durations.Count == 0 ? 0m : Math.Round((decimal)durations.Average(), 2);
        }

        return new VelocityKpiResponse
        {
            AverageHoursToQualified = ComputeAverageHours(DefaultPipelineStages.Qualified.Id),
            AverageHoursToProposal = ComputeAverageHours(DefaultPipelineStages.Proposal.Id),
            AverageHoursToWon = ComputeAverageHours(DefaultPipelineStages.Won.Id)
        };
    }

    private static SlaKpiResponse BuildSla(AnalyticsAdvancedDataSnapshot snapshot)
    {
        if (snapshot.Leads.Count == 0)
        {
            return new SlaKpiResponse();
        }

        const double slaHours = 24;
        var assignmentByLead = snapshot.Assignments
            .GroupBy(x => x.LeadId)
            .ToDictionary(x => x.Key, x => x.Min(a => a.AssignedAtUtc));

        var firstResponseByLead = snapshot.Opportunities
            .GroupJoin(
                snapshot.StageHistory,
                opportunity => opportunity.Id,
                history => history.OpportunityId,
                (opportunity, history) => new
                {
                    opportunity.LeadId,
                    FirstResponseAtUtc = history.Any()
                        ? history.Min(x => x.ChangedAtUtc)
                        : (DateTime?)null
                })
            .Where(x => x.FirstResponseAtUtc.HasValue)
            .GroupBy(x => x.LeadId)
            .ToDictionary(x => x.Key, x => x.Min(v => v.FirstResponseAtUtc!.Value));

        var assignmentWithinSla = 0;
        var firstResponseWithinSla = 0;
        var breaches = 0;

        foreach (var lead in snapshot.Leads)
        {
            var assignWithin = assignmentByLead.TryGetValue(lead.Id, out var assignedAt) &&
                               (assignedAt - lead.CreatedAtUtc).TotalHours <= slaHours;
            var responseWithin = firstResponseByLead.TryGetValue(lead.Id, out var firstResponseAt) &&
                                 (firstResponseAt - lead.CreatedAtUtc).TotalHours <= slaHours;

            if (assignWithin)
            {
                assignmentWithinSla++;
            }

            if (responseWithin)
            {
                firstResponseWithinSla++;
            }

            if (!assignWithin || !responseWithin)
            {
                breaches++;
            }
        }

        return new SlaKpiResponse
        {
            AssignmentWithinSlaRate = Percentage(assignmentWithinSla, snapshot.Leads.Count),
            FirstResponseWithinSlaRate = Percentage(firstResponseWithinSla, snapshot.Leads.Count),
            SlaBreaches = breaches
        };
    }

    private static OnboardingActivationKpiResponse BuildOnboardingActivation(AnalyticsAdvancedDataSnapshot snapshot)
    {
        var newCustomers = snapshot.Customers.Count;
        var activatedCustomers = snapshot.Customers.Count(x => x.TrackingActivations > 0);

        var activationDurations = snapshot.Customers
            .Where(x => x.TrackingActivations > 0 && x.LastTrackingActivatedAtUtc.HasValue)
            .Select(x => (x.LastTrackingActivatedAtUtc!.Value - x.CreatedAtUtc).TotalHours)
            .Where(x => x >= 0)
            .ToList();

        return new OnboardingActivationKpiResponse
        {
            NewCustomers = newCustomers,
            ActivatedCustomers = activatedCustomers,
            ActivationRate = Percentage(activatedCustomers, newCustomers),
            AverageHoursToFirstActivation = activationDurations.Count == 0
                ? 0m
                : Math.Round((decimal)activationDurations.Average(), 2)
        };
    }

    private static decimal Percentage(int numerator, int denominator)
    {
        if (denominator == 0)
        {
            return 0m;
        }

        return Math.Round((decimal)numerator * 100m / denominator, 2);
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

    private static string ResolveTeamKey(Domain.Assignment.LeadAssignment assignment)
    {
        if (!string.IsNullOrWhiteSpace(assignment.RuleKey))
        {
            return assignment.RuleKey.Trim().ToLowerInvariant();
        }

        return string.IsNullOrWhiteSpace(assignment.Strategy)
            ? "unassigned"
            : assignment.Strategy.Trim().ToLowerInvariant();
    }

    private static IReadOnlyList<SegmentMetricResponse> BuildSegments(
        IReadOnlyList<Domain.Leads.Lead> leads,
        Func<Domain.Leads.Lead, string> segmentKeySelector,
        IReadOnlyDictionary<Guid, List<Opportunity>> opportunitiesByLead)
    {
        return leads
            .GroupBy(segmentKeySelector)
            .Select(group =>
            {
                var leadIds = group.Select(x => x.Id).ToList();
                var opportunities = leadIds
                    .SelectMany(leadId => opportunitiesByLead.TryGetValue(leadId, out var leadOpportunities) ? leadOpportunities : [])
                    .ToList();

                var wonLeads = leadIds.Count(leadId => opportunitiesByLead.TryGetValue(leadId, out var leadOpportunities)
                    && leadOpportunities.Any(x => x.StageId == DefaultPipelineStages.Won.Id));

                return new SegmentMetricResponse
                {
                    Key = string.IsNullOrWhiteSpace(group.Key) ? "unknown" : group.Key,
                    TotalLeads = leadIds.Count,
                    WonLeads = wonLeads,
                    ConversionRate = Percentage(wonLeads, leadIds.Count),
                    PipelineRevenue = opportunities.Sum(x => x.Value),
                    WonRevenue = opportunities.Where(x => x.StageId == DefaultPipelineStages.Won.Id).Sum(x => x.Value)
                };
            })
            .OrderByDescending(x => x.WonRevenue)
            .ThenByDescending(x => x.TotalLeads)
            .ThenBy(x => x.Key)
            .ToList();
    }

    private static string NormalizeSegmentKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value.Trim().ToLowerInvariant();
    }
}
