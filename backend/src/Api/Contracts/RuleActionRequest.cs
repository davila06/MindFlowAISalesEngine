using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class RuleActionRequest
{
    [Required]
    [MaxLength(100)]
    public string Type { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Value { get; init; } = string.Empty;
}
