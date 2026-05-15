using System.Text;
using Api.Application.Assignment;
using Api.Application.Common.Interfaces;
using Api.Application.Onboarding;
using Api.Application.RulesEngine;
using Api.Contracts;
using Api.Domain.Assignment;
using Api.Domain.Leads;
using Api.Domain.Pipeline;

namespace Api.Application.Pipeline;

public class PipelineService : IPipelineService
{
    private const string ManualActor = "user";
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadAssignmentRepository _leadAssignmentRepository;
    private readonly IPipelineStageRepository _pipelineStageRepository;
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IOpportunityStageHistoryRepository _historyRepository;
    private readonly IOnboardingService _onboardingService;
    private readonly IRuleEventListener _ruleEventListener;
    private readonly IStageWipLimitStore _stageWipLimitStore;
    private readonly ITenantContext _tenantContext;

    public PipelineService(
        ILeadRepository leadRepository,
        ILeadAssignmentRepository leadAssignmentRepository,
        IPipelineStageRepository pipelineStageRepository,
        IOpportunityRepository opportunityRepository,
        IOpportunityStageHistoryRepository historyRepository,
        IOnboardingService onboardingService,
        IRuleEventListener ruleEventListener,
        IStageWipLimitStore stageWipLimitStore,
        ITenantContext tenantContext)
    {
        _leadRepository = leadRepository;
        _leadAssignmentRepository = leadAssignmentRepository;
        _pipelineStageRepository = pipelineStageRepository;
        _opportunityRepository = opportunityRepository;
        _historyRepository = historyRepository;
        _onboardingService = onboardingService;
        _ruleEventListener = ruleEventListener;
        _stageWipLimitStore = stageWipLimitStore;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<PipelineStageResponse>> GetStagesAsync(CancellationToken cancellationToken)
    {
        await _pipelineStageRepository.SeedDefaultsIfEmptyAsync(cancellationToken);

        var stages = await _pipelineStageRepository.ListAsync(cancellationToken);
        var opportunities = await _opportunityRepository.ListAsync(cancellationToken);
        var stageNameById = stages.ToDictionary(x => x.Id, x => x.Name);
        var limits = _stageWipLimitStore.GetAll(_tenantContext.TenantId, stageNameById);
        var counts = opportunities.GroupBy(x => x.StageId).ToDictionary(x => x.Key, x => x.Count());

        return stages.Select(stage => MapStage(
            stage,
            counts.TryGetValue(stage.Id, out var count) ? count : 0,
            limits[stage.Id]))
            .ToList();
    }

    public async Task<PipelineBoardResponse> GetBoardAsync(PipelineBoardQueryRequest query, CancellationToken cancellationToken)
    {
        var boardData = await BuildBoardDataAsync(query, applyPaging: true, cancellationToken);

        return new PipelineBoardResponse
        {
            Page = boardData.Page,
            PageSize = boardData.PageSize,
            TotalCount = boardData.TotalCount,
            HasMore = boardData.HasMore,
            Stages = boardData.Stages,
            Opportunities = boardData.Opportunities
        };
    }

    public async Task<string> ExportBoardCsvAsync(PipelineBoardQueryRequest query, CancellationToken cancellationToken)
    {
        var boardData = await BuildBoardDataAsync(query, applyPaging: false, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("opportunityId,title,stage,ownerUserId,leadSource,leadScore,riskLabel,value,updatedAtUtc,versionToken");

        foreach (var item in boardData.Opportunities)
        {
            builder.AppendLine(string.Join(',',
                EscapeCsv(item.Id),
                EscapeCsv(item.Title),
                EscapeCsv(boardData.StageNamesById.TryGetValue(item.StageId, out var stageName) ? stageName : string.Empty),
                EscapeCsv(item.OwnerUserId?.ToString()),
                EscapeCsv(item.LeadSource),
                item.LeadScore.ToString(),
                EscapeCsv(item.RiskLabel),
                item.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                EscapeCsv(item.UpdatedAtUtc.ToString("O")),
                EscapeCsv(item.VersionToken)));
        }

        return builder.ToString();
    }

    public async Task<PipelineStageSlaAlertResponse> GetStageSlaAlertsAsync(int? defaultSlaHours, CancellationToken cancellationToken)
    {
        var stages = await GetStagesAsync(cancellationToken);
        var opportunities = await _opportunityRepository.ListAsync(cancellationToken);
        var stageNameById = stages.ToDictionary(x => x.Id, x => x.Name);

        var nowUtc = DateTime.UtcNow;
        var items = opportunities
            .Where(x => stageNameById.ContainsKey(x.StageId))
            .Select(opportunity =>
            {
                var stageName = stageNameById[opportunity.StageId];
                var stageSlaHours = ResolveStageSlaHours(stageName, defaultSlaHours);
                var minutesInStage = (int)Math.Max(0, (nowUtc - opportunity.UpdatedAtUtc).TotalMinutes);
                var slaMinutes = Math.Max(0, stageSlaHours * 60);
                var exceededByMinutes = Math.Max(0, minutesInStage - slaMinutes);
                var isBreached = minutesInStage >= slaMinutes;

                return new PipelineStageSlaAlertItemResponse
                {
                    OpportunityId = opportunity.Id,
                    LeadId = opportunity.LeadId,
                    StageId = opportunity.StageId,
                    StageName = stageName,
                    MinutesInStage = minutesInStage,
                    SlaMinutes = slaMinutes,
                    ExceededByMinutes = exceededByMinutes,
                    Severity = ResolveSeverity(exceededByMinutes),
                    IsBreached = isBreached
                };
            })
            .Where(x => x.IsBreached)
            .OrderByDescending(x => x.ExceededByMinutes)
            .ToList();

        return new PipelineStageSlaAlertResponse
        {
            TotalOpportunitiesEvaluated = opportunities.Count,
            TotalBreaches = items.Count,
            Items = items
        };
    }

    public async Task<OpportunityResponse> CreateOpportunityAsync(OpportunityCreateRequest request, CancellationToken cancellationToken)
    {
        await _pipelineStageRepository.SeedDefaultsIfEmptyAsync(cancellationToken);

        var title = NormalizeTitle(request.Title);
        var errors = ValidateCreate(request.LeadId, request.StageId, title, request.Value);
        if (errors.Count > 0)
        {
            throw new PipelineValidationException(errors);
        }

        if (!await _leadRepository.ExistsAsync(request.LeadId, cancellationToken))
        {
            throw new PipelineValidationException(new Dictionary<string, string[]>
            {
                ["leadId"] = ["Lead does not exist."]
            });
        }

        var stage = await _pipelineStageRepository.GetByIdAsync(request.StageId, cancellationToken);
        if (stage is null)
        {
            throw new PipelineValidationException(new Dictionary<string, string[]>
            {
                ["stageId"] = ["Stage does not exist."]
            });
        }

        await EnsureWipCapacityAsync(stage.Id, stage.Name, cancellationToken);

        var opportunity = new Opportunity(request.LeadId, request.StageId, title!, request.Value);
        await _opportunityRepository.AddAsync(opportunity, cancellationToken);

        return await MapOpportunityAsync(opportunity, cancellationToken);
    }

    public async Task<OpportunityResponse> MoveOpportunityStageAsync(Guid opportunityId, MoveOpportunityStageRequest request, CancellationToken cancellationToken)
    {
        await _pipelineStageRepository.SeedDefaultsIfEmptyAsync(cancellationToken);

        if (opportunityId == Guid.Empty)
        {
            throw new PipelineValidationException(new Dictionary<string, string[]>
            {
                ["opportunityId"] = ["OpportunityId is required."]
            });
        }

        if (request.TargetStageId == Guid.Empty)
        {
            throw new PipelineValidationException(new Dictionary<string, string[]>
            {
                ["targetStageId"] = ["TargetStageId is required."]
            });
        }

        var opportunity = await _opportunityRepository.GetByIdAsync(opportunityId, cancellationToken)
            ?? throw new PipelineNotFoundException($"Opportunity with id '{opportunityId}' was not found.");

        if (!string.IsNullOrWhiteSpace(request.ExpectedVersionToken) &&
            !string.Equals(opportunity.CurrentVersionToken, request.ExpectedVersionToken, StringComparison.Ordinal))
        {
            throw new PipelineConcurrencyException("The opportunity was modified by another process. Reload the board and retry the move.");
        }

        var targetStage = await _pipelineStageRepository.GetByIdAsync(request.TargetStageId, cancellationToken);
        if (targetStage is null)
        {
            throw new PipelineValidationException(new Dictionary<string, string[]>
            {
                ["targetStageId"] = ["Target stage does not exist."]
            });
        }

        var currentStageId = opportunity.StageId;
        if (currentStageId == request.TargetStageId)
        {
            return await MapOpportunityAsync(opportunity, cancellationToken);
        }

        var stages = await _pipelineStageRepository.ListAsync(cancellationToken);
        var currentStage = stages.FirstOrDefault(x => x.Id == currentStageId);
        var requestedStage = stages.FirstOrDefault(x => x.Id == request.TargetStageId);

        if (currentStage is null || requestedStage is null)
        {
            throw new PipelineValidationException(new Dictionary<string, string[]>
            {
                ["targetStageId"] = ["Invalid stage transition."]
            });
        }

        var orderDelta = requestedStage.Order - currentStage.Order;
        var isMoveToWon = string.Equals(requestedStage.Name, DefaultPipelineStages.Won.Name, StringComparison.OrdinalIgnoreCase);
        if (orderDelta > 1 && !isMoveToWon)
        {
            throw new PipelineValidationException(new Dictionary<string, string[]>
            {
                ["targetStageId"] = ["Cannot skip stages while moving forward."]
            });
        }

        if (orderDelta < 0 && string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new PipelineValidationException(new Dictionary<string, string[]>
            {
                ["reason"] = ["Reason is required when moving backward in the pipeline."]
            });
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new PipelineValidationException(new Dictionary<string, string[]>
            {
                ["reason"] = ["Reason is required for every stage transition."]
            });
        }

        await EnsureWipCapacityAsync(request.TargetStageId, requestedStage.Name, cancellationToken, opportunity.Id);

        opportunity.MoveToStage(request.TargetStageId);

        var history = new OpportunityStageHistory(
            opportunity.Id,
            currentStageId,
            request.TargetStageId,
            NormalizeReason(request.Reason),
            NormalizeActor(request.Actor),
            false);

        await _historyRepository.AddAsync(history, cancellationToken);
        await _opportunityRepository.SaveChangesAsync(cancellationToken);

        await _ruleEventListener.OnOpportunityStageChangedAsync(opportunity.Id, currentStage.Name, requestedStage.Name, cancellationToken);

        if (string.Equals(targetStage.Name, DefaultPipelineStages.Won.Name, StringComparison.OrdinalIgnoreCase))
        {
            await _onboardingService.EnsureForWonOpportunityAsync(opportunity.LeadId, cancellationToken);
        }

        var refreshedOpportunity = await _opportunityRepository.GetByIdAsync(opportunity.Id, cancellationToken) ?? opportunity;
        return await MapOpportunityAsync(refreshedOpportunity, cancellationToken);
    }

    public async Task<IReadOnlyList<OpportunityStageHistoryResponse>> GetOpportunityHistoryAsync(Guid opportunityId, CancellationToken cancellationToken)
    {
        var opportunity = await _opportunityRepository.GetByIdAsync(opportunityId, cancellationToken)
            ?? throw new PipelineNotFoundException($"Opportunity with id '{opportunityId}' was not found.");

        var history = await _historyRepository.ListByOpportunityAsync(opportunity.Id, cancellationToken);
        var stages = await _pipelineStageRepository.ListAsync(cancellationToken);
        var stageNames = stages.ToDictionary(x => x.Id, x => x.Name);

        return history
            .Select(x => new OpportunityStageHistoryResponse
            {
                Id = x.Id,
                OpportunityId = x.OpportunityId,
                FromStageId = x.FromStageId,
                ToStageId = x.ToStageId,
                FromStageName = stageNames.TryGetValue(x.FromStageId, out var fromStageName) ? fromStageName : string.Empty,
                ToStageName = stageNames.TryGetValue(x.ToStageId, out var toStageName) ? toStageName : string.Empty,
                Reason = x.Reason,
                Actor = x.Actor,
                IsAutomated = x.IsAutomated,
                ChangedAtUtc = x.ChangedAtUtc
            })
            .ToList();
    }

    public async Task<PipelineThroughputResponse> GetThroughputAsync(DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken cancellationToken)
    {
        await _pipelineStageRepository.SeedDefaultsIfEmptyAsync(cancellationToken);
        var stages = await _pipelineStageRepository.ListAsync(cancellationToken);
        var history = await _historyRepository.ListByChangedRangeAsync(startDateUtc, endDateUtc, cancellationToken);

        return new PipelineThroughputResponse
        {
            StartDateUtc = startDateUtc,
            EndDateUtc = endDateUtc,
            Items = stages.Select(stage => new PipelineStageThroughputItemResponse
            {
                StageId = stage.Id,
                StageName = stage.Name,
                EnteredCount = history.Count(x => x.ToStageId == stage.Id),
                ExitedCount = history.Count(x => x.FromStageId == stage.Id)
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<PipelineWipLimitResponse>> GetWipLimitsAsync(CancellationToken cancellationToken)
    {
        await _pipelineStageRepository.SeedDefaultsIfEmptyAsync(cancellationToken);
        var stages = await _pipelineStageRepository.ListAsync(cancellationToken);
        var stageNames = stages.ToDictionary(x => x.Id, x => x.Name);
        var limits = _stageWipLimitStore.GetAll(_tenantContext.TenantId, stageNames);

        return stages.Select(stage => new PipelineWipLimitResponse
        {
            StageId = stage.Id,
            StageName = stage.Name,
            Limit = limits[stage.Id]
        }).ToList();
    }

    public async Task<PipelineWipLimitResponse> UpdateWipLimitAsync(Guid stageId, PipelineWipLimitUpdateRequest request, CancellationToken cancellationToken)
    {
        await _pipelineStageRepository.SeedDefaultsIfEmptyAsync(cancellationToken);
        var stage = await _pipelineStageRepository.GetByIdAsync(stageId, cancellationToken)
            ?? throw new PipelineNotFoundException($"Stage with id '{stageId}' was not found.");

        var limit = _stageWipLimitStore.SetLimit(_tenantContext.TenantId, stageId, request.Limit);
        return new PipelineWipLimitResponse
        {
            StageId = stage.Id,
            StageName = stage.Name,
            Limit = limit
        };
    }

    private static string? NormalizeTitle(string? title)
    {
        return string.IsNullOrWhiteSpace(title)
            ? null
            : title.Trim().ToLowerInvariant();
    }

    private static string? NormalizeReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim();
    }

    private static Dictionary<string, string[]> ValidateCreate(Guid leadId, Guid stageId, string? title, decimal value)
    {
        var errors = new Dictionary<string, string[]>();

        if (leadId == Guid.Empty)
        {
            errors["leadId"] = ["LeadId is required."];
        }

        if (stageId == Guid.Empty)
        {
            errors["stageId"] = ["StageId is required."];
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["Opportunity title is required."];
        }

        if (value < 0)
        {
            errors["value"] = ["Opportunity value cannot be negative."];
        }

        return errors;
    }

    private PipelineStageResponse MapStage(PipelineStage stage, int opportunityCount, int wipLimit)
    {
        return new PipelineStageResponse
        {
            Id = stage.Id,
            Name = stage.Name,
            Order = stage.Order,
            Color = stage.Color,
            WipLimit = wipLimit,
            OpportunityCount = opportunityCount,
            IsOverWipLimit = opportunityCount > wipLimit
        };
    }

    private async Task<OpportunityResponse> MapOpportunityAsync(Opportunity opportunity, CancellationToken cancellationToken)
    {
        var leads = await _leadRepository.ListAsync(cancellationToken);
        var lead = leads.FirstOrDefault(x => x.Id == opportunity.LeadId);
        var assignments = await _leadAssignmentRepository.GetAllAsync(cancellationToken);
        var ownerUserId = assignments
            .Where(x => x.LeadId == opportunity.LeadId)
            .OrderByDescending(x => x.AssignedAtUtc)
            .Select(x => (Guid?)x.UserId)
            .FirstOrDefault();

        return new OpportunityResponse
        {
            Id = opportunity.Id,
            LeadId = opportunity.LeadId,
            StageId = opportunity.StageId,
            OwnerUserId = ownerUserId,
            Title = opportunity.Title,
            LeadSource = lead?.Source ?? string.Empty,
            LeadScore = lead?.Score ?? 0,
            RiskLabel = ResolveRiskLabel(opportunity, lead),
            Value = opportunity.Value,
            VersionToken = opportunity.CurrentVersionToken,
            CreatedAtUtc = opportunity.CreatedAtUtc,
            UpdatedAtUtc = opportunity.UpdatedAtUtc
        };
    }

    private async Task EnsureWipCapacityAsync(Guid stageId, string stageName, CancellationToken cancellationToken, Guid? excludedOpportunityId = null)
    {
        var opportunities = await _opportunityRepository.ListAsync(cancellationToken);
        var occupancy = opportunities.Count(x => x.StageId == stageId && x.Id != excludedOpportunityId);
        var limit = _stageWipLimitStore.GetLimit(_tenantContext.TenantId, stageId, stageName);
        if (occupancy >= limit)
        {
            throw new PipelineValidationException(new Dictionary<string, string[]>
            {
                ["targetStageId"] = [$"Stage '{stageName}' reached its WIP limit of {limit}."]
            });
        }
    }

    private async Task<BoardData> BuildBoardDataAsync(PipelineBoardQueryRequest? rawQuery, bool applyPaging, CancellationToken cancellationToken)
    {
        await _pipelineStageRepository.SeedDefaultsIfEmptyAsync(cancellationToken);

        var query = NormalizeQuery(rawQuery);
        var stages = await _pipelineStageRepository.ListAsync(cancellationToken);
        var opportunities = await _opportunityRepository.ListAsync(cancellationToken);
        var leads = await _leadRepository.ListAsync(cancellationToken);
        var assignments = await _leadAssignmentRepository.GetAllAsync(cancellationToken);

        var leadById = leads.ToDictionary(x => x.Id);
        var ownerByLeadId = assignments
            .GroupBy(x => x.LeadId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(a => a.AssignedAtUtc).First());

        var stageNamesById = stages.ToDictionary(x => x.Id, x => x.Name);
        var wipLimits = _stageWipLimitStore.GetAll(_tenantContext.TenantId, stageNamesById);

        var enriched = opportunities
            .Select(opportunity => BuildBoardOpportunity(opportunity, leadById, ownerByLeadId))
            .Where(x => MatchesQuery(x, query))
            .ToList();

        var ordered = Order(enriched, query).ToList();
        var totalCount = ordered.Count;
        var paged = applyPaging
            ? ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList()
            : ordered;

        var counts = enriched.GroupBy(x => x.Opportunity.StageId).ToDictionary(x => x.Key, x => x.Count());

        return new BoardData(
            query.Page,
            query.PageSize,
            totalCount,
            applyPaging && query.Page * query.PageSize < totalCount,
            stages.Select(stage => MapStage(stage, counts.TryGetValue(stage.Id, out var count) ? count : 0, wipLimits[stage.Id])).ToList(),
            paged.Select(x => x.Response).ToList(),
            stageNamesById);
    }

    private BoardOpportunity BuildBoardOpportunity(
        Opportunity opportunity,
        IReadOnlyDictionary<Guid, Lead> leadById,
        IReadOnlyDictionary<Guid, LeadAssignment> ownerByLeadId)
    {
        leadById.TryGetValue(opportunity.LeadId, out var lead);
        ownerByLeadId.TryGetValue(opportunity.LeadId, out var assignment);

        return new BoardOpportunity(
            opportunity,
            lead,
            assignment,
            new OpportunityResponse
            {
                Id = opportunity.Id,
                LeadId = opportunity.LeadId,
                StageId = opportunity.StageId,
                OwnerUserId = assignment?.UserId,
                Title = opportunity.Title,
                LeadSource = lead?.Source ?? string.Empty,
                LeadScore = lead?.Score ?? 0,
                RiskLabel = ResolveRiskLabel(opportunity, lead),
                Value = opportunity.Value,
                VersionToken = opportunity.CurrentVersionToken,
                CreatedAtUtc = opportunity.CreatedAtUtc,
                UpdatedAtUtc = opportunity.UpdatedAtUtc
            });
    }

    private static PipelineBoardQueryRequest NormalizeQuery(PipelineBoardQueryRequest? query)
    {
        var normalizedPage = Math.Max(1, query?.Page ?? 1);
        var normalizedPageSize = Math.Clamp(query?.PageSize ?? 100, 1, 250);

        return new PipelineBoardQueryRequest
        {
            OwnerUserId = query?.OwnerUserId,
            Source = string.IsNullOrWhiteSpace(query?.Source) ? null : query!.Source.Trim(),
            MinScore = query?.MinScore,
            MaxScore = query?.MaxScore,
            RiskLabel = string.IsNullOrWhiteSpace(query?.RiskLabel) ? null : query!.RiskLabel.Trim(),
            SortBy = string.IsNullOrWhiteSpace(query?.SortBy) ? "updatedat" : query!.SortBy.Trim().ToLowerInvariant(),
            SortDirection = string.IsNullOrWhiteSpace(query?.SortDirection) ? "desc" : query!.SortDirection.Trim().ToLowerInvariant(),
            Page = normalizedPage,
            PageSize = normalizedPageSize
        };
    }

    private static bool MatchesQuery(BoardOpportunity opportunity, PipelineBoardQueryRequest query)
    {
        if (query.OwnerUserId.HasValue && opportunity.Assignment?.UserId != query.OwnerUserId.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Source) && !string.Equals(opportunity.Lead?.Source, query.Source, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (query.MinScore.HasValue && (opportunity.Lead?.Score ?? 0) < query.MinScore.Value)
        {
            return false;
        }

        if (query.MaxScore.HasValue && (opportunity.Lead?.Score ?? 0) > query.MaxScore.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.RiskLabel) && !string.Equals(opportunity.Response.RiskLabel, query.RiskLabel, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static IOrderedEnumerable<BoardOpportunity> Order(IEnumerable<BoardOpportunity> opportunities, PipelineBoardQueryRequest query)
    {
        var descending = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var sortBy = query.SortBy ?? "updatedat";

        return (sortBy, descending) switch
        {
            ("score", true) => opportunities.OrderByDescending(x => x.Lead?.Score ?? 0).ThenByDescending(x => x.Opportunity.UpdatedAtUtc),
            ("score", false) => opportunities.OrderBy(x => x.Lead?.Score ?? 0).ThenByDescending(x => x.Opportunity.UpdatedAtUtc),
            ("value", true) => opportunities.OrderByDescending(x => x.Opportunity.Value).ThenByDescending(x => x.Opportunity.UpdatedAtUtc),
            ("value", false) => opportunities.OrderBy(x => x.Opportunity.Value).ThenByDescending(x => x.Opportunity.UpdatedAtUtc),
            ("source", true) => opportunities.OrderByDescending(x => x.Lead?.Source ?? string.Empty).ThenByDescending(x => x.Opportunity.UpdatedAtUtc),
            ("source", false) => opportunities.OrderBy(x => x.Lead?.Source ?? string.Empty).ThenByDescending(x => x.Opportunity.UpdatedAtUtc),
            ("title", true) => opportunities.OrderByDescending(x => x.Opportunity.Title).ThenByDescending(x => x.Opportunity.UpdatedAtUtc),
            ("title", false) => opportunities.OrderBy(x => x.Opportunity.Title).ThenByDescending(x => x.Opportunity.UpdatedAtUtc),
            ("risk", true) => opportunities.OrderByDescending(x => x.Response.RiskLabel).ThenByDescending(x => x.Opportunity.UpdatedAtUtc),
            ("risk", false) => opportunities.OrderBy(x => x.Response.RiskLabel).ThenByDescending(x => x.Opportunity.UpdatedAtUtc),
            ("createdat", true) => opportunities.OrderByDescending(x => x.Opportunity.CreatedAtUtc),
            ("createdat", false) => opportunities.OrderBy(x => x.Opportunity.CreatedAtUtc),
            ("updatedat", false) => opportunities.OrderBy(x => x.Opportunity.UpdatedAtUtc),
            _ => opportunities.OrderByDescending(x => x.Opportunity.UpdatedAtUtc)
        };
    }

    private static string ResolveRiskLabel(Opportunity opportunity, Lead? lead)
    {
        var score = lead?.Score ?? 0;
        var ageHours = (DateTime.UtcNow - opportunity.UpdatedAtUtc).TotalHours;

        if (score < 30 || ageHours >= 96)
        {
            return "high";
        }

        if (score < 60 || ageHours >= 48)
        {
            return "medium";
        }

        return "low";
    }

    private static string NormalizeActor(string? actor)
    {
        return string.IsNullOrWhiteSpace(actor)
            ? ManualActor
            : actor.Trim().ToLowerInvariant();
    }

    private static string EscapeCsv(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r'))
        {
            return $"\"{text.Replace("\"", "\"\"")}\"";
        }

        return text;
    }

    private sealed record BoardOpportunity(Opportunity Opportunity, Lead? Lead, LeadAssignment? Assignment, OpportunityResponse Response);
    private sealed record BoardData(int Page, int PageSize, int TotalCount, bool HasMore, List<PipelineStageResponse> Stages, List<OpportunityResponse> Opportunities, IReadOnlyDictionary<Guid, string> StageNamesById);

    private static int ResolveStageSlaHours(string stageName, int? defaultSlaHours)
    {
        if (defaultSlaHours.HasValue)
        {
            return Math.Max(0, defaultSlaHours.Value);
        }

        return stageName switch
        {
            "new" => 24,
            "qualified" => 48,
            "proposal" => 72,
            "won" => 120,
            _ => 48
        };
    }

    private static string ResolveSeverity(int exceededByMinutes)
    {
        if (exceededByMinutes >= 24 * 60)
        {
            return "critical";
        }

        if (exceededByMinutes >= 8 * 60)
        {
            return "high";
        }

        if (exceededByMinutes >= 2 * 60)
        {
            return "medium";
        }

        return "low";
    }
}