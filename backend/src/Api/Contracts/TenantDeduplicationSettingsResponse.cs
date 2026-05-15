namespace Api.Contracts;

public class TenantDeduplicationSettingsResponse
{
    public bool EnforceDuplicateRejection { get; init; }
    public int DedupEmailDistanceThreshold { get; init; }
    public int DedupPhoneSuffixLength { get; init; }
}
