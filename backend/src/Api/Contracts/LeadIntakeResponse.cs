namespace Api.Contracts;

public class LeadIntakeResponse
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string Source { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string Campaign { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? ServiceInterest { get; init; }
    public int Score { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string ScoringVersion { get; init; } = string.Empty;
    public DateTime? ScoredAtUtc { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
