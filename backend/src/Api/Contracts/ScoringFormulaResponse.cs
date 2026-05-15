namespace Api.Contracts;

public class ScoringFormulaResponse
{
    public string Version { get; init; } = string.Empty;
    public int HasEmailPoints { get; init; }
    public int HasPhonePoints { get; init; }
    public int SourceReferralPoints { get; init; }
    public int SourceWebPoints { get; init; }
    public int SourceAdsPoints { get; init; }
    public int SourceOtherPoints { get; init; }
    public int EmailPhoneBonusPoints { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
