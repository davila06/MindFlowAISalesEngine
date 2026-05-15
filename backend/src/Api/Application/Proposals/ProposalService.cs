using Api.Application.Common.Interfaces;
using Api.Application.Common.Security;
using Api.Application.Email;
using Api.Application.Observability;
using Api.Application.Onboarding;
using Api.Application.RulesEngine;
using Api.Contracts;
using Api.Domain.Leads;
using Api.Domain.Proposals;
using Microsoft.Extensions.Logging;

namespace Api.Application.Proposals;

public class ProposalService : IProposalService
{
    private const string DefaultTemplateName = "proposal.standard";
    private const string DefaultTemplateDisplayName = "Standard proposal";
    private const int DefaultProposalExpiryDays = 14;
    private static readonly TimeSpan ReminderDelay = TimeSpan.FromHours(72);
    private static readonly TimeSpan SmartReminderCooldown = TimeSpan.FromHours(24);
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMinutes(30);
    private const int MaxRetryAttempts = 3;

    private readonly ILeadRepository _leadRepository;
    private readonly IProposalRepository _proposalRepository;
    private readonly IProposalReminderJobRepository _proposalReminderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProposalPdfGenerator _pdfGenerator;
    private readonly IEmailService _emailService;
    private readonly IPoisonQueueAlertService _poisonQueueAlertService;
    private readonly IRuleEventListener _ruleEventListener;
    private readonly ILogger<ProposalService> _logger;

    public ProposalService(
        ILeadRepository leadRepository,
        IProposalRepository proposalRepository,
        IProposalReminderJobRepository proposalReminderRepository,
        ICustomerRepository customerRepository,
        IProposalPdfGenerator pdfGenerator,
        IEmailService emailService,
        IPoisonQueueAlertService poisonQueueAlertService,
        IRuleEventListener ruleEventListener,
        ILogger<ProposalService> logger)
    {
        _leadRepository = leadRepository;
        _proposalRepository = proposalRepository;
        _proposalReminderRepository = proposalReminderRepository;
        _customerRepository = customerRepository;
        _pdfGenerator = pdfGenerator;
        _emailService = emailService;
        _poisonQueueAlertService = poisonQueueAlertService;
        _ruleEventListener = ruleEventListener;
        _logger = logger;
    }

    public async Task<ProposalResponse> CreateAsync(CreateProposalRequest request, CancellationToken cancellationToken)
    {
        var title = NormalizeTitle(request.Title);
        var currency = NormalizeCurrency(request.Currency);
        var recipientName = NormalizeName(request.RecipientName);

        var errors = Validate(request.LeadId, title, request.Amount, currency);
        if (errors.Count > 0)
        {
            throw new ProposalValidationException(errors);
        }

        var lead = await _leadRepository.GetByIdAsync(request.LeadId, cancellationToken);
        if (lead is null)
        {
            throw new ProposalValidationException(new Dictionary<string, string[]>
            {
                ["leadId"] = ["Lead does not exist."]
            });
        }

        var template = await GetOrCreateCurrentTemplateAsync(DefaultTemplateName, cancellationToken);
        var proposal = BuildProposal(lead, title!, request.Amount, currency!, recipientName, template, null);

        await _proposalRepository.AddAsync(proposal, cancellationToken);
        await TrySendProposalAsync(proposal, cancellationToken);

        var reminderJob = new ProposalReminderJob(
            proposal.Id,
            proposal.LeadId,
            proposal.RecipientEmail,
            DateTime.UtcNow.Add(ReminderDelay));

        await _proposalReminderRepository.AddAsync(reminderJob, cancellationToken);

        _logger.LogInformation(
            "Proposal {ProposalId} created for lead {LeadId} with template {TemplateName} v{TemplateVersion}",
            proposal.Id,
            proposal.LeadId,
            proposal.TemplateName,
            proposal.TemplateVersion);

        return MapToResponse(proposal, reminderJob);
    }

