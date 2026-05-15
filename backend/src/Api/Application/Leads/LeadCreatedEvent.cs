namespace Api.Application.Leads;

public record LeadCreatedEvent(Guid LeadId, string? Email, string? Phone, string Source, DateTime CreatedAtUtc);
