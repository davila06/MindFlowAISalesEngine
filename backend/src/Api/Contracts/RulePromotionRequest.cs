using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public sealed class RulePromotionRequest
{
    [Required]
    [MaxLength(12)]
    public string TargetEnvironment { get; init; } = "dev";

    [Required]
    [MaxLength(160)]
    public string ApprovedBy { get; init; } = string.Empty;
}