    public async Task<ProposalTemplateResponse> CreateTemplateAsync(CreateProposalTemplateRequest request, CancellationToken cancellationToken)
    {
        var templateName = string.IsNullOrWhiteSpace(request.Name) ? DefaultTemplateName : request.Name.Trim();
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? templateName : request.DisplayName.Trim();

        if (string.Equals(templateName, DefaultTemplateName, StringComparison.OrdinalIgnoreCase))
        {
            await EnsureDefaultTemplateAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(request.HtmlBody))
        {
            throw new ProposalValidationException(new Dictionary<string, string[]>
            {
                ["htmlBody"] = ["HtmlBody is required."]
            });
        }

        var templates = await _proposalRepository.ListTemplatesAsync(cancellationToken);
        var sameName = templates.Where(x => string.Equals(x.Name, templateName, StringComparison.OrdinalIgnoreCase)).ToList();
        var nextVersion = sameName.Count == 0 ? 1 : sameName.Max(x => x.Version) + 1;

        if (request.MakeCurrent)
        {
            foreach (var current in sameName.Where(x => x.IsCurrent))
            {
                current.ClearCurrent();
            }
        }

        var template = new ProposalTemplate(templateName, displayName, request.HtmlBody.Trim(), nextVersion, request.MakeCurrent || sameName.Count == 0);
        await _proposalRepository.AddTemplateAsync(template, cancellationToken);
        await _proposalRepository.SaveChangesAsync(cancellationToken);

        return MapTemplate(template);
    }

    public async Task<IReadOnlyList<ProposalTemplateResponse>> ListTemplatesAsync(CancellationToken cancellationToken)
    {
        await EnsureDefaultTemplateAsync(cancellationToken);
        var templates = await _proposalRepository.ListTemplatesAsync(cancellationToken);
        return templates.Select(MapTemplate).ToList();
    }

