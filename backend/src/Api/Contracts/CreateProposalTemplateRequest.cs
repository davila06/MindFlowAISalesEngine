using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class CreateProposalTemplateRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    public string HtmlBody { get; init; } = string.Empty;

    public bool MakeCurrent { get; init; } = true;
}
