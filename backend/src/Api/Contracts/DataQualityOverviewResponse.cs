namespace Api.Contracts;

public sealed class DataQualityOverviewResponse
{
    public int TotalLeads { get; init; }
    public int LeadsWithEmail { get; init; }
    public int LeadsWithPhone { get; init; }
    public int LeadsWithBothContacts { get; init; }
    public int DuplicateEmailCandidates { get; init; }
    public int DuplicatePhoneCandidates { get; init; }
    public decimal ContactCompletenessPercent { get; init; }
    public int DataAnomalyEvents { get; init; }
}
