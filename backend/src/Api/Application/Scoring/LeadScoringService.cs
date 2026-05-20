using Api.Application.Common.Interfaces;
using Api.Contracts;
using Api.Domain.Leads;
using Api.Domain.Scoring;
using System.Text.Json;

namespace Api.Application.Scoring;


public class LeadScoringService : ILeadScoringService
{
    private static readonly LeadScoringFormula DefaultFormula = new();

    private readonly ILeadRepository _leadRepository;
    private readonly ILeadAuditSnapshotRepository _leadAuditSnapshotRepository;
    private readonly ILeadPriorityThresholdStore _thresholdStore;
    private readonly ILeadScoringFormulaStore _formulaStore;
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IPipelineStageRepository _pipelineStageRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ILeadScoringAIService _aiService;

    public LeadScoringService(
        ILeadRepository leadRepository,
        ILeadAuditSnapshotRepository leadAuditSnapshotRepository,
        ILeadPriorityThresholdStore thresholdStore,
        ILeadScoringFormulaStore formulaStore,
        IOpportunityRepository opportunityRepository,
        IPipelineStageRepository pipelineStageRepository,
        ITenantContext tenantContext,
        ILeadScoringAIService aiService)
    {
        _leadRepository = leadRepository;
        _leadAuditSnapshotRepository = leadAuditSnapshotRepository;
        _thresholdStore = thresholdStore;
        _formulaStore = formulaStore;
        _opportunityRepository = opportunityRepository;
        _pipelineStageRepository = pipelineStageRepository;
        _tenantContext = tenantContext;
        _aiService = aiService;
    }

    public async Task<LeadScoreResponse?> ScoreLeadAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        // Intentar obtener el score desde el microservicio de IA
        LeadAIScoreResult? aiResult = null;
        try
        {
            aiResult = await _aiService.PredictScoreAsync(leadId, cancellationToken);
        }
        catch
        {
            // Si falla, continuar con el método tradicional
        }

        if (aiResult != null)
        {
            lead.SetScore(aiResult.Score, aiResult.Priority, aiResult.ModelVersion);
            await _leadRepository.SaveChangesAsync(cancellationToken);
            await _leadAuditSnapshotRepository.AddAsync(
                new LeadAuditSnapshot(
                    lead.Id,
                    "lead.score.updated.ai",
                    "scoring-ai",
                    JsonSerializer.Serialize(new { lead.Score, lead.Priority, lead.ScoringVersion, lead.ScoredAtUtc })),
                cancellationToken);

            return new LeadScoreResponse
            {
                LeadId = lead.Id,
                Score = lead.Score,
                Priority = lead.Priority,
                ScoringVersion = lead.ScoringVersion,
                ScoredAtUtc = lead.ScoredAtUtc
            };
        }

        // Fallback: método tradicional
        var formula = await _formulaStore.GetCurrentAsync(_tenantContext.TenantId, cancellationToken);
        var (score, _) = EvaluateScore(lead.Email, lead.Phone, lead.Source, formula);
        var thresholds = await _thresholdStore.GetCurrentAsync(_tenantContext.TenantId, cancellationToken);
        var priority = ResolvePriority(score, thresholds);

        lead.SetScore(score, priority, formula.Version);
        await _leadRepository.SaveChangesAsync(cancellationToken);
        await _leadAuditSnapshotRepository.AddAsync(
            new LeadAuditSnapshot(
                lead.Id,
                "lead.score.updated",
                "scoring-engine",
                JsonSerializer.Serialize(new { lead.Score, lead.Priority, lead.ScoringVersion, lead.ScoredAtUtc })),
            cancellationToken);

