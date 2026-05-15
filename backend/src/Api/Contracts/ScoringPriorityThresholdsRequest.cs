namespace Api.Contracts;

public sealed class ScoringPriorityThresholdsRequest
{
    public int HotMinScore { get; init; }
    public int WarmMinScore { get; init; }
}
