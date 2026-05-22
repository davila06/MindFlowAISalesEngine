using System.Net.Mail;
using Api.Application.Assignment;
using Api.Application.Common.Interfaces;
using Api.Application.DataGovernance;
using Api.Application.Email;
using Api.Application.FollowUp;
using Api.Application.RulesEngine;
using Api.Application.Scoring;
using Api.Contracts;
using Api.Domain.Leads;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhoneNumbers;

namespace Api.Application.Leads;

public class LeadIntakeService : ILeadIntakeService
{
    private const string DataAnomalyEventPrefix = "lead.data_anomaly.";

    private readonly ILeadRepository _leadRepository;
    private readonly ILeadCreatedEventPublisher _eventPublisher;
    private readonly IEmailService _emailService;
    private readonly IFollowUpService _followUpService;
    private readonly ILeadAssignmentService _leadAssignmentService;
    private readonly DataGovernanceOptions _dataGovernanceOptions;
    private readonly ITenantDataGovernanceStore _tenantDataGovernanceStore;
    private readonly ITenantContext _tenantContext;
    private readonly ILeadScoringService _leadScoringService;
    private readonly IRuleEventListener _ruleEventListener;
    private readonly ILeadAuditSnapshotRepository _leadAuditSnapshotRepository;
    private readonly ILeadActivityService _activityService;
    private readonly ILogger<LeadIntakeService> _logger;

    public LeadIntakeService(
        ILeadRepository leadRepository,
        ILeadCreatedEventPublisher eventPublisher,
        IEmailService emailService,
        IFollowUpService followUpService,
        ILeadAssignmentService leadAssignmentService,
        ILeadScoringService leadScoringService,
        IRuleEventListener ruleEventListener,
        ILeadAuditSnapshotRepository leadAuditSnapshotRepository,
        ITenantDataGovernanceStore tenantDataGovernanceStore,
        ITenantContext tenantContext,
        ILeadActivityService activityService,
        ILogger<LeadIntakeService> logger,
        IOptions<DataGovernanceOptions> dataGovernanceOptions)
    {
        _leadRepository = leadRepository;
        _eventPublisher = eventPublisher;
        _emailService = emailService;
        _followUpService = followUpService;
        _leadAssignmentService = leadAssignmentService;
        _leadScoringService = leadScoringService;
        _ruleEventListener = ruleEventListener;
        _leadAuditSnapshotRepository = leadAuditSnapshotRepository;
        _tenantDataGovernanceStore = tenantDataGovernanceStore;
        _tenantContext = tenantContext;
        _activityService = activityService;
        _logger = logger;
        _dataGovernanceOptions = dataGovernanceOptions.Value;
    }

