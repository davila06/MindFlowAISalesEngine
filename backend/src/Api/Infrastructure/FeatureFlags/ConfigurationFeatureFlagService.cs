using Api.Application.Common.FeatureFlags;
using Microsoft.Extensions.Configuration;

namespace Api.Infrastructure.FeatureFlags;

/// <summary>
/// OPS-05 | Configuration-backed feature flag service.
///
/// Resolution order (highest priority wins):
///   1. Tenant-specific override: Features:Tenants:{tenantId}:{flagName}
///   2. Global flag:              Features:{flagName}
///   3. Default:                  false
///
/// This makes progressive per-tenant rollouts possible without a deployment.
/// In production, bind the Features section to Azure App Configuration or
/// a Key Vault-backed config provider for live updates.
/// </summary>
public sealed class ConfigurationFeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _configuration;

    public ConfigurationFeatureFlagService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public bool IsEnabled(string flagName, string? tenantId = null)
    {
        // 1. Tenant-specific override
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            var tenantValue = _configuration[$"Features:Tenants:{tenantId}:{flagName}"];
            if (bool.TryParse(tenantValue, out var tenantFlag))
                return tenantFlag;
        }

        // 2. Global flag
        var globalValue = _configuration[$"Features:{flagName}"];
        if (bool.TryParse(globalValue, out var globalFlag))
            return globalFlag;

        // 3. Default: disabled
        return false;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, bool> GetAll(string? tenantId = null)
    {
        var allKeys = new[]
        {
            Application.Common.FeatureFlags.FeatureFlags.CanaryFeatures,
            Application.Common.FeatureFlags.FeatureFlags.BetaRulesEngine,
            Application.Common.FeatureFlags.FeatureFlags.DisableDataRetentionBackground,
            Application.Common.FeatureFlags.FeatureFlags.ObservabilityIncrementalAggregation,
            Application.Common.FeatureFlags.FeatureFlags.MultiChannelEmailDispatch,
            Application.Common.FeatureFlags.FeatureFlags.PipelineWipLimits,
        };

        return allKeys.ToDictionary(k => k, k => IsEnabled(k, tenantId));
    }
}
