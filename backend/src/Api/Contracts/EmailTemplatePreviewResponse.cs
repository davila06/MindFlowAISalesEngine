namespace Api.Contracts;

public sealed class EmailTemplatePreviewResponse
{
    public string Subject { get; init; } = string.Empty;
    public string BodyHtml { get; init; } = string.Empty;
}