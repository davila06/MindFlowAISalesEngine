namespace Api.Contracts;

public class ScoreRuleResponse
{
    public string Key { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Points { get; init; }
}
