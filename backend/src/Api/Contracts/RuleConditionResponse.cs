namespace Api.Contracts;

public class RuleConditionResponse
{
    public Guid Id { get; init; }
    public string Field { get; init; } = string.Empty;
    public string Operator { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}