    public async Task<LeadIntakeResponse> IntakeAsync(LeadIntakeRequest request, CancellationToken cancellationToken)
    {
        var effectiveGovernance = _tenantDataGovernanceStore.GetOrDefault(_tenantContext.TenantId, _dataGovernanceOptions);
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedPhone = NormalizePhone(request.Phone);
        var source = string.IsNullOrWhiteSpace(request.Source)
            ? LeadSourceCatalog.Other
            : LeadSourceCatalog.Normalize(request.Source);
        var channel = NormalizeToken(request.Channel, "inbound");
        var campaign = NormalizeToken(request.Campaign, "organic");
        var country = NormalizeCountry(request.Country);

        if (country.Length != 2)
        {
            throw new LeadIntakeValidationException(new Dictionary<string, string[]>
            {
                ["country"] = ["Country must use ISO-3166 alpha-2 format."]
            });
        }

        var errors = Validate(normalizedEmail, normalizedPhone, source);
        if (!IsPhoneValidForCountry(normalizedPhone, country))
        {
            errors["phone"] = ["Phone number is invalid for the provided country."];
        }

        if (errors.Count > 0)
        {
            throw new LeadIntakeValidationException(errors);
        }

        var leads = await _leadRepository.ListAsync(cancellationToken);
        var duplicate = leads.FirstOrDefault(existing =>
            (!string.IsNullOrWhiteSpace(existing.Email) && string.Equals(existing.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(existing.Phone) && string.Equals(existing.Phone, normalizedPhone, StringComparison.OrdinalIgnoreCase)) ||
            IsFuzzyDuplicate(existing, normalizedEmail, normalizedPhone, effectiveGovernance));

        if (duplicate is not null && effectiveGovernance.EnforceDuplicateRejection)
        {
            throw new LeadIntakeValidationException(new Dictionary<string, string[]>
            {
                ["lead"] = ["Duplicate lead detected."]
            });
        }

        var serviceInterest = string.IsNullOrWhiteSpace(request.ServiceInterest) ? null : request.ServiceInterest.Trim();
        var lead = new Lead(normalizedEmail, normalizedPhone, source, channel, campaign, country, serviceInterest);
        await _leadRepository.AddAsync(lead, cancellationToken);

        var intakePayload = JsonSerializer.Serialize(new
        {
            lead.Source,
            lead.Channel,
            lead.Campaign,
            lead.Country,
            lead.ServiceInterest,
            lead.Email,
            lead.Phone
        });
        await _leadAuditSnapshotRepository.AddAsync(
            new LeadAuditSnapshot(lead.Id, "lead.intake.created", "system", intakePayload),
            cancellationToken);
        await TrackDataAnomaliesAsync(lead, duplicate, cancellationToken);

        _logger.LogInformation(
            "Lead intake persisted with id {LeadId} from source {Source}",
            lead.Id,
            lead.Source);

        var leadCreatedEvent = new LeadCreatedEvent(
            lead.Id,
            lead.Email,
            lead.Phone,
            lead.Source,
            lead.CreatedAtUtc);

        await _eventPublisher.PublishAsync(leadCreatedEvent, cancellationToken);
        await _emailService.SendLeadWelcomeAsync(lead.Id, lead.Email, cancellationToken);
        await _leadScoringService.ScoreLeadAsync(lead.Id, cancellationToken);
        await _followUpService.ScheduleAsync(lead.Id, lead.Email, cancellationToken);
        await _leadAssignmentService.AssignLeadAsync(lead.Id, cancellationToken);
        await _ruleEventListener.OnLeadCreatedAsync(lead.Id, cancellationToken);
        await _activityService.RecordAsync(lead.Id, LeadActivity.ActivityTypes.LeadCreated,
            title: "Lead created",
            description: $"Source: {lead.Source} | Channel: {lead.Channel} | Campaign: {lead.Campaign}",
            cancellationToken: cancellationToken);
        var scoredLead = await _leadScoringService.GetLeadScoreAsync(lead.Id, cancellationToken);

        return new LeadIntakeResponse
        {
            Id = lead.Id,
            Email = lead.Email,
            Phone = lead.Phone,
            Source = lead.Source,
            Channel = lead.Channel,
            Campaign = lead.Campaign,
            Country = lead.Country,
            ServiceInterest = lead.ServiceInterest,
            Score = scoredLead?.Score ?? lead.Score,
            Priority = scoredLead?.Priority ?? lead.Priority,
            ScoringVersion = scoredLead?.ScoringVersion ?? lead.ScoringVersion,
            ScoredAtUtc = scoredLead?.ScoredAtUtc ?? lead.ScoredAtUtc,
            CreatedAtUtc = lead.CreatedAtUtc
        };
    }

    public async Task<MergeLeadsResponse> MergeAsync(MergeLeadsRequest request, CancellationToken cancellationToken)
    {
        if (request.PrimaryLeadId == request.DuplicateLeadId)
        {
            throw new LeadIntakeValidationException(new Dictionary<string, string[]>
            {
                ["duplicateLeadId"] = ["Primary and duplicate lead ids must be different."]
            });
        }

        var primary = await _leadRepository.GetByIdAsync(request.PrimaryLeadId, cancellationToken);
        var duplicate = await _leadRepository.GetByIdAsync(request.DuplicateLeadId, cancellationToken);
        var errors = new Dictionary<string, string[]>();

        if (primary is null)
        {
            errors["primaryLeadId"] = ["Primary lead not found."];
        }

        if (duplicate is null)
        {
            errors["duplicateLeadId"] = ["Duplicate lead not found."];
        }

        if (errors.Count > 0)
        {
            throw new LeadIntakeValidationException(errors);
        }

        await _leadAuditSnapshotRepository.AddAsync(
            new LeadAuditSnapshot(
                request.PrimaryLeadId,
                "lead.merged.primary",
                "system",
                JsonSerializer.Serialize(new { request.PrimaryLeadId, request.DuplicateLeadId, request.Reason })),
            cancellationToken);

        await _leadAuditSnapshotRepository.AddAsync(
            new LeadAuditSnapshot(
                request.DuplicateLeadId,
                "lead.merged.duplicate",
                "system",
                JsonSerializer.Serialize(new { request.PrimaryLeadId, request.DuplicateLeadId, request.Reason })),
            cancellationToken);

        return new MergeLeadsResponse
        {
            PrimaryLeadId = request.PrimaryLeadId,
            DuplicateLeadId = request.DuplicateLeadId,
            Reason = request.Reason.Trim(),
            MergedAtUtc = DateTime.UtcNow
        };
    }

    private static Dictionary<string, string[]> Validate(string? email, string? phone, string? source)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(source))
        {
            errors["source"] = ["Source is required."];
        }

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            errors["contact"] = ["At least one contact field is required (email or phone)."];
        }

        if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
        {
            errors["email"] = ["Email format is invalid."];
        }

        return errors;
    }

    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsPhoneValidForCountry(string? normalizedPhone, string country)
    {
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return true;
        }

        if (string.Equals(country, "xx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            var phoneNumberUtil = PhoneNumberUtil.GetInstance();
            var parsed = phoneNumberUtil.Parse(normalizedPhone, country.ToUpperInvariant());
            var validationResult = phoneNumberUtil.IsPossibleNumberWithReason(parsed);
            return validationResult is PhoneNumberUtil.ValidationResult.IS_POSSIBLE
                or PhoneNumberUtil.ValidationResult.IS_POSSIBLE_LOCAL_ONLY;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsFuzzyDuplicate(Lead existing, string? email, string? phone, DataGovernanceOptions options)
    {
        if (options.DedupEmailDistanceThreshold <= 0 && options.DedupPhoneSuffixLength <= 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(existing.Email) && !string.IsNullOrWhiteSpace(email))
        {
            var distance = LevenshteinDistance(existing.Email.ToLowerInvariant(), email.ToLowerInvariant());
            if (options.DedupEmailDistanceThreshold > 0
                && distance <= options.DedupEmailDistanceThreshold)
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(existing.Phone) && !string.IsNullOrWhiteSpace(phone))
        {
            var suffixLength = options.DedupPhoneSuffixLength;
            var existingDigits = new string(existing.Phone.Where(char.IsDigit).ToArray());
            var incomingDigits = new string(phone.Where(char.IsDigit).ToArray());
            if (suffixLength > 0 && existingDigits.Length >= suffixLength && incomingDigits.Length >= suffixLength)
            {
                return string.Equals(
                    existingDigits[^suffixLength..],
                    incomingDigits[^suffixLength..],
                    StringComparison.Ordinal);
            }
        }

        return false;
    }

    private async Task TrackDataAnomaliesAsync(Lead lead, Lead? duplicate, CancellationToken cancellationToken)
    {
        if (duplicate is not null)
        {
            var duplicatePayload = JsonSerializer.Serialize(new
            {
                LeadEmail = lead.Email,
                LeadPhone = lead.Phone,
                MatchedLeadId = duplicate.Id,
                MatchedLeadEmail = duplicate.Email,
                MatchedLeadPhone = duplicate.Phone
            });

            await _leadAuditSnapshotRepository.AddAsync(
                new LeadAuditSnapshot(
                    lead.Id,
                    $"{DataAnomalyEventPrefix}duplicate_candidate",
                    "system",
                    duplicatePayload),
                cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(lead.Email) || string.IsNullOrWhiteSpace(lead.Phone))
        {
            var completenessPayload = JsonSerializer.Serialize(new
            {
                lead.Email,
                lead.Phone,
                MissingEmail = string.IsNullOrWhiteSpace(lead.Email),
                MissingPhone = string.IsNullOrWhiteSpace(lead.Phone)
            });

            await _leadAuditSnapshotRepository.AddAsync(
                new LeadAuditSnapshot(
                    lead.Id,
                    $"{DataAnomalyEventPrefix}incomplete_contact",
                    "system",
                    completenessPayload),
                cancellationToken);
        }
    }

    private static string NormalizeToken(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim().ToLowerInvariant();
    }

    private static string NormalizeCountry(string? country)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            return "xx";
        }

        return country.Trim().ToLowerInvariant();
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        var matrix = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++)
        {
            matrix[i, 0] = i;
        }

        for (var j = 0; j <= b.Length; j++)
        {
            matrix[0, j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var substitutionCost = a[i - 1] == b[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + substitutionCost);
            }
        }

        return matrix[a.Length, b.Length];
    }
}
