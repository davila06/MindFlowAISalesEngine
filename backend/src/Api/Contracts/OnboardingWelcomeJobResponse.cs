namespace Api.Contracts;

public class OnboardingWelcomeJobResponse
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public Guid LeadId { get; init; }
    public string? ToEmail { get; init; }
    public string Status { get; init; } = string.Empty;
    public int AttemptNumber { get; init; }
    public DateTime ScheduledAtUtc { get; init; }
    public DateTime DueAtUtc { get; init; }
    public DateTime? ExecutedAtUtc { get; init; }
    public string? ErrorMessage { get; init; }
}