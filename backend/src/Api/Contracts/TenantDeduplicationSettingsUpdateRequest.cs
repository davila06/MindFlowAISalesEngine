using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class TenantDeduplicationSettingsUpdateRequest
{
    public bool EnforceDuplicateRejection { get; init; }

    [Range(0, 20)]
    public int DedupEmailDistanceThreshold { get; init; }

    [Range(0, 12)]
    public int DedupPhoneSuffixLength { get; init; }
}
