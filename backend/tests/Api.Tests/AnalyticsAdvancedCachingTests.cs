using Api.Application.AnalyticsAdvanced;
using Api.Application.Common.Interfaces;
using Api.Contracts.Analytics;
using Api.Infrastructure.AnalyticsAdvanced;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Api.Tests;

public class AnalyticsAdvancedCachingTests
{
    [Fact]
    public async Task LoadSnapshotAsync_SameQuery_UsesCacheAndCallsInnerOnce()
    {
        var inner = new FakeAnalyticsAdvancedDataRepository();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var tenantContext = new FakeTenantContext("default");
        var options = Options.Create(new AnalyticsAdvancedCacheOptions
        {
            Enabled = true,
            SnapshotTtlSeconds = 120
        });

        var cached = new CachedAnalyticsAdvancedDataRepository(inner, memoryCache, tenantContext, options);

        var query = new AnalyticsAdvancedQuery
        {
            GroupBy = "day",
            Source = "web",
            StartDateUtc = DateTime.UtcNow.AddDays(-7),
            EndDateUtc = DateTime.UtcNow
        };

        var first = await cached.LoadSnapshotAsync(query, CancellationToken.None);
        var second = await cached.LoadSnapshotAsync(query, CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(1, inner.Calls);
    }
}

file sealed class FakeAnalyticsAdvancedDataRepository : IAnalyticsAdvancedDataRepository
{
    public int Calls { get; private set; }

    public Task<AnalyticsAdvancedDataSnapshot> LoadSnapshotAsync(AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        Calls++;
        return Task.FromResult(new AnalyticsAdvancedDataSnapshot());
    }
}

file sealed class FakeTenantContext : ITenantContext
{
    public FakeTenantContext(string tenantId)
    {
        TenantId = tenantId;
    }

    public string TenantId { get; }
    public string UserRole => "admin";
}