    public async Task<ProposalResponse?> GetByIdAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, cancellationToken);
        if (proposal is null)
        {
            return null;
        }

        var reminder = await _proposalReminderRepository.GetByProposalIdAsync(proposal.Id, cancellationToken);
        return MapToResponse(proposal, reminder);
    }

    public async Task<IReadOnlyList<ProposalResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var proposals = await _proposalRepository.ListAsync(cancellationToken);
        var responses = new List<ProposalResponse>(proposals.Count);

        foreach (var proposal in proposals)
        {
            var reminder = await _proposalReminderRepository.GetByProposalIdAsync(proposal.Id, cancellationToken);
            responses.Add(MapToResponse(proposal, reminder));
        }

        return responses;
    }

    public async Task<IReadOnlyList<ProposalReminderJobResponse>> GetReminderDeadLetterAsync(CancellationToken cancellationToken)
    {
        var jobs = await _proposalReminderRepository.GetDeadLetterAsync(cancellationToken);
        return jobs.Select(MapReminderJob).ToList();
    }

    public async Task<IReadOnlyList<ProposalReminderJobResponse>> GetReminderPoisonQueueAsync(CancellationToken cancellationToken)
    {
        var jobs = await _proposalReminderRepository.GetPoisonQueueAsync(cancellationToken);
        return jobs.Select(MapReminderJob).ToList();
    }

    public async Task<(byte[] PdfBytes, string FileName)?> GetPdfAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, cancellationToken);
        return proposal is null ? null : (proposal.PdfContent, proposal.PdfFileName);
    }

    public async Task<ProposalResponse?> SignAsync(Guid proposalId, ProposalSignRequest request, CancellationToken cancellationToken)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, cancellationToken);
        if (proposal is null)
        {
            return null;
        }

        proposal.Sign(request.SignerName, request.SignerEmail);
        await _proposalRepository.SaveChangesAsync(cancellationToken);
        var reminder = await _proposalReminderRepository.GetByProposalIdAsync(proposal.Id, cancellationToken);
        return MapToResponse(proposal, reminder);
    }

    public async Task<ProposalResponse?> ExpireAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        var proposal = await _proposalRepository.GetByIdAsync(proposalId, cancellationToken);
        if (proposal is null)
        {
            return null;
        }

        proposal.Expire();
        await _proposalRepository.SaveChangesAsync(cancellationToken);
        var reminder = await _proposalReminderRepository.GetByProposalIdAsync(proposal.Id, cancellationToken);
        return MapToResponse(proposal, reminder);
    }

    public async Task<ProposalResponse?> RenewAsync(Guid proposalId, ProposalRenewRequest request, CancellationToken cancellationToken)
    {
        var sourceProposal = await _proposalRepository.GetByIdAsync(proposalId, cancellationToken);
        if (sourceProposal is null)
        {
            return null;
        }

        var lead = await _leadRepository.GetByIdAsync(sourceProposal.LeadId, cancellationToken);
        if (lead is null)
        {
            return null;
        }

        var template = await GetOrCreateCurrentTemplateAsync(sourceProposal.TemplateName, cancellationToken);
        var renewedProposal = BuildProposal(
            lead,
            sourceProposal.Title,
            sourceProposal.Amount,
            sourceProposal.Currency,
            sourceProposal.RecipientName,
            template,
            sourceProposal.Id);

        await _proposalRepository.AddAsync(renewedProposal, cancellationToken);
        renewedProposal.MarkSent(request.NewExpiryDays <= 0 ? DefaultProposalExpiryDays : request.NewExpiryDays);
        sourceProposal.MarkRenewed();
        await _proposalRepository.SaveChangesAsync(cancellationToken);

        var reminder = new ProposalReminderJob(
            renewedProposal.Id,
            renewedProposal.LeadId,
            renewedProposal.RecipientEmail,
            DateTime.UtcNow.Add(ReminderDelay));

        await _proposalReminderRepository.AddAsync(reminder, cancellationToken);
        return MapToResponse(renewedProposal, reminder);
    }

    public async Task<ProposalKpiResponse> GetKpisAsync(CancellationToken cancellationToken)
    {
        var proposals = await _proposalRepository.ListAsync(cancellationToken);
        var leadIds = proposals.Select(x => x.LeadId).Distinct().ToArray();
        var customers = leadIds.Length == 0
            ? []
            : await _customerRepository.ListByLeadIdsAsync(leadIds, cancellationToken);

        var reminderJobs = new List<ProposalReminderJob>();
        foreach (var proposal in proposals)
        {
            var reminder = await _proposalReminderRepository.GetByProposalIdAsync(proposal.Id, cancellationToken);
            if (reminder is not null)
            {
                reminderJobs.Add(reminder);
            }
        }

        var total = proposals.Count;
        var viewed = proposals.Count(x => x.ViewCount > 0 || string.Equals(x.Status, ProposalStatus.Viewed, StringComparison.Ordinal));
        var signed = proposals.Count(x => string.Equals(x.Status, ProposalStatus.Signed, StringComparison.Ordinal));
        var won = customers.Count;

        return new ProposalKpiResponse
        {
            TotalProposals = total,
            SignedProposals = signed,
            ProposalToWonRate = total == 0 ? 0 : Math.Round((decimal)won / total * 100m, 2),
            TrackedProposals = viewed,
            AverageViewsPerProposal = total == 0 ? 0 : Math.Round(proposals.Average(x => (decimal)x.ViewCount), 2)
        };
    }

    public async Task ForceReminderDueAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        var reminder = await _proposalReminderRepository.GetByProposalIdAsync(proposalId, cancellationToken);
        if (reminder is null)
        {
            return;
        }

        reminder.ForceDue();
        await _proposalReminderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RequeueReminderAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        var reminder = await _proposalReminderRepository.GetByProposalIdAsync(proposalId, cancellationToken);
        if (reminder is null)
        {
            return;
        }

        if (!string.Equals(reminder.Status, ProposalReminderStatus.Failed, StringComparison.Ordinal)
            && !string.Equals(reminder.Status, ProposalReminderStatus.Poisoned, StringComparison.Ordinal))
        {
            return;
        }

        reminder.Requeue(DateTime.UtcNow);
        await _proposalReminderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteDueRemindersAsync(CancellationToken cancellationToken)
    {
        var dueJobs = await _proposalReminderRepository.GetScheduledDueAsync(DateTime.UtcNow, cancellationToken);
        if (dueJobs.Count == 0)
        {
            return;
        }

        foreach (var job in dueJobs)
        {
            var proposal = await _proposalRepository.GetByIdAsync(job.ProposalId, cancellationToken);
            if (proposal is null)
            {
                await HandleRetryFailureAsync(job, "ProposalNotFound", cancellationToken);
                await _proposalReminderRepository.SaveChangesAsync(cancellationToken);
                continue;
            }

            if (proposal.LastViewedAtUtc.HasValue
                && proposal.LastViewedAtUtc.Value >= DateTime.UtcNow.Subtract(SmartReminderCooldown)
                && !string.Equals(proposal.Status, ProposalStatus.Signed, StringComparison.Ordinal)
                && !string.Equals(proposal.Status, ProposalStatus.Expired, StringComparison.Ordinal))
            {
                var rescheduledDueAt = DateTime.UtcNow.Add(SmartReminderCooldown);
                proposal.RescheduleReminderWindow(1);
                job.ScheduleRetry(rescheduledDueAt, "TrackedRecently");
                await _proposalRepository.SaveChangesAsync(cancellationToken);
                await _proposalReminderRepository.SaveChangesAsync(cancellationToken);
                continue;
            }

            try
            {
                var trackingUrl = $"/api/proposals/track/{proposal.TrackingToken}";
                var sent = await _emailService.SendProposalReminderAsync(
                    proposal.LeadId,
                    proposal.RecipientEmail,
                    proposal.RecipientName ?? "Prospect",
                    proposal.Title,
                    trackingUrl,
                    cancellationToken);

                if (sent)
                {
                    job.MarkSent();
                }
                else
                {
                    await HandleRetryFailureAsync(job, "ReminderNotSent", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleRetryFailureAsync(job, ex.Message, cancellationToken);
            }

            await _proposalReminderRepository.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task TrackAsync(string trackingToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(trackingToken))
        {
            return;
        }

        var proposal = await _proposalRepository.GetByTrackingTokenAsync(trackingToken.Trim(), cancellationToken);
        if (proposal is null)
        {
            return;
        }

        proposal.TrackView();
        await _proposalRepository.SaveChangesAsync(cancellationToken);
    }

    private Proposal BuildProposal(
        Lead lead,
        string title,
        decimal amount,
        string currency,
        string? recipientName,
        ProposalTemplate template,
        Guid? renewedFromProposalId)
    {
        var pdfBytes = _pdfGenerator.Generate(
            title,
            amount,
            currency,
            recipientName ?? "Prospect",
            DateTime.UtcNow,
            "/api/proposals/track/{trackingToken}");

        return new Proposal(
            lead.Id,
            title,
            amount,
            currency,
            recipientName,
            lead.Email,
            template.Name,
            template.Version,
            BuildSafeFileName(title),
            pdfBytes,
            renewedFromProposalId);
    }

    private async Task TrySendProposalAsync(Proposal proposal, CancellationToken cancellationToken)
    {
        var resolvedTrackingUrl = $"/api/proposals/track/{proposal.TrackingToken}";
        var sent = await _emailService.SendProposalAsync(
            proposal.LeadId,
            proposal.RecipientEmail,
            proposal.RecipientName ?? "Prospect",
            proposal.Title,
            proposal.Amount,
            proposal.Currency,
            resolvedTrackingUrl,
            proposal.PdfContent,
            proposal.PdfFileName,
            cancellationToken);

        if (!sent)
        {
            return;
        }

        proposal.MarkSent(DefaultProposalExpiryDays);
        await _proposalRepository.SaveChangesAsync(cancellationToken);
        await _ruleEventListener.OnProposalSentAsync(proposal.LeadId, proposal.Id, cancellationToken);
    }

    private async Task<ProposalTemplate> GetOrCreateCurrentTemplateAsync(string templateName, CancellationToken cancellationToken)
    {
        var current = await _proposalRepository.GetCurrentTemplateAsync(templateName, cancellationToken);
        if (current is not null)
        {
            return current;
        }

        await EnsureDefaultTemplateAsync(cancellationToken);
        return (await _proposalRepository.GetCurrentTemplateAsync(templateName, cancellationToken))!;
    }

    private async Task EnsureDefaultTemplateAsync(CancellationToken cancellationToken)
    {
        var current = await _proposalRepository.GetCurrentTemplateAsync(DefaultTemplateName, cancellationToken);
        if (current is not null)
        {
            return;
        }

        var template = new ProposalTemplate(
            DefaultTemplateName,
            DefaultTemplateDisplayName,
            "<section><h1>Proposal</h1><p>Default commercial proposal template.</p></section>",
            1,
            isCurrent: true);

        await _proposalRepository.AddTemplateAsync(template, cancellationToken);
        await _proposalRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleRetryFailureAsync(ProposalReminderJob job, string errorMessage, CancellationToken cancellationToken)
    {
        if (job.AttemptNumber >= MaxRetryAttempts)
        {
            job.MarkPoisoned(errorMessage);
            var queueDepth = await _proposalReminderRepository.CountPoisonedAsync(cancellationToken) + 1;
            await _poisonQueueAlertService.NotifyGrowthAsync("proposal-reminder", queueDepth, cancellationToken);
            return;
        }

        var nextAttempt = job.AttemptNumber + 1;
        job.ScheduleRetry(DateTime.UtcNow.Add(GetRetryDelay(nextAttempt)), errorMessage);
    }

    private static TimeSpan GetRetryDelay(int attemptNumber)
    {
        return TimeSpan.FromMinutes(RetryBaseDelay.TotalMinutes * attemptNumber);
    }

    private static ProposalResponse MapToResponse(Proposal proposal, ProposalReminderJob? reminder)
    {
        return new ProposalResponse
        {
            Id = proposal.Id,
            LeadId = proposal.LeadId,
            Title = proposal.Title,
            Amount = proposal.Amount,
            Currency = proposal.Currency,
            Status = proposal.Status,
            TemplateVersion = proposal.TemplateVersion,
            TrackingToken = proposal.TrackingToken,
            ViewCount = proposal.ViewCount,
            LastViewedAtUtc = proposal.LastViewedAtUtc,
            ExpiresAtUtc = proposal.ExpiresAtUtc,
            SignedAtUtc = proposal.SignedAtUtc,
            RenewedFromProposalId = proposal.RenewedFromProposalId,
            ReminderCount = reminder is null ? 0 : 1,
            ReminderStatus = reminder?.Status ?? "None",
            ReminderAttemptNumber = reminder?.AttemptNumber ?? 0,
            CreatedAtUtc = proposal.CreatedAtUtc,
            SentAtUtc = proposal.SentAtUtc
        };
    }

    private static ProposalTemplateResponse MapTemplate(ProposalTemplate template)
    {
        return new ProposalTemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            DisplayName = template.DisplayName,
            HtmlBody = template.HtmlBody,
            Version = template.Version,
            IsCurrent = template.IsCurrent,
            CreatedAtUtc = template.CreatedAtUtc
        };
    }

    private static ProposalReminderJobResponse MapReminderJob(ProposalReminderJob job)
    {
        return new ProposalReminderJobResponse
        {
            Id = job.Id,
            ProposalId = job.ProposalId,
            LeadId = job.LeadId,
            ToEmail = PiiMasking.MaskEmail(job.ToEmail),
            Status = job.Status,
            AttemptNumber = job.AttemptNumber,
            ScheduledAtUtc = job.ScheduledAtUtc,
            DueAtUtc = job.DueAtUtc,
            ExecutedAtUtc = job.ExecutedAtUtc,
            ErrorMessage = job.ErrorMessage
        };
    }

    private static string? NormalizeTitle(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeCurrency(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? NormalizeName(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Dictionary<string, string[]> Validate(Guid leadId, string? title, decimal amount, string? currency)
    {
        var errors = new Dictionary<string, string[]>();

        if (leadId == Guid.Empty)
        {
            errors["leadId"] = ["LeadId is required."];
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = ["Title is required."];
        }

        if (amount <= 0)
        {
            errors["amount"] = ["Amount must be greater than zero."];
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            errors["currency"] = ["Currency is required."];
        }

        return errors;
    }

    private static string BuildSafeFileName(string title)
    {
        var safe = new string(title.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = "proposal";
        }

        return $"{safe}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
    }
}
