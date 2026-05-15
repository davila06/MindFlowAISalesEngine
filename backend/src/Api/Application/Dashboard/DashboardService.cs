using Api.Application.Common.Interfaces;
using Api.Application.Common.Security;
using Api.Contracts;
using Api.Domain.Pipeline;

namespace Api.Application.Dashboard;

public class DashboardService : IDashboardService
{
    private const int DefaultDays = 7;
    private const string DataAnomalyEventPrefix = "lead.data_anomaly.";

    private readonly ILeadRepository _leadRepository;
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly ILeadAuditSnapshotRepository _leadAuditSnapshotRepository;

    public DashboardService(
        ILeadRepository leadRepository,
        IOpportunityRepository opportunityRepository,
        ILeadAuditSnapshotRepository leadAuditSnapshotRepository)
    {
        _leadRepository = leadRepository;
        _opportunityRepository = opportunityRepository;
        _leadAuditSnapshotRepository = leadAuditSnapshotRepository;
    }

    public async Task<DashboardOverviewResponse> GetOverviewAsync(int days, CancellationToken cancellationToken)
    {
        var lookbackDays = days <= 0 ? DefaultDays : days;

        var leads = await _leadRepository.ListAsync(cancellationToken);
        var opportunities = await _opportunityRepository.ListAsync(cancellationToken);

        var totalLeads = leads.Count;
        var totalOpportunities = opportunities.Count;
        var wonOpportunities = opportunities.Count(x => x.StageId == DefaultPipelineStages.Won.Id);

        var conversionRate = totalOpportunities == 0
            ? 0m
            : Math.Round((decimal)wonOpportunities * 100m / totalOpportunities, 2, MidpointRounding.AwayFromZero);

        var pipelineValue = Math.Round(opportunities.Sum(x => x.Value), 2, MidpointRounding.AwayFromZero);

        var today = DateTime.UtcNow.Date;
        var rangeStart = today.AddDays(-(lookbackDays - 1));

        var grouped = leads
            .Where(x => x.CreatedAtUtc.Date >= rangeStart)
            .GroupBy(x => x.CreatedAtUtc.Date)
            .ToDictionary(x => x.Key, x => x.Count());

        var leadsPerDay = Enumerable.Range(0, lookbackDays)
            .Select(offset =>
            {
                var date = rangeStart.AddDays(offset);
                grouped.TryGetValue(date, out var count);
                return new LeadsPerDayPointResponse
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Count = count
                };
            })
            .ToList();

