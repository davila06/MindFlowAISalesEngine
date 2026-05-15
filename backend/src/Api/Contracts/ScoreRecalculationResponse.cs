namespace Api.Contracts;

public sealed class ScoreRecalculationResponse
{
    public int ProcessedLeads { get; init; }
    public string ScoringVersion { get; init; } = string.Empty;
}
