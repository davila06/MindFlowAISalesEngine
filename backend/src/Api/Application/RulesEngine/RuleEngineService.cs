using System.Diagnostics;
using System.Text.Json;
using Api.Application.Common.Interfaces;
using Api.Application.Onboarding;
using Api.Application.Sequences;
using Api.Contracts;
using Api.Domain.Leads;
using Api.Domain.Pipeline;
using Api.Domain.Rules;
using Api.Domain.Scoring;
using Microsoft.Extensions.Logging;

namespace Api.Application.RulesEngine;

public class RuleEngineService : IRuleService, IRuleEventListener
{
    private const string LeadCreatedTrigger = "lead.created";
    private const string StageChangedTrigger = "stage_changed";
    private const string LeadRespondedTrigger = "lead.responded";
    private const string ProposalSentTrigger = "proposal.sent";
    private const int DryRunSampleSize = 200;
    private const int MaxActionsPerExecution = 5;

    private static readonly HashSet<string> LeadConditionFields =
    [
        "source", "priority", "score", "has_email", "has_phone"
    ];

    private static readonly HashSet<string> StageConditionFields =
    [
        "source", "priority", "score", "has_email", "has_phone", "from_stage", "to_stage", "stage_name"
    ];

    private static readonly HashSet<string> AllowedOperators =
    [
        "eq", "neq", "contains", "gte", "lte"
    ];

    private static readonly HashSet<string> AllowedActionTypes =
    [
        "add_score", "set_priority", "move_stage", "enroll_sequence", "send_whatsapp"
    ];

    private readonly IRuleRepository _ruleRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly ILeadAuditSnapshotRepository _leadAuditSnapshotRepository;
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IPipelineStageRepository _pipelineStageRepository;
    private readonly IOpportunityStageHistoryRepository _opportunityStageHistoryRepository;
    private readonly IOnboardingService _onboardingService;
    private readonly ISequenceService _sequenceService;
    private readonly WhatsApp.WhatsAppService _whatsAppService;
    private readonly ILogger<RuleEngineService> _logger;

    public RuleEngineService(
        IRuleRepository ruleRepository,
        ILeadRepository leadRepository,
        ILeadAuditSnapshotRepository leadAuditSnapshotRepository,
        IOpportunityRepository opportunityRepository,
        IPipelineStageRepository pipelineStageRepository,
        IOpportunityStageHistoryRepository opportunityStageHistoryRepository,
        IOnboardingService onboardingService,
        ISequenceService sequenceService,
        WhatsApp.WhatsAppService whatsAppService,
        ILogger<RuleEngineService> logger)
    {
        _ruleRepository = ruleRepository;
        _leadRepository = leadRepository;
        _leadAuditSnapshotRepository = leadAuditSnapshotRepository;
        _opportunityRepository = opportunityRepository;
        _pipelineStageRepository = pipelineStageRepository;
        _opportunityStageHistoryRepository = opportunityStageHistoryRepository;
        _onboardingService = onboardingService;
        _sequenceService = sequenceService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<RuleResponse> CreateAsync(RuleCreateRequest request, CancellationToken cancellationToken)
    {
        var sanitized = Sanitize(request);
        ValidateOrThrow(sanitized);

        var rule = new Rule(
            sanitized.Name,
            sanitized.Trigger,
            sanitized.IsActive,
            sanitized.Priority,
            sanitized.ConflictPolicy,
            sanitized.ExecutionStartHourUtc,
            sanitized.ExecutionEndHourUtc,
            sanitized.CooldownMinutes,
            sanitized.AllowDestructiveActions,
            sanitized.Environment,
            sanitized.ApprovalStatus,
            sanitized.Conditions.Select(x => new RuleCondition(x.Field, x.Operator, x.Value)),
            sanitized.Actions.Select(x => new RuleAction(x.Type, x.Value)));

        await _ruleRepository.AddAsync(rule, cancellationToken);
        await PersistRevisionAsync(rule, "created", cancellationToken);
        return Map(rule);
    }

    public async Task<IReadOnlyList<RuleResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rules = await _ruleRepository.GetAllAsync(cancellationToken);
        return rules.Select(Map).ToList();
    }

