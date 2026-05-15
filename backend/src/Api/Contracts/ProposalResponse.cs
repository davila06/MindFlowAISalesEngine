namespace Api.Contracts;

public class ProposalResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int TemplateVersion { get; init; }
    public string TrackingToken { get; init; } = string.Empty;
    public int ViewCount { get; init; }
    public DateTime? LastViewedAtUtc { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public DateTime? SignedAtUtc { get; init; }
    public Guid? RenewedFromProposalId { get; init; }
    public int ReminderCount { get; init; }
    public string ReminderStatus { get; init; } = string.Empty;
    public int ReminderAttemptNumber { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? SentAtUtc { get; init; }
}
