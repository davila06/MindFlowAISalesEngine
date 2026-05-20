namespace Api.Domain.WhatsApp;

/// <summary>Inbound or outbound WhatsApp message record.</summary>
public class WhatsAppMessage
{
    public static class Directions
    {
        public const string Inbound  = "inbound";
        public const string Outbound = "outbound";
    }

    public static class Statuses
    {
        public const string Pending   = "pending";
        public const string Sent      = "sent";
        public const string Delivered = "delivered";
        public const string Read      = "read";
        public const string Failed    = "failed";
        public const string Received  = "received";
    }

    public Guid Id { get; private set; }
    /// <summary>Meta-assigned message ID (for deduplication and status callbacks).</summary>
    public string? ExternalMessageId { get; private set; }
    public string ContactPhone { get; private set; } = string.Empty;
    public string Direction { get; private set; } = Directions.Outbound;
    public string? Body { get; private set; }
    public string? TemplateName { get; private set; }
    public string Status { get; private set; } = Statuses.Pending;
    public Guid? LeadId { get; private set; }
    public DateTime SentAtUtc { get; private set; }

    private WhatsAppMessage() { }

    public static WhatsAppMessage CreateOutbound(string contactPhone, string? body, string? templateName, Guid? leadId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contactPhone);
        return new WhatsAppMessage
        {
            Id = Guid.NewGuid(),
            ContactPhone = contactPhone,
            Direction = Directions.Outbound,
            Body = body,
            TemplateName = templateName,
            Status = Statuses.Pending,
            LeadId = leadId,
            SentAtUtc = DateTime.UtcNow
        };
    }

    public static WhatsAppMessage CreateInbound(string externalMessageId, string contactPhone, string body, Guid? leadId = null)
    {
        return new WhatsAppMessage
        {
            Id = Guid.NewGuid(),
            ExternalMessageId = externalMessageId,
            ContactPhone = contactPhone,
            Direction = Directions.Inbound,
            Body = body,
            Status = Statuses.Received,
            LeadId = leadId,
            SentAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateStatus(string status, string? externalMessageId = null)
    {
        Status = status;
        if (externalMessageId is not null)
            ExternalMessageId = externalMessageId;
    }
}
