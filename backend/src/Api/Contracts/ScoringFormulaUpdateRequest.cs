using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class ScoringFormulaUpdateRequest
{
    [Required]
    [MaxLength(32)]
    public string Version { get; init; } = string.Empty;

    [Range(0, 100)]
    public int HasEmailPoints { get; init; }

    [Range(0, 100)]
    public int HasPhonePoints { get; init; }

    [Range(0, 100)]
    public int SourceReferralPoints { get; init; }

    [Range(0, 100)]
    public int SourceWebPoints { get; init; }

    [Range(0, 100)]
    public int SourceAdsPoints { get; init; }

    [Range(0, 100)]
    public int SourceOtherPoints { get; init; }

    [Range(0, 100)]
    public int EmailPhoneBonusPoints { get; init; }
}
