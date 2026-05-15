using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public sealed class EmailTemplateVersionRequest
{
    [Required, MaxLength(500)]
    public string Subject { get; init; } = string.Empty;

    [Required]
    public string BodyHtml { get; init; } = string.Empty;

    public IReadOnlyList<string> RequiredVariables { get; init; } = Array.Empty<string>();
}