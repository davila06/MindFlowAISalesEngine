namespace Api.Contracts;

public class ScoreExplainabilityResponse
{
    public Guid LeadId { get; init; }
    public int Score { get; init; }
    public string Priority { get; init; } = string.Empty;
    public string FormulaVersion { get; init; } = string.Empty;
    public IReadOnlyList<ScoreContributionItem> Contributions { get; init; } = [];
}

public class ScoreContributionItem
{
    public string Key { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Points { get; init; }
    public bool Applied { get; init; }
}
