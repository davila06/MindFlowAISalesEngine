using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class RuleConditionRequest
{
    [Required]
    [MaxLength(100)]
    public string Field { get; init; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Operator { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Value { get; init; } = string.Empty;
}