    public async Task<RuleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        return rule is null ? null : Map(rule);
    }

    public async Task<RuleResponse?> UpdateAsync(Guid id, RuleUpdateRequest request, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        var sanitized = Sanitize(request);
        ValidateOrThrow(sanitized);

        var updatedConditions = sanitized.Conditions
            .Select(x => new RuleCondition(x.Field, x.Operator, x.Value))
            .ToList();
        var updatedActions = sanitized.Actions
            .Select(x => new RuleAction(x.Type, x.Value))
            .ToList();

        await PersistRevisionAsync(rule, "before_update", cancellationToken);

        rule.UpdateMetadata(
            sanitized.Name,
            sanitized.Trigger,
            sanitized.IsActive,
            sanitized.Priority,
            sanitized.ConflictPolicy,
            sanitized.ExecutionStartHourUtc,
            sanitized.ExecutionEndHourUtc,
            sanitized.CooldownMinutes,
            sanitized.AllowDestructiveActions,
            sanitized.Environment,
            sanitized.ApprovalStatus);

        await _ruleRepository.ReplaceDefinitionChildrenAsync(rule.Id, updatedConditions, updatedActions, cancellationToken);

        rule.ReplaceDefinitionChildren(updatedConditions, updatedActions);

        await _ruleRepository.SaveChangesAsync(cancellationToken);
        return Map(rule);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        _ruleRepository.Remove(rule);
        await _ruleRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RuleDriftSummaryResponse> GetDriftSummaryAsync(CancellationToken cancellationToken)
    {
        var rules = await _ruleRepository.GetAllAsync(cancellationToken);
        return new RuleDriftSummaryResponse
        {
            TotalRules = rules.Count,
            DraftRules = rules.Count(x => string.Equals(x.ApprovalStatus, "draft", StringComparison.OrdinalIgnoreCase)),
            RejectedRules = rules.Count(x => string.Equals(x.ApprovalStatus, "rejected", StringComparison.OrdinalIgnoreCase)),
            NonProductionActiveRules = rules.Count(x => x.IsActive && !string.Equals(x.Environment, "prod", StringComparison.OrdinalIgnoreCase)),
            ByEnvironment = rules
                .GroupBy(x => x.Environment)
                .OrderBy(x => x.Key)
                .Select(x => new RuleDriftEnvironmentResponse
                {
                    Environment = x.Key,
                    Count = x.Count()
                })
                .ToList()
        };
    }

    public async Task<RuleResponse?> PromoteAsync(Guid id, RulePromotionRequest request, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        var targetEnvironment = NormalizeEnvironment(request.TargetEnvironment);
        var approvedBy = string.IsNullOrWhiteSpace(request.ApprovedBy)
            ? "system"
            : request.ApprovedBy.Trim().ToLowerInvariant();

        await PersistRevisionAsync(rule, "before_promote", cancellationToken);
        rule.Promote(targetEnvironment, approvedBy);
        await _ruleRepository.SaveChangesAsync(cancellationToken);
        return Map(rule);
    }

    public async Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        if (!string.Equals(rule.ApprovalStatus, "approved", StringComparison.OrdinalIgnoreCase))
        {
            throw new RuleValidationException(new Dictionary<string, string[]>
            {
                ["approvalStatus"] = ["Rule must be approved before activation."]
            });
        }

        await PersistRevisionAsync(rule, "before_activate", cancellationToken);
        rule.Activate();
        await _ruleRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        await PersistRevisionAsync(rule, "before_deactivate", cancellationToken);
        rule.Deactivate();
        await _ruleRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<RuleDryRunResponse?> DryRunAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        var leads = await _ruleRepository.GetLeadsSnapshotAsync(DryRunSampleSize, cancellationToken);
        var totalEvaluated = leads.Count;
        var matchedCount = 0;
        var appliedCount = 0;
        var sample = new List<Guid>();

        foreach (var lead in leads)
        {
            var matched = rule.Trigger == StageChangedTrigger
                ? MatchesStageChangeRule(lead, "new", "qualified", rule.Conditions)
                : MatchesAll(lead, rule.Conditions);

            if (!matched)
            {
                continue;
            }

            matchedCount++;
            var applied = rule.Actions.Any(x => x.Type is "add_score" or "set_priority");
            if (applied)
            {
                appliedCount++;
            }

            if (sample.Count < 25)
            {
                sample.Add(lead.Id);
            }
        }

        var notes = rule.Trigger is LeadCreatedTrigger or LeadRespondedTrigger or ProposalSentTrigger or StageChangedTrigger
            ? null
            : "Dry-run uses lead snapshots and may not represent this trigger with full fidelity.";

        return new RuleDryRunResponse
        {
            RuleId = rule.Id,
            Trigger = rule.Trigger,
            TotalEvaluated = totalEvaluated,
            MatchedCount = matchedCount,
            AppliedCount = appliedCount,
            SampleEntityIds = sample,
            Notes = notes
        };
    }

    public async Task<RuleMetricsResponse?> GetMetricsAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        var logs = await _ruleRepository.GetExecutionLogsByRuleAsync(id, cancellationToken);
        if (logs.Count == 0)
        {
            return new RuleMetricsResponse { RuleId = id };
        }

        var total = logs.Count;
        var matched = logs.Count(x => x.Matched);
        var applied = logs.Count(x => x.Applied);

        return new RuleMetricsResponse
        {
            RuleId = id,
            TotalExecutions = total,
            MatchedExecutions = matched,
            AppliedExecutions = applied,
            MatchRatePercent = Math.Round((decimal)matched * 100m / total, 2),
            ApplyRatePercent = Math.Round((decimal)applied * 100m / total, 2),
            AverageDurationMs = Math.Round(logs.Average(x => x.DurationMs), 2),
            LastExecutedAtUtc = logs.Max(x => x.ExecutedAtUtc)
        };
    }

    public async Task<RuleResponse?> RollbackAsync(Guid id, int? targetVersion, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        var revision = await _ruleRepository.GetRevisionAsync(id, targetVersion, cancellationToken);
        if (revision is null)
        {
            return null;
        }

        var snapshot = JsonSerializer.Deserialize<RuleSnapshot>(revision.SnapshotJson);
        if (snapshot is null)
        {
            return null;
        }

        rule.Update(
            snapshot.Name,
            snapshot.Trigger,
            snapshot.IsActive,
            snapshot.Priority,
            snapshot.ConflictPolicy,
            snapshot.ExecutionStartHourUtc,
            snapshot.ExecutionEndHourUtc,
            snapshot.CooldownMinutes,
            snapshot.AllowDestructiveActions,
            snapshot.Environment,
            snapshot.ApprovalStatus,
            snapshot.Conditions.Select(x => new RuleCondition(x.Field, x.Operator, x.Value)),
            snapshot.Actions.Select(x => new RuleAction(x.Type, x.Value)));

        await _ruleRepository.SaveChangesAsync(cancellationToken);
        return Map(rule);
    }

    public Task<IReadOnlyList<RuleTemplateResponse>> GetTemplatesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RuleTemplateResponse> templates =
        [
            new RuleTemplateResponse
            {
                Key = "unknown-source-boost",
                Name = "Boost Unknown Source",
                Description = "Increase score when source is unknown for fast triage.",
                Template = new RuleCreateRequest
                {
                    Name = "Unknown source score boost",
                    Trigger = LeadCreatedTrigger,
                    IsActive = false,
                    Priority = 200,
                    ConflictPolicy = "first_wins",
                    Conditions =
                    [
                        new RuleConditionRequest { Field = "source", Operator = "eq", Value = "unknown" }
                    ],
                    Actions =
                    [
                        new RuleActionRequest { Type = "add_score", Value = "20" }
                    ]
                }
            },
            new RuleTemplateResponse
            {
                Key = "proposal-followup-priority",
                Name = "Proposal Sent Priority",
                Description = "Raise priority when proposal is sent.",
                Template = new RuleCreateRequest
                {
                    Name = "Proposal sent elevate priority",
                    Trigger = ProposalSentTrigger,
                    IsActive = false,
                    Priority = 180,
                    ConflictPolicy = "merge",
                    Conditions =
                    [
                        new RuleConditionRequest { Field = "score", Operator = "gte", Value = "60" }
                    ],
                    Actions =
                    [
                        new RuleActionRequest { Type = "set_priority", Value = "High" }
                    ]
                }
            },
            new RuleTemplateResponse
            {
                Key = "stage-auto-advance",
                Name = "Qualified to Proposal Auto Move",
                Description = "Auto-move stage with guardrails and conflict stop.",
                Template = new RuleCreateRequest
                {
                    Name = "Qualified to proposal auto move",
                    Trigger = StageChangedTrigger,
                    IsActive = false,
                    Priority = 300,
                    ConflictPolicy = "first_wins",
                    AllowDestructiveActions = false,
                    Conditions =
                    [
                        new RuleConditionRequest { Field = "from_stage", Operator = "eq", Value = "New" },
                        new RuleConditionRequest { Field = "to_stage", Operator = "eq", Value = "Qualified" },
                        new RuleConditionRequest { Field = "score", Operator = "gte", Value = "75" }
                    ],
                    Actions =
                    [
                        new RuleActionRequest { Type = "move_stage", Value = "Proposal" }
                    ]
                }
            }
        ];

        return Task.FromResult(templates);
    }

    public async Task<RuleFixtureTestResponse?> TestFixtureAsync(RuleFixtureTestRequest request, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        var trigger = NormalizeTrigger(request.Trigger);
        var fixtureLead = new Lead(
            request.Lead.HasEmail ? "fixture@novamind.test" : null,
            request.Lead.HasPhone ? "+15550000000" : null,
            request.Lead.Source);
        fixtureLead.SetScore(request.Lead.Score, request.Lead.Priority, "fixture");

        var matched = trigger == StageChangedTrigger
            ? MatchesStageChangeRule(fixtureLead, request.Lead.FromStage ?? "new", request.Lead.ToStage ?? "qualified", rule.Conditions)
            : MatchesAll(fixtureLead, rule.Conditions);

        var actionsApplied = new List<string>();
        var skipped = new List<string>();

        if (!matched)
        {
            return new RuleFixtureTestResponse
            {
                RuleId = rule.Id,
                Matched = false,
                Applied = false,
                ActionsApplied = actionsApplied,
                SkippedReasons = skipped
            };
        }

        foreach (var action in rule.Actions)
        {
            if (action.Type == "move_stage")
            {
                actionsApplied.Add($"move_stage:{action.Value}");
                continue;
            }

            if (ApplyAction(fixtureLead, action))
            {
                actionsApplied.Add($"{action.Type}:{action.Value}");
            }
            else
            {
                skipped.Add($"invalid_action:{action.Type}");
            }
        }

        return new RuleFixtureTestResponse
        {
            RuleId = rule.Id,
            Matched = true,
            Applied = actionsApplied.Count > 0,
            ActionsApplied = actionsApplied,
            SkippedReasons = skipped
        };
    }

    public async Task OnLeadCreatedAsync(Guid leadId, CancellationToken cancellationToken)
    {
        await ExecuteLeadRulesAsync(LeadCreatedTrigger, leadId, cancellationToken);
    }

    public async Task OnOpportunityStageChangedAsync(Guid opportunityId, string fromStageName, string toStageName, CancellationToken cancellationToken)
    {
        var opportunity = await _opportunityRepository.GetByIdAsync(opportunityId, cancellationToken);
        if (opportunity is null)
        {
            return;
        }

        var lead = await _leadRepository.GetByIdAsync(opportunity.LeadId, cancellationToken);
        if (lead is null)
        {
            return;
        }

        var rules = await _ruleRepository.GetActiveByTriggerAsync(StageChangedTrigger, cancellationToken);
        if (rules.Count == 0)
        {
            return;
        }

        await _pipelineStageRepository.SeedDefaultsIfEmptyAsync(cancellationToken);
        var stages = await _pipelineStageRepository.ListAsync(cancellationToken);
        var actionCounter = 0;

        foreach (var rule in rules.OrderByDescending(x => x.Priority).ThenByDescending(x => x.UpdatedAtUtc))
        {
            var sw = Stopwatch.StartNew();
            var matched = MatchesStageChangeRule(lead, fromStageName, toStageName, rule.Conditions);
            var skippedReason = await EvaluateSkipReasonAsync(rule, lead.Id, cancellationToken);

            if (!matched || skippedReason is not null)
            {
                sw.Stop();
                await _ruleRepository.AddExecutionLogAsync(
                    new RuleExecutionLog(rule.Id, StageChangedTrigger, "opportunity", opportunityId, matched, false, 0, skippedReason ?? "not_matched", (decimal)sw.Elapsed.TotalMilliseconds),
                    cancellationToken);
                continue;
            }

            foreach (var action in rule.Actions)
            {
                if (!string.Equals(action.Type, "move_stage", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(action.Value, fromStageName, StringComparison.OrdinalIgnoreCase))
                {
                    sw.Stop();
                    await _ruleRepository.AddExecutionLogAsync(
                        new RuleExecutionLog(rule.Id, StageChangedTrigger, "opportunity", opportunityId, true, false, 0, "loop_guard", (decimal)sw.Elapsed.TotalMilliseconds),
                        cancellationToken);
                    continue;
                }

                var targetStage = stages.FirstOrDefault(x => string.Equals(x.Name, action.Value, StringComparison.OrdinalIgnoreCase));
                if (targetStage is null || targetStage.Id == opportunity.StageId)
                {
                    continue;
                }

                var previousStageId = opportunity.StageId;
                opportunity.MoveToStage(targetStage.Id);
                await _opportunityStageHistoryRepository.AddAsync(
                    new OpportunityStageHistory(
                        opportunity.Id,
                        previousStageId,
                        targetStage.Id,
                        $"Auto-move by rule: {rule.Name}",
                        "rules-engine",
                        true),
                    cancellationToken);
                await _opportunityRepository.SaveChangesAsync(cancellationToken);

                actionCounter++;
                sw.Stop();
                await _ruleRepository.AddExecutionLogAsync(
                    new RuleExecutionLog(rule.Id, StageChangedTrigger, "opportunity", opportunityId, true, true, 1, null, (decimal)sw.Elapsed.TotalMilliseconds),
                    cancellationToken);

                if (string.Equals(targetStage.Name, DefaultPipelineStages.Won.Name, StringComparison.OrdinalIgnoreCase))
                {
                    await _onboardingService.EnsureForWonOpportunityAsync(opportunity.LeadId, cancellationToken);
                }

                _logger.LogInformation("Rules Engine auto-moved opportunity {OpportunityId} from {FromStage} to {ToStage} using rule {RuleName}", opportunity.Id, toStageName, targetStage.Name, rule.Name);

                if (actionCounter >= MaxActionsPerExecution || string.Equals(rule.ConflictPolicy, "first_wins", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
        }
    }

    public async Task OnLeadRespondedAsync(Guid leadId, CancellationToken cancellationToken)
    {
        await ExecuteLeadRulesAsync(LeadRespondedTrigger, leadId, cancellationToken);
    }

    public async Task OnProposalSentAsync(Guid leadId, Guid proposalId, CancellationToken cancellationToken)
    {
        await ExecuteLeadRulesAsync(ProposalSentTrigger, leadId, cancellationToken);
    }

    private async Task ExecuteLeadRulesAsync(string trigger, Guid leadId, CancellationToken cancellationToken)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead is null)
        {
            return;
        }

        var rules = await _ruleRepository.GetActiveByTriggerAsync(trigger, cancellationToken);
        if (rules.Count == 0)
        {
            return;
        }

        var changed = false;
        var actionCounter = 0;

        foreach (var rule in rules.OrderByDescending(x => x.Priority).ThenByDescending(x => x.UpdatedAtUtc))
        {
            var sw = Stopwatch.StartNew();
            var matched = MatchesAll(lead, rule.Conditions);
            var skippedReason = await EvaluateSkipReasonAsync(rule, lead.Id, cancellationToken);
            var actionsApplied = 0;

            if (matched && skippedReason is null)
            {
                foreach (var action in rule.Actions)
                {
                    if (ApplyAction(lead, action))
                    {
                        changed = true;
                        actionsApplied++;
                        actionCounter++;
                    }
                    else if (await ApplyAsyncActionAsync(lead, action, cancellationToken))
                    {
                        actionsApplied++;
                        actionCounter++;
                    }

                    if (actionCounter >= MaxActionsPerExecution)
                    {
                        skippedReason = "stop_condition_max_actions";
                        break;
                    }
                }
            }

            sw.Stop();
            await _ruleRepository.AddExecutionLogAsync(
                new RuleExecutionLog(rule.Id, trigger, "lead", lead.Id, matched, actionsApplied > 0, actionsApplied, skippedReason ?? (matched ? null : "not_matched"), (decimal)sw.Elapsed.TotalMilliseconds),
                cancellationToken);

            if (actionsApplied > 0 && string.Equals(rule.ConflictPolicy, "first_wins", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (actionCounter >= MaxActionsPerExecution)
            {
                break;
            }
        }

        if (changed)
        {
            await _leadRepository.SaveChangesAsync(cancellationToken);
            await _leadAuditSnapshotRepository.AddAsync(
                new LeadAuditSnapshot(
                    lead.Id,
                    "lead.rule.applied",
                    "rules-engine",
                    JsonSerializer.Serialize(new { lead.Score, lead.Priority, lead.ScoringVersion, lead.ScoredAtUtc, Trigger = trigger })),
                cancellationToken);
            _logger.LogInformation("Rules Engine applied changes for lead {LeadId} using trigger {Trigger}", leadId, trigger);
        }
    }

    private async Task<string?> EvaluateSkipReasonAsync(Rule rule, Guid entityId, CancellationToken cancellationToken)
    {
        if (!IsWithinExecutionWindow(rule, DateTime.UtcNow))
        {
            return "outside_time_window";
        }

        if (rule.CooldownMinutes <= 0)
        {
            return null;
        }

        var last = await _ruleRepository.GetLastAppliedExecutionAsync(rule.Id, entityId, cancellationToken);
        if (last is null)
        {
            return null;
        }

        return last.ExecutedAtUtc.AddMinutes(rule.CooldownMinutes) > DateTime.UtcNow
            ? "cooldown_active"
            : null;
    }

    private static bool IsWithinExecutionWindow(Rule rule, DateTime utcNow)
    {
        if (!rule.ExecutionStartHourUtc.HasValue && !rule.ExecutionEndHourUtc.HasValue)
        {
            return true;
        }

        if (!rule.ExecutionStartHourUtc.HasValue || !rule.ExecutionEndHourUtc.HasValue)
        {
            return false;
        }

        var currentHour = utcNow.Hour;
        var start = rule.ExecutionStartHourUtc.Value;
        var end = rule.ExecutionEndHourUtc.Value;

        if (start == end)
        {
            return true;
        }

        return start < end
            ? currentHour >= start && currentHour < end
            : currentHour >= start || currentHour < end;
    }

    private static bool MatchesAll(Lead lead, IReadOnlyList<RuleCondition> conditions)
    {
        foreach (var condition in conditions)
        {
            if (!Evaluate(lead, condition))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Evaluate(Lead lead, RuleCondition condition)
    {
        var op = condition.Operator;
        var field = condition.Field;
        var value = condition.Value;

        return field switch
        {
            "source" => CompareText(lead.Source, op, value),
            "priority" => CompareText(lead.Priority, op, value),
            "score" => CompareNumber(lead.Score, op, value),
            "has_email" => CompareBool(!string.IsNullOrWhiteSpace(lead.Email), op, value),
            "has_phone" => CompareBool(!string.IsNullOrWhiteSpace(lead.Phone), op, value),
            _ => false
        };
    }

    private static bool MatchesStageChangeRule(Lead lead, string fromStageName, string toStageName, IReadOnlyList<RuleCondition> conditions)
    {
        foreach (var condition in conditions)
        {
            if (!EvaluateStageChange(lead, fromStageName, toStageName, condition))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EvaluateStageChange(Lead lead, string fromStageName, string toStageName, RuleCondition condition)
    {
        return condition.Field switch
        {
            "from_stage" => CompareText(fromStageName, condition.Operator, condition.Value),
            "to_stage" => CompareText(toStageName, condition.Operator, condition.Value),
            "stage_name" => CompareText(toStageName, condition.Operator, condition.Value),
            "source" => CompareText(lead.Source, condition.Operator, condition.Value),
            "score" => CompareNumber(lead.Score, condition.Operator, condition.Value),
            _ => Evaluate(lead, condition)
        };
    }

    private static bool CompareText(string current, string op, string expected)
    {
        return op switch
        {
            "eq" => string.Equals(current, expected, StringComparison.OrdinalIgnoreCase),
            "neq" => !string.Equals(current, expected, StringComparison.OrdinalIgnoreCase),
            "contains" => current.Contains(expected, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool CompareNumber(int current, string op, string expected)
    {
        if (!int.TryParse(expected, out var parsed))
        {
            return false;
        }

        return op switch
        {
            "eq" => current == parsed,
            "neq" => current != parsed,
            "gte" => current >= parsed,
            "lte" => current <= parsed,
            _ => false
        };
    }

    private static bool CompareBool(bool current, string op, string expected)
    {
        if (!bool.TryParse(expected, out var parsed))
        {
            return false;
        }

        return op switch
        {
            "eq" => current == parsed,
            "neq" => current != parsed,
            _ => false
        };
    }

    private static bool ApplyAction(Lead lead, RuleAction action)
    {
        return action.Type switch
        {
            "add_score" => ApplyAddScore(lead, action.Value),
            "set_priority" => ApplySetPriority(lead, action.Value),
            _ => false
        };
    }

    private async Task<bool> ApplyAsyncActionAsync(Lead lead, RuleAction action, CancellationToken cancellationToken)
    {
        switch (action.Type)
        {
            case "enroll_sequence":
                if (!Guid.TryParse(action.Value, out var sequenceId))
                {
                    _logger.LogWarning("enroll_sequence action has invalid sequence id '{Value}'.", action.Value);
                    return false;
                }
                try
                {
                    await _sequenceService.EnrollLeadAsync(lead.Id, sequenceId, cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "enroll_sequence failed for lead {LeadId} / sequence {SequenceId}.", lead.Id, sequenceId);
                    return false;
                }

            case "send_whatsapp":
                // action.Value format: "<phone>|<message>"
                var parts = action.Value.Split('|', 2);
                if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]))
                {
                    _logger.LogWarning("send_whatsapp action value must be '<phone>|<message>'.");
                    return false;
                }
                try
                {
                    await _whatsAppService.SendTextAsync(parts[0].Trim(), parts[1], lead.Id, cancellationToken);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "send_whatsapp failed for lead {LeadId}.", lead.Id);
                    return false;
                }

            default:
                return false;
        }
    }

    private static bool ApplyAddScore(Lead lead, string value)
    {
        if (!int.TryParse(value, out var delta))
        {
            return false;
        }

        var newScore = Math.Clamp(lead.Score + delta, 0, 100);
        var priority = ResolvePriority(newScore);
        lead.SetScore(newScore, priority, "rule-engine");
        return true;
    }

    private static bool ApplySetPriority(Lead lead, string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        lead.SetScore(lead.Score, normalized, "rule-engine");
        return true;
    }

    private static string ResolvePriority(int score)
    {
        if (score >= LeadScorePriority.HighThreshold)
        {
            return LeadScorePriority.High;
        }

        if (score >= LeadScorePriority.MediumThreshold)
        {
            return LeadScorePriority.Medium;
        }

        return LeadScorePriority.Low;
    }

    private static RuleResponse Map(Rule rule)
    {
        return new RuleResponse
        {
            Id = rule.Id,
            Name = rule.Name,
            Trigger = rule.Trigger,
            IsActive = rule.IsActive,
            Priority = rule.Priority,
            ConflictPolicy = rule.ConflictPolicy,
            ExecutionStartHourUtc = rule.ExecutionStartHourUtc,
            ExecutionEndHourUtc = rule.ExecutionEndHourUtc,
            CooldownMinutes = rule.CooldownMinutes,
            AllowDestructiveActions = rule.AllowDestructiveActions,
            Version = rule.Version,
            Environment = rule.Environment,
            ApprovalStatus = rule.ApprovalStatus,
            ApprovedBy = rule.ApprovedBy,
            ApprovedAtUtc = rule.ApprovedAtUtc,
            CreatedAtUtc = rule.CreatedAtUtc,
            UpdatedAtUtc = rule.UpdatedAtUtc,
            Conditions = rule.Conditions.Select(x => new RuleConditionResponse
            {
                Id = x.Id,
                Field = x.Field,
                Operator = x.Operator,
                Value = x.Value
            }).ToList(),
            Actions = rule.Actions.Select(x => new RuleActionResponse
            {
                Id = x.Id,
                Type = x.Type,
                Value = x.Value
            }).ToList()
        };
    }

    private static string NormalizeEnvironment(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "dev"
            : value.Trim().ToLowerInvariant();

        return normalized switch
        {
            "dev" or "stg" or "prod" or "sandbox" => normalized,
            _ => "dev"
        };
    }

    private static string NormalizeApprovalStatus(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "approved"
            : value.Trim().ToLowerInvariant();

        return normalized switch
        {
            "draft" or "approved" or "rejected" => normalized,
            _ => "approved"
        };
    }

    private static string NormalizeTrigger(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? LeadCreatedTrigger
            : value.Trim().ToLowerInvariant();

        return normalized switch
        {
            "lead.created" => LeadCreatedTrigger,
            "pipeline.stage.changed" or "stage_changed" => StageChangedTrigger,
            "lead.responded" => LeadRespondedTrigger,
            "proposal.sent" => ProposalSentTrigger,
            _ => normalized
        };
    }

    private static string NormalizeConflictPolicy(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? "first_wins"
            : value.Trim().ToLowerInvariant();

        return normalized switch
        {
            "first_wins" or "merge" => normalized,
            _ => "first_wins"
        };
    }

    private static bool IsDestructiveAction(RuleActionRequest action)
    {
        return string.Equals(action.Type, "move_stage", StringComparison.OrdinalIgnoreCase)
               && (string.Equals(action.Value, "won", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(action.Value, "lost", StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateOrThrow(SanitizedRuleRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Conditions.Count == 0)
        {
            errors["conditions"] = ["At least one condition is required."];
        }

        if (request.Actions.Count == 0)
        {
            errors["actions"] = ["At least one action is required."];
        }

        if (request.ExecutionStartHourUtc.HasValue ^ request.ExecutionEndHourUtc.HasValue)
        {
            errors["executionWindow"] = ["Both executionStartHourUtc and executionEndHourUtc must be provided."];
        }

        var allowedFields = request.Trigger == StageChangedTrigger
            ? StageConditionFields
            : LeadConditionFields;

        foreach (var condition in request.Conditions)
        {
            if (!allowedFields.Contains(condition.Field))
            {
                errors[$"condition:{condition.Field}"] = [$"Field '{condition.Field}' is not allowed for trigger '{request.Trigger}'."];
            }

            if (!AllowedOperators.Contains(condition.Operator))
            {
                errors[$"operator:{condition.Operator}"] = [$"Operator '{condition.Operator}' is not supported."];
            }
        }

        foreach (var action in request.Actions)
        {
            if (!AllowedActionTypes.Contains(action.Type))
            {
                errors[$"action:{action.Type}"] = [$"Action '{action.Type}' is not supported."];
            }

            if (action.Type == "move_stage" && request.Trigger != StageChangedTrigger)
            {
                errors[$"action:{action.Type}"] = ["move_stage action is only allowed for stage_changed trigger."];
            }

            if (IsDestructiveAction(action) && !request.AllowDestructiveActions)
            {
                errors["allowDestructiveActions"] = ["Destructive stage movements require allowDestructiveActions=true."];
            }
        }

        if (errors.Count > 0)
        {
            throw new RuleValidationException(errors);
        }
    }

    private static SanitizedRuleRequest Sanitize(RuleCreateRequest request)
    {
        return new SanitizedRuleRequest
        {
            Name = request.Name.Trim(),
            Trigger = NormalizeTrigger(request.Trigger),
            IsActive = request.IsActive,
            Priority = request.Priority,
            ConflictPolicy = NormalizeConflictPolicy(request.ConflictPolicy),
            ExecutionStartHourUtc = request.ExecutionStartHourUtc,
            ExecutionEndHourUtc = request.ExecutionEndHourUtc,
            CooldownMinutes = Math.Max(0, request.CooldownMinutes),
            AllowDestructiveActions = request.AllowDestructiveActions,
            Environment = NormalizeEnvironment(request.Environment),
            ApprovalStatus = NormalizeApprovalStatus(request.ApprovalStatus),
            Conditions = request.Conditions
                .Select(x => new RuleConditionRequest
                {
                    Field = x.Field.Trim().ToLowerInvariant(),
                    Operator = x.Operator.Trim().ToLowerInvariant(),
                    Value = x.Value.Trim()
                })
                .ToList(),
            Actions = request.Actions
                .Select(x => new RuleActionRequest
                {
                    Type = x.Type.Trim().ToLowerInvariant(),
                    Value = x.Value.Trim()
                })
                .ToList()
        };
    }

    private static SanitizedRuleRequest Sanitize(RuleUpdateRequest request)
    {
        return new SanitizedRuleRequest
        {
            Name = request.Name.Trim(),
            Trigger = NormalizeTrigger(request.Trigger),
            IsActive = request.IsActive,
            Priority = request.Priority,
            ConflictPolicy = NormalizeConflictPolicy(request.ConflictPolicy),
            ExecutionStartHourUtc = request.ExecutionStartHourUtc,
            ExecutionEndHourUtc = request.ExecutionEndHourUtc,
            CooldownMinutes = Math.Max(0, request.CooldownMinutes),
            AllowDestructiveActions = request.AllowDestructiveActions,
            Environment = NormalizeEnvironment(request.Environment),
            ApprovalStatus = NormalizeApprovalStatus(request.ApprovalStatus),
            Conditions = request.Conditions
                .Select(x => new RuleConditionRequest
                {
                    Field = x.Field.Trim().ToLowerInvariant(),
                    Operator = x.Operator.Trim().ToLowerInvariant(),
                    Value = x.Value.Trim()
                })
                .ToList(),
            Actions = request.Actions
                .Select(x => new RuleActionRequest
                {
                    Type = x.Type.Trim().ToLowerInvariant(),
                    Value = x.Value.Trim()
                })
                .ToList()
        };
    }

    private async Task PersistRevisionAsync(Rule rule, string reason, CancellationToken cancellationToken)
    {
        var snapshot = new RuleSnapshot
        {
            Name = rule.Name,
            Trigger = rule.Trigger,
            IsActive = rule.IsActive,
            Priority = rule.Priority,
            ConflictPolicy = rule.ConflictPolicy,
            ExecutionStartHourUtc = rule.ExecutionStartHourUtc,
            ExecutionEndHourUtc = rule.ExecutionEndHourUtc,
            CooldownMinutes = rule.CooldownMinutes,
            AllowDestructiveActions = rule.AllowDestructiveActions,
            Environment = rule.Environment,
            ApprovalStatus = rule.ApprovalStatus,
            ApprovedBy = rule.ApprovedBy,
            ApprovedAtUtc = rule.ApprovedAtUtc,
            Conditions = rule.Conditions.Select(x => new RuleConditionSnapshot
            {
                Field = x.Field,
                Operator = x.Operator,
                Value = x.Value
            }).ToList(),
            Actions = rule.Actions.Select(x => new RuleActionSnapshot
            {
                Type = x.Type,
                Value = x.Value
            }).ToList()
        };

        var revision = new RuleRevision(rule.Id, rule.Version, JsonSerializer.Serialize(snapshot), reason);
        await _ruleRepository.AddRevisionAsync(revision, cancellationToken);
    }

    private sealed class SanitizedRuleRequest
    {
        public string Name { get; init; } = string.Empty;
        public string Trigger { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public int Priority { get; init; }
        public string ConflictPolicy { get; init; } = string.Empty;
        public int? ExecutionStartHourUtc { get; init; }
        public int? ExecutionEndHourUtc { get; init; }
        public int CooldownMinutes { get; init; }
        public bool AllowDestructiveActions { get; init; }
        public string Environment { get; init; } = string.Empty;
        public string ApprovalStatus { get; init; } = string.Empty;
        public List<RuleConditionRequest> Conditions { get; init; } = [];
        public List<RuleActionRequest> Actions { get; init; } = [];
    }

    private sealed class RuleSnapshot
    {
        public string Name { get; init; } = string.Empty;
        public string Trigger { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public int Priority { get; init; }
        public string ConflictPolicy { get; init; } = string.Empty;
        public int? ExecutionStartHourUtc { get; init; }
        public int? ExecutionEndHourUtc { get; init; }
        public int CooldownMinutes { get; init; }
        public bool AllowDestructiveActions { get; init; }
        public string Environment { get; init; } = string.Empty;
        public string ApprovalStatus { get; init; } = string.Empty;
        public string? ApprovedBy { get; init; }
        public DateTime? ApprovedAtUtc { get; init; }
        public List<RuleConditionSnapshot> Conditions { get; init; } = [];
        public List<RuleActionSnapshot> Actions { get; init; } = [];
    }

    private sealed class RuleConditionSnapshot
    {
        public string Field { get; init; } = string.Empty;
        public string Operator { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
    }

    private sealed class RuleActionSnapshot
    {
        public string Type { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
    }
}
