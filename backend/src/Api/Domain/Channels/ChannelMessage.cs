using System;
using System.Collections.Generic;

namespace Api.Domain.Channels;

public enum ChannelType
{
    Email,
    Phone,
    Chat,
    WhatsApp,
    Social
}

public class ChannelMessage
{
    public Guid Id { get; set; }
    public ChannelType Type { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
    public string? ExternalId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
