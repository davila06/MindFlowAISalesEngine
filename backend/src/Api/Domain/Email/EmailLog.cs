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

    // Email tracking fields
    public Guid TrackingToken { get; private set; }
    public int OpenCount { get; private set; }
    public int ClickCount { get; private set; }
    public DateTime? FirstOpenedAtUtc { get; private set; }
    public DateTime? LastOpenedAtUtc { get; private set; }
    public DateTime? FirstClickedAtUtc { get; private set; }
    public bool IsAppleMpp { get; private set; }

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
        TrackingToken = Guid.NewGuid();
        OpenCount = 0;
        ClickCount = 0;
    }

    public void UpdateDelivery(string status, bool succeeded, string? errorMessage)
    {
        Status = status;
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
        SentAtUtc = DateTime.UtcNow;
    }

    public void RecordOpen(bool isAppleMpp = false)
    {
        var now = DateTime.UtcNow;
        FirstOpenedAtUtc ??= now;
        LastOpenedAtUtc = now;
        // Apple MPP sends pixel instantly on delivery — count only once and flag
        if (isAppleMpp)
        {
            IsAppleMpp = true;
            if (OpenCount == 0) OpenCount = 1;
            return;
        }
        OpenCount++;
    }

    public void RecordClick()
    {
        var now = DateTime.UtcNow;
        FirstClickedAtUtc ??= now;
        ClickCount++;
        // A click implies a real open
        if (OpenCount == 0)
        {
            FirstOpenedAtUtc ??= now;
            LastOpenedAtUtc = now;
            OpenCount = 1;
        }
    }
}
