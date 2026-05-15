namespace Api.Application.Scoring;

public sealed class LeadScoringFormula
{
    public string Version { get; init; } = "v2.0";
    public int HasEmailPoints { get; init; } = 25;
    public int HasPhonePoints { get; init; } = 25;
    public int SourceReferralPoints { get; init; } = 30;
    public int SourceWebPoints { get; init; } = 20;
    public int SourceAdsPoints { get; init; } = 15;
    public int SourceOtherPoints { get; init; } = 10;
    public int EmailPhoneBonusPoints { get; init; } = 10;
    public DateTime UpdatedAtUtc { get; init; } = DateTime.UtcNow;
}
