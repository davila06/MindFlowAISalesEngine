namespace Api.Contracts;

public sealed class EmailTemplateVersionResponse
{
    public Guid Id { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public int Version { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string BodyHtml { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
    public IReadOnlyList<string> RequiredVariables { get; init; } = Array.Empty<string>();
}