        return new LeadScoreResponse
        {
            LeadId = lead.Id,
            Score = lead.Score,
            Priority = lead.Priority,
            ScoringVersion = lead.ScoringVersion,
            ScoredAtUtc = lead.ScoredAtUtc
        };
    }

    public async Task<LeadScoreResponse?> GetLeadScoreAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        return new LeadScoreResponse
        {
            LeadId = lead.Id,
            Score = lead.Score,
            Priority = lead.Priority,
            ScoringVersion = lead.ScoringVersion,
            ScoredAtUtc = lead.ScoredAtUtc
        };
    }

    public async Task<ScoringPriorityThresholdsResponse> GetPriorityThresholdsAsync(CancellationToken cancellationToken)
    {
        var thresholds = await _thresholdStore.GetCurrentAsync(_tenantContext.TenantId, cancellationToken);
        return new ScoringPriorityThresholdsResponse
        {
            HotMinScore = thresholds.HotMinScore,
            WarmMinScore = thresholds.WarmMinScore,
            ColdMaxScore = Math.Max(thresholds.WarmMinScore - 1, 0)
        };
    }

    public async Task<ScoringPriorityThresholdsResponse> UpdatePriorityThresholdsAsync(ScoringPriorityThresholdsRequest request, CancellationToken cancellationToken)
    {
        var thresholds = await _thresholdStore.UpdateAsync(_tenantContext.TenantId, request.HotMinScore, request.WarmMinScore, cancellationToken);
        return new ScoringPriorityThresholdsResponse
        {
            HotMinScore = thresholds.HotMinScore,
            WarmMinScore = thresholds.WarmMinScore,
            ColdMaxScore = Math.Max(thresholds.WarmMinScore - 1, 0)
        };
    }

    public async Task<ScoringFormulaResponse> GetCurrentFormulaAsync(CancellationToken cancellationToken)
    {
        var formula = await _formulaStore.GetCurrentAsync(_tenantContext.TenantId, cancellationToken);
        return MapFormula(formula);
    }

    public async Task<IReadOnlyList<ScoringFormulaResponse>> GetFormulaVersionsAsync(CancellationToken cancellationToken)
    {
        var versions = await _formulaStore.ListVersionsAsync(_tenantContext.TenantId, cancellationToken);
        return versions.Select(MapFormula).ToList();
    }

    public async Task<ScoringFormulaProposalResponse> CreateFormulaProposalAsync(ScoringFormulaProposalRequest request, CancellationToken cancellationToken)
    {
        var proposal = await _formulaStore.CreateProposalAsync(
            _tenantContext.TenantId,
            request.RequestedBy,
            new LeadScoringFormula
            {
                Version = request.Formula.Version,
                HasEmailPoints = request.Formula.HasEmailPoints,
                HasPhonePoints = request.Formula.HasPhonePoints,
                SourceReferralPoints = request.Formula.SourceReferralPoints,
                SourceWebPoints = request.Formula.SourceWebPoints,
                SourceAdsPoints = request.Formula.SourceAdsPoints,
                SourceOtherPoints = request.Formula.SourceOtherPoints,
                EmailPhoneBonusPoints = request.Formula.EmailPhoneBonusPoints,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        return MapProposal(proposal);
    }

    public async Task<ScoringFormulaProposalResponse?> ApproveFormulaProposalAsync(Guid proposalId, string approvedBy, CancellationToken cancellationToken)
    {
        var proposal = await _formulaStore.ApproveProposalAsync(_tenantContext.TenantId, proposalId, approvedBy, cancellationToken);
        return proposal is null ? null : MapProposal(proposal);
    }

    public async Task<IReadOnlyList<ScoringFormulaProposalResponse>> GetFormulaProposalsAsync(CancellationToken cancellationToken)
    {
        var proposals = await _formulaStore.ListProposalsAsync(_tenantContext.TenantId, cancellationToken);
        return proposals.Select(MapProposal).ToList();
    }

    public async Task<ScoreExplainabilityResponse?> GetLeadExplainabilityAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        var formula = await _formulaStore.GetCurrentAsync(_tenantContext.TenantId, cancellationToken);
        var (_, contributions) = EvaluateScore(lead.Email, lead.Phone, lead.Source, formula);

        return new ScoreExplainabilityResponse
        {
            LeadId = lead.Id,
            Score = lead.Score,
            Priority = lead.Priority,
            FormulaVersion = lead.ScoringVersion,
            Contributions = contributions.Select(x => new Api.Contracts.ScoreContributionItem
            {
                Key = x.Key,
                Description = x.Description,
                Points = x.Points,
                Applied = x.Applied
            }).ToList()
        };
    }

    public async Task<ScoringSimulationResponse> SimulateAsync(ScoringSimulationRequest request, CancellationToken cancellationToken)
    {
        var formula = await _formulaStore.GetCurrentAsync(_tenantContext.TenantId, cancellationToken);
        var thresholds = await _thresholdStore.GetCurrentAsync(_tenantContext.TenantId, cancellationToken);

        var results = request.Samples
            .Select((sample, index) =>
            {
                var (score, _) = EvaluateScore(sample.Email, sample.Phone, sample.Source, formula);
                return new ScoringSimulationResultItem
                {
                    Index = index,
                    Score = score,
                    Priority = ResolvePriority(score, thresholds)
                };
            })
            .ToList();

        return new ScoringSimulationResponse
        {
            Results = results,
            AverageScore = results.Count == 0 ? 0 : decimal.Round((decimal)results.Average(x => x.Score), 2),
            HighPriorityRatePercent = results.Count == 0
                ? 0
                : decimal.Round(100m * results.Count(x => x.Priority == LeadScorePriority.High) / results.Count, 2)
        };
    }

    public async Task<ScoringConversionLoopResponse> GetConversionLoopAsync(CancellationToken cancellationToken)
    {
        var leads = await _leadRepository.ListAsync(cancellationToken);
        var opportunities = await _opportunityRepository.ListAsync(cancellationToken);
        var stages = await _pipelineStageRepository.ListAsync(cancellationToken);
        var wonStageIds = stages
            .Where(x => string.Equals(x.Name, "won", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .ToHashSet();

        var wonLeadIds = opportunities
            .Where(x => wonStageIds.Contains(x.StageId))
            .Select(x => x.LeadId)
            .ToHashSet();

        var thresholds = await _thresholdStore.GetCurrentAsync(_tenantContext.TenantId, cancellationToken);
        var buckets = new[] { "hot", "warm", "cold" }
            .ToDictionary(x => x, _ => new List<Guid>());

        foreach (var lead in leads)
        {
            var bucket = lead.Score >= thresholds.HotMinScore
                ? "hot"
                : lead.Score >= thresholds.WarmMinScore
                    ? "warm"
                    : "cold";
            buckets[bucket].Add(lead.Id);
        }

        return new ScoringConversionLoopResponse
        {
            Buckets = buckets.Select(x =>
            {
                var won = x.Value.Count(wonLeadIds.Contains);
                return new ScoringConversionBucketItem
                {
                    Bucket = x.Key,
                    Leads = x.Value.Count,
                    Won = won,
                    ConversionRatePercent = x.Value.Count == 0 ? 0 : decimal.Round((won * 100m) / x.Value.Count, 2)
                };
            }).ToList()
        };
    }

    public async Task<ScoringDriftResponse> GetScoreDriftAsync(ScoringDriftQueryRequest request, CancellationToken cancellationToken)
    {
        if (request.CurrentSampleSize <= 0)
        {
            throw new ArgumentException("CurrentSampleSize must be greater than 0.", nameof(request.CurrentSampleSize));
        }

        if (request.BaselineSampleSize <= 0)
        {
            throw new ArgumentException("BaselineSampleSize must be greater than 0.", nameof(request.BaselineSampleSize));
        }

        if (request.DriftThresholdPercent <= 0)
        {
            throw new ArgumentException("DriftThresholdPercent must be greater than 0.", nameof(request.DriftThresholdPercent));
        }

        var leads = await _leadRepository.ListAsync(cancellationToken);
        var current = leads.Take(request.CurrentSampleSize).ToList();
        var baseline = leads.Skip(request.CurrentSampleSize).Take(request.BaselineSampleSize).ToList();

        var currentAverageScore = current.Count == 0 ? 0m : decimal.Round((decimal)current.Average(x => x.Score), 2);
        var baselineAverageScore = baseline.Count == 0 ? 0m : decimal.Round((decimal)baseline.Average(x => x.Score), 2);

        var currentHighRate = current.Count == 0
            ? 0m
            : decimal.Round(100m * current.Count(x => x.Priority == LeadScorePriority.High) / current.Count, 2);
        var baselineHighRate = baseline.Count == 0
            ? 0m
            : decimal.Round(100m * baseline.Count(x => x.Priority == LeadScorePriority.High) / baseline.Count, 2);

        var averageDeltaPercent = ComputePercentDelta(currentAverageScore, baselineAverageScore);
        var highRateDeltaPercent = ComputePercentDelta(currentHighRate, baselineHighRate);

        var signals = new List<string>();
        if (current.Count == 0 || baseline.Count == 0)
        {
            signals.Add("insufficient_samples");
        }
        else
        {
            if (averageDeltaPercent <= -request.DriftThresholdPercent)
            {
                signals.Add("average_score_drop");
            }

            if (highRateDeltaPercent <= -request.DriftThresholdPercent)
            {
                signals.Add("high_priority_rate_drop");
            }
        }

        return new ScoringDriftResponse
        {
            CurrentSampleCount = current.Count,
            BaselineSampleCount = baseline.Count,
            CurrentAverageScore = currentAverageScore,
            BaselineAverageScore = baselineAverageScore,
            AverageScoreDeltaPercent = averageDeltaPercent,
            CurrentHighPriorityRatePercent = currentHighRate,
            BaselineHighPriorityRatePercent = baselineHighRate,
            HighPriorityRateDeltaPercent = highRateDeltaPercent,
            DriftThresholdPercent = request.DriftThresholdPercent,
            HasDrift = signals.Any(x => x != "insufficient_samples"),
            DriftSignals = signals
        };
    }

    public async Task<ScoreRecalculationResponse> RecalculateScoresAsync(
        DateTime? startDateUtc,
        DateTime? endDateUtc,
        CancellationToken cancellationToken)
    {
        var leads = await _leadRepository.ListByCreatedRangeAsync(startDateUtc, endDateUtc, cancellationToken);
        var formula = await _formulaStore.GetCurrentAsync(_tenantContext.TenantId, cancellationToken);
        var thresholds = await _thresholdStore.GetCurrentAsync(_tenantContext.TenantId, cancellationToken);
        foreach (var lead in leads)
        {
            var (score, _) = EvaluateScore(lead.Email, lead.Phone, lead.Source, formula);
            var priority = ResolvePriority(score, thresholds);
            lead.SetScore(score, priority, formula.Version);
            await _leadAuditSnapshotRepository.AddAsync(
                new LeadAuditSnapshot(
                    lead.Id,
                    "lead.score.recalculated",
                    "scoring-engine",
                    JsonSerializer.Serialize(new { lead.Score, lead.Priority, lead.ScoringVersion, lead.ScoredAtUtc })),
                cancellationToken);
        }

        await _leadRepository.SaveChangesAsync(cancellationToken);
        return new ScoreRecalculationResponse
        {
            ProcessedLeads = leads.Count,
            ScoringVersion = formula.Version
        };
    }

    public IReadOnlyList<ScoreRuleResponse> GetRules()
    {
        var rules = BuildRules(DefaultFormula);
        return rules
            .Select(x => new ScoreRuleResponse
            {
                Key = x.Key,
                Description = x.Description,
                Points = x.Points
            })
            .ToList();
    }

    private static (int Score, List<ScoreContribution> Contributions) EvaluateScore(string? email, string? phone, string source, LeadScoringFormula formula)
    {
        var rules = BuildRules(formula);
        var score = 0;
        var contributions = new List<ScoreContribution>();

        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasPhone = !string.IsNullOrWhiteSpace(phone);

        score += ApplyRule(hasEmail, rules, "has_email", contributions);
        score += ApplyRule(hasPhone, rules, "has_phone", contributions);

        var sourceRule = source switch
        {
            "referral" => "source_referral",
            "web" => "source_web",
            "ads" => "source_ads",
            _ => "source_other"
        };

        score += ApplyRule(true, rules, sourceRule, contributions);
        score += ApplyRule(hasEmail && hasPhone, rules, "has_email_and_phone_bonus", contributions);

        return (Math.Clamp(score, 0, 100), contributions);
    }

    private static string ResolvePriority(int score, LeadPriorityThresholds thresholds)
    {
        if (score >= thresholds.HotMinScore)
        {
            return LeadScorePriority.High;
        }

        if (score >= thresholds.WarmMinScore)
        {
            return LeadScorePriority.Medium;
        }

        return LeadScorePriority.Low;
    }

    private static decimal ComputePercentDelta(decimal current, decimal baseline)
    {
        if (baseline == 0)
        {
            return current == 0 ? 0 : 100;
        }

        return decimal.Round(((current - baseline) / baseline) * 100m, 2);
    }

    private static IReadOnlyList<ScoreRule> BuildRules(LeadScoringFormula formula)
    {
        return
        [
            new("has_email", "Lead provides a valid email", formula.HasEmailPoints),
            new("has_phone", "Lead provides a valid phone", formula.HasPhonePoints),
            new("source_referral", "Lead source is referral", formula.SourceReferralPoints),
            new("source_web", "Lead source is web", formula.SourceWebPoints),
            new("source_ads", "Lead source is ads", formula.SourceAdsPoints),
            new("source_other", "Lead source is another channel", formula.SourceOtherPoints),
            new("has_email_and_phone_bonus", "Lead has both email and phone", formula.EmailPhoneBonusPoints)
        ];
    }

    private static int ApplyRule(bool applies, IReadOnlyList<ScoreRule> rules, string key, ICollection<ScoreContribution> contributions)
    {
        var rule = rules.First(x => x.Key == key);
        contributions.Add(new ScoreContribution(key, rule.Description, rule.Points, applies));
        return applies ? rule.Points : 0;
    }

    private static ScoringFormulaResponse MapFormula(LeadScoringFormula formula)
    {
        return new ScoringFormulaResponse
        {
            Version = formula.Version,
            HasEmailPoints = formula.HasEmailPoints,
            HasPhonePoints = formula.HasPhonePoints,
            SourceReferralPoints = formula.SourceReferralPoints,
            SourceWebPoints = formula.SourceWebPoints,
            SourceAdsPoints = formula.SourceAdsPoints,
            SourceOtherPoints = formula.SourceOtherPoints,
            EmailPhoneBonusPoints = formula.EmailPhoneBonusPoints,
            UpdatedAtUtc = formula.UpdatedAtUtc
        };
    }

    private static ScoringFormulaProposalResponse MapProposal(ScoringFormulaProposal proposal)
    {
        return new ScoringFormulaProposalResponse
        {
            ProposalId = proposal.ProposalId,
            Status = proposal.Status,
            RequestedBy = proposal.RequestedBy,
            RequestedAtUtc = proposal.RequestedAtUtc,
            ApprovedAtUtc = proposal.ApprovedAtUtc,
            ApprovedBy = proposal.ApprovedBy,
            Formula = MapFormula(proposal.Formula)
        };
    }

    private sealed record ScoreContribution(string Key, string Description, int Points, bool Applied);
}
