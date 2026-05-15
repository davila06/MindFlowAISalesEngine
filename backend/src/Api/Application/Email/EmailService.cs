using Api.Domain.Email;
using Microsoft.Extensions.Logging;

namespace Api.Application.Email;

public class EmailService : IEmailService
{
    private const string WelcomeTemplateName  = "lead.welcome";
    private const string FollowUpTemplateName = "lead.followup";
    private const string ProposalTemplateName = "proposal.standard";
    private const string ProposalReminderTemplateName = "proposal.reminder";
    private const string CustomerWelcomeTemplateName = "customer.welcome";
    private const string AnalyticsAlertTemplateName = "alert.analytics.degradation";

    private readonly ISmtpSettingsRepository _smtpSettingsRepository;
    private readonly IEmailStopListRepository _emailStopListRepository;
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly IEmailLogRepository _emailLogRepository;
    private readonly IEmailDispatchJobRepository _emailDispatchJobRepository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        ISmtpSettingsRepository smtpSettingsRepository,
        IEmailStopListRepository emailStopListRepository,
        IEmailTemplateRepository emailTemplateRepository,
        IEmailLogRepository emailLogRepository,
        IEmailDispatchJobRepository emailDispatchJobRepository,
        IEmailSender emailSender,
        ILogger<EmailService> logger)
    {
        _smtpSettingsRepository = smtpSettingsRepository;
        _emailStopListRepository = emailStopListRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _emailLogRepository = emailLogRepository;
        _emailDispatchJobRepository = emailDispatchJobRepository;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task SendLeadWelcomeAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken)
    {
        await QueueEmailAsync(
            leadId,
            toEmail,
            WelcomeTemplateName,
            "Welcome — we received your inquiry",
            "<p>Thank you for your interest. Our team will contact you soon.</p>",
            payload: null,
            attachmentBytes: null,
            attachmentFileName: null,
            attachmentContentType: null,
            cancellationToken);
    }

    public async Task SendLeadFollowUpAsync(Guid leadId, string? toEmail, CancellationToken cancellationToken)
    {
        var settings = await _smtpSettingsRepository.GetActiveAsync(cancellationToken);

        if (settings is null)
        {
            _logger.LogWarning(
                "No active SMTP settings configured. Skipping follow-up email for lead {LeadId}", leadId);

            var skipped = new EmailLog(leadId, toEmail, subject: null, FollowUpTemplateName,
                succeeded: false, status: "Skipped", errorMessage: "NoSmtpConfigured");
            await _emailLogRepository.AddAsync(skipped, cancellationToken);
            return;
        }

        var template = await _emailTemplateRepository.GetByNameAsync(FollowUpTemplateName, cancellationToken);
        var subject  = template?.Subject  ?? "Following up on your inquiry — MindFlow";
        var bodyHtml = template?.BodyHtml ??
            "<p>Hi, we wanted to follow up on your recent inquiry. Please let us know if we can help.</p>" +
            "<p>Best regards,<br/>The MindFlow Team</p>";

        try
        {
            await _emailSender.SendAsync(
                settings.Host, settings.Port, settings.Username, settings.Password, settings.EnableSsl,
                settings.FromEmail, settings.FromName ?? "MindFlow",
                toEmail ?? string.Empty, subject, bodyHtml,
                attachmentBytes: null,
                attachmentFileName: null,
                attachmentContentType: null,
                cancellationToken);

            _logger.LogInformation("Follow-up email sent to {ToEmail} for lead {LeadId}", toEmail, leadId);

            var sent = new EmailLog(leadId, toEmail, subject, FollowUpTemplateName,
                succeeded: true, status: "Sent", errorMessage: null);
            await _emailLogRepository.AddAsync(sent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send follow-up email to {ToEmail} for lead {LeadId}", toEmail, leadId);

            var failed = new EmailLog(leadId, toEmail, subject, FollowUpTemplateName,
                succeeded: false, status: "Failed", errorMessage: ex.Message);
            await _emailLogRepository.AddAsync(failed, cancellationToken);
            throw;
        }
    }

    public async Task<bool> SendProposalAsync(
        Guid leadId,
        string? toEmail,
        string recipientName,
        string proposalTitle,
        decimal amount,
        string currency,
        string trackingUrl,
        byte[] pdfBytes,
        string pdfFileName,
        CancellationToken cancellationToken)
    {
        var bodyHtml =
            "<p>Hello {{recipient_name}},</p><p>We prepared your proposal <strong>{{proposal_title}}</strong> for {{amount}} {{currency}}.</p><p>Track status: <a href='{{tracking_url}}'>view proposal</a></p>";

        var payload = new Dictionary<string, string>
        {
            ["recipient_name"] = recipientName,
            ["proposal_title"] = proposalTitle,
            ["amount"] = amount.ToString("0.00"),
            ["currency"] = currency,
            ["tracking_url"] = trackingUrl
        };

        return await QueueEmailAsync(
            leadId,
            toEmail,
            ProposalTemplateName,
            "Your proposal is ready — MindFlow",
            bodyHtml,
            payload,
            pdfBytes,
            pdfFileName,
            "application/pdf",
            cancellationToken);
    }

    public async Task<bool> SendProposalReminderAsync(
        Guid leadId,
        string? toEmail,
        string recipientName,
        string proposalTitle,
        string trackingUrl,
        CancellationToken cancellationToken)
    {
        var bodyHtml =
            "<p>Hello {{recipient_name}},</p><p>Just checking in regarding <strong>{{proposal_title}}</strong>.</p><p>Track status: <a href='{{tracking_url}}'>open proposal</a></p>";

        var payload = new Dictionary<string, string>
        {
            ["recipient_name"] = recipientName,
            ["proposal_title"] = proposalTitle,
            ["tracking_url"] = trackingUrl
        };

        return await QueueEmailAsync(
            leadId,
            toEmail,
            ProposalReminderTemplateName,
            "Proposal reminder — MindFlow",
            bodyHtml,
            payload,
            attachmentBytes: null,
            attachmentFileName: null,
            attachmentContentType: null,
            cancellationToken);
    }

    private static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> payload)
    {
        var result = template;
        foreach (var item in payload)
        {
            result = result.Replace($"{{{{{item.Key}}}}}", item.Value, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    public async Task<bool> SendCustomerWelcomeAsync(
        Guid leadId,
        string? toEmail,
        string trackingUrl,
        CancellationToken cancellationToken)
    {
        var bodyHtml =
            "<p>Welcome onboard.</p><p>Track onboarding progress: <a href='{{tracking_url}}'>open tracking</a></p>";

        return await QueueEmailAsync(
            leadId,
            toEmail,
            CustomerWelcomeTemplateName,
            "Welcome to onboarding — MindFlow",
            bodyHtml,
            new Dictionary<string, string>
        {
            ["tracking_url"] = trackingUrl
        },
            attachmentBytes: null,
            attachmentFileName: null,
            attachmentContentType: null,
            cancellationToken);
    }

    public async Task<bool> SendAnalyticsDegradationAlertAsync(
        string toEmail,
        string endpointName,
        string metricName,
        decimal observedValue,
        decimal thresholdValue,
        DateTime triggeredAtUtc,
        CancellationToken cancellationToken)
    {
        var bodyHtml =
            "<p>Degradation detected.</p><p>Endpoint: {{endpoint_name}}</p><p>Metric: {{metric_name}}</p><p>Observed: {{observed_value}}</p><p>Threshold: {{threshold_value}}</p><p>TriggeredAtUtc: {{triggered_at_utc}}</p>";

        var payload = new Dictionary<string, string>
        {
            ["endpoint_name"] = endpointName,
            ["metric_name"] = metricName,
            ["observed_value"] = observedValue.ToString("0.##"),
            ["threshold_value"] = thresholdValue.ToString("0.##"),
            ["triggered_at_utc"] = triggeredAtUtc.ToString("o")
        };

        return await QueueEmailAsync(
            Guid.Empty,
            toEmail,
            AnalyticsAlertTemplateName,
            "Analytics degradation detected — MindFlow",
            bodyHtml,
            payload,
            attachmentBytes: null,
            attachmentFileName: null,
            attachmentContentType: null,
            cancellationToken);
    }

    private async Task<bool> QueueEmailAsync(
        Guid leadId,
        string? toEmail,
        string templateName,
        string fallbackSubject,
        string fallbackBodyHtml,
        IReadOnlyDictionary<string, string>? payload,
        byte[]? attachmentBytes,
        string? attachmentFileName,
        string? attachmentContentType,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(toEmail)
            && await _emailStopListRepository.ExistsAsync(toEmail, cancellationToken))
        {
            await _emailLogRepository.AddAsync(
                new EmailLog(
                    leadId,
                    toEmail,
                    subject: null,
                    templateName,
                    succeeded: false,
                    status: "Suppressed",
                    errorMessage: "StopListMatched"),
                cancellationToken);
            return false;
        }

        var settings = await _smtpSettingsRepository.GetActiveAsync(cancellationToken);
        if (settings is null || string.IsNullOrWhiteSpace(toEmail))
        {
            var skipped = new EmailLog(
                leadId,
                toEmail,
                subject: null,
                templateName,
                succeeded: false,
                status: "Skipped",
                errorMessage: settings is null ? "NoSmtpConfigured" : "NoRecipientEmail");
            await _emailLogRepository.AddAsync(skipped, cancellationToken);
            return false;
        }

        var template = await _emailTemplateRepository.GetByNameAsync(templateName, cancellationToken);
        var subject = template?.Subject ?? fallbackSubject;
        var bodyHtml = template?.BodyHtml ?? fallbackBodyHtml;

        if (payload is not null)
        {
            subject = ApplyTemplate(subject, payload);
            bodyHtml = ApplyTemplate(bodyHtml, payload);
        }

        var correlationId = Guid.NewGuid().ToString("N");
        var job = new EmailDispatchJob(
            leadId,
            settings.ProviderType,
            correlationId,
            toEmail,
            subject,
            bodyHtml,
            templateName,
            attachmentBytes,
            attachmentFileName,
            attachmentContentType);

        await _emailDispatchJobRepository.AddAsync(job, cancellationToken);
        await _emailLogRepository.AddAsync(
            new EmailLog(
                leadId,
                toEmail,
                subject,
                templateName,
                succeeded: false,
                status: "Queued",
                errorMessage: null,
                correlationId: correlationId),
            cancellationToken);

        _logger.LogInformation(
            "Queued email dispatch {CorrelationId} for template {TemplateName} and lead {LeadId}",
            correlationId,
            templateName,
            leadId);

        return true;
    }
}
