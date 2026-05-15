using System.Collections.Concurrent;
using Api.Application.DataGovernance;

namespace Api.Infrastructure.DataGovernance;

public sealed class InMemoryTenantDataGovernanceStore : ITenantDataGovernanceStore
{
    private readonly ConcurrentDictionary<string, DataGovernanceOptions> _settingsByTenant = new(StringComparer.OrdinalIgnoreCase);

    public DataGovernanceOptions GetOrDefault(string tenantId, DataGovernanceOptions defaults)
    {
        var normalizedTenant = NormalizeTenant(tenantId);
        return _settingsByTenant.TryGetValue(normalizedTenant, out var settings)
            ? Clone(settings)
            : Clone(defaults);
    }

    public DataGovernanceOptions Set(string tenantId, DataGovernanceOptions settings)
    {
        var normalizedTenant = NormalizeTenant(tenantId);
        var normalized = new DataGovernanceOptions
        {
            EnforceDuplicateRejection = settings.EnforceDuplicateRejection,
            DedupEmailDistanceThreshold = Math.Clamp(settings.DedupEmailDistanceThreshold, 0, 20),
            DedupPhoneSuffixLength = Math.Clamp(settings.DedupPhoneSuffixLength, 0, 12)
        };

        _settingsByTenant[normalizedTenant] = normalized;
        return Clone(normalized);
    }

    private static string NormalizeTenant(string tenantId)
    {
        return string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId.Trim().ToLowerInvariant();
    }

    private static DataGovernanceOptions Clone(DataGovernanceOptions source)
    {
        return new DataGovernanceOptions
        {
            EnforceDuplicateRejection = source.EnforceDuplicateRejection,
            DedupEmailDistanceThreshold = source.DedupEmailDistanceThreshold,
            DedupPhoneSuffixLength = source.DedupPhoneSuffixLength
        };
    }
}
