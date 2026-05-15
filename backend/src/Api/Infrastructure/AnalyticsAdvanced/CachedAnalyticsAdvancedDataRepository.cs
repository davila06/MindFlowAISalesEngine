using System.Globalization;
using Api.Application.AnalyticsAdvanced;
using Api.Application.Common.Interfaces;
using Api.Contracts.Analytics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Api.Infrastructure.AnalyticsAdvanced;

public sealed class CachedAnalyticsAdvancedDataRepository : IAnalyticsAdvancedDataRepository
{
    private readonly IAnalyticsAdvancedDataRepository _inner;
    private readonly IMemoryCache _cache;
    private readonly ITenantContext _tenantContext;
    private readonly AnalyticsAdvancedCacheOptions _options;

    public CachedAnalyticsAdvancedDataRepository(
        IAnalyticsAdvancedDataRepository inner,
        IMemoryCache cache,
        ITenantContext tenantContext,
        IOptions<AnalyticsAdvancedCacheOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _tenantContext = tenantContext;
        _options = options.Value;
    }

    public Task<AnalyticsAdvancedDataSnapshot> LoadSnapshotAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        if (!_options.Enabled || _options.SnapshotTtlSeconds <= 0)
        {
            return _inner.LoadSnapshotAsync(query, cancellationToken);
        }

        var cacheKey = BuildCacheKey(_tenantContext.TenantId, query);
        return _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_options.SnapshotTtlSeconds);
            return await _inner.LoadSnapshotAsync(query, cancellationToken);
        })!;
    }

    private static string BuildCacheKey(string tenantId, AnalyticsAdvancedQuery query)
    {
        return string.Join("|",
            "analytics-advanced-snapshot",
            tenantId.Trim().ToLowerInvariant(),
            query.StartDateUtc?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "null",
            query.EndDateUtc?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "null",
            (query.GroupBy ?? "day").Trim().ToLowerInvariant(),
            query.Stage?.Trim().ToLowerInvariant() ?? "null",
            query.Source?.Trim().ToLowerInvariant() ?? "null",
            query.Tenant?.Trim().ToLowerInvariant() ?? "null");
    }
}
