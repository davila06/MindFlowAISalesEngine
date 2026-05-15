namespace Api.Contracts;

public sealed class EmailLogResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string? CorrelationId { get; init; }
    public string? ToEmail { get; init; }
    public string? Subject { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime SentAtUtc { get; init; }
}
