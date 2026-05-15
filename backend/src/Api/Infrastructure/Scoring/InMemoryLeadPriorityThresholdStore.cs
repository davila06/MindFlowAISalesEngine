using System.Collections.Concurrent;
using Api.Application.Scoring;

namespace Api.Infrastructure.Scoring;

public sealed class InMemoryLeadPriorityThresholdStore : ILeadPriorityThresholdStore
{
    private readonly ConcurrentDictionary<string, LeadPriorityThresholds> _thresholdsByTenant = new(StringComparer.OrdinalIgnoreCase);

    public Task<LeadPriorityThresholds> GetCurrentAsync(string tenantId, CancellationToken cancellationToken)
    {
        var normalizedTenant = NormalizeTenantId(tenantId);
        var thresholds = _thresholdsByTenant.GetOrAdd(normalizedTenant, _ => LeadPriorityThresholds.Default);
        return Task.FromResult(thresholds);
    }

    public Task<LeadPriorityThresholds> UpdateAsync(string tenantId, int hotMinScore, int warmMinScore, CancellationToken cancellationToken)
    {
        Validate(hotMinScore, warmMinScore);

        var normalizedTenant = NormalizeTenantId(tenantId);
        var thresholds = new LeadPriorityThresholds
        {
            HotMinScore = hotMinScore,
            WarmMinScore = warmMinScore
        };

        _thresholdsByTenant[normalizedTenant] = thresholds;
        return Task.FromResult(thresholds);
    }

    private static string NormalizeTenantId(string tenantId)
    {
        return string.IsNullOrWhiteSpace(tenantId)
            ? "default"
            : tenantId.Trim().ToLowerInvariant();
    }

    private static void Validate(int hotMinScore, int warmMinScore)
    {
        if (hotMinScore < 0 || hotMinScore > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(hotMinScore), "HotMinScore must be between 0 and 100.");
        }

        if (warmMinScore < 0 || warmMinScore > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(warmMinScore), "WarmMinScore must be between 0 and 100.");
        }

        if (warmMinScore > hotMinScore)
        {
            throw new ArgumentException("WarmMinScore must be less than or equal to HotMinScore.");
        }
    }
}
