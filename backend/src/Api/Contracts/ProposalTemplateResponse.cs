namespace Api.Contracts;

public class ProposalTemplateResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public int Version { get; init; }
    public bool IsCurrent { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
