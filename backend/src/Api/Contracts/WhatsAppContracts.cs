namespace Api.Contracts;

// ---- WhatsApp ----

public record WhatsAppSendRequest(string ToPhone, string Body, Guid? LeadId);
public record WhatsAppOptInRequest(string Phone);

public record WhatsAppContactResponse(
    Guid Id,
    string PhoneNumber,
    string? DisplayName,
    bool OptedIn,
    DateTime? OptedInAtUtc,
    DateTime? OptedOutAtUtc,
    Guid? LeadId);

public record WhatsAppMessageResponse(
    Guid Id,
    string? ExternalMessageId,
    string ContactPhone,
    string Direction,
    string? Body,
    string? TemplateName,
    string Status,
    Guid? LeadId,
    DateTime SentAtUtc);
