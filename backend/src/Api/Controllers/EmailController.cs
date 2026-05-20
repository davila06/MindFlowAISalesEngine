using Api.Application.Email;
using Api.Application.Common.Security;
using Api.Application.Security;
using Api.Contracts;
using Api.Domain.Email;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/email")]
public class EmailController : ControllerBase
{
    private static readonly HashSet<string> AllowedTemplateVariables = new(StringComparer.OrdinalIgnoreCase)
    {
        "lead.name",
        "lead.email",
        "company.name",
        "pipeline.stage",
        "recipient_name",
        "proposal_title",
        "amount",
        "currency",
        "tracking_url",
        "endpoint_name",
        "metric_name",
        "observed_value",
        "threshold_value",
        "triggered_at_utc"
    };

    private readonly ISmtpSettingsRepository _smtpSettingsRepository;
    private readonly IEmailStopListRepository _emailStopListRepository;
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly IEmailLogRepository _emailLogRepository;
    private readonly IEmailDispatchService _emailDispatchService;
    private readonly IAdminAuditService _adminAuditService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        ISmtpSettingsRepository smtpSettingsRepository,
        IEmailStopListRepository emailStopListRepository,
        IEmailTemplateRepository emailTemplateRepository,
        IEmailLogRepository emailLogRepository,
        IEmailDispatchService emailDispatchService,
        IAdminAuditService adminAuditService,
        ILogger<EmailController> logger)
    {
        _smtpSettingsRepository = smtpSettingsRepository;
        _emailStopListRepository = emailStopListRepository;
        _emailTemplateRepository = emailTemplateRepository;
        _emailLogRepository = emailLogRepository;
        _emailDispatchService = emailDispatchService;
        _adminAuditService = adminAuditService;
        _logger = logger;
    }

    [HttpGet("smtp-settings")]
    public async Task<IActionResult> GetSmtpSettings(CancellationToken cancellationToken)
    {
        var settings = await _smtpSettingsRepository.GetActiveAsync(cancellationToken);

        if (settings is null)
            return NotFound();

        return Ok(MapToResponse(settings));
    }

    [HttpPut("smtp-settings")]
    public async Task<IActionResult> UpsertSmtpSettings(
        [FromBody] SmtpSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var settings = new SmtpSettings(
            request.ProviderType,
            request.ProviderBaseUrl,
            request.ApiKey,
            request.Host,
            request.Port,
            request.Username,
            request.Password,
            request.FromEmail,
            request.FromName,
            request.EnableSsl);

        await _smtpSettingsRepository.UpsertAsync(settings, cancellationToken);
        await _adminAuditService.RecordAsync(
            "smtp_settings_updated",
            "email/smtp-settings",
            $"Provider={request.ProviderType}; Host={request.Host}; Port={request.Port}; EnableSsl={request.EnableSsl}",
            cancellationToken);

        _logger.LogInformation(
            "Email provider settings updated for {ProviderType} host {Host}:{Port}",
            request.ProviderType,
            request.Host,
            request.Port);

        var updated = await _smtpSettingsRepository.GetActiveAsync(cancellationToken);
        return Ok(MapToResponse(updated!));
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetEmailLogs(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var logs = await _emailLogRepository.GetAllAsync(cancellationToken);

        var logQuery = logs
            .OrderByDescending(item => item.SentAtUtc)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim();
            logQuery = logQuery.Where(item =>
                item.TemplateName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                item.Status.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                (item.ToEmail ?? string.Empty).Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (page is > 0 && pageSize is > 0)
        {
            logQuery = logQuery
                .Skip((page.Value - 1) * pageSize.Value)
                .Take(pageSize.Value);
        }

        var response = logQuery.Select(item => new EmailLogResponse
        {
            Id = item.Id,
            LeadId = item.LeadId,
            CorrelationId = item.CorrelationId,
            ToEmail = PiiMasking.MaskEmail(item.ToEmail),
            Subject = item.Subject,
            TemplateName = item.TemplateName,
            Status = item.Status,
            Succeeded = item.Succeeded,
            ErrorMessage = item.ErrorMessage,
            SentAtUtc = item.SentAtUtc,
            OpenCount = item.OpenCount,
            ClickCount = item.ClickCount,
            FirstOpenedAtUtc = item.FirstOpenedAtUtc,
            FirstClickedAtUtc = item.FirstClickedAtUtc
        });

        return Ok(response);
    }

    [HttpPost("templates/{templateKey}/versions")]
    public async Task<IActionResult> CreateTemplateVersion(
        string templateKey,
        [FromBody] EmailTemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var invalidVariables = CollectVariables(request.Subject, request.BodyHtml)
            .Concat(request.RequiredVariables ?? Array.Empty<string>())
            .Where(x => !AllowedTemplateVariables.Contains(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (invalidVariables.Length > 0)
        {
            return BadRequest(new
            {
                message = "Template contains unsupported variables.",
                invalidVariables
            });
        }

        var versions = await _emailTemplateRepository.GetVersionsAsync(templateKey, cancellationToken);
        foreach (var version in versions.Where(x => x.IsCurrent))
        {
            version.MarkAsHistorical();
        }

        if (versions.Count > 0)
        {
            await _emailTemplateRepository.SaveChangesAsync(cancellationToken);
        }

        var template = new EmailTemplate(
            templateKey,
            versions.Count == 0 ? 1 : versions.Max(x => x.Version) + 1,
            request.Subject.Trim(),
            request.BodyHtml.Trim(),
            request.RequiredVariables);

        await _emailTemplateRepository.AddAsync(template, cancellationToken);

        return Created(
            $"/api/email/templates/{templateKey}/versions/{template.Version}",
            MapTemplate(template));
    }

    [HttpPost("templates/{templateKey}/preview")]
    public async Task<IActionResult> PreviewTemplate(
        string templateKey,
        [FromBody] EmailTemplatePreviewRequest request,
        CancellationToken cancellationToken)
    {
        var template = await _emailTemplateRepository.GetCurrentByNameAsync(templateKey, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        var variables = request.Variables ?? new Dictionary<string, string>();
        var missing = template.GetRequiredVariables()
            .Where(x => !variables.ContainsKey(x))
            .ToArray();

        if (missing.Length > 0)
        {
            return BadRequest(new { message = "Missing variables for preview.", missingVariables = missing });
        }

        return Ok(new EmailTemplatePreviewResponse
        {
            Subject = RenderTemplate(template.Subject, variables),
            BodyHtml = RenderTemplate(template.BodyHtml, variables)
        });
    }

    [HttpPost("templates/{templateKey}/rollback")]
    public async Task<IActionResult> RollbackTemplate(
        string templateKey,
        [FromBody] EmailTemplateRollbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var target = await _emailTemplateRepository.GetByNameAndVersionAsync(templateKey, request.TargetVersion, cancellationToken);
        if (target is null)
        {
            return NotFound();
        }

        var versions = await _emailTemplateRepository.GetVersionsAsync(templateKey, cancellationToken);
        foreach (var version in versions)
        {
            version.MarkAsHistorical();
        }

        target.MarkAsCurrent();
        await _emailTemplateRepository.SaveChangesAsync(cancellationToken);

        return Ok(MapTemplate(target));
    }

    [HttpPost("stop-list")]
    public async Task<IActionResult> AddStopListEntry(
        [FromBody] EmailStopListRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await _emailStopListRepository.AddAsync(
            new EmailStopListEntry(request.Email, request.Reason ?? "unsubscribe"),
            cancellationToken);

        return Ok();
    }

    [HttpPost("dispatch/execute-due")]
    public async Task<IActionResult> ExecuteDueDispatches(CancellationToken cancellationToken)
    {
        var processed = await _emailDispatchService.ExecuteDueAsync(cancellationToken);
        return Ok(new { processed });
    }

    [HttpPost("logs/{logId:guid}/retry")]
    public async Task<IActionResult> RetryLog(Guid logId, CancellationToken cancellationToken)
    {
        await _emailDispatchService.RetryAsync(logId, cancellationToken);
        return Ok();
    }

    [HttpGet("tracking/metrics")]
    public async Task<IActionResult> GetTrackingMetrics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var metrics = await _emailLogRepository.GetMetricsByTemplateAsync(from, to, cancellationToken);
        var response = metrics.Select(m => new EmailTrackingMetricsResponse
        {
            TemplateName = m.TemplateName,
            TotalSent = m.TotalSent,
            TotalOpened = m.TotalOpened,
            TotalClicked = m.TotalClicked,
            OpenRatePercent = m.TotalSent == 0 ? 0 : Math.Round((double)m.TotalOpened / m.TotalSent * 100, 1),
            ClickRatePercent = m.TotalSent == 0 ? 0 : Math.Round((double)m.TotalClicked / m.TotalSent * 100, 1),
            ClickToOpenRatePercent = m.TotalOpened == 0 ? 0 : Math.Round((double)m.TotalClicked / m.TotalOpened * 100, 1)
        });
        return Ok(response);
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis(CancellationToken cancellationToken)
    {
        var snapshot = await _emailDispatchService.GetKpisAsync(cancellationToken);
        return Ok(new EmailKpiResponse
        {
            TotalCount = snapshot.TotalCount,
            SentCount = snapshot.SentCount,
            FailedCount = snapshot.FailedCount,
            QueuedCount = snapshot.QueuedCount,
            BouncedCount = snapshot.BouncedCount,
            ByChannel = snapshot.ByChannel.Select(x => new EmailChannelKpiItemResponse
            {
                Channel = x.Channel,
                TotalCount = x.TotalCount,
                SentCount = x.SentCount,
                FailedCount = x.FailedCount,
                QueuedCount = x.QueuedCount,
                BouncedCount = x.BouncedCount,
                ErrorRatePercent = x.ErrorRatePercent
            }).ToArray(),
            ErrorRatePercent = snapshot.ErrorRatePercent
        });
    }

    private static SmtpSettingsResponse MapToResponse(SmtpSettings s) => new()
    {
        Id = s.Id,
        ProviderType = s.ProviderType,
        ProviderBaseUrl = s.ProviderBaseUrl,
        Host = s.Host,
        Port = s.Port,
        Username = s.Username,
        FromEmail = s.FromEmail,
        FromName = s.FromName,
        EnableSsl = s.EnableSsl,
        UpdatedAtUtc = s.UpdatedAtUtc
    };

    private static EmailTemplateVersionResponse MapTemplate(EmailTemplate template) => new()
    {
        Id = template.Id,
        TemplateKey = template.Name,
        Version = template.Version,
        Subject = template.Subject,
        BodyHtml = template.BodyHtml,
        IsCurrent = template.IsCurrent,
        RequiredVariables = template.GetRequiredVariables()
    };

    private static IReadOnlyList<string> CollectVariables(string subject, string bodyHtml)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(
            $"{subject} {bodyHtml}",
            "\\{\\{\\s*([a-zA-Z0-9._]+)\\s*\\}\\}");

        return matches
            .Select(x => x.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string RenderTemplate(string template, IReadOnlyDictionary<string, string> variables)
    {
        var rendered = template;
        foreach (var variable in variables)
        {
            rendered = rendered.Replace($"{{{{{variable.Key}}}}}", variable.Value, StringComparison.OrdinalIgnoreCase);
        }

        return rendered;
    }
}
