namespace Api.Domain.Email;

public class EmailLog
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? ToEmail { get; private set; }
    public string? Subject { get; private set; }
    public string TemplateName { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty; // Sent | Failed | Skipped
    public bool Succeeded { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime SentAtUtc { get; private set; }

    private EmailLog() { }

    public EmailLog(Guid leadId, string? toEmail, string? subject, string templateName,
        bool succeeded, string status, string? errorMessage, string? correlationId = null)
    {
        Id = Guid.NewGuid();
        LeadId = leadId;
        CorrelationId = correlationId;
        ToEmail = toEmail;
        Subject = subject;
        TemplateName = templateName;
        Succeeded = succeeded;
        Status = status;
        ErrorMessage = errorMessage;
        SentAtUtc = DateTime.UtcNow;
    }

    public void UpdateDelivery(string status, bool succeeded, string? errorMessage)
    {
        Status = status;
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
        SentAtUtc = DateTime.UtcNow;
    }
}
