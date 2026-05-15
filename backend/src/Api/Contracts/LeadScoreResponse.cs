namespace Api.Contracts;

public class LeadScoreResponse
{
    public Guid LeadId { get; init; }
    public int Score { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string ScoringVersion { get; init; } = string.Empty;
    public DateTime? ScoredAtUtc { get; init; }
}
