namespace Api.Application.Scoring;

public sealed class LeadPriorityThresholds
{
    public int HotMinScore { get; init; }
    public int WarmMinScore { get; init; }

    public static LeadPriorityThresholds Default => new()
    {
        HotMinScore = 80,
        WarmMinScore = 50
    };
}
