namespace Api.Contracts;

public sealed class ScoringPriorityThresholdsResponse
{
    public int HotMinScore { get; init; }
    public int WarmMinScore { get; init; }
    public int ColdMaxScore { get; init; }
}
