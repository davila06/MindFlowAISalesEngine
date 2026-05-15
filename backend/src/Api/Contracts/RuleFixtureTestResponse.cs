namespace Api.Contracts;

public class RuleFixtureTestResponse
{
    public Guid RuleId { get; init; }
    public bool Matched { get; init; }
    public bool Applied { get; init; }
    public List<string> ActionsApplied { get; init; } = [];
    public List<string> SkippedReasons { get; init; } = [];
}
