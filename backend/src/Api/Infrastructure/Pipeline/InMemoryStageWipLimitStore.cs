using System.Collections.Concurrent;
using Api.Application.Pipeline;
using Api.Domain.Pipeline;

namespace Api.Infrastructure.Pipeline;

public sealed class InMemoryStageWipLimitStore : IStageWipLimitStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, int>> _limitsByTenant = new(StringComparer.OrdinalIgnoreCase);

    public int GetLimit(string tenantId, Guid stageId, string stageName)
    {
        var tenantLimits = _limitsByTenant.GetOrAdd(NormalizeTenant(tenantId), _ => new ConcurrentDictionary<Guid, int>());
        return tenantLimits.TryGetValue(stageId, out var limit) ? limit : GetDefault(stageName);
    }

    public IReadOnlyDictionary<Guid, int> GetAll(string tenantId, IReadOnlyDictionary<Guid, string> stageNames)
    {
        return stageNames.ToDictionary(x => x.Key, x => GetLimit(tenantId, x.Key, x.Value));
    }

    public int SetLimit(string tenantId, Guid stageId, int limit)
    {
        var tenantLimits = _limitsByTenant.GetOrAdd(NormalizeTenant(tenantId), _ => new ConcurrentDictionary<Guid, int>());
        tenantLimits[stageId] = Math.Max(0, limit);
        return tenantLimits[stageId];
    }

    private static string NormalizeTenant(string tenantId)
    {
        return string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId.Trim().ToLowerInvariant();
    }

    private static int GetDefault(string stageName)
    {
        _ = stageName;
        return 100000;
    }
}
