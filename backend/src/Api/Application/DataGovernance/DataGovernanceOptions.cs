namespace Api.Application.DataGovernance;

public sealed class DataGovernanceOptions
{
    public bool EnforceDuplicateRejection { get; set; } = false;
    public int DedupEmailDistanceThreshold { get; set; } = 0;
    public int DedupPhoneSuffixLength { get; set; } = 0;
}
