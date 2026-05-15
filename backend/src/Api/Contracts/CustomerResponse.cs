namespace Api.Contracts;

public class CustomerResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Segment { get; init; } = string.Empty;
    public string PlaybookKey { get; init; } = string.Empty;
    public decimal HealthScore { get; init; }
    public string TrackingToken { get; init; } = string.Empty;
    public int TrackingActivations { get; init; }
    public DateTime? LastTrackingActivatedAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
