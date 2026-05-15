namespace Api.Domain.Email;

public sealed class EmailDispatchJob
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string ProviderType { get; private set; } = SmtpSettings.SmtpProviderType;
    public string? ToEmail { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string BodyHtml { get; private set; } = string.Empty;
    public string TemplateName { get; private set; } = string.Empty;
    public byte[]? AttachmentBytes { get; private set; }
    public string? AttachmentFileName { get; private set; }
    public string? AttachmentContentType { get; private set; }
    public string Status { get; private set; } = "Queued";
    public int AttemptCount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime DueAtUtc { get; private set; }
    public string? LastError { get; private set; }

    private EmailDispatchJob() { }

    public EmailDispatchJob(
        Guid leadId,
        string providerType,
        string? correlationId,
        string? toEmail,
        string subject,
        string bodyHtml,
        string templateName,
        byte[]? attachmentBytes,
        string? attachmentFileName,
        string? attachmentContentType,
        DateTime? dueAtUtc = null)
    {
        Id = Guid.NewGuid();
        LeadId = leadId;
        ProviderType = string.IsNullOrWhiteSpace(providerType)
            ? SmtpSettings.SmtpProviderType
            : providerType.Trim().ToLowerInvariant();
        CorrelationId = correlationId;
        ToEmail = toEmail;
        Subject = subject;
        BodyHtml = bodyHtml;
        TemplateName = templateName;
        AttachmentBytes = attachmentBytes;
        AttachmentFileName = attachmentFileName;
        AttachmentContentType = attachmentContentType;
        Status = "Queued";
        AttemptCount = 0;
        CreatedAtUtc = DateTime.UtcNow;
        DueAtUtc = dueAtUtc ?? DateTime.UtcNow;
    }

    public void MarkSent()
    {
        Status = "Sent";
        AttemptCount += 1;
        LastError = null;
        DueAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = "Failed";
        AttemptCount += 1;
        LastError = errorMessage;
        DueAtUtc = DateTime.UtcNow;
    }

    public void Requeue(DateTime dueAtUtc)
    {
        Status = "Queued";
        DueAtUtc = dueAtUtc;
        LastError = null;
    }
}