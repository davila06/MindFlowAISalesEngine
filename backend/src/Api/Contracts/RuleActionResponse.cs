namespace Api.Contracts;

public class RuleActionResponse
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