        return new DashboardOverviewResponse
        {
            GeneratedAtUtc = DateTime.UtcNow,
            TotalLeads = totalLeads,
            TotalOpportunities = totalOpportunities,
            WonOpportunities = wonOpportunities,
            ConversionRate = conversionRate,
            PipelineValue = pipelineValue,
            LeadsPerDay = leadsPerDay
        };
    }

    public async Task<DataQualityOverviewResponse> GetDataQualityOverviewAsync(CancellationToken cancellationToken)
    {
        var leads = await _leadRepository.ListAsync(cancellationToken);
        var totalLeads = leads.Count;
        var leadsWithEmail = leads.Count(x => !string.IsNullOrWhiteSpace(x.Email));
        var leadsWithPhone = leads.Count(x => !string.IsNullOrWhiteSpace(x.Phone));
        var leadsWithBothContacts = leads.Count(x => !string.IsNullOrWhiteSpace(x.Email) && !string.IsNullOrWhiteSpace(x.Phone));

        var duplicateEmailCandidates = leads
            .Where(x => !string.IsNullOrWhiteSpace(x.Email))
            .GroupBy(x => x.Email!.Trim().ToLowerInvariant())
            .Count(group => group.Count() > 1);

        var duplicatePhoneCandidates = leads
            .Where(x => !string.IsNullOrWhiteSpace(x.Phone))
            .GroupBy(x => x.Phone!.Trim())
            .Count(group => group.Count() > 1);

        var contactCompletenessPercent = totalLeads == 0
            ? 0m
            : Math.Round((decimal)leadsWithBothContacts * 100m / totalLeads, 2, MidpointRounding.AwayFromZero);
        var dataAnomalyEvents = await _leadAuditSnapshotRepository.CountByEventTypePrefixAsync(
            DataAnomalyEventPrefix,
            cancellationToken);

        return new DataQualityOverviewResponse
        {
            TotalLeads = totalLeads,
            LeadsWithEmail = leadsWithEmail,
            LeadsWithPhone = leadsWithPhone,
            LeadsWithBothContacts = leadsWithBothContacts,
            DuplicateEmailCandidates = duplicateEmailCandidates,
            DuplicatePhoneCandidates = duplicatePhoneCandidates,
            ContactCompletenessPercent = contactCompletenessPercent,
            DataAnomalyEvents = dataAnomalyEvents
        };
    }

    public async Task<IReadOnlyList<DataAnomalyEventResponse>> GetDataAnomalyEventsAsync(
        string? eventType,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken)
    {
        var suffix = string.IsNullOrWhiteSpace(eventType)
            ? string.Empty
            : eventType.Trim().ToLowerInvariant();
        var prefix = string.IsNullOrWhiteSpace(suffix)
            ? DataAnomalyEventPrefix
            : $"{DataAnomalyEventPrefix}{suffix}";

        var snapshots = await _leadAuditSnapshotRepository.QueryByEventTypePrefixAsync(prefix, startUtc, endUtc, cancellationToken);
        return snapshots.Select(x => new DataAnomalyEventResponse
        {
            Id = x.Id,
            LeadId = x.LeadId,
            EventType = x.EventType,
            Actor = x.Actor,
            PayloadJson = PiiMasking.MaskJsonPayload(x.PayloadJson),
            CreatedAtUtc = x.CreatedAtUtc
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<QaHealthReportResponse> GetQaHealthReportAsync(
        int windowDays,
        CancellationToken cancellationToken)
    {
        var window = windowDays <= 0 ? 7 : windowDays;
        var since  = DateTime.UtcNow.AddDays(-window);
        var warnings = new List<string>();

        var leads        = await _leadRepository.ListAsync(cancellationToken);
        var opportunities = await _opportunityRepository.ListAsync(cancellationToken);

        // ── Lead metrics ──────────────────────────────────────────────────────
        var totalLeads    = leads.Count;
        var newInWindow   = leads.Count(l => l.CreatedAtUtc >= since);
        var withEmail     = leads.Count(l => !string.IsNullOrWhiteSpace(l.Email));
        var emailComplete = totalLeads == 0 ? 100m
            : Math.Round((decimal)withEmail * 100m / totalLeads, 1, MidpointRounding.AwayFromZero);

        var dupCandidates = leads
            .Where(l => !string.IsNullOrWhiteSpace(l.Email))
            .GroupBy(l => l.Email!.Trim().ToLowerInvariant())
            .Count(g => g.Count() > 1);

        // ── Scoring coverage ──────────────────────────────────────────────────
        var withVersion     = leads.Count(l => !string.IsNullOrWhiteSpace(l.ScoringVersion));
        var scoringCoverage = totalLeads == 0 ? 100m
            : Math.Round((decimal)withVersion * 100m / totalLeads, 1, MidpointRounding.AwayFromZero);

        // ── Pipeline metrics ──────────────────────────────────────────────────
        var activeOpps = opportunities.Count(o =>
            o.StageId != DefaultPipelineStages.Won.Id);
        var wonInWindow = opportunities.Count(o =>
            o.StageId == DefaultPipelineStages.Won.Id &&
            o.UpdatedAtUtc >= since);
        var convRate = opportunities.Count == 0 ? 0m
            : Math.Round((decimal)opportunities.Count(o => o.StageId == DefaultPipelineStages.Won.Id)
                         * 100m / opportunities.Count, 1, MidpointRounding.AwayFromZero);

        // ── Anomaly count ─────────────────────────────────────────────────────
        var anomalyCount = await _leadAuditSnapshotRepository.CountByEventTypePrefixAsync(
            DataAnomalyEventPrefix, cancellationToken);

        // ── Warning generation ────────────────────────────────────────────────
        if (emailComplete < 80m)
            warnings.Add($"Email completeness ({emailComplete}%) below 80% threshold.");
        if (dupCandidates > 10)
            warnings.Add($"High duplicate email candidate count: {dupCandidates}.");
        if (scoringCoverage < 90m)
            warnings.Add($"Scoring coverage ({scoringCoverage}%) below 90% threshold.");
        if (anomalyCount > 50)
            warnings.Add($"Elevated data anomaly events: {anomalyCount}.");

        // ── Quality score ─────────────────────────────────────────────────────
        var score = 100;
        score -= warnings.Count * 10;
        score -= dupCandidates > 0 ? Math.Min(dupCandidates, 20) : 0;
        score  = Math.Max(0, score);

        var grade = score >= 90 ? "A"
                  : score >= 80 ? "B"
                  : score >= 70 ? "C"
                  : score >= 60 ? "D"
                  : "F";

        return new QaHealthReportResponse
        {
            GeneratedAtUtc         = DateTime.UtcNow,
            ReportWindowLabel      = $"Last {window} days",
            WindowDays             = window,
            TotalLeads             = totalLeads,
            NewLeadsInWindow       = newInWindow,
            LeadEmailCompleteness  = emailComplete,
            DuplicateCandidateCount = dupCandidates,
            ActiveOpportunities    = activeOpps,
            WonInWindow            = wonInWindow,
            ConversionRatePercent  = convRate,
            LeadsWithScoringVersion = withVersion,
            ScoringCoveragePercent = scoringCoverage,
            AnomalyEventsInWindow  = anomalyCount,
            QualityScore           = score,
            QualityGrade           = grade,
            Warnings               = warnings
        };
    }
}

