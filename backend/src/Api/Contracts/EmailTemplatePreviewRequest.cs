namespace Api.Contracts;

public sealed class EmailTemplatePreviewRequest
{
    public IReadOnlyDictionary<string, string> Variables { get; init; } = new Dictionary<string, string>();
}