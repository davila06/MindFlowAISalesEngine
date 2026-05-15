namespace Api.Contracts;

public class RuleTemplateResponse
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public RuleCreateRequest Template { get; init; } = new();
}
