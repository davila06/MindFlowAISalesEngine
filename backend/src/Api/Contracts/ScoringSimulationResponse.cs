namespace Api.Contracts;

public class ScoringSimulationResponse
{
    public IReadOnlyList<ScoringSimulationResultItem> Results { get; init; } = [];
    public decimal AverageScore { get; init; }
    public decimal HighPriorityRatePercent { get; init; }
}

public class ScoringSimulationResultItem
{
    public int Index { get; init; }
    public int Score { get; init; }
    public string Priority { get; init; } = string.Empty;
}